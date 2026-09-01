using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

/// <summary>
/// Attach to every bad piece. No other components needed.
/// When dropped, waits briefly, plays a snappy burst animation, then destroys itself.
/// </summary>
public class DesaparecerObjeto : MonoBehaviour
{
    [Header("Burst Animation")]
    public float burstDuration = 0.25f;
    public float maxScale = 1.15f;

    [Header("Audio")]
    public AudioClip burstSound;
    [Range(0f, 1f)]
    public float burstVolume = 1f;

    [Header("Particles (optional)")]
    public ParticleSystem burstParticles;

    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;
    private Vector3 originalScale;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectExited.AddListener(OnDrop);
        originalScale = transform.localScale;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
    }

    private void OnDrop(SelectExitEventArgs args)
    {
        grabInteractable.selectExited.RemoveListener(OnDrop);
        grabInteractable.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        StartCoroutine(WaitThenBurst());
    }

    private IEnumerator WaitThenBurst()
    {
        yield return new WaitForSeconds(0.2f);

        // Play sound at burst moment
        if (burstSound != null)
            audioSource.PlayOneShot(burstSound, burstVolume);

        if (burstParticles != null)
        {
            burstParticles.transform.SetParent(null);
            burstParticles.Play();
        }

        float elapsed = 0f;
        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / burstDuration);

            float scaleFactor;
            if (t < 0.2f)
                scaleFactor = Mathf.Lerp(1f, maxScale, t * 5f);
            else
                scaleFactor = Mathf.Lerp(maxScale, 0f, (t - 0.2f) * 1.25f);

            transform.localScale = originalScale * scaleFactor;
            yield return null;
        }

        if (BadPieceManager.Instance != null)
            BadPieceManager.Instance.OnBadPieceRemoved(gameObject);

        Destroy(gameObject);
    }
}