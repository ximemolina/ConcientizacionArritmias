using UnityEngine;

public class DesactivarAlCaer : MonoBehaviour
{
    [Header("SFX Caida")]
    public AudioSource SFX_Caida_Source;

    private void OnCollisionEnter(Collision collision)
    {
        if (!gameObject.activeSelf)
            return;

        bool hitFloorTag = collision.gameObject.CompareTag("Suelo");
        bool hitFromAbove = collision.contactCount > 0 && collision.GetContact(0).normal.y > 0.5f;

        if (!hitFloorTag && !hitFromAbove)
            return;

        gameObject.SetActive(false);
        if (SFX_Caida_Source != null)
            SFX_Caida_Source.Play();
    }
}
