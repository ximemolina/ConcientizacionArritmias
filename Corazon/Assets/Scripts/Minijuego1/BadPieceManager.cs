using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Orchestrates the game flow: watches the tutorial gate, tracks bad piece removal,
/// and triggers good piece float-in. Delegates audio to GameAudioManager and
/// animation to PieceFloatAnimator (added per-piece at runtime).
/// 
/// Requires GameAudioManager to be present in the scene.
/// </summary>
public class BadPieceManager : MonoBehaviour
{
    public static BadPieceManager Instance { get; private set; }

    [Header("Pieces")]
    public List<GameObject> badPieces = new List<GameObject>();
    public List<GameObject> goodPieces = new List<GameObject>();

    [Header("Float Targets")]
    public List<Transform> floatTargets = new List<Transform>();

    [Header("Float Settings")]
    public float lerpSpeed = 0.8f;
    public float arrivalThreshold = 0.015f;
    public float hoverAmplitude = 0.04f;
    public float hoverFrequency = 1.1f;
    public float staggerDelay = 0.25f;

    [Header("Rotation Settings")]
    public bool applyRotationDuringFloat = true;
    public float hoverRotationSpeed = 15f;

    [Header("Tutorial Gate")]
    [Tooltip("Music starts when the CoachingCardRoot child of this object is deactivated.")]
    public GameObject tutorialObject;

    // -------------------------------------------------------------------------

    private int remaining;
    private bool triggered = false;
    private bool musicStarted = false;
    private List<GameObject> createdTargets = new List<GameObject>();

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        remaining = 0;
        foreach (var bp in badPieces)
            if (bp != null) remaining++;

        // Hide good pieces until bad phase is over
        foreach (var gp in goodPieces)
            if (gp != null) gp.SetActive(false);

        Debug.Log($"[BadPieceManager] Ready. {remaining} bad piece(s), {goodPieces.Count} good piece(s).");

        if (tutorialObject == null)
            Debug.LogWarning("[BadPieceManager] No tutorialObject assigned — bad-phase music will never start.");

        if (GameAudioManager.Instance == null)
            Debug.LogError("[BadPieceManager] GameAudioManager not found in scene!");
    }

    // -------------------------------------------------------------------------

    public void OnBadPieceRemoved(GameObject piece)
    {
        var puzzleComponent = piece.GetComponent<CorrectRotationPuzzle>();
        if (puzzleComponent != null)
        {
            puzzleComponent.enabled = false;
            var grabInteractable = piece.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
                grabInteractable.enabled = false;
        }

        remaining--;
        Debug.Log($"[BadPieceManager] '{piece.name}' removed. {remaining} left.");

        if (remaining <= 0 && !triggered)
        {
            triggered = true;
            StartCoroutine(FloatGoodPiecesIn());
        }

        Destroy(piece);
    }

    // -------------------------------------------------------------------------

    private IEnumerator FloatGoodPiecesIn()
    {
        Debug.Log("[BadPieceManager] Floating good pieces in!");
        GameAudioManager.Instance?.PlayGoodPhaseMusic();

        for (int i = 0; i < goodPieces.Count; i++)
        {
            GameObject obj = goodPieces[i];
            if (obj == null) continue;  // ? null check FIRST

            var rotationPuzzle = obj.GetComponent<CorrectRotationPuzzle>();
            if (rotationPuzzle == null)
            {
                Debug.LogError($"[BadPieceManager] Good piece '{obj.name}' is missing CorrectRotationPuzzle!");
                continue;
            }

            // Assign targetSlot BEFORE activating so Awake() doesn't fail
            if (rotationPuzzle.targetSlot == null)
            {
                GameObject rotationTarget = new GameObject($"{obj.name}_RotationTarget");
                rotationTarget.transform.SetParent(this.transform);
                rotationTarget.transform.position = obj.transform.position;
                rotationTarget.transform.rotation = Quaternion.Euler(0, 270, 0);
                rotationPuzzle.targetSlot = rotationTarget.transform;
                createdTargets.Add(rotationTarget);
            }

            obj.SetActive(true);

            // Ignore collisions between this piece and all other good pieces
            Collider[] thisColliders = obj.GetComponentsInChildren<Collider>();
            for (int j = 0; j < goodPieces.Count; j++)
            {
                if (j == i || goodPieces[j] == null) continue;
                Collider[] otherColliders = goodPieces[j].GetComponentsInChildren<Collider>();
                foreach (var c1 in thisColliders)
                    foreach (var c2 in otherColliders)
                        Physics.IgnoreCollision(c1, c2, true);
            }

            Vector3 targetPos = floatTargets.Count > 0
                ? floatTargets[i % floatTargets.Count].position
                : obj.transform.position + Vector3.up * 1.2f;

            var animator = obj.AddComponent<PieceFloatAnimator>();
            animator.lerpSpeed = lerpSpeed;
            animator.arrivalThreshold = arrivalThreshold;
            animator.hoverAmplitude = hoverAmplitude;
            animator.hoverFrequency = hoverFrequency;
            animator.applyRotationDuringFloat = applyRotationDuringFloat;
            animator.hoverRotationSpeed = hoverRotationSpeed;
            animator.Initialize(targetPos, rotationPuzzle.targetSlot.rotation);

            yield return new WaitForSeconds(staggerDelay);
        }
    }

    // -------------------------------------------------------------------------

    private void Update()
    {
        if (!musicStarted && tutorialObject != null)
        {
            Transform cardRoot = tutorialObject.transform.Find("CoachingCardRoot");
            bool tutorialDismissed = cardRoot != null
                ? !cardRoot.gameObject.activeInHierarchy
                : !tutorialObject.activeInHierarchy;

            if (tutorialDismissed)
            {
                musicStarted = true;
                Debug.Log("[BadPieceManager] Tutorial dismissed — starting bad-phase music.");
                GameAudioManager.Instance?.PlayBadPhaseMusic();
            }
        }
    }

    // -------------------------------------------------------------------------

    private void OnDestroy()
    {
        foreach (var target in createdTargets)
            if (target != null) Destroy(target);
        createdTargets.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (floatTargets == null) return;
        Gizmos.color = Color.green;
        foreach (var t in floatTargets)
        {
            if (t == null) continue;
            Gizmos.DrawWireSphere(t.position, 0.12f);
            Gizmos.DrawLine(t.position, t.position + Vector3.up * 0.25f);
        }
    }

    // Called externally (e.g. HeartBeat script)
    public void StopMusic() => GameAudioManager.Instance?.StopMusic();
    public void stopHeartbeat() => GameAudioManager.Instance?.StopHeartbeat();
}