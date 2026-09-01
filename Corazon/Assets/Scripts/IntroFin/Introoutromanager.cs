using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

/// <summary>
/// Manages the intro and outro sequences of the experience.
/// Handles its own background music independently from GameAudioManager.
///
/// INTRO FLOW:
///   1. Scene loads ? introMusic plays + introVO plays automatically, start button visible
///   2. User presses start button ? startButtonVO plays + tutorial begins
///   3. Tutorial dismisses ? begin2Button appears + begin2VO plays
///   4. User presses begin2Button ? delay ? "blink" fade out/in (reveals objects, rotates objects, changes light color) ? postTutorialMusic crossfades in + postTutorialVO plays ? postTutorialVO2 plays ? nextButton appears
///   5. nextButton pressed ? music stops + outroButton appears
///
/// OUTRO FLOW:
///   1. Teletransportacion plays outroVO1 via PlayIntroAudio
///   2. outroVO2 plays after outroVO1 finishes
///   3. outroVO3 plays after outroVO2 finishes
///   4. endObject appears
/// </summary>
public class IntroOutroManager : MonoBehaviour
{
    public static IntroOutroManager Instance { get; private set; }

    [Header("Background Music")]
    [Tooltip("Plays on scene load.")]
    public AudioClip introMusic;

    [Tooltip("Crossfades in when postTutorialVO starts.")]
    public AudioClip postTutorialMusic;

    [Tooltip("How long music crossfades take (seconds).")]
    public float musicCrossfadeDuration = 1.5f;

    [Range(0f, 1f)]
    public float musicVolume = 0.8f;

    [Header("Intro Voiceovers")]
    [Tooltip("Plays automatically when the scene loads.")]
    public AudioClip introVO;

    [Tooltip("Plays when the start button is pressed, alongside the tutorial appearing.")]
    public AudioClip startButtonVO;

    [Tooltip("Plays alongside begin2Button after tutorial dismisses.")]
    public AudioClip begin2VO;

    [Tooltip("Plays after begin2Button is pressed (after delay + blink).")]
    public AudioClip postTutorialVO;

    [Tooltip("Plays immediately after postTutorialVO finishes, before nextButton appears.")]
    public AudioClip postTutorialVO2;

    [Header("Intro UI")]
    [Tooltip("Visible at start, pressing it begins the tutorial.")]
    public GameObject startButton;

    [Tooltip("The tutorial object — hidden until start button is pressed.")]
    public GameObject tutorialObject;

    [Tooltip("Appears after tutorial dismisses alongside begin2VO.")]
    public GameObject begin2Button;

    [Tooltip("Appears after postTutorialVO2 finishes. Leads to Game 1.")]
    public GameObject nextButton;

    [Tooltip("Button that leads back to the outro scene. Hidden until nextButton is pressed.")]
    public GameObject outroButton;

    [Header("Timing")]
    [Tooltip("Delay in seconds between begin2Button press and the blink/postTutorialVO.")]
    public float begin2ToPostTutorialDelay = 1.5f;

    [Tooltip("Delay between postTutorialVO and postTutorialVO2.")]
    public float postTutorialBetweenDelay = 0.5f;

    [Header("Blink Transition (Camera Fade)")]
    [Tooltip("Quad mesh (Unlit/Transparent material) parented to the camera, used to simulate a blink.")]
    public Renderer blinkOverlay;

    [Tooltip("Color the overlay fades to (usually black, alpha will be animated).")]
    public Color blinkColor = Color.black;

    [Tooltip("How long the fade-out (eyes closing) takes.")]
    public float blinkFadeOutDuration = 0.4f;

    [Tooltip("How long the screen stays fully covered before fading back in.")]
    public float blinkHoldDuration = 0.3f;

    [Tooltip("How long the fade-in (eyes opening) takes.")]
    public float blinkFadeInDuration = 0.4f;

    [Header("Blink — Objects To Reveal")]
    [Tooltip("Objects that will be set active during the blink (while screen is covered).")]
    public GameObject[] objectsToReveal;

    [Header("Blink — Objects To Unreveal")]
    [Tooltip("Objects that will be set unactive during the blink (while screen is covered).")]
    public GameObject[] objectsToUnreveal;

    [Header("Blink — Objects To Rotate")]
    [Tooltip("Objects that will be instantly rotated to a new rotation during the blink.")]
    public Transform[] objectsToRotate;

    [Tooltip("New rotation (Euler angles) applied to each object in objectsToRotate during the blink.")]
    public Vector3[] newRotations;

    [Header("Blink — Lighting Change")]
    [Tooltip("Light(s) whose color will change during the blink.")]
    public Light[] lightsToChangeColor;

    [Tooltip("Color to restore the lights to when nextButton is pressed.")]
    public Color normalLightColor = Color.white;

    [Tooltip("New color applied to the lights during the blink.")]
    public Color newLightColor = Color.white;

    [Header("Outro Voiceovers")]
    [Tooltip("Plays after outroVO1 (fired by Teletransportacion).")]
    public AudioClip outroVO2;

    [Tooltip("Plays after outroVO2.")]
    public AudioClip outroVO3;

    [Tooltip("Delay between outro VOs.")]
    public float outroBetweenDelay = 1f;

    [Header("Outro UI")]
    [Tooltip("Appears after all outro VOs finish (e.g. 'End' text).")]
    public GameObject endObject;

    [Header("Outro Music")]
    public AudioClip outroMusic;

    [SerializeField] private QuadMaterialSwitcher materialSwitcher1;
    [SerializeField] private QuadMaterialSwitcher materialSwitcher2;

    // -------------------------------------------------------------------------

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;

    private bool startButtonPressed = false;
    private bool tutorialDone = false;
    private bool outroStarted = false;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Set up two music sources for crossfading
        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceB = gameObject.AddComponent<AudioSource>();

        foreach (var src in new[] { musicSourceA, musicSourceB })
        {
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
            src.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        if (startButton != null) startButton.SetActive(false);
        if (tutorialObject != null) tutorialObject.SetActive(false);
        if (begin2Button != null) begin2Button.SetActive(false);
        if (nextButton != null) nextButton.SetActive(false);
        if (outroButton != null) outroButton.SetActive(false);
        if (endObject != null) endObject.SetActive(false);

        // Ensure blink overlay starts fully transparent and non-blocking
        if (blinkOverlay != null)
        {
            Color c = blinkColor;
            c.a = 0f;
            blinkOverlay.material.color = c;
            blinkOverlay.gameObject.SetActive(true);
        }

        // Play intro music and VO on scene load
        if (introMusic != null)
            StartCoroutine(FadeInMusic(musicSourceA, introMusic));

        if (introVO != null)
            GameAudioManager.Instance?.PlayIntroAudio(introVO);
    }

    private void Update()
    {
        if (!startButtonPressed) return;

        if (!tutorialDone && tutorialObject != null)
        {
            Transform cardRoot = tutorialObject.transform.Find("CoachingCardRoot");
            bool dismissed = cardRoot != null
                ? !cardRoot.gameObject.activeInHierarchy
                : !tutorialObject.activeInHierarchy;

            if (dismissed)
            {
                tutorialDone = true;
                StartCoroutine(AfterTutorialSequence());
            }
        }
    }

    // -------------------------------------------------------------------------

    /// <summary>Called by the start button's OnClick.</summary>
    public void OnStartButtonPressed()
    {
        startButtonPressed = true;
        if (startButton != null) startButton.SetActive(false);
        if (tutorialObject != null) tutorialObject.SetActive(true);

        if (startButtonVO != null)
            GameAudioManager.Instance?.PlayTutorialAudio(startButtonVO);
    }

    /// <summary>Called by the begin2 button's OnClick.</summary>
    public void OnBegin2ButtonPressed()
    {
        GameAudioManager.Instance?.PlayTutorialAudio(null); // cuts begin2VO if still playing

        if (begin2Button != null) begin2Button.SetActive(false);
        StartCoroutine(PostTutorialSequence());

    }

    /// <summary>Called by the next button's OnClick (leads to Game 1).</summary>
    public void OnTeleportedToGame()
    {
        if (nextButton != null) nextButton.SetActive(false);
        if (outroButton != null) outroButton.SetActive(true);
        StartCoroutine(FadeOutAndStop(musicSourceA));
        StartCoroutine(FadeOutAndStop(musicSourceB));

        // Restore lights to their normal color
        if (lightsToChangeColor != null)
        {
            foreach (Light l in lightsToChangeColor)
            {
                if (l != null)
                    l.color = normalLightColor;
            }
        }
    }

    // -------------------------------------------------------------------------

    private IEnumerator AfterTutorialSequence()
    {
        if (begin2Button != null) begin2Button.SetActive(true);

        if (begin2VO != null)
            GameAudioManager.Instance?.PlayTutorialAudio(begin2VO);

        yield return null;
    }

    private IEnumerator PostTutorialSequence()
    {
        yield return new WaitForSeconds(begin2ToPostTutorialDelay);

        // "Blink" transition: fade out, change the scene while covered, fade back in
        yield return StartCoroutine(BlinkTransition());

        // Crossfade to post-tutorial music
        if (postTutorialMusic != null)
            StartCoroutine(CrossfadeMusic(musicSourceA, musicSourceB, postTutorialMusic));

        if (postTutorialVO != null)
        {
            GameAudioManager.Instance?.PlayTutorialAudio(postTutorialVO);
            yield return new WaitForSeconds(postTutorialVO.length);
            yield return new WaitForSeconds(postTutorialBetweenDelay);
        }

        if (nextButton != null)
            nextButton.SetActive(true);

        if (postTutorialVO2 != null)
        {
            GameAudioManager.Instance?.PlayTutorialAudio(postTutorialVO2);
            yield return new WaitForSeconds(postTutorialVO2.length);
        }
    }

    /// <summary>
    /// Simulates a long blink: fades the screen to blinkColor, applies scene changes
    /// (revealing objects, rotating objects, changing light colors) while covered,
    /// then fades back in.
    /// </summary>
    private IEnumerator BlinkTransition()
    {
        if (blinkOverlay == null)
        {
            // No overlay assigned — just apply changes instantly with no visual cover.
            ApplyBlinkChanges();
            yield break;
        }

        // Fade out (eyes closing)
        yield return StartCoroutine(FadeOverlay(0f, 1f, blinkFadeOutDuration));

        // Apply all scene changes while the screen is covered
        ApplyBlinkChanges();

        // Hold while fully covered
        if (blinkHoldDuration > 0f)
            yield return new WaitForSeconds(blinkHoldDuration);

        // Fade in (eyes opening)
        yield return StartCoroutine(FadeOverlay(1f, 0f, blinkFadeInDuration));

    }

    /// <summary>Applies object reveals, rotations, and light color changes for the blink.</summary>
    private void ApplyBlinkChanges()
    {
        if (materialSwitcher1 != null)
            materialSwitcher1.SwitchToMaterial2();

        if (materialSwitcher2 != null)
            materialSwitcher2.SwitchToMaterial2();

        if (objectsToReveal != null)
        {
            foreach (GameObject obj in objectsToReveal)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        if (objectsToUnreveal != null)
        {
            foreach (GameObject obj in objectsToUnreveal)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        if (objectsToRotate != null)
        {
            for (int i = 0; i < objectsToRotate.Length; i++)
            {
                if (objectsToRotate[i] == null) continue;

                if (newRotations != null && i < newRotations.Length)
                    objectsToRotate[i].rotation = Quaternion.Euler(newRotations[i]);
            }
        }

        if (lightsToChangeColor != null)
        {
            foreach (Light l in lightsToChangeColor)
            {
                if (l != null)
                    l.color = newLightColor;
            }
        }
    }

    private IEnumerator FadeOverlay(float fromAlpha, float toAlpha, float duration)
    {
        Color c = blinkColor;

        if (duration <= 0f)
        {
            c.a = toAlpha;
            blinkOverlay.material.color = c;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            blinkOverlay.material.color = c;
            yield return null;
        }

        c.a = toAlpha;
        blinkOverlay.material.color = c;
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Call from Teletransportacion when the player arrives for the outro.
    /// outroVO1 is already handled by Teletransportacion.PlayIntroAudio —
    /// pass its duration so this picks up right after.
    /// </summary>
    public void StartOutro(float outroVO1Duration)
    {
        if (outroStarted) return;
        outroStarted = true;
        StartCoroutine(OutroSequence(outroVO1Duration));
    }

    private IEnumerator OutroSequence(float outroVO1Duration)
    {
        if (outroMusic != null)
            StartCoroutine(FadeInMusic(musicSourceA, outroMusic));

        yield return new WaitForSeconds(outroVO1Duration + outroBetweenDelay);

        if (outroVO2 != null)
        {
            GameAudioManager.Instance?.PlayTutorialAudio(outroVO2);
            yield return new WaitForSeconds(outroVO2.length + outroBetweenDelay);
        }

        if (outroVO3 != null)
        {
            GameAudioManager.Instance?.PlayTutorialAudio(outroVO3);
            yield return new WaitForSeconds(outroVO3.length);
        }

        if (endObject != null)
            endObject.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Music helpers
    // -------------------------------------------------------------------------

    private IEnumerator FadeInMusic(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.volume = 0f;
        source.Play();

        float elapsed = 0f;
        while (elapsed < musicCrossfadeDuration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, musicVolume, elapsed / musicCrossfadeDuration);
            yield return null;
        }
        source.volume = musicVolume;
    }

    private IEnumerator CrossfadeMusic(AudioSource fadeOut, AudioSource fadeIn, AudioClip newClip)
    {
        float startVolume = fadeOut.volume;

        fadeIn.clip = newClip;
        fadeIn.volume = 0f;
        fadeIn.Play();

        float elapsed = 0f;
        while (elapsed < musicCrossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / musicCrossfadeDuration;
            fadeOut.volume = Mathf.Lerp(startVolume, 0f, t);
            fadeIn.volume = Mathf.Lerp(0f, musicVolume, t);
            yield return null;
        }

        fadeOut.volume = 0f;
        fadeOut.Stop();
        fadeIn.volume = musicVolume;
    }

    private IEnumerator FadeOutAndStop(AudioSource source)
    {
        if (source == null || !source.isPlaying) yield break;
        float startVolume = source.volume;
        float elapsed = 0f;
        while (elapsed < musicCrossfadeDuration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicCrossfadeDuration);
            yield return null;
        }
        source.Stop();
        source.volume = 0f;
    }

    // -------------------------------------------------------------------------
    public void CuartoOrdenado()
    {
        if (objectsToReveal != null)
        {
            foreach (GameObject obj in objectsToReveal)
            {
                if (obj != null)
                    DestroyImmediate(obj);
            }
        }

        if (objectsToUnreveal != null)
        {
            foreach (GameObject obj in objectsToUnreveal)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }
    }
}