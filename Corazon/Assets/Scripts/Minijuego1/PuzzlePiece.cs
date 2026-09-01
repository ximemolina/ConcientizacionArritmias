using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

/// <summary>
/// Your original PuzzlePiece — extended with:
///   • FloatToPosition()  — smooth lerp travel to a grab-friendly position
///   • Hover idle loop    — gentle bob + slow spin once arrived
///   • OnGrab()           — stops the float/hover when the player grabs the piece
///
/// Everything else (XRGrabInteractable wiring, gravity toggle) is unchanged.
/// </summary>
public class PuzzlePiece : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Float / hover settings (only relevant for good pieces)

    [Header("Float Animation")]
    [Tooltip("Lerp speed toward the grab position (higher = faster travel).")]
    public float lerpSpeed = 3f;

    [Tooltip("Distance at which the piece is considered 'arrived'.")]
    public float arrivalThreshold = 0.015f;

    [Tooltip("Amplitude of the idle hover bob once arrived (metres).")]
    public float hoverAmplitude = 0.04f;

    [Tooltip("Frequency of the idle hover bob (cycles per second).")]
    public float hoverFrequency = 1.1f;

    [Tooltip("Slow Y-axis rotation while hovering (degrees per second).")]
    public float idleRotationSpeed = 25f;

    // -------------------------------------------------------------------------
    // Private state

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;

    private bool isFloating = false;
    private bool hasArrived = false;
    private Vector3 hoverOrigin;   // world position the bob oscillates around
    private float hoverTimer = 0f;

    // -------------------------------------------------------------------------
    // Original Start — unchanged except we cache grabInteractable once

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        rb.useGravity = false;
        rb.isKinematic = true;

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    // -------------------------------------------------------------------------
    // Original grab callbacks — unchanged

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Stop float/hover so physics/XRI can take over
        StopAllCoroutines();
        isFloating = false;
        hasArrived = false;

        rb.isKinematic = false;
        rb.useGravity = false;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    // -------------------------------------------------------------------------
    // NEW: called by BadPieceManager when all bad pieces are gone

    /// <summary>
    /// Smoothly lerps this piece to <paramref name="targetPosition"/>, then
    /// starts a gentle hover bob so the player knows it's ready to grab.
    /// </summary>
    public void FloatToPosition(Vector3 targetPosition)
    {
        StopAllCoroutines();
        isFloating = true;
        hasArrived = false;

        // Make fully kinematic so we drive the position ourselves
        rb.isKinematic = true;
        rb.useGravity = false;

        StartCoroutine(LerpToTarget(targetPosition));
    }

    private IEnumerator LerpToTarget(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > arrivalThreshold)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                target,
                lerpSpeed * Time.deltaTime
            );

            // Slow spin during travel so the piece feels alive
            transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime, Space.World);

            yield return null;
        }

        // Snap cleanly and begin hover
        transform.position = target;
        hoverOrigin = target;
        hoverTimer = 0f;
        isFloating = false;
        hasArrived = true;

        Debug.Log($"[PuzzlePiece] '{name}' is ready to grab.");
    }

    // -------------------------------------------------------------------------
    // Hover idle loop

    private void Update()
    {
        if (!hasArrived) return;

        hoverTimer += Time.deltaTime;

        // Bob up and down around the arrival position
        float yOffset = Mathf.Sin(hoverTimer * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
        transform.position = hoverOrigin + Vector3.up * yOffset;

        // Slow Y-axis spin
        transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime, Space.World);
    }
}