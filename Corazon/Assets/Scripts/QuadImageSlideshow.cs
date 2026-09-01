using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class QuadImageSlideshow : MonoBehaviour
{
    [Header("Imágenes")]
    [Tooltip("Lista de imágenes que se irán mostrando en orden")]
    public Texture2D[] images;

    [Header("Configuración de tiempo")]
    [Tooltip("Segundos que dura cada imagen en pantalla")]
    public float secondsPerImage = 2f;

    [Header("Opciones")]
    [Tooltip("Si está activo, al llegar a la última imagen vuelve a la primera")]
    public bool loop = false;

    [Tooltip("Si está activo, se puede volver a presionar el botón mientras corre para reiniciar")]
    public bool allowRestart = true;

    private Renderer quadRenderer;
    private Coroutine slideshowCoroutine;
    private int currentIndex = 0;
    private bool isRunning = false;

    void Awake()
    {
        quadRenderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// Llama esta función desde el OnClick() de tu botón en el Inspector.
    /// </summary>
    public void StartSlideshow()
    {
        if (images == null || images.Length == 0)
        {
            Debug.LogWarning($"[{name}] No hay imágenes asignadas en la lista.");
            return;
        }

        if (isRunning)
        {
            if (!allowRestart) return;

            // Reinicia desde cero
            StopCoroutine(slideshowCoroutine);
        }

        currentIndex = 0;
        slideshowCoroutine = StartCoroutine(RunSlideshow());
    }

    /// <summary>
    /// Permite detener el slideshow manualmente si lo necesitas.
    /// </summary>
    public void StopSlideshow()
    {
        if (slideshowCoroutine != null)
            StopCoroutine(slideshowCoroutine);

        isRunning = false;
    }

    private IEnumerator RunSlideshow()
    {
        isRunning = true;

        do
        {
            for (int i = 0; i < images.Length; i++)
            {
                currentIndex = i;
                SetImage(images[currentIndex]);
                yield return new WaitForSeconds(secondsPerImage);
            }
        }
        while (loop);

        isRunning = false;
    }

    private void SetImage(Texture2D tex)
    {
        if (tex == null) return;

        // mainTexture funciona con la mayoría de shaders (Standard, URP Lit, Unlit, etc.)
        quadRenderer.material.mainTexture = tex;
    }
}
