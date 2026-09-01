using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates the two-wave puzzle flow:
///   Wave 1: 3 objects float in ? player places them ? Wave 2 starts
///   Wave 2: 3 objects float in ? player places them ? Final object fades in
///
/// Attach to a manager GameObject in the scene.
/// Does NOT use PieceFloatAnimator — uses its own FloatInOnly coroutine so
/// WallObject's grab/drop behaviour is left completely untouched.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave 1 Objects")]
    [Tooltip("The 3 objects for the first wave. They start hidden at their own scene positions.")]
    public List<GameObject> wave1Objects = new List<GameObject>();

    [Tooltip("Where each wave 1 object floats TO. Match order with wave1Objects.")]
    public List<Transform> wave1FloatTargets = new List<Transform>();

    [Header("Wave 2 Objects")]
    [Tooltip("The 3 objects for the second wave.")]
    public List<GameObject> wave2Objects = new List<GameObject>();

    [Tooltip("Where each wave 2 object floats TO. Match order with wave2Objects.")]
    public List<Transform> wave2FloatTargets = new List<Transform>();

    [Header("Final Object")]
    [Tooltip("The single object that appears after both waves are complete.")]
    public GameObject finalObject;

    [Tooltip("How long the final object takes to fade in.")]
    public float fadeDuration = 1.5f;

    [Header("Float Settings")]
    [Tooltip("How fast objects move to their float target.")]
    public float lerpSpeed = 0.8f;

    [Tooltip("Minimum time each object takes to float in (seconds).")]
    public float minFloatDuration = 1.5f;

    [Tooltip("Delay between each object floating in.")]
    public float staggerDelay = 0.3f;

    [Header("Return To Target")]
    [Tooltip("Delay (seconds) before a misplaced object floats back to its slot, after touching ground or the wrong container.")]
    public float returnDelay = 0.75f;

    [Tooltip("World Y height below which an object is considered to have hit the floor. Adjust to match your floor's height.")]
    public float groundY = -0.02f;

    [Header("Tutorial Gate")]
    [Tooltip("Wave 1 starts when the CoachingCardRoot child of this object is deactivated.")]
    public GameObject tutorialObject;

    [Header("Door")]
    public Transform door;
    public Vector3 doorOpenPosition;
    public float doorSpeed = 2f;

    [Header("Open door position")]
    public Transform doorOpenTarget;

    [Header("Quad of portal")]
    [SerializeField] private QuadImageSlideshow slideshow;

    [Header("Audio")]
    [Tooltip("Plays when the final object appears.")]
    public AudioClip finalObjectVO;
    private int totalPlaced = 0;
    private int totalFilled = 0;
    // -------------------------------------------------------------------------

    private int wave1Filled = 0;
    private int wave2Filled = 0;
    private bool wave1Complete = false;
    private bool wave2Complete = false;
    private bool tutorialDone = false;
    private bool doorMoving = false;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Hide wave 1 at start — wave 2 stays visible in scene
        //foreach (var obj in wave1Objects) if (obj != null) obj.SetActive(false);

        if (finalObject != null)
            finalObject.SetActive(false);
    }

    private void Update()
    {
        // Watch for tutorial dismissal to start wave 1
        if (!tutorialDone && tutorialObject != null)
        {
            Transform cardRoot = tutorialObject.transform.Find("CoachingCardRoot");
            bool dismissed = cardRoot != null
                ? !cardRoot.gameObject.activeInHierarchy
                : !tutorialObject.activeInHierarchy;

            if (dismissed)
            {
                tutorialDone = true;
                StartCoroutine(SpawnWave(wave1Objects, wave1FloatTargets));
            }
        }

        // Move door when game is won
        if (doorMoving && door != null && doorOpenTarget != null)
        {
            slideshow.StartSlideshow();
            float targetZ = doorOpenTarget.position.z;
            Vector3 targetPosition = new Vector3(door.position.x, door.position.y, targetZ);

            door.position = Vector3.MoveTowards(
                door.position,
                targetPosition,
                doorSpeed * Time.deltaTime
            );

            if (Vector3.Distance(door.position, targetPosition) < 0.001f)
            {
                door.position = targetPosition;
                doorMoving = false;
                Debug.Log("[WaveManager] Door opened!");
            }
        }
    }

    // -------------------------------------------------------------------------

    public void OnWave1ContainerFilled()
    {
        wave1Filled++;
        Debug.Log($"[WaveManager] Wave 1: {wave1Filled}/{wave1Objects.Count} filled.");

        if (!wave1Complete && wave1Filled >= wave1Objects.Count)
        {
            wave1Complete = true;
            Debug.Log("[WaveManager] Wave 1 complete! Spawning wave 2.");
            StartCoroutine(SpawnWave(wave2Objects, wave2FloatTargets));
        }
    }

    public void OnWave1ContainerEmptied()
    {
        wave1Filled = Mathf.Max(0, wave1Filled - 1);
        wave1Complete = false;
    }

    public void OnWave2ContainerFilled()
    {
        wave2Filled++;
        Debug.Log($"[WaveManager] Wave 2: {wave2Filled}/{wave2Objects.Count} filled.");

        if (!wave2Complete && wave2Filled >= wave2Objects.Count)
        {
            wave2Complete = true;
            Debug.Log("[WaveManager] Wave 2 complete! Showing final object.");
            StartCoroutine(ShowFinalObject());
            doorMoving = true;
        }
    }

    public void OnWave2ContainerEmptied()
    {
        wave2Filled = Mathf.Max(0, wave2Filled - 1);
        wave2Complete = false;
    }

    // -------------------------------------------------------------------------

    private IEnumerator SpawnWave(List<GameObject> objects, List<Transform> targets)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            GameObject obj = objects[i];
            if (obj == null) continue;

            obj.SetActive(true);

            // Ignore collisions between this object and all others in the same wave
            Collider[] thisColliders = obj.GetComponentsInChildren<Collider>();
            for (int j = 0; j < objects.Count; j++)
            {
                if (j == i || objects[j] == null) continue;
                Collider[] otherColliders = objects[j].GetComponentsInChildren<Collider>();
                foreach (var c1 in thisColliders)
                    foreach (var c2 in otherColliders)
                        Physics.IgnoreCollision(c1, c2, true);
            }

            // Ground-touch detection — floats the object back if it falls below groundY
            if (obj.GetComponent<GroundContact>() == null)
            {
                var gc = obj.AddComponent<GroundContact>();
                gc.returnDelay = returnDelay;
                gc.groundY = groundY;
            }

            Vector3 targetPos = targets != null && targets.Count > i && targets[i] != null
                ? targets[i].position
                : obj.transform.position + Vector3.up * 1.2f;

            Quaternion targetRot = targets != null && targets.Count > i && targets[i] != null
                ? targets[i].rotation
                : obj.transform.rotation;

            StartCoroutine(FloatInOnly(obj, targetPos, targetRot));

            yield return new WaitForSeconds(staggerDelay);
        }
    }

    /// <summary>
    /// Moves an object to a target position/rotation over time without touching
    /// its XR listeners. WallObject handles all grab/drop behaviour.
    /// </summary>
    private IEnumerator FloatInOnly(GameObject obj, Vector3 targetPos, Quaternion targetRot)
    {
        if (obj == null) yield break;

        Rigidbody rb = obj.GetComponent<Rigidbody>();

        // Lock physics during float-in so it doesn't fall
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        Vector3 startPos = obj.transform.position;
        Quaternion startRot = obj.transform.rotation;
        float journeyLength = Vector3.Distance(startPos, targetPos);
        float duration = Mathf.Max(journeyLength / lerpSpeed, minFloatDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (obj == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            obj.transform.position = Vector3.Lerp(startPos, targetPos, t);
            obj.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        if (obj == null) yield break;

        obj.transform.position = targetPos;
        obj.transform.rotation = targetRot;

        // Restore WallObject's initial state so it's ready to be grabbed
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // Allow this object to trigger a ground-return again later
        var groundContact = obj.GetComponent<GroundContact>();
        if (groundContact != null)
            groundContact.ResetReturnState();

        Debug.Log($"[WaveManager] '{obj.name}' floated in and ready.");
    }

    // -------------------------------------------------------------------------

    private IEnumerator ShowFinalObject()
    {
        if (finalObject == null) yield break;
        Debug.Log("[WaveManager] Final object fully visible.");
        GameAudioManager.Instance?.PlayTutorialAudio(finalObjectVO);
        var renderers = finalObject.GetComponentsInChildren<Renderer>();
        var originalColors = new Color[renderers.Length];

        finalObject.SetActive(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i].material;
            originalColors[i] = mat.color;
            var c = mat.color;
            c.a = 0f;
            mat.color = c;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            for (int i = 0; i < renderers.Length; i++)
            {
                var mat = renderers[i].material;
                var c = originalColors[i];
                c.a = Mathf.Lerp(0f, originalColors[i].a, t);
                mat.color = c;
            }

            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i].material;
            mat.color = originalColors[i];
        }


    }


    public void OnContainerFilled()
    {
        totalFilled++;
        Debug.Log($"[WaveManager] Total filled: {totalFilled}");

        if (totalFilled == wave1Objects.Count)
        {
            Debug.Log("[WaveManager] Wave 1 complete! Spawning wave 2.");

            // Reset all containers for wave 2
            foreach (var container in FindObjectsByType<Clasificacion>(FindObjectsSortMode.None))
                container.Reset();

            StartCoroutine(SpawnWave(wave2Objects, wave2FloatTargets));
        }
        else if (totalFilled == wave1Objects.Count + wave2Objects.Count)
        {
            Debug.Log("[WaveManager] Wave 2 complete! Showing final object.");
            StartCoroutine(ShowFinalObject());
            doorMoving = true;
        }
    }


    public void OnValidObjectPlaced()
    {
        totalPlaced++;
        Debug.Log($"[WaveManager] Total placed: {totalPlaced}");

        if (totalPlaced == wave1Objects.Count)
        {
            Debug.Log("[WaveManager] Wave 1 complete! Spawning wave 2.");
            StartCoroutine(SpawnWave(wave2Objects, wave2FloatTargets));
        }
        else if (totalPlaced == wave1Objects.Count + wave2Objects.Count)
        {
            Debug.Log("[WaveManager] Wave 2 complete! Showing final object.");
            StartCoroutine(ShowFinalObject());
            doorMoving = true;
        }
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Finds the float target for a given object (checks both waves) and floats it back there.
    /// Called by Clasificacion (wrong container) or GroundContact (touched floor).
    /// </summary>
    public void ReturnObjectToTarget(GameObject obj, float delay = 0.75f)
    {
        if (obj == null) return;

        Vector3? targetPos = null;
        Quaternion targetRot = obj.transform.rotation;

        int idx = wave1Objects.IndexOf(obj);
        if (idx >= 0 && wave1FloatTargets.Count > idx && wave1FloatTargets[idx] != null)
        {
            targetPos = wave1FloatTargets[idx].position;
            targetRot = wave1FloatTargets[idx].rotation;
        }
        else
        {
            idx = wave2Objects.IndexOf(obj);
            if (idx >= 0 && wave2FloatTargets.Count > idx && wave2FloatTargets[idx] != null)
            {
                targetPos = wave2FloatTargets[idx].position;
                targetRot = wave2FloatTargets[idx].rotation;
            }
        }

        if (targetPos == null)
        {
            Debug.LogWarning($"[WaveManager] No float target found for '{obj.name}' — can't return it.");
            return;
        }

        StartCoroutine(ReturnAfterDelay(obj, targetPos.Value, targetRot, delay));
    }

    private IEnumerator ReturnAfterDelay(GameObject obj, Vector3 targetPos, Quaternion targetRot, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj == null) yield break;
        StartCoroutine(FloatInOnly(obj, targetPos, targetRot));
    }
}