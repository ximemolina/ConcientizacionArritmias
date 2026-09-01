using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach this script to each SLOT (the fixed target position in the puzzle).
/// Assign the "correctPiece" in the Inspector — this is the specific puzzle block
/// that belongs in this slot.
///
/// How it works:
///   1. The player grabs a block (XRGrabInteractable) and moves it near a slot.
///   2. When the block enters the slot's trigger collider, we check if it matches.
///   3. If it matches → the block snaps, locks in place, and is marked as solved.
///   4. If it doesn't match → nothing happens (player keeps holding it).
/// </summary>
[RequireComponent(typeof(Collider))]
public class PuzzleSnapController : MonoBehaviour
{
    [Tooltip("Voiceover played through GameAudioManager when this piece is correctly placed.")]
    public AudioClip placedVO;

    [Header("Puzzle Configuration")]
    [Tooltip("The exact GameObject that belongs in this slot.")]
    public GameObject correctPiece;

    [Tooltip("How close (meters) the piece must be before it snaps.")]
    public float snapDistance = 0.15f;

    [Tooltip("How smoothly the piece slides into place (0 = instant).")]
    [Range(0f, 20f)]
    public float snapSpeed = 10f;

    [Header("Feedback (optional)")]
    [Tooltip("Particle effect played when a piece is correctly placed.")]
    public ParticleSystem successEffect;

    [Tooltip("Audio clip played on successful placement.")]
    public AudioClip successSound;

    [Tooltip("Volume of the placement sound effect (0-1).")]
    [Range(0f, 1f)]
    public float successSoundVolume = 1f;

    // ── Internal state ──────────────────────────────────────────────────────
    private bool _isSolved = false;
    private AudioSource _audioSource;

    // Keeps a reference while we're lerping the piece into position
    private GameObject _snappingPiece = null;

    // ───────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Make sure the trigger collider is set to Is Trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // Add an AudioSource if we need one for sound feedback
        if (successSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // 3-D audio in VR
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Called by Unity when a collider enters this trigger.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (_isSolved) return;                          // slot already filled
        if (other.gameObject != correctPiece) return;   // wrong piece

        // Check the piece is actually being held (not just rolling in)
        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab == null) return;

        // Force the interactor to drop the object before we lock it
        if (grab.isSelected)
        {
            // XRI 2.x API — works with XRDirectInteractor & XRRayInteractor
            grab.interactionManager.SelectExit(
                grab.firstInteractorSelecting,
                grab
            );
        }

        // Begin snapping
        _snappingPiece = other.gameObject;
        _isSolved = true;

        LockPiece(_snappingPiece);
        PlayFeedback();
    }

    // ───────────────────────────────────────────────────────────────────────
    private void Update()
    {
        // Smoothly slide the piece into the exact slot position/rotation
        if (_snappingPiece != null)
        {
            _snappingPiece.transform.position = Vector3.Lerp(
                _snappingPiece.transform.position,
                transform.position,
                Time.deltaTime * snapSpeed
            );

            _snappingPiece.transform.rotation = Quaternion.Lerp(
                _snappingPiece.transform.rotation,
                transform.rotation,
                Time.deltaTime * snapSpeed
            );

            // Once close enough, finalize and stop lerping
            if (Vector3.Distance(_snappingPiece.transform.position, transform.position) < 0.001f)
            {
                _snappingPiece.transform.SetPositionAndRotation(transform.position, transform.rotation);
                _snappingPiece = null;
            }
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Disables physics and XR interaction so the piece stays put forever.
    /// </summary>
    private void LockPiece(GameObject piece)
    {
        // Freeze the Rigidbody
        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Prevent the player from grabbing it again
        XRGrabInteractable grab = piece.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            grab.enabled = false;
        }

        // Optional: parent to this slot so it moves with the slot (e.g. on a moving platform)
        // piece.transform.SetParent(transform);

        Debug.Log($"[Puzzle] ✅ Piece '{piece.name}' locked into slot '{gameObject.name}'.");
    }

    // ───────────────────────────────────────────────────────────────────────
    private void PlayFeedback()
    {
        if (successEffect != null)
        {
            successEffect.transform.position = transform.position;
            successEffect.Play();
        }

        if (_audioSource != null && successSound != null)
        {
            _audioSource.PlayOneShot(successSound, successSoundVolume);
        }

        // Per-slot voiceover through the central audio manager
        GameAudioManager.Instance?.PlayTutorialAudio(placedVO);
    }

    // ───────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Public getter so a GameManager can poll all slots and detect puzzle completion.
    /// </summary>
    public bool IsSolved => _isSolved;

    // ───────────────────────────────────────────────────────────────────────
    // Editor helper: draw a wire sphere so you can see the snap radius in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isSolved ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, snapDistance);
    }
}