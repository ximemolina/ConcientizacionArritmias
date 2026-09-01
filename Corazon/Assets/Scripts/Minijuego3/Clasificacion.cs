using UnityEngine;

public class Clasificacion : MonoBehaviour
{
    [Header("Objetos válidos")]
    public GameObject[] validObjects;

    [Header("Particle System")]
    public ParticleSystem particles;

    [Header("Sonidos")]
    public AudioClip validSound;    // sonido para objeto correcto
    public AudioClip invalidSound;  // sonido para objeto incorrecto
    public float soundVolume = 1f;

    [Header("Efecto de luz")]
    public Light pointLight;        // referencia al Point Light
    public float lightDuration = 1f; // tiempo que dura encendido
    public Color validLightColor = Color.yellow; // color para objeto válido
    public Color invalidLightColor = Color.red;  // color para objeto inválido

    [Header("Wave")]
    public int waveNumber = 1;

    [Header("Rebote")]
    public float bounceForce = 6f;

    [Header("Destrucción")]
    public float destroyDelay = 0.5f;

    [Header("Retorno a posición")]
    [Tooltip("Tiempo de espera antes de que el objeto incorrecto flote de vuelta a su lugar.")]
    public float returnDelay = 0.75f;

    private int validCount = 0;
    private bool isFull = false;

    private void Awake()
    {
        if (particles == null)
            particles = GetComponent<ParticleSystem>();
        if (particles != null)
            particles.Stop();

        if (pointLight != null)
            pointLight.gameObject.SetActive(false); // luz apagada al inicio
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Clasificacion] Trigger entered by: {other.gameObject.name}, valid: {IsValidObject(other.gameObject)}");

        bool valid = IsValidObject(other.gameObject);

        if (valid)
        {
            // Partículas solo para objetos válidos
            if (particles != null)
            {
                var main = particles.main;
                main.loop = false;
                particles.Play();
            }

            // Sonido de objeto válido
            PlaySound2D(validSound);

            // Luz de objeto válido (amarillo configurable)
            if (pointLight != null)
            {
                pointLight.color = validLightColor;
                pointLight.gameObject.SetActive(true);
                CancelInvoke(nameof(DisableLight));
                Invoke(nameof(DisableLight), lightDuration);
            }

            validCount++;
            WaveManager.Instance?.OnValidObjectPlaced();
            Destroy(other.gameObject, destroyDelay);
        }
        else
        {
            // Sonido de objeto inválido
            PlaySound2D(invalidSound);

            // Luz de objeto inválido (rojo configurable)
            if (pointLight != null)
            {
                pointLight.color = invalidLightColor;
                pointLight.gameObject.SetActive(true);
                CancelInvoke(nameof(DisableLight));
                Invoke(nameof(DisableLight), lightDuration);
            }

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 bounceDirection = (other.transform.position - transform.position).normalized;
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);
            }

            // Float the misplaced object back to its correct wave slot after a short delay
            WaveManager.Instance?.ReturnObjectToTarget(other.gameObject, returnDelay);
        }
    }

    private void PlaySound2D(AudioClip clip)
    {
        if (clip == null) return;

        GameObject tempGO = new GameObject("TempAudio_" + clip.name);
        AudioSource tempSource = tempGO.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = soundVolume;
        tempSource.spatialBlend = 0f; // 2D, audible sin importar la posición
        tempSource.Play();
        Destroy(tempGO, clip.length);
    }

    private void DisableLight()
    {
        if (pointLight != null)
            pointLight.gameObject.SetActive(false);
    }

    private bool IsValidObject(GameObject obj)
    {
        foreach (GameObject validObj in validObjects)
        {
            if (validObj == obj) return true;
        }
        return false;
    }

    public void Reset()
    {
        validCount = 0;
        isFull = false;
    }
}