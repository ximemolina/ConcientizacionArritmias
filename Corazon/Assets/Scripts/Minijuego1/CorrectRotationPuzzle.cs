using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class CorrectRotationPuzzle : MonoBehaviour
{
    [Header("Reference Object")]
    public Transform targetSlot;

    [Header("Rotation Speed")]
    public float rotationSpeed = 90f;

    [Header("Placement Detection")]
    [Tooltip("How close (meters) the piece must be to the slot to count as placed.")]
    public float placementDistanceThreshold = 0.15f;
    [Tooltip("How aligned (degrees) the piece must be to the slot to count as placed.")]
    public float placementAngleThreshold = 15f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private XRGrabInteractable _grab;
    private bool _isHeld;
    private bool _isDestroyed = false;
    private bool _isPlaced = false;
    private Rigidbody _rb;
    private Quaternion _initialTargetRotation;

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();

        if (_grab == null)
        {
            Debug.LogError($"[CorrectRotation] No XRGrabInteractable on {name}");
            enabled = false;
            return;
        }

        if (targetSlot == null)
        {
            Debug.LogError($"[CorrectRotation] No targetSlot assigned on {name} - disabling script");
            enabled = false;
            return;
        }

        _initialTargetRotation = targetSlot.rotation;

        // Configure the grab for rotation control
        _grab.trackRotation = false;  // XRI won't control rotation
        _grab.trackPosition = true;    // XRI controls position

        // Set movement type
        _grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        // Add listeners
        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);

        if (enableDebugLogs)
            Debug.Log($"[CorrectRotation] '{name}' initialized, target: {targetSlot.name}");
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (_isDestroyed || this == null || gameObject == null) return;

        _isHeld = true;

        // Configure rigidbody for grabbed state
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = false;
            // Freeze all rotation axes - we'll control rotation manually
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            // Keep position movement free
            _rb.constraints &= ~RigidbodyConstraints.FreezePositionX;
            _rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
            _rb.constraints &= ~RigidbodyConstraints.FreezePositionZ;
        }

        if (enableDebugLogs)
            Debug.Log($"[CorrectRotation] '{name}' grabbed - rotation control active");
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (_isDestroyed || this == null || gameObject == null) return;

        _isHeld = false;

        // Restore rigidbody
        if (_rb != null)
        {
            _rb.constraints = RigidbodyConstraints.None;
        }

        if (enableDebugLogs)
            Debug.Log($"[CorrectRotation] '{name}' released");

        // Check for correct placement on release
        CheckPlacement();
    }

    private void FixedUpdate()
    {
        if (_isDestroyed || this == null || gameObject == null) return;
        if (!_isHeld || targetSlot == null) return;

        // Apply rotation
        Quaternion currentRot = transform.rotation;
        Quaternion targetRot = targetSlot.rotation;

        // Only rotate if not already aligned
        float angleToTarget = Quaternion.Angle(currentRot, targetRot);
        if (angleToTarget > 0.01f)
        {
            Quaternion newRotation = Quaternion.RotateTowards(
                currentRot,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime
            );

            transform.rotation = newRotation;

            // Sync with rigidbody if it exists
            if (_rb != null && !_rb.isKinematic)
            {
                _rb.MoveRotation(newRotation);
            }

            if (enableDebugLogs && angleToTarget > 1f)
                Debug.Log($"[CorrectRotation] '{name}' rotating: {angleToTarget:F1}° to target");
        }
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Checks whether the piece is close enough and aligned enough to the target slot.
    /// If so, snaps it into place, plays the placed VO, and locks it.
    /// </summary>
    private void CheckPlacement()
    {
        if (_isPlaced || targetSlot == null) return;

        float distance = Vector3.Distance(transform.position, targetSlot.position);
        float angle = Quaternion.Angle(transform.rotation, targetSlot.rotation);

        if (enableDebugLogs)
            Debug.Log($"[CorrectRotation] '{name}' release check — dist: {distance:F3}m, angle: {angle:F1}°");

        if (distance <= placementDistanceThreshold && angle <= placementAngleThreshold)
        {
            _isPlaced = true;

            // Snap to exact slot position and rotation
            transform.position = targetSlot.position;
            transform.rotation = targetSlot.rotation;

            // Lock in place
            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.constraints = RigidbodyConstraints.FreezeAll;
            }

            // Disable grabbing so the piece can't be picked up again
            if (_grab != null)
                _grab.enabled = false;

            Debug.Log($"[CorrectRotation] '{name}' correctly placed!");

            // Fire the voiceover
            GameAudioManager.Instance?.PlayGoodPiecePlacedVO();
        }
    }

    // -------------------------------------------------------------------------

    private void OnDestroy()
    {
        _isDestroyed = true;

        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnGrab);
            _grab.selectExited.RemoveListener(OnRelease);
        }
    }
}