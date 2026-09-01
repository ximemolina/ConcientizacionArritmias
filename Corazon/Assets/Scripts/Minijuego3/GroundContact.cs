using UnityEngine;

/// <summary>
/// Detects when this object falls below a height threshold (i.e. it landed
/// on the floor) and tells WaveManager to float it back to its slot.
/// Added automatically by WaveManager when each wave object is spawned —
/// no tags, layers, or per-prefab setup required.
/// </summary>
public class GroundContact : MonoBehaviour
{
    [Tooltip("If this object's world Y position drops below this value, it's treated as having hit the ground.")]
    public float groundY = 0.1f;

    [Tooltip("Delay before floating back, in seconds.")]
    public float returnDelay = 0.75f;

    private bool returning = false;

    private void Update()
    {
        if (returning) return;

        if (transform.position.y <= groundY)
        {
            returning = true;
            WaveManager.Instance?.ReturnObjectToTarget(gameObject, returnDelay);
        }
    }

    // Called by WaveManager's FloatInOnly once the object is back in place,
    // so it can trigger a ground-return again later without being stuck.
    public void ResetReturnState()
    {
        returning = false;
    }
}