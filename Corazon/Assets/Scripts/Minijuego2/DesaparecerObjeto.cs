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

  
}