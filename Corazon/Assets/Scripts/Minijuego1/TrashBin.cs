using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;
public class TrashBin : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Tag que deben tener los objetos de basura para ser aceptados")]
    public string trashTag = "Trash";

    [Tooltip("Retraso antes de destruir el objeto (en segundos)")]
    public float delayBeforeDestroy = 0f;
    private XRGrabInteractable grabInteractable;
    [Header("Efectos (opcional)")]
    [Tooltip("Sonido al botar la basura")]
    public AudioClip disposeSound;

    [Tooltip("Efecto de partículas al desaparecer")]
    public ParticleSystem disposeEffect;

    private AudioSource audioSource;

    private void Awake()
    {
        // Si hay un sonido asignado, aseguramos tener un AudioSource
        if (disposeSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(trashTag))
        {
            return;
        }

        GameObject trashObject = other.gameObject;

        // Sonido / partículas igual que antes...
        if (disposeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(disposeSound);
        }
        if (disposeEffect != null)
        {
            Instantiate(disposeEffect, trashObject.transform.position, Quaternion.identity);
        }

        // Soltar de la mano si está agarrado
        var grabInteractable = trashObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            grabInteractable.interactionManager.SelectExit(
                grabInteractable.interactorsSelecting[0],
                grabInteractable
            );
        }

        // En vez de Destroy directo, avisamos al manager
        if (BadPieceManager.Instance != null)
        {
            BadPieceManager.Instance.OnBadPieceRemoved(trashObject);
        }
        else
        {
            Debug.LogWarning("[TrashBin] No se encontró BadPieceManager.Instance, destruyendo directamente.");
            Destroy(trashObject, delayBeforeDestroy);
        }
    }



}