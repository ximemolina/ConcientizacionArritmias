using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class PuzzleManager : MonoBehaviour
{
    [Header("All Slots in This Puzzle")]
    public PuzzleSnapController[] slots;

    [Header("Door Settings")]
    public GameObject door;
    public Vector3 targetPosition;
    public float doorSpeed = 2f;

    [Header("Objects To Hide On Completion")]
    public GameObject[] objectsToDisappear;

    [Header("Objects To Show On Completion")]
    public GameObject[] objectsToAppear;

    [Header("Scale Animation Settings")]
    public float scaleDuration = 1.8f; // was 0.35f, much slower now

    [Header("Completion Event")]
    public UnityEvent OnPuzzleComplete;

    [SerializeField] private QuadImageSlideshow slideshow;

    private bool _completed = false;
    private bool _doorMoving = false;

    // Store original scales so we restore them correctly
    private Dictionary<GameObject, Vector3> _originalScales = new Dictionary<GameObject, Vector3>();

    // -------------------------------------------------------------------------

    private void Start()
    {
        // Store original scales and hide appear objects
        if (objectsToAppear != null)
            foreach (var obj in objectsToAppear)
                if (obj != null)
                {
                    _originalScales[obj] = obj.transform.localScale;
                    obj.SetActive(false);
                }

        if (objectsToDisappear != null)
            foreach (var obj in objectsToDisappear)
                if (obj != null)
                    _originalScales[obj] = obj.transform.localScale;
    }

    // -------------------------------------------------------------------------

    private void Update()
    {
        if (!_completed)
        {
            foreach (var slot in slots)
                if (!slot.IsSolved) return;

            _completed = true;
            _doorMoving = true;
            Debug.Log("[Puzzle] 🎉 Puzzle complete!");
            GameAudioManager.Instance?.PlayPuzzleCompleteVO();
            slideshow.StartSlideshow();
            StartCoroutine(CompletionSequence());
            
            OnPuzzleComplete?.Invoke();
            
        }

        if (_doorMoving && door != null)
        {
            door.transform.position = Vector3.MoveTowards(
                door.transform.position,
                targetPosition,
                doorSpeed * Time.deltaTime
            );
            if (Vector3.Distance(door.transform.position, targetPosition) < 0.001f)
            {
                door.transform.position = targetPosition;
                _doorMoving = false;
                Debug.Log("[Puzzle] 🚪 Door opened!");
            }
        }
    }

    // -------------------------------------------------------------------------
    private IEnumerator CompletionSequence()
    {
        yield return new WaitForSeconds(1f);

        // Scale down objects to hide
        yield return StartCoroutine(ScaleObjects(objectsToDisappear, scaleDown: true));

        if (objectsToDisappear != null)
            foreach (var obj in objectsToDisappear)
                if (obj != null) obj.SetActive(false);

        // Activate and scale up objects to show (including button if you add it there)
        if (objectsToAppear != null)
            foreach (var obj in objectsToAppear)
                if (obj != null)
                {
                    if (_originalScales.TryGetValue(obj, out Vector3 originalScale))
                        obj.transform.localScale = originalScale;
                    obj.SetActive(true);
                }

        yield return StartCoroutine(ScaleObjects(objectsToAppear, scaleDown: false));

    }

    // -------------------------------------------------------------------------

    private IEnumerator ScaleObjects(GameObject[] objects, bool scaleDown)
    {
        if (objects == null) yield break;

        var bottomPositions = new Dictionary<GameObject, Vector3>();
        var originalScalesLocal = new Dictionary<GameObject, Vector3>();

        foreach (var obj in objects)
        {
            if (obj == null) continue;
            originalScalesLocal[obj] = obj.transform.localScale;

            // Calculate combined bounds of all children
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                float minY = float.MaxValue;
                foreach (var r in renderers)
                    minY = Mathf.Min(minY, r.bounds.min.y);
                bottomPositions[obj] = new Vector3(obj.transform.position.x, minY, obj.transform.position.z);
            }
            else
                bottomPositions[obj] = obj.transform.position;
        }

        float elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaleDuration);

            float smooth = scaleDown
                ? 1f - (1f - t) * (1f - t) * (1f - t)
                : t * t * t;

            foreach (var obj in objects)
            {
                if (obj == null) continue;
                Vector3 original = originalScalesLocal[obj];
                float scaleY = scaleDown ? Mathf.Lerp(1f, 0f, smooth) : Mathf.Lerp(0f, 1f, smooth);
                obj.transform.localScale = new Vector3(original.x, original.y * scaleY, original.z);

                if (bottomPositions.ContainsKey(obj))
                {
                    Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        float currentMinY = float.MaxValue;
                        foreach (var r in renderers)
                            currentMinY = Mathf.Min(currentMinY, r.bounds.min.y);
                        float diff = bottomPositions[obj].y - currentMinY;
                        obj.transform.position += new Vector3(0, diff, 0);
                    }
                }
            }

            yield return null;
        }

        foreach (var obj in objects)
        {
            if (obj == null) continue;
            Vector3 original = originalScalesLocal[obj];
            obj.transform.localScale = scaleDown
                ? new Vector3(original.x, 0f, original.z)
                : original;
        }
    }

    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (door == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPosition, 0.15f);
        Gizmos.DrawLine(door.transform.position, targetPosition);
    }
}