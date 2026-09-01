using NUnit.Framework.Interfaces;
using UnityEngine;

public class HeartBeat : MonoBehaviour
{
    [Header("Beat Settings")]
    public float beatsPerMinute = 72f;
    public float beatScale = 1.25f;
    public float baseScale = 1f;

    [Header("Timing")]
    [Range(0f, 1f)]
    public float contractRatio = 0.15f;
    [Range(0f, 1f)]
    public float expandRatio = 0.2f;

    [Header("Audio")]
    public AudioClip beatSound;
    [Range(0f, 1f)]
    public float volume = 1f;
    public float pitch = 1f;

    private AudioSource _audioSource;
    private float _beatInterval;
    private float _timer;
    private Vector3 _baseScaleVec;
    private Vector3 _peakScaleVec;

    private enum BeatPhase { Rest, Contract, Expand }
    private BeatPhase _phase = BeatPhase.Rest;
    private float _phaseTimer;

    public static HeartBeat Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    public void Resume()
    {
        if (_audioSource == null) return; // Start() hasn't run yet, nothing to resume
        enabled = true;
    }
    public void Stop()
    {
        _audioSource?.Stop();
        enabled = false;
    }

    private void Start()
    {
        _baseScaleVec = Vector3.one * baseScale;
        _peakScaleVec = Vector3.one * beatScale;
        _beatInterval = 60f / beatsPerMinute;
        transform.localScale = _baseScaleVec;

        _audioSource = gameObject.GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.volume = volume;
        _audioSource.pitch = pitch;
        _audioSource.clip = beatSound;

        // Sonido espacial
        _audioSource.spatialBlend = 0f;
        _audioSource.minDistance = 1f;
        _audioSource.maxDistance = 3f;

        // IMPORTANT: must set rolloffMode to Custom or the curve below is ignored
        _audioSource.rolloffMode = AudioRolloffMode.Custom;

        AnimationCurve rolloffCurve = new AnimationCurve();
        rolloffCurve.AddKey(1f, 1f);
        rolloffCurve.AddKey(3f, 0f);
        _audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloffCurve);
    }

    private void Update()
    {
        _beatInterval = 60f / beatsPerMinute;

        float contractDuration = _beatInterval * contractRatio;
        float expandDuration = _beatInterval * expandRatio;

        switch (_phase)
        {
            case BeatPhase.Rest:
                _timer += Time.deltaTime;
                if (_timer >= _beatInterval - contractDuration - expandDuration)
                {
                    _timer = 0f;
                    _phaseTimer = 0f;
                    _phase = BeatPhase.Contract;
                    PlayBeat();
                }
                break;

            case BeatPhase.Contract:
                _phaseTimer += Time.deltaTime;
                float cT = Mathf.Clamp01(_phaseTimer / contractDuration);
                transform.localScale = Vector3.Lerp(_baseScaleVec, _peakScaleVec, EaseInOut(cT));
                if (_phaseTimer >= contractDuration)
                {
                    _phaseTimer = 0f;
                    _phase = BeatPhase.Expand;
                }
                break;

            case BeatPhase.Expand:
                _phaseTimer += Time.deltaTime;
                float eT = Mathf.Clamp01(_phaseTimer / expandDuration);
                transform.localScale = Vector3.Lerp(_peakScaleVec, _baseScaleVec, EaseInOut(eT));
                if (_phaseTimer >= expandDuration)
                {
                    _phaseTimer = 0f;
                    _phase = BeatPhase.Rest;
                }
                break;
        }

        _audioSource.volume = volume;
        _audioSource.pitch = pitch;
    }

    private void PlayBeat()
    {
        if (beatSound == null || _audioSource == null) return;
        _audioSource.PlayOneShot(beatSound, volume);
    }

    private float EaseInOut(float t) => t * t * (3f - 2f * t);

    private void OnValidate()
    {
        beatsPerMinute = Mathf.Clamp(beatsPerMinute, 20f, 300f);
        beatScale = Mathf.Max(baseScale, beatScale);
        pitch = Mathf.Clamp(pitch, 0.1f, 3f);
    }
}