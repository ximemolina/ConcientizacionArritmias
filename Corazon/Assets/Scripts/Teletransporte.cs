using UnityEngine;
using System.Collections;

public class Teletransportacion : MonoBehaviour
{
    private const string SkyExposureProperty = "_Exposure";

    public Transform Target;
    public GameObject ThePlayer;
    public Camera PlayerCamera;

    [Header("Audio")]
    [Tooltip("Plays when the player enters this portal. Leave empty for no audio.")]
    public AudioClip arrivalAudio;

    [Header("Outro")]
    [Tooltip("If this portal leads to the outro scene, assign the outroVO1 clip here so the manager knows its duration.")]
    public AudioClip outroVO1;

    [Header("Heart Room")]
    [Tooltip("Check this on the portal that leads INTO the heart's room.")]
    public bool resumeHeartbeatOnArrival = false;

    [Header("Sky")]
    [Tooltip("If enabled, sets the skybox exposure when arriving through this portal.")]
    public bool applySkyExposure = false;

    [Tooltip("Procedural skybox _Exposure to apply on arrival.")]
    public float skyExposure = 1.5f;

    [Tooltip("How long the sky and GameLight blend takes (seconds).")]
    public float skyExposureLerpDuration = 0.8f;

    [Header("Main Light")]
    [Tooltip("Directional light that actually lights the rooms. If empty, uses RenderSettings.sun (GameLight).")]
    public Light gameLight;

    [Tooltip("If enabled, lerps GameLight intensity on arrival.")]
    public bool applyGameLightIntensity = false;

    [Tooltip("GameLight intensity to apply on arrival.")]
    public float gameLightIntensity = 3f;

    [Tooltip("If enabled, lerps GameLight color on arrival. Darker rooms should use a cooler blue.")]
    public bool applyGameLightColor = false;

    [Tooltip("GameLight color to apply on arrival.")]
    public Color gameLightColor = new Color(1f, 0.9764706f, 0.8039216f);

    private static Material runtimeSkybox;
    private static Coroutine skyExposureCoroutine;
    private static Teletransportacion skyExposureHost;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == ThePlayer)
        {
            Vector3 cameraOffset = PlayerCamera.transform.position - ThePlayer.transform.position;
            cameraOffset.y = 0;
            ThePlayer.transform.position = Target.position - cameraOffset;
            float camaraYaw = PlayerCamera.transform.eulerAngles.y;
            float destinoYaw = Target.eulerAngles.y;
            float deltaYaw = destinoYaw - camaraYaw;
            ThePlayer.transform.Rotate(0, deltaYaw, 0, Space.World);

            ApplyDayLightingIfNeeded();

            if (arrivalAudio != null)
                StartCoroutine(PlayArrivalAudioDelayed());
        }
    }
    public void Teleport()
    {
        Vector3 cameraOffset = PlayerCamera.transform.position - ThePlayer.transform.position;
        cameraOffset.y = 0;

        Vector3 newPosition = Target.position - cameraOffset;
        newPosition.y = ThePlayer.transform.position.y; // ? preserve current height
        ThePlayer.transform.position = newPosition;

        float camaraYaw = PlayerCamera.transform.eulerAngles.y;
        float destinoYaw = Target.eulerAngles.y;
        float deltaYaw = destinoYaw - camaraYaw;
        ThePlayer.transform.Rotate(0, deltaYaw, 0, Space.World);

        GameAudioManager.Instance?.StopAll();
        IntroOutroManager.Instance?.OnTeleportedToGame();

        ApplyDayLightingIfNeeded();

        if (resumeHeartbeatOnArrival)
            HeartBeat.Instance?.Resume();

        if (arrivalAudio != null)
            StartCoroutine(PlayArrivalAudioDelayed());
        if (outroVO1 != null)
            IntroOutroManager.Instance?.StartOutro(outroVO1.length);
    }
    public IEnumerator PlayArrivalAudioDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        GameAudioManager.Instance?.PlayIntroAudio(arrivalAudio);
    }

    private void ApplyDayLightingIfNeeded()
    {
        if (!applySkyExposure && !applyGameLightIntensity && !applyGameLightColor)
            return;

        Material sky = null;
        if (applySkyExposure)
        {
            sky = GetRuntimeSkybox();
            if (sky != null && !sky.HasProperty(SkyExposureProperty))
                sky = null;
        }

        Light light = (applyGameLightIntensity || applyGameLightColor)
            ? (gameLight != null ? gameLight : RenderSettings.sun)
            : null;

        if (sky == null && light == null)
            return;

        if (skyExposureHost != null && skyExposureCoroutine != null)
            skyExposureHost.StopCoroutine(skyExposureCoroutine);

        skyExposureHost = this;
        skyExposureCoroutine = StartCoroutine(LerpDayLighting(sky, light, skyExposureLerpDuration));
    }

    private static Material GetRuntimeSkybox()
    {
        Material current = RenderSettings.skybox;
        if (current == null)
            return null;

        if (runtimeSkybox == null)
        {
            runtimeSkybox = new Material(current);
            runtimeSkybox.name = current.name + " (Runtime)";
            RenderSettings.skybox = runtimeSkybox;
        }
        else if (RenderSettings.skybox != runtimeSkybox)
        {
            RenderSettings.skybox = runtimeSkybox;
        }

        return runtimeSkybox;
    }

    private IEnumerator LerpDayLighting(Material sky, Light light, float duration)
    {
        float startExposure = sky != null ? sky.GetFloat(SkyExposureProperty) : 0f;
        float startIntensity = light != null ? light.intensity : 0f;
        Color startColor = light != null ? light.color : Color.white;

        if (duration <= 0f)
        {
            ApplyDayLightingImmediate(sky, light);
            skyExposureCoroutine = null;
            skyExposureHost = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (sky != null)
                sky.SetFloat(SkyExposureProperty, Mathf.Lerp(startExposure, skyExposure, t));
            if (light != null)
            {
                if (applyGameLightIntensity)
                    light.intensity = Mathf.Lerp(startIntensity, gameLightIntensity, t);
                if (applyGameLightColor)
                    light.color = Color.Lerp(startColor, gameLightColor, t);
            }
            yield return null;
        }

        ApplyDayLightingImmediate(sky, light);
        skyExposureCoroutine = null;
        skyExposureHost = null;
    }

    private void ApplyDayLightingImmediate(Material sky, Light light)
    {
        if (sky != null)
            sky.SetFloat(SkyExposureProperty, skyExposure);
        if (light == null)
            return;
        if (applyGameLightIntensity)
            light.intensity = gameLightIntensity;
        if (applyGameLightColor)
            light.color = gameLightColor;
    }
}
