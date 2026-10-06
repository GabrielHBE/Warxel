using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Warxel/Camera/Thermal Vision Post FX")]
public class ThermalVisionPostFX : CameraPostFX
{
    [Header("Thermal Camera")]
    [Tooltip("Camera affected by thermal vision. Defaults to the parent camera.")]
    [SerializeField] private Camera targetCamera;

    [Header("White Hot Image")]
    [SerializeField, Range(0f, 1f)] private float backgroundBrightness = 0.3f;
    [SerializeField, Range(0.1f, 4f)] private float contrast = 1.4f;
    [SerializeField, Range(1f, 8f)] private float hotBrightness = 2f;
    [SerializeField, Range(0f, 1f)] private float heatGlow = 0.15f;
    [SerializeField, Range(0f, 0.2f)] private float noiseIntensity = 0.025f;
    [SerializeField, Range(0f, 1f)] private float vignetteIntensity = 0.2f;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float turnOnDuration = 0.15f;
    [SerializeField, Min(0f)] private float turnOffDuration = 0.15f;

    private static readonly Dictionary<Camera, ThermalVisionPostFX> CameraEffects =
        new Dictionary<Camera, ThermalVisionPostFX>();
    private int hotLayerMask;
    private bool requestedActive;

    public struct RenderSettings
    {
        public int layerMask;
        public Vector4 image;
        public Vector4 sensor;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearCameraEffects() => CameraEffects.Clear();

    protected override void InitializeVolume()
    {
        // Layer selection requires a render pass; it does not modify other PostFX volumes.
        if (targetCamera == null) targetCamera = GetComponentInParent<Camera>();
        hotLayerMask = LayerMask.GetMask("Vehicle", "PlayerHitBox", "Projectile");
        currentMultiplier = 0f;
        requestedActive = false;
    }

    private void OnEnable()
    {
        if (targetCamera == null) targetCamera = GetComponentInParent<Camera>();
        if (targetCamera != null) CameraEffects[targetCamera] = this;
    }

    private void OnDisable()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = null;
        currentMultiplier = 0f;
        requestedActive = false;
        if (targetCamera != null && CameraEffects.TryGetValue(targetCamera, out ThermalVisionPostFX effect) && effect == this)
            CameraEffects.Remove(targetCamera);
    }

    public override void SetActive(bool active)
    {
        if (!isActiveAndEnabled || requestedActive == active) return;
        if (targetCamera == null)
        {
            targetCamera = GetComponentInParent<Camera>();
            if (targetCamera == null)
            {
                Debug.LogWarning("ThermalVisionPostFX requires a target camera or a parent Camera.", this);
                return;
            }
        }

        CameraEffects[targetCamera] = this;
        requestedActive = active;
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        float duration = active ? turnOnDuration : turnOffDuration;
        if (duration <= 0f)
        {
            currentMultiplier = active ? 1f : 0f;
            transitionCoroutine = null;
            return;
        }

        transitionCoroutine = StartCoroutine(Transition(active ? 1f : 0f, duration));
    }

    private IEnumerator Transition(float target, float duration)
    {
        float start = currentMultiplier;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentMultiplier = Mathf.SmoothStep(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        currentMultiplier = target;
        transitionCoroutine = null;
    }

    public static bool TryGetRenderSettings(Camera camera, out RenderSettings settings)
    {
        settings = default;
        if (camera == null || !CameraEffects.TryGetValue(camera, out ThermalVisionPostFX effect) ||
            effect == null || !effect.isActiveAndEnabled || effect.currentMultiplier <= 0.0001f) return false;

        settings.layerMask = effect.hotLayerMask;
        settings.image = new Vector4(effect.currentMultiplier, effect.backgroundBrightness, effect.contrast, effect.hotBrightness);
        settings.sensor = new Vector4(effect.heatGlow, effect.noiseIntensity, effect.vignetteIntensity, Time.unscaledTime);
        return true;
    }
}
