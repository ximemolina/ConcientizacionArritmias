using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach this to every bad-piece GameObject.
/// Also make sure that same GameObject is in BadPieceManager's badPieces[] list.
///
/// Removal methods (use one or combine):
///   A) THROW  — release the piece with enough velocity.
///   B) ZONE   — enter a trigger collider tagged "RemoveZone".
///   C) MANUAL — call MarkAsRemoved() from any event/button.
/// </summary>
public class BadPiece : MonoBehaviour
{
    [Header("Removal Settings")]
    public float throwVelocityThreshold = 1.5f;
    public string removeZoneTag = "RemoveZone";
    public float destroyDelay = 0.4f;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private bool removed = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
            grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        StartCoroutine(CheckThrowNextFrame());
    }

    private System.Collections.IEnumerator CheckThrowNextFrame()
    {
        yield return null;
        if (rb != null && rb.linearVelocity.magnitude >= throwVelocityThreshold)
            MarkAsRemoved();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(removeZoneTag))
            MarkAsRemoved();
    }

    public void MarkAsRemoved()
    {
        if (removed) return;
        removed = true;

        Debug.Log($"[BadPiece] '{name}' removed.");
        BadPieceManager.Instance?.OnBadPieceRemoved(gameObject);
        Destroy(gameObject, destroyDelay);
    }

    private void OnDestroy()
    {
        if (!removed)
            BadPieceManager.Instance?.OnBadPieceRemoved(gameObject);
    }
}