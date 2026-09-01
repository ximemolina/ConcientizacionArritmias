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
    private float hoverTimer = 0f;
    private Quaternion targetRotation;

    // -------------------------------------------------------------------------
    [Header("Behaviour")]
    public bool returnToFloatOnRelease = true;
    /// <summary>
    /// Call this once after the piece is set up to begin floating it to its target position.
    /// </summary>
    public void Initialize(Vector3 targetPos, Quaternion targetRot, float staggerDelay = 0f)
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        floatPosition = targetPos;
        targetRotation = targetRot;

        if (grab != null)
        {
            grab.trackRotation = false;
            grab.trackPosition = true;
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

            grab.selectEntered.RemoveAllListeners();
            grab.selectExited.RemoveAllListeners();
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

        yield return StartCoroutine(LerpToFloatPositionAndRotation(transform.position, floatPosition, targetRotation));
    }

    private IEnumerator LerpToFloatPositionAndRotation(Vector3 fromPos, Vector3 toPos, Quaternion toRot)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None;
        }

        float startTime = Time.time;
        float journeyLength = Vector3.Distance(fromPos, toPos);
        float duration = Mathf.Max(journeyLength / lerpSpeed, 1.5f);

        float rotationStartTime = Time.time;
        Quaternion fromRot = transform.rotation;
        float rotationDuration = 2.0f;

        while (!isHeld && !isSnapped)
        {
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

        transform.position = toPos;
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
        float journeyLength = Vector3.Distance(from, floatPosition);
        float duration = Mathf.Max(journeyLength / lerpSpeed, 0.5f);

        while (!isHeld && !isSnapped)
        {
            float elapsed = Time.time - startTime;
            float fraction = Mathf.Clamp01(elapsed / duration);

            transform.position = Vector3.Lerp(from, floatPosition, Mathf.SmoothStep(0, 1, fraction));

            if (fraction >= 1f)
            {
                transform.position = floatPosition;
                break;
            }

            yield return null;
        }

        if (isHeld || isSnapped) yield break;

        transform.position = floatPosition;
        hoverTimer = 0f;
    }

    // -------------------------------------------------------------------------

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

        if (enableHovering && returnToFloatOnRelease)
            StartCoroutine(ReturnToFloat(transform.position));

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
        if (isHeld || isSnapped) return;
        if (!enableHovering) return;

        // Mark as snapped if the grab interactable was disabled externally (by CorrectRotationPuzzle)
        if (grab != null && !grab.enabled)
        {
            isSnapped = true;
            return;
        }

        if (rb != null && !rb.isKinematic) return;
        if (Vector3.Distance(transform.position, floatPosition) > arrivalThreshold * 4f) return;

        // Hover
        hoverTimer += Time.deltaTime;
        float yOffset = Mathf.Sin(hoverTimer * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
        transform.position = floatPosition + Vector3.up * yOffset;

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