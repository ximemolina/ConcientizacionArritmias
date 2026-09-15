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

    [Header("Player-relative float")]
    [Tooltip("Headset/camera used to place floating pieces. If empty, Camera.main is used.")]
    public Transform playerHead;
    [Tooltip("If true, pieces keep following the headset while floating. Leave off so they stay in world space after appearing.")]
    public bool followPlayerWhileFloating = false;
    [Tooltip("Ignore head pitch/roll so pieces stay level in front of the player.")]
    public bool flattenPlayerYaw = true;
    [Tooltip("Distance in front of the player (meters).")]
    public float floatDistance = 1.35f;
    [Tooltip("Vertical offset from eye height (negative = slightly below).")]
    public float heightOffset = -0.1f;
    [Tooltip("Scales the existing 2x2 layout so it fits in front of the player.")]
    public float formationScale = 0.55f;
    [Tooltip("Extra multiplier for left/right spacing between pieces.")]
    public float horizontalSpread = 1.85f;
    [Tooltip("Extra height added only to the upper two pieces (meters).")]
    public float topHeightBonus = 0.4f;
    [Tooltip("How quickly pieces follow the player. 0 = snap.")]
    public float followSmoothing = 8f;

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
            Debug.LogWarning("[BadPieceManager] No tutorialObject assigned ? bad-phase music will never start.");

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

        Vector3 formationCentroid = GetFormationCentroid();

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

            Transform player = ResolvePlayerHead();
            Vector3 localOffset = GetPlayerLocalOffset(i, formationCentroid);
            Vector3 targetPos = player != null
                ? GetWorldPositionFromPlayer(player, localOffset)
                : (floatTargets.Count > 0
                    ? floatTargets[i % floatTargets.Count].position
                    : obj.transform.position + Vector3.up * 1.2f);

            var animator = obj.AddComponent<PieceFloatAnimator>();
            animator.lerpSpeed = lerpSpeed;
            animator.arrivalThreshold = arrivalThreshold;
            animator.hoverAmplitude = hoverAmplitude;
            animator.hoverFrequency = hoverFrequency;
            animator.applyRotationDuringFloat = applyRotationDuringFloat;
            animator.hoverRotationSpeed = hoverRotationSpeed;
            animator.Initialize(
                targetPos,
                rotationPuzzle.targetSlot.rotation,
                0f,
                player,
                localOffset,
                followPlayerWhileFloating,
                flattenPlayerYaw,
                followSmoothing);

            yield return new WaitForSeconds(staggerDelay);
        }
    }

    // -------------------------------------------------------------------------

    private Transform ResolvePlayerHead()
    {
        if (playerHead != null)
            return playerHead;

        Camera cam = Camera.main;
        if (cam != null)
            return cam.transform;

        Debug.LogWarning("[BadPieceManager] No player head found ? using fixed float targets.");
        return null;
    }

    private Vector3 GetFormationCentroid()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (var t in floatTargets)
        {
            if (t == null) continue;
            sum += t.position;
            count++;
        }
        return count > 0 ? sum / count : Vector3.zero;
    }

    private Vector3 GetPlayerLocalOffset(int index, Vector3 centroid)
    {
        Vector3 worldOffset = Vector3.zero;
        if (floatTargets.Count > 0)
        {
            Transform target = floatTargets[index % floatTargets.Count];
            if (target != null)
                worldOffset = target.position - centroid;
        }
        else
        {
            float fallbackX = (index % 2 == 0) ? -0.28f : 0.28f;
            float fallbackY = (index < 2) ? 0.22f : -0.22f;
            worldOffset = new Vector3(0f, fallbackY / Mathf.Max(formationScale, 0.01f), fallbackX / Mathf.Max(formationScale, 0.01f));
        }

        float x = worldOffset.z * formationScale * horizontalSpread;
        float y = worldOffset.y * formationScale + heightOffset;
        if (worldOffset.y > 0f)
            y += topHeightBonus;

        return new Vector3(x, y, floatDistance);
    }

    private Vector3 GetWorldPositionFromPlayer(Transform player, Vector3 localOffset)
    {
        Vector3 forward = player.forward;
        if (flattenPlayerYaw)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(player.up, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            else
                forward.Normalize();
        }

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.right;
        else
            right.Normalize();

        return player.position
            + right * localOffset.x
            + Vector3.up * localOffset.y
            + forward * localOffset.z;
    }

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
                Debug.Log("[BadPieceManager] Tutorial dismissed ? starting bad-phase music.");
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