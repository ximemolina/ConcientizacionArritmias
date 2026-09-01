using UnityEngine;

/// <summary>
/// Attach to each container trigger zone.
/// Set waveNumber to 1 or 2 in the Inspector so it reports to the right wave in WaveManager.
/// </summary>
public class ContainerChecker : MonoBehaviour
{
    [Tooltip("How many objects need to be inside to count as full.")]
    public int requiredCount = 3;

    [Tooltip("Which wave this container belongs to (1 or 2).")]
    public int waveNumber = 1;

    private int currentCount = 0;
    private bool isFull = false;

    // -------------------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlaceableObject"))
        {
            currentCount++;
            CheckIfFull();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlaceableObject"))
        {
            currentCount--;
            if (isFull)
            {
                isFull = false;
                NotifyEmptied();
            }
        }
    }

    // -------------------------------------------------------------------------

    private void CheckIfFull()
    {
        if (!isFull && currentCount >= requiredCount)
        {
            isFull = true;
            NotifyFilled();
        }
    }

    private void NotifyFilled()
    {
        if (waveNumber == 1)
            WaveManager.Instance?.OnWave1ContainerFilled();
        else if (waveNumber == 2)
            WaveManager.Instance?.OnWave2ContainerFilled();
    }

    private void NotifyEmptied()
    {
        if (waveNumber == 1)
            WaveManager.Instance?.OnWave1ContainerEmptied();
        else if (waveNumber == 2)
            WaveManager.Instance?.OnWave2ContainerEmptied();
    }
}