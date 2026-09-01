using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles all music and audio playback for the game.
/// Owns the persistent MusicHost AudioSources (A and B) for crossfading,
/// and a dedicated one-shot source (C) for voiceover/narration.
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Music Clips")]
    [Tooltip("Plays while the player is removing bad pieces.")]
    public AudioClip badPhaseMusic;

    [Tooltip("Plays when all bad pieces are gone and good pieces float in.")]
    public AudioClip goodPhaseMusic;

    [Header("Phase Voiceovers")]
    [Tooltip("Plays once when bad phase music starts (tutorial dismissed).")]
    public AudioClip badPhaseStartVO;

    [Tooltip("Plays once when good phase music starts (all bad pieces removed).")]
    public AudioClip goodPhaseStartVO;

    [Header("Good Piece Placed Voiceovers")]
    [Tooltip("Played when a good piece is correctly placed. Picked randomly if multiple assigned.")]
    public List<AudioClip> goodPiecePlacedVOs = new List<AudioClip>();

    [Header("Settings")]
    [Tooltip("How long crossfades take (seconds).")]
    public float crossfadeDuration = 1.5f;

    [Header("Puzzle Complete")]
    public AudioClip puzzleCompleteVO;
    public AudioClip puzzleCompleteVO2;

    public float musicVolume = 1f;
    public float voiceVolume = 1f;

    private GameObject musicHost; // ? add this field

    // -------------------------------------------------------------------------

    private AudioSource audioSourceA;   // music channel 1
    private AudioSource audioSourceB;   // music channel 2
    private AudioSource audioSourceVO;  // one-shot voice/narration channel

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;


        GameObject musicHost = new GameObject("MusicHost");
        musicHost.transform.SetParent(null);
        DontDestroyOnLoad(musicHost);

        audioSourceA = musicHost.AddComponent<AudioSource>();
        audioSourceB = musicHost.AddComponent<AudioSource>();
        audioSourceVO = musicHost.AddComponent<AudioSource>();

        foreach (var src in new[] { audioSourceA, audioSourceB })
        {
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
            src.spatialBlend = 0f;
        }

        audioSourceVO.loop = false;
        audioSourceVO.playOnAwake = false;
        audioSourceVO.volume = voiceVolume;
        audioSourceVO.spatialBlend = 0f;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by Teletransportacion when the player physically arrives in a room.
    /// Each portal passes its own clip, so this can be reused across portals.
    /// </summary>
    public void PlayIntroAudio(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSourceVO.isPlaying) audioSourceVO.Stop();
        PlayOneShot(clip);
    }

    /// <summary>
    /// Called by TutoManager when a step becomes visible.
    /// Stops any currently playing voiceover and plays the new clip immediately.
    /// Pass null to just stop the current voiceover.
    /// TutoManager should hold a List<AudioClip> frameVoiceovers and call:
    ///     GameAudioManager.Instance?.PlayTutorialAudio(frameVoiceovers[currentStep]);
    /// This method is reusable across all tutorial scenes — just populate the list per scene.
    /// </summary>
    public void PlayTutorialAudio(AudioClip clip)
    {
        if (audioSourceVO.isPlaying)
            audioSourceVO.Stop();

        if (clip == null) return;

        PlayOneShot(clip);
    }

    public void StopTutorialAudio(AudioClip clip)
    {
        if (clip == null) return;

        StopOneShot(clip);
    }

    /// <summary>
    /// Called by BadPieceManager when the tutorial UI dismisses.
    /// Starts bad-phase music and plays the bad phase start voiceover.
    /// </summary>
    public void PlayBadPhaseMusic()
    {
        if (badPhaseMusic == null)
        {
            Debug.LogError("[GameAudioManager] badPhaseMusic is NULL — assign it in the Inspector.");
            return;
        }

        StartCoroutine(FadeInMusic(audioSourceA, badPhaseMusic, crossfadeDuration));

        if (badPhaseStartVO != null)
        {
            if (audioSourceVO.isPlaying) audioSourceVO.Stop();
            PlayOneShot(badPhaseStartVO);
        }
    }

    /// <summary>
    /// Called by BadPieceManager when all bad pieces are removed.
    /// Crossfades to good-phase music and plays the good phase start voiceover.
    /// </summary>
    public void PlayGoodPhaseMusic()
    {
        if (goodPhaseMusic == null)
        {
            Debug.LogWarning("[GameAudioManager] No goodPhaseMusic assigned.");
            return;
        }

        StartCoroutine(CrossfadeMusic(audioSourceA, audioSourceB, goodPhaseMusic, crossfadeDuration));

        if (goodPhaseStartVO != null)
            StartCoroutine(PlayGoodPhaseVODelayed());
    }

    private IEnumerator PlayGoodPhaseVODelayed()
    {
        yield return new WaitForSeconds(1f);
        if (audioSourceVO.isPlaying) audioSourceVO.Stop();
        PlayOneShot(goodPhaseStartVO);
    }

    /// <summary>
    /// Called by CorrectRotationPuzzle when a good piece is correctly placed.
    /// Picks a random clip from goodPiecePlacedVOs. Safe to call if the list is empty.
    /// </summary>
    public void PlayGoodPiecePlacedVO()
    {
        if (goodPiecePlacedVOs == null || goodPiecePlacedVOs.Count == 0) return;

        AudioClip clip = goodPiecePlacedVOs[Random.Range(0, goodPiecePlacedVOs.Count)];
        if (clip == null) return;

        if (audioSourceVO.isPlaying) audioSourceVO.Stop();
        PlayOneShot(clip);
    }

    public void StopMusic(float fadeDuration = 1f)
    {
        StartCoroutine(FadeOutAndStop(audioSourceA, fadeDuration));
        StartCoroutine(FadeOutAndStop(audioSourceB, fadeDuration));
    }

    public void StopHeartbeat()
    {
        StartCoroutine(FadeOutAndStop(audioSourceB, 1f));
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private void PlayOneShot(AudioClip clip)
    {
        audioSourceVO.clip = clip;
        audioSourceVO.volume = voiceVolume;
        audioSourceVO.Play();
    }

    private void StopOneShot(AudioClip clip)
    {
        Debug.Log($"StopOneShot llamado. isPlaying antes: {audioSourceVO.isPlaying}, clip actual: {audioSourceVO.clip?.name}");
        audioSourceVO.Stop();
        Debug.Log($"isPlaying después: {audioSourceVO.isPlaying}");
    }

    private IEnumerator FadeInMusic(AudioSource source, AudioClip clip, float duration)
    {
        source.loop = true;
        source.clip = clip;
        source.volume = 0f;
        source.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, musicVolume, elapsed / duration);
            yield return null;
        }
        source.volume = musicVolume;
    }

    private IEnumerator CrossfadeMusic(AudioSource fadeOut, AudioSource fadeIn, AudioClip newClip, float duration)
    {
        float startVolume = fadeOut.volume;

        fadeIn.loop = true;
        fadeIn.clip = newClip;
        fadeIn.volume = 0f;
        fadeIn.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fadeOut.volume = Mathf.Lerp(startVolume, 0f, t);
            fadeIn.volume = Mathf.Lerp(0f, musicVolume, t);
            yield return null;
        }

        fadeOut.volume = 0f;
        fadeOut.Stop();
        fadeIn.volume = musicVolume;
    }

    private IEnumerator FadeOutAndStop(AudioSource source, float duration)
    {
        if (source == null || !source.isPlaying) yield break;
        float startVolume = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        source.Stop();
        source.volume = 0f;
    }

    public void PlayPuzzleCompleteVO()
    {
        StopMusic();
        StartCoroutine(PlayAfterFade());
    }

    private IEnumerator PlayAfterFade()
    {
        yield return new WaitForSeconds(crossfadeDuration);

        if (puzzleCompleteVO != null)
        {
            if (audioSourceVO.isPlaying) audioSourceVO.Stop();
            PlayOneShot(puzzleCompleteVO);

            // Wait for it to finish, then play the second one
            if (puzzleCompleteVO2 != null)
            {
                yield return new WaitForSeconds(puzzleCompleteVO.length+ 0.5f);
                PlayOneShot(puzzleCompleteVO2);
            }
        }
    }
    public void StopAll()
    {
        Debug.Log("[GameAudioManager] StopAll called");
        StopAllCoroutines();

        audioSourceA.Stop();
        audioSourceA.volume = 0f;
        audioSourceA.clip = null;

        audioSourceB.Stop();
        audioSourceB.volume = 0f;
        audioSourceB.clip = null;

        audioSourceVO.Stop();
        audioSourceVO.clip = null;

        HeartBeat.Instance?.Stop(); // ? add this
    }

}