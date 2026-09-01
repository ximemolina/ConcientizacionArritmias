using UnityEngine;

/// <summary>
/// Attach this script to a sliding door GameObject.
/// It plays a whoosh sound based on how fast the door is moving linearly.
/// Works with physics-driven, XR interactable, or animated sliding doors.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DoorWhooshSound : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("The whoosh audio clip to play (looping swoosh/air sound works best).")]
    public AudioClip whooshClip;

    [Header("Trigger Settings")]
    [Tooltip("Minimum speed (units/sec) to start playing the whoosh.")]
    public float minSpeed = 0.05f;

    [Tooltip("Speed (units/sec) at which volume and pitch reach maximum.")]
    public float maxSpeed = 2f;

    [Header("Sound Shaping")]
    [Tooltip("Volume range mapped from min to max speed.")]
    public float minVolume = 0.1f;
    public float maxVolume = 1.0f;

    [Tooltip("Pitch range mapped from min to max speed. Faster = higher pitch.")]
    public float minPitch = 0.8f;
    public float maxPitch = 1.3f;

    [Tooltip("How quickly the sound fades out when the door slows down (seconds).")]
    public float fadeOutTime = 0.15f;

    // ?? Internals ????????????????????????????????????????????????????????????

    private AudioSource _audioSource;
    private Vector3 _previousPosition;
    private float _currentSpeed;   // smoothed units/sec
    private float _fadeTimer;

    private const float SmoothFactor = 8f;

    // ?? Unity Lifecycle ??????????????????????????????????????????????????????

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        _audioSource.clip = whooshClip;
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f;  // full 3D audio in VR
        _audioSource.volume = 0f;
        _audioSource.Stop();
    }

    private void Start()
    {
        _previousPosition = transform.position;
    }

    private void Update()
    {
        // ?? 1. Measure linear speed this frame ???????????????????????????????
        float distance = Vector3.Distance(transform.position, _previousPosition);
        float rawSpeed = distance / Time.deltaTime;   // units per second
        _previousPosition = transform.position;

        // Smooth to avoid single-frame spikes
        _currentSpeed = Mathf.Lerp(_currentSpeed, rawSpeed, Time.deltaTime * SmoothFactor);

        // ?? 2. Decide whether to play ????????????????????????????????????????
        bool moving = _currentSpeed >= minSpeed;

        if (moving)
        {
            _fadeTimer = fadeOutTime;

            if (!_audioSource.isPlaying)
                _audioSource.Play();

            float t = Mathf.InverseLerp(minSpeed, maxSpeed, _currentSpeed);

            _audioSource.volume = Mathf.Lerp(minVolume, maxVolume, t);
            _audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, t);
        }
        else if (_audioSource.isPlaying)
        {
            // ?? 3. Fade out gracefully when the door stops ???????????????????
            _fadeTimer -= Time.deltaTime;

            float fadeT = Mathf.Clamp01(_fadeTimer / fadeOutTime);
            _audioSource.volume = Mathf.Lerp(0f, minVolume, fadeT);

            if (_fadeTimer <= 0f)
                _audioSource.Stop();
        }
    }

    /// <summary>Call this to force-stop the sound (e.g. door locked mid-slide).</summary>
    public void ForceStop()
    {
        _currentSpeed = 0f;
        _audioSource.Stop();
    }
}