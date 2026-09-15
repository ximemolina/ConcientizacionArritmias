using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;

public class LanzadorTutorial : MonoBehaviour
{
    [Header("Configuración")]
    public float velocidad = 5f;
    public float intervalo = 1f;

    [Header("Altura de lanzamiento")]
    public float alturaMaxima = 3f; // hasta donde sube el objeto

    [Header("Objeto 1 - Se congela al bajar")]
    public float alturaCongeladoObjeto1 = 1f; // donde se congela al caer

    [Header("Objeto 2 - Se congela al bajar y luego cae")]
    public float offsetEsperaObjeto2 = 1f;
    public float alturaCongeladoObjeto2 = 1f; // donde se congela al caer
    public float tiempoCongeladoObjeto2 = 3f;

    [Header("Objetos a lanzar")]
    public GameObject[] objetos;

    [Header("Componentes a activar cuando el objeto 2 se desactive")]
    public GameObject[] m_ObjectsToActivate;

    [Header("Efecto de confetti")]
    public GameObject confettiEffect;

    [Header("Audios")]
    public AudioClip audio1; // Suena al lanzar el objeto 1
    public AudioClip audio2; // Suena al lanzar el objeto 2
    public AudioClip audio3; // Suena cuando el objeto 1 es agarrado

    [Header("SFX Correcto")]
    public AudioSource SFX_Correct_Source;

    [Header("SFX Lanzamiento")]
    public AudioSource SFX_Lanzamiento_Source;

    [Header("Player-relative spawn")]
    [Tooltip("Headset/camera used to place tutorial objects. If empty, Camera.main is used.")]
    public Transform playerHead;
    [Tooltip("Launch objects in front of the player once. They stay in world space after that.")]
    public bool spawnInFrontOfPlayer = true;
    [Tooltip("Distance in front of the player (meters).")]
    public float floatDistance = 1.35f;
    [Tooltip("Vertical offset from eye height at launch (negative = below the headset).")]
    public float heightOffset = -1.25f;

    private AudioSource audioSource;
    private Vector3[] posicionesIniciales;
    private bool objeto1Desactivado = false;
    private bool hasPlayerAnchor;
    private Vector3 launchAnchor;

    void Start()
    {
        posicionesIniciales = new Vector3[objetos.Length];
        for (int i = 0; i < objetos.Length; i++)
        {
            posicionesIniciales[i] = objetos[i].transform.position;
        }

        foreach (GameObject componente in m_ObjectsToActivate)
        {
            if (componente != null)
                componente.SetActive(false);
        }

        // Obtener o agregar AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void Relanzar()
    {
        objeto1Desactivado = false;
        CachePlayerLaunchAnchor();
        StartCoroutine(LanzarTodos());
    }

    private IEnumerator LanzarTodos()
    {
        // --- OBJETO 1 ---
        if (objetos.Length > 0 && objetos[0] != null)
        {
            GameObject obj1 = objetos[0];
            Rigidbody rb1 = obj1.GetComponent<Rigidbody>();

            if (rb1 != null)
            {
                obj1.SetActive(true);
                obj1.transform.position = GetLaunchPosition(0);
                rb1.isKinematic = false;
                rb1.useGravity = true;
                rb1.linearVelocity = Vector3.zero;
                rb1.angularVelocity = Vector3.zero;

                // Reproducir audio 1 al lanzar el objeto 1
                ReproducirAudio(audio1);

                if (SFX_Lanzamiento_Source != null)
                    SFX_Lanzamiento_Source.Play();
                float fuerza1 = Mathf.Sqrt(2f * Physics.gravity.magnitude * alturaMaxima);
                rb1.AddForce(Vector3.up * fuerza1, ForceMode.VelocityChange);

                yield return StartCoroutine(SubirYCongelarAlBajar(obj1, rb1, alturaCongeladoObjeto1));
            }
        }

        // --- ESPERAR A QUE EL OBJETO 1 SEA DESACTIVADO (agarrado) ---
        // OnGrabbed y OnGrabbedCorrecto ya reproducen audio3 y setean objeto1Desactivado = true
        yield return new WaitUntil(() => objeto1Desactivado);
        Debug.Log("Objeto 1 agarrado, esperando que termine el audio 3...");

        // --- ESPERAR A QUE TERMINE EL AUDIO 3 ANTES DE LANZAR EL OBJETO 2 ---
        if (audioSource != null && audioSource.isPlaying)
        {
            yield return new WaitUntil(() => !audioSource.isPlaying);
        }
        Debug.Log("Audio 3 terminado, esperando offset...");

        // --- OFFSET ANTES DE LANZAR OBJETO 2 ---
        yield return new WaitForSeconds(offsetEsperaObjeto2);

        // --- OBJETO 2 ---
        if (objetos.Length > 1 && objetos[1] != null)
        {
            GameObject obj2 = objetos[1];
            Rigidbody rb2 = obj2.GetComponent<Rigidbody>();

            if (rb2 != null)
            {
                obj2.SetActive(true);
                obj2.transform.position = GetLaunchPosition(1);
                rb2.isKinematic = false;
                rb2.useGravity = true;
                rb2.linearVelocity = Vector3.zero;
                rb2.angularVelocity = Vector3.zero;

                // Reproducir audio 2 al lanzar el objeto 2
                ReproducirAudio(audio2);

                if (SFX_Lanzamiento_Source != null)
                    SFX_Lanzamiento_Source.Play();

                float fuerza2 = Mathf.Sqrt(2f * Physics.gravity.magnitude * alturaMaxima);
                rb2.AddForce(Vector3.up * fuerza2, ForceMode.VelocityChange);

                yield return StartCoroutine(SubirYCongelarAlBajar(obj2, rb2, alturaCongeladoObjeto2));

                Debug.Log($"Objeto 2 congelado, esperando a que termine el audio 2 y luego {tiempoCongeladoObjeto2} segundos...");

                // --- ESPERAR A QUE TERMINE EL AUDIO 2 ANTES DE DEJAR CAER EL OBJETO 2 ---
                if (audioSource != null && audioSource.isPlaying)
                {
                    yield return new WaitUntil(() => !audioSource.isPlaying);
                }

                // Espera adicional configurada en el inspector
                yield return new WaitForSeconds(tiempoCongeladoObjeto2);

                // Descongelar y dejar caer
                rb2.isKinematic = false;
                rb2.useGravity = true;
                Debug.Log("Objeto 2 cayendo...");

                yield return StartCoroutine(WaitUntilFallenOrInactive(obj2, rb2));

                if (obj2.activeSelf)
                    obj2.SetActive(false);

                Debug.Log("Objeto 2 desactivado, activando componentes...");
                activateObjects();
            }
        }
    }

    private void CachePlayerLaunchAnchor()
    {
        hasPlayerAnchor = false;
        if (!spawnInFrontOfPlayer)
            return;

        Transform player = playerHead != null ? playerHead : (Camera.main != null ? Camera.main.transform : null);
        if (player == null)
        {
            Debug.LogWarning("[LanzadorTutorial] No player head found — using original launch positions.");
            return;
        }

        Vector3 forward = player.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(player.up, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        launchAnchor = player.position + forward * floatDistance + Vector3.up * heightOffset;
        hasPlayerAnchor = true;
    }

    private Vector3 GetLaunchPosition(int index)
    {
        if (hasPlayerAnchor)
            return launchAnchor;

        if (posicionesIniciales != null && index >= 0 && index < posicionesIniciales.Length)
            return posicionesIniciales[index];

        return transform.position;
    }

    private IEnumerator SubirYCongelarAlBajar(GameObject obj, Rigidbody rb, float alturaCongelado)
    {
        float alturaInicial = obj.transform.position.y;
        float alturaCongeladoMundo = alturaInicial + alturaCongelado;

        // --- PASO 1: Esperar a que empiece a caer (velocidad vertical negativa) ---
        yield return new WaitUntil(() => rb.linearVelocity.y < 0 || !obj.activeSelf);

        if (!obj.activeSelf) yield break;

        Debug.Log($"{obj.name} en pico, comenzando a bajar...");

        // --- PASO 2: Esperar a que baje hasta la altura de congelado ---
        yield return new WaitUntil(() => obj.transform.position.y <= alturaCongeladoMundo || !obj.activeSelf);

        if (!obj.activeSelf) yield break;

        // --- PASO 3: Congelar ---
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;

        obj.transform.position = new Vector3(obj.transform.position.x, alturaCongeladoMundo, obj.transform.position.z);
        Debug.Log($"{obj.name} congelado a altura: {obj.transform.position.y}");
    }

    private IEnumerator WaitUntilFallenOrInactive(GameObject obj, Rigidbody rb)
    {
        float timeout = 4f;
        float elapsed = 0f;
        float startY = obj.transform.position.y;

        while (obj.activeSelf && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            bool landed = obj.transform.position.y < startY - 0.4f
                          && rb != null
                          && Mathf.Abs(rb.linearVelocity.y) < 0.15f;
            if (landed)
                yield break;
            yield return null;
        }
    }

    void activateObjects()
    {
        if (m_ObjectsToActivate != null && m_ObjectsToActivate.Length > 0)
        {
            foreach (GameObject obj in m_ObjectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"Objeto activado: {obj.name}");

                    SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();
                    if (smr != null)
                    {
                        smr.enabled = true;
                        Debug.Log($"SkinnedMeshRenderer activado en: {obj.name}");
                    }

                    Transform child = obj.transform.Find("CoachingCardRoot");
                    if (child != null)
                    {
                        child.gameObject.SetActive(true);
                        Debug.Log("Objeto hijo 'CoachingCardRoot' activado.");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("No se asignaron objetos en la lista m_ObjectsToActivate.");
        }
    }

    private void ReproducirAudio(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    public void OnGrabbedCorrecto(SelectEnterEventArgs args)
    {
        GameObject grabbedObject = args.interactableObject.transform.gameObject;

        if (confettiEffect != null)
        {
            confettiEffect.transform.position = grabbedObject.transform.position;
            confettiEffect.SetActive(true);
            SFX_Correct_Source.Play();

            ParticleSystem ps = confettiEffect.GetComponent<ParticleSystem>();
            if (ps != null)
                StartCoroutine(DesactivarEfectoConfetti(ps.main.duration));
        }

        if (objetos.Length > 0 && grabbedObject == objetos[0])
        {
            // Reproducir audio 3 cuando el objeto 1 es agarrado (versión correcta)
            ReproducirAudio(audio3);
            objeto1Desactivado = true;
        }

        grabbedObject.SetActive(false);
    }

    private IEnumerator DesactivarEfectoConfetti(float tiempo)
    {
        yield return new WaitForSeconds(tiempo);
        confettiEffect.SetActive(false);
    }
}