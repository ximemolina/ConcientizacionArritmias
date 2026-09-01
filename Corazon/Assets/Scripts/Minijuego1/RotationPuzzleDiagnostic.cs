using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
public class RotationPuzzleDiagnostic : MonoBehaviour
{
    [ContextMenu("Diagnose All Good Pieces")]
    void DiagnoseGoodPieces()
    {
        if (BadPieceManager.Instance == null)
        {
            Debug.LogError("BadPieceManager instance not found!");
            return;
        }

        var goodPieces = BadPieceManager.Instance.goodPieces;
        Debug.Log($"=== Diagnosing {goodPieces.Count} good pieces ===");

        for (int i = 0; i < goodPieces.Count; i++)
        {
            var piece = goodPieces[i];
            if (piece == null)
            {
                Debug.LogWarning($"Piece {i} is null!");
                continue;
            }

            Debug.Log($"\n--- Piece {i}: {piece.name} ---");

            // Check CorrectRotationPuzzle
            var rotationScript = piece.GetComponent<CorrectRotationPuzzle>();
            if (rotationScript == null)
            {
                Debug.LogError($"  ❌ MISSING CorrectRotationPuzzle component!");
            }
            else
            {
                Debug.Log($"  ✅ Has CorrectRotationPuzzle");
                Debug.Log($"     - Enabled: {rotationScript.enabled}");
                Debug.Log($"     - TargetSlot: {(rotationScript.targetSlot != null ? rotationScript.targetSlot.name : "NULL")}");
                Debug.Log($"     - Rotation Speed: {rotationScript.rotationSpeed}");
            }

            // Check XRGrabInteractable
            var grab = piece.GetComponent<XRGrabInteractable>();
            if (grab == null)
            {
                Debug.LogError($"  ❌ MISSING XRGrabInteractable component!");
            }
            else
            {
                Debug.Log($"  ✅ Has XRGrabInteractable");
                Debug.Log($"     - Enabled: {grab.enabled}");
                Debug.Log($"     - trackRotation: {grab.trackRotation}");
                Debug.Log($"     - trackPosition: {grab.trackPosition}");
            }

            // Check Rigidbody
            var rb = piece.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError($"  ❌ MISSING Rigidbody component!");
            }
            else
            {
                Debug.Log($"  ✅ Has Rigidbody");
                Debug.Log($"     - isKinematic: {rb.isKinematic}");
                Debug.Log($"     - useGravity: {rb.useGravity}");
            }
        }
    }
}