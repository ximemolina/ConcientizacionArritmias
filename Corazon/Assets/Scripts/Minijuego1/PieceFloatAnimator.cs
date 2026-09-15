using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Handles floating, hovering, and rotation animation for a single good piece.
/// One instance is created per piece by BadPieceManager when good pieces spawn in.
/// </summary>
public class PieceFloatAnimator : MonoBehaviour
{
    [Header("Float Settings")]
    public float lerpSpeed = 0.8f;
    public float arrivalThreshold = 0.015f;
    public float hoverAmplitude = 0.04f;
    public float hoverFrequency = 1.1f;
    public bool enableHovering = true;
    [Header("Rotation Settings")]
    public bool applyRotationDuringFloat = true;
    public float hoverRotationSpeed = 15f;

    // -------------------------------------------------------------------------

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private Vector3 floatPosition;
    private bool isHeld = false;
    private bool isSnapped = false;
    private bool isTraveling = false;
    private float hoverTimer = 0f;
    private Quaternion targetRotation;

    private Transform followAnchor;
    private Vector3 playerLocalOffset;
    private bool followPlayer;
    private bool flattenYaw;
    private float followSmoothing;
    private Vector3 smoothedFloatPosition;
    private bool hasSmoothedPosition;

    // -------------------------------------------------------------------------
    [Header("Behaviour")]
    public bool returnToFloatOnRelease = true;
    /// <summary>
    /// Call this once after the piece is set up to begin floating it to its target position.
    /// </summary>
    public void Initialize(Vector3 targetPos, Quaternion targetRot, float staggerDelay = 0f)
    {
        Initialize(targetPos, targetRot, staggerDelay, null, Vector3.zero, false, true, 0f);
    }

    public void Initialize(
        Vector3 targetPos,
        Quaternion targetRot,
        float staggerDelay,
        Transform follow,
        Vector3 localOffset,
        bool followWhileFloating,
        bool flattenPlayerYaw,
        float smoothing)
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        followAnchor = follow;
        playerLocalOffset = localOffset;
        followPlayer = followWhileFloating && follow != null;
        flattenYaw = flattenPlayerYaw;
        followSmoothing = smoothing;
        floatPosition = targetPos;
        targetRotation = targetRot;
        smoothedFloatPosition = GetDesiredFloatPosition();
        hasSmoothedPosition = true;

        if (grab != null)
        {
            grab.trackRotation = false;
            grab.trackPosition = true;
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

            grab.selectEntered.AddListener((args) => OnGrabbed());
            grab.selectExited.AddListener((args) => OnReleased());
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
        }

        StartCoroutine(FloatIn(staggerDelay));
    }

    // -------------------------------------------------------------------------

    private IEnumerator FloatIn(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        isTraveling = true;
        yield return StartCoroutine(LerpToFloatPositionAndRotation(transform.position, targetRotation));
        isTraveling = false;
    }

    private IEnumerator LerpToFloatPositionAndRotation(Vector3 fromPos, Quaternion toRot)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None;
        }

        Vector3 initialTarget = GetCurrentFloatPosition();
        float startTime = Time.time;
        float journeyLength = Vector3.Distance(fromPos, initialTarget);
        float duration = Mathf.Max(journeyLength / lerpSpeed, 1.5f);

        float rotationStartTime = Time.time;
        Quaternion fromRot = transform.rotation;
        float rotationDuration = 2.0f;

        while (!isHeld && !isSnapped)
        {
            Vector3 toPos = GetCurrentFloatPosition();
            float elapsed = Time.time - startTime;
            float fraction = Mathf.Clamp01(elapsed / duration);
            float smooth = Mathf.SmoothStep(0, 1, fraction);

            transform.position = Vector3.Lerp(fromPos, toPos, smooth);

            if (applyRotationDuringFloat)
            {
                float rotFraction = Mathf.Clamp01((Time.time - rotationStartTime) / rotationDuration);
                transform.rotation = Quaternion.Slerp(fromRot, toRot, Mathf.SmoothStep(0, 1, rotFraction));
            }

            if (fraction >= 1f)
            {
                transform.position = toPos;
                if (applyRotationDuringFloat) transform.rotation = toRot;
                break;
            }

            yield return null;
        }

        if (isHeld || isSnapped) yield break;

        transform.position = GetCurrentFloatPosition();
        if (applyRotationDuringFloat) transform.rotation = toRot;

        hoverTimer = 0f;
        Debug.Log($"[PieceFloatAnimator] '{gameObject.name}' ready to grab.");
    }

    private IEnumerator ReturnToFloat(Vector3 from)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None;
        }

        float startTime = Time.time;
        float journeyLength = Vector3.Distance(from, GetCurrentFloatPosition());
        float duration = Mathf.Max(journeyLength / lerpSpeed, 0.5f);

        while (!isHeld && !isSnapped)
        {
            Vector3 target = GetCurrentFloatPosition();
            float elapsed = Time.time - startTime;
            float fraction = Mathf.Clamp01(elapsed / duration);

            transform.position = Vector3.Lerp(from, target, Mathf.SmoothStep(0, 1, fraction));

            if (fraction >= 1f)
            {
                transform.position = target;
                break;
            }

            yield return null;
        }

        if (isHeld || isSnapped) yield break;

        transform.position = GetCurrentFloatPosition();
        hoverTimer = 0f;
    }

    private IEnumerator ReturnToFloatAndFinish(Vector3 from)
    {
        yield return ReturnToFloat(from);
        isTraveling = false;
    }

    private Vector3 GetDesiredFloatPosition()
    {
        if (followAnchor == null)
            return floatPosition;

        Vector3 origin = followAnchor.position;
        Vector3 forward = followAnchor.forward;

        if (flattenYaw)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(followAnchor.up, Vector3.up);
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

        return origin
            + right * playerLocalOffset.x
            + Vector3.up * playerLocalOffset.y
            + forward * playerLocalOffset.z;
    }

    private Vector3 GetCurrentFloatPosition()
    {
        if (!followPlayer || followAnchor == null)
            return floatPosition;

        Vector3 desired = GetDesiredFloatPosition();

        if (!hasSmoothedPosition)
        {
            smoothedFloatPosition = desired;
            hasSmoothedPosition = true;
            return desired;
        }

        if (followSmoothing <= 0f)
            return desired;

        float t = 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
        smoothedFloatPosition = Vector3.Lerp(smoothedFloatPosition, desired, t);
        return smoothedFloatPosition;
    }

    // -------------------------------------------------------------------------

    public void MarkSnapped()
    {
        isSnapped = true;
        isHeld = false;
        isTraveling = false;
        followPlayer = false;
        StopAllCoroutines();
    }

    private void OnGrabbed()
    {
        isHeld = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            var rotationPuzzle = GetComponent<CorrectRotationPuzzle>();
            if (rotationPuzzle != null && rotationPuzzle.enabled)
            {
                rb.constraints = RigidbodyConstraints.FreezeRotationX |
                                 RigidbodyConstraints.FreezeRotationZ;
            }
        }
    }

    private void OnReleased()
    {
        if (isSnapped) return;
        isHeld = false;

        if (grab != null && !grab.enabled)
        {
            MarkSnapped();
            return;
        }

        if (enableHovering && returnToFloatOnRelease)
        {
            isTraveling = true;
            StartCoroutine(ReturnToFloatAndFinish(transform.position));
        }

        // If hovering disabled, just restore gravity so it drops normally
        if (!enableHovering && rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    // -------------------------------------------------------------------------

    private void Update()
    {
        if (isSnapped) return;

        if (grab != null && !grab.enabled)
        {
            MarkSnapped();
            return;
        }

        if (isHeld) return;
        if (!enableHovering) return;
        if (isTraveling) return;

        if (rb != null && !rb.isKinematic) return;

        Vector3 hoverOrigin = GetCurrentFloatPosition();
        if (!followPlayer && Vector3.Distance(transform.position, hoverOrigin) > arrivalThreshold * 4f) return;

        // Hover
        hoverTimer += Time.deltaTime;
        float yOffset = Mathf.Sin(hoverTimer * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
        transform.position = hoverOrigin + Vector3.up * yOffset;

        // Rotate toward target
        var puzzle = GetComponent<CorrectRotationPuzzle>();
        if (puzzle != null && puzzle.targetSlot != null)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                puzzle.targetSlot.rotation,
                hoverRotationSpeed * Time.deltaTime
            );
        }
    }
}