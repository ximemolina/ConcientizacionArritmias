using UnityEngine;

public class HacerSonarAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private void OnEnable()
    {
        audioSource.Play();
    }
}