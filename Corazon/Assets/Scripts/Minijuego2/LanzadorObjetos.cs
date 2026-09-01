using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;

public class LanzadorObjetos : MonoBehaviour
{
    [Header("Configuración")]
    public float velocidad = 5f;
    public float intervalo = 1f;

    [Header("Altura de lanzamiento")]
    public float altura = 1f; // configurable desde el inspector

    [Header("Objetos a lanzar")]
    public GameObject[] objetos; // lista de objetos

    [Header("Efecto de explosión")]
    public GameObject explosionEffect; // el objeto de partículas ya en la escena, desactivado
    [Header("Efecto de humo")]
    public GameObject smokeEffect;
    [Header("Efecto de confetti")]
    public GameObject confettiEffect;

    [Header("SFX Correcto")]
    public AudioSource SFX_Correct_Source;

    [Header("SFX Incorrecto")]
    public AudioSource SFX_Incorrect_Source;

    [Header("SFX Lanzamiento")]
    public AudioSource SFX_Lanzamiento_Source;

    private Vector3[] posicionesIniciales; // posiciones originales
    private int indiceActual = 0;

    void Start()
    {
        // Guardar la posición inicial de cada objeto
        posicionesIniciales = new Vector3[objetos.Length];
        for (int i = 0; i < objetos.Length; i++)
        {
            posicionesIniciales[i] = objetos[i].transform.position;
            //objetos[i].SetActive(false); // desactivar al inicio si quieres
        }
    }

    // Método público que puedes llamar desde un botón
    public void Relanzar(float duracion)
    {
        //Shuffle();
        // Inicia la corutina que lanza todos los objetos uno por uno
        StartCoroutine(LanzarTodos(duracion));
    }

    // Método para mezclar el orden de los objetos
    private void Shuffle()
    {
        for (int i = objetos.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            // Intercambiar objetos
            GameObject tempObj = objetos[i];
            objetos[i] = objetos[randomIndex];
            objetos[randomIndex] = tempObj;

            // Intercambiar posiciones iniciales para que coincidan
            Vector3 tempPos = posicionesIniciales[i];
            posicionesIniciales[i] = posicionesIniciales[randomIndex];
            posicionesIniciales[randomIndex] = tempPos;
        }
    }


    // Método para lanzar cada objeto con un intervalo
    private IEnumerator LanzarTodos(float duracion)
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            Shuffle();

            for (int i = 0; i < objetos.Length; i++)
            {
                // Verificar si el tiempo ya se agotó dentro del loop
                if (tiempoTranscurrido >= duracion) yield break;

                GameObject objeto = objetos[i];
                if (objeto == null) continue;

                Rigidbody rb = objeto.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    objeto.SetActive(true);
                    objeto.transform.position = posicionesIniciales[i];
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    SFX_Lanzamiento_Source.Play();

                    // Reproducir el AudioSource propio del objeto, si tiene uno
                    AudioSource objetoAudioSource = objeto.GetComponent<AudioSource>();
                    if (objetoAudioSource != null)
                    {
                        if (!objetoAudioSource.enabled)
                        {
                            objetoAudioSource.enabled = true;
                        }
                        objetoAudioSource.Play();
                    }
                    else
                    {
                        Debug.LogWarning($"El objeto '{objeto.name}' no tiene un componente AudioSource.");
                    }

                    float fuerza = Mathf.Sqrt(2f * Physics.gravity.magnitude * altura);
                    rb.AddForce(Vector3.up * fuerza, ForceMode.VelocityChange);
                }

                tiempoTranscurrido += intervalo;
                yield return new WaitForSeconds(intervalo);
            }
        }

        Debug.Log("Tiempo de lanzamiento agotado.");
    }

    // Métodos para cuando el objeto sea agarrado
    public void OnGrabbed(SelectEnterEventArgs args)
    {
        GameObject grabbedObject = args.interactableObject.transform.gameObject;

        // En vez de destruir, lo desactivamos
        grabbedObject.SetActive(false);
    }

    private IEnumerator DesactivarEfecto(float tiempo)
    {
        yield return new WaitForSeconds(tiempo);
        explosionEffect.SetActive(false);
    }

    //
    public void OnGrabbedExplosionSmoke(SelectEnterEventArgs args)
    {
        GameObject grabbedObject = args.interactableObject.transform.gameObject;

        if (explosionEffect != null && smokeEffect != null)
        {
            // Mover el efecto a la posición del objeto
            explosionEffect.transform.position = grabbedObject.transform.position;
            smokeEffect.transform.position = grabbedObject.transform.position;

            // Activar el efecto
            explosionEffect.SetActive(true);
            smokeEffect.SetActive(true);

            // Reproducir el sonido incorrecto
            SFX_Incorrect_Source.Play();

            // Desactivarlo cuando termine la partícula
            ParticleSystem ps = explosionEffect.GetComponent<ParticleSystem>();
            ParticleSystem psSmoke = smokeEffect.GetComponent<ParticleSystem>();
            if (ps != null && psSmoke != null)
                StartCoroutine(DesactivarEfecto2(ps.main.duration));
        }

        grabbedObject.SetActive(false);
    }

    private IEnumerator DesactivarEfecto2(float tiempo)
    {
        yield return new WaitForSeconds(tiempo);
        explosionEffect.SetActive(false);
        smokeEffect.SetActive(false);
    }

    public void OnGrabbedCorrecto(SelectEnterEventArgs args)
    {
        GameObject grabbedObject = args.interactableObject.transform.gameObject;

        if (confettiEffect != null)
        {
            // Mover el efecto a la posición del objeto
            confettiEffect.transform.position = grabbedObject.transform.position;

            // Activar el efecto
            confettiEffect.SetActive(true);

            // Reproducir el sonido correcto
            SFX_Correct_Source.Play();

            // Desactivarlo cuando termine la partícula
            ParticleSystem ps = confettiEffect.GetComponent<ParticleSystem>();
            if (ps != null)
                StartCoroutine(DesactivarEfectoConfetti(ps.main.duration));
        }

        grabbedObject.SetActive(false);
    }

    private IEnumerator DesactivarEfectoConfetti(float tiempo)
    {
        yield return new WaitForSeconds(tiempo);
        confettiEffect.SetActive(false);
    }
}