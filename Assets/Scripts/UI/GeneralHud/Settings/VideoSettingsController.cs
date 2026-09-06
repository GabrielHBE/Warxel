using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class VideoSettingsView
{
    public TMP_Dropdown GraphicPreset;
    public Slider RenderDistance;
    public Toggle EnableShadows;
    public TMP_Dropdown ShadowsQuality;
    public TMP_Dropdown MeshesQuality;
    public TMP_Dropdown RainQuality;
    public Toggle LimitFps;
    public TMP_InputField MaxFps;
    public Slider Vsync;
    public Slider Brightness;
    public Slider RenderScale;
    public Toggle CustomResolution;
    public TMP_InputField ResolutionWidth;
    public TMP_InputField ResolutionHeight;
    public TMP_Dropdown ScreenMode;
    public Slider InfantryFov;
    public Slider JetFov;
    public Slider TankFov;
    public Slider HelicopterFov;
    public Slider CameraShakeIntensity;
    public Toggle Vignette;
    public Slider MotionBlur;
}

internal sealed class VideoSettingsController : ISettingsSection
{
    private static readonly string[] Keys =
    {
        SettingsKeys.GRAPHIC_PRESET,
        SettingsKeys.RENDER_DISTANCE,
        SettingsKeys.ENABLE_SHADOWS,
        SettingsKeys.SHADOWS_QUALITY,
        SettingsKeys.MESHES_QUALITY,
        SettingsKeys.RAIN_QUALITY,
        SettingsKeys.LIMIT_FPS,
        SettingsKeys.MAX_FPS,
        SettingsKeys.VSYNC,
        SettingsKeys.BRIGHTNESS,
        SettingsKeys.RENDER_SCALE,
        SettingsKeys.CUSTOM_RESOLUTION,
        SettingsKeys.RESOLUTION_WIDTH,
        SettingsKeys.RESOLUTION_HEIGHT,
        SettingsKeys.SCREEN_MODE,
        SettingsKeys.INFANTRY_FOV,
        SettingsKeys.JET_FOV,
        SettingsKeys.TANK_FOV,
        SettingsKeys.HELICOPTER_FOV,
        SettingsKeys.CAMERA_SHAKE_INTENSITY,
        SettingsKeys.VIGNETTE,
        SettingsKeys.MOTION_BLUR
    };

    private readonly Video model;
    private readonly VideoSettingsView view;
    private readonly ISettingsStore store;
    private readonly Defaults defaults;

    public VideoSettingsController(Video model, VideoSettingsView view, ISettingsStore store)
    {
        this.model = model;
        this.view = view;
        this.store = store;
        defaults = new Defaults(model);
    }

    public void Load() => ApplyValues(true);

    public void ResetToDefaults()
    {
        for (int i = 0; i < Keys.Length; i++)
        {
            store.DeleteKey(Keys[i]);
        }

        ApplyValues(false);
    }

    public void SetGraphicPreset(int index)
    {
        model.graphic_preset = (Video.GraphicSettings)index;
        store.SetInt(SettingsKeys.GRAPHIC_PRESET, index);
    }

    public void SetRenderDistance(float value) => SetFloat(value, newValue => model.render_distance = newValue, SettingsKeys.RENDER_DISTANCE);

    public void SetShadowsEnabled()
    {
        if (view.EnableShadows == null) return;
        

        model.enable_shadows = view.EnableShadows.isOn;
        store.SetInt(SettingsKeys.ENABLE_SHADOWS, model.enable_shadows ? 1 : 0);
    }

    public void SetShadowsQuality(int index)
    {
        model.shadows = (Video.GraphicSettings)index;
        store.SetInt(SettingsKeys.SHADOWS_QUALITY, index);
    }

    public void SetMeshesQuality(int index)
    {
        model.meshes = (Video.GraphicSettings)index;
        store.SetInt(SettingsKeys.MESHES_QUALITY, index);
    }

    public void SetRainQuality(int index)
    {
        model.rain_quality = (Video.GraphicSettings)index;
        store.SetInt(SettingsKeys.RAIN_QUALITY, index);
    }

    public void SetLimitFps()
    {
        if (view.LimitFps == null) return;
        

        model.limit_fps = view.LimitFps.isOn;
        store.SetInt(SettingsKeys.LIMIT_FPS, model.limit_fps ? 1 : 0);
    }

    public void SetMaxFps()
    {
        if (view.MaxFps == null || !int.TryParse(view.MaxFps.text, out int maxFps))
        {
            Debug.LogWarning("Invalid maximum FPS value.");
            return;
        }

        model.max_fps = maxFps;
        store.SetFloat(SettingsKeys.MAX_FPS, maxFps);
        Application.targetFrameRate = maxFps;
        QualitySettings.vSyncCount = 0;
    }

    public void SetVsync(float value) => SetFloat(value, newValue => model.Vsync = newValue, SettingsKeys.VSYNC);
    public void SetBrightness(float value) => SetFloat(value, newValue => model.brightness = newValue, SettingsKeys.BRIGHTNESS);
    public void SetRenderScale(float value) => SetFloat(value, newValue => model.render_scale = newValue, SettingsKeys.RENDER_SCALE);

    public void SetCustomResolution()
    {
        if (view.CustomResolution == null) return;
        
        model.custom_resolution = view.CustomResolution.isOn;
        store.SetInt(SettingsKeys.CUSTOM_RESOLUTION, model.custom_resolution ? 1 : 0);
    }

    public void SetResolutionWidth(string value) => SetResolution(value, 0, SettingsKeys.RESOLUTION_WIDTH);
    public void SetResolutionHeight(string value) => SetResolution(value, 1, SettingsKeys.RESOLUTION_HEIGHT);
    public void SetScreenMode(int index) => store.SetInt(SettingsKeys.SCREEN_MODE, index);
    public void SetInfantryFov(float value) => SetFloat(value, newValue => model.infantary_fov = newValue, SettingsKeys.INFANTRY_FOV);
    public void SetJetFov(float value) => SetFloat(value, newValue => model.jet_fov = newValue, SettingsKeys.JET_FOV);
    public void SetTankFov(float value) => SetFloat(value, newValue => model.tank_fov = newValue, SettingsKeys.TANK_FOV);
    public void SetHelicopterFov(float value) => SetFloat(value, newValue => model.helicopter_fov = newValue, SettingsKeys.HELICOPTER_FOV);
    public void SetCameraShakeIntensity(float value) => SetFloat(value, newValue => model.camera_shake_intensity = newValue, SettingsKeys.CAMERA_SHAKE_INTENSITY);

    public void SetVignette()
    {
        if (view.Vignette == null)  return;
        

        model.vignette = view.Vignette.isOn;
        store.SetInt(SettingsKeys.VIGNETTE, model.vignette ? 1 : 0);
    }

    public void SetMotionBlur(float value) => SetFloat(value, newValue => model.motion_blur = newValue, SettingsKeys.MOTION_BLUR);

    private void ApplyValues(bool useStoredValues)
    {
        int graphicPreset = GetInt(SettingsKeys.GRAPHIC_PRESET, defaults.GraphicPreset, useStoredValues);
        int shadowsQuality = GetInt(SettingsKeys.SHADOWS_QUALITY, defaults.ShadowsQuality, useStoredValues);
        int meshesQuality = GetInt(SettingsKeys.MESHES_QUALITY, defaults.MeshesQuality, useStoredValues);
        int rainQuality = GetInt(SettingsKeys.RAIN_QUALITY, defaults.RainQuality, useStoredValues);
        int screenMode = GetInt(SettingsKeys.SCREEN_MODE, defaults.ScreenMode, useStoredValues);

        model.graphic_preset = (Video.GraphicSettings)graphicPreset;
        model.render_distance = GetFloat(SettingsKeys.RENDER_DISTANCE, defaults.RenderDistance, useStoredValues);
        model.enable_shadows = GetBool(SettingsKeys.ENABLE_SHADOWS, defaults.EnableShadows, useStoredValues);
        model.shadows = (Video.GraphicSettings)shadowsQuality;
        model.meshes = (Video.GraphicSettings)meshesQuality;
        model.rain_quality = (Video.GraphicSettings)rainQuality;
        model.limit_fps = GetBool(SettingsKeys.LIMIT_FPS, defaults.LimitFps, useStoredValues);
        model.max_fps = GetFloat(SettingsKeys.MAX_FPS, defaults.MaxFps, useStoredValues);
        model.Vsync = GetFloat(SettingsKeys.VSYNC, defaults.Vsync, useStoredValues);
        model.brightness = GetFloat(SettingsKeys.BRIGHTNESS, defaults.Brightness, useStoredValues);
        model.render_scale = GetFloat(SettingsKeys.RENDER_SCALE, defaults.RenderScale, useStoredValues);
        model.custom_resolution = GetBool(SettingsKeys.CUSTOM_RESOLUTION, defaults.CustomResolution, useStoredValues);
        EnsureResolutionArray();
        model.resolution[0] = GetInt(SettingsKeys.RESOLUTION_WIDTH, defaults.ResolutionWidth, useStoredValues);
        model.resolution[1] = GetInt(SettingsKeys.RESOLUTION_HEIGHT, defaults.ResolutionHeight, useStoredValues);
        model.infantary_fov = GetFloat(SettingsKeys.INFANTRY_FOV, defaults.InfantryFov, useStoredValues);
        model.jet_fov = GetFloat(SettingsKeys.JET_FOV, defaults.JetFov, useStoredValues);
        model.tank_fov = GetFloat(SettingsKeys.TANK_FOV, defaults.TankFov, useStoredValues);
        model.helicopter_fov = GetFloat(SettingsKeys.HELICOPTER_FOV, defaults.HelicopterFov, useStoredValues);
        model.camera_shake_intensity = GetFloat(SettingsKeys.CAMERA_SHAKE_INTENSITY, defaults.CameraShakeIntensity, useStoredValues);
        model.vignette = GetBool(SettingsKeys.VIGNETTE, defaults.Vignette, useStoredValues);
        model.motion_blur = GetFloat(SettingsKeys.MOTION_BLUR, defaults.MotionBlur, useStoredValues);

        if (view.ScreenMode != null && view.ScreenMode.options.Count == 0 && model.screen_mode != null)
        {
            view.ScreenMode.AddOptions(model.screen_mode);
        }

        view.GraphicPreset?.SetValueWithoutNotify(graphicPreset);
        view.RenderDistance?.SetValueWithoutNotify(model.render_distance);
        view.EnableShadows?.SetIsOnWithoutNotify(model.enable_shadows);
        view.ShadowsQuality?.SetValueWithoutNotify(shadowsQuality);
        view.MeshesQuality?.SetValueWithoutNotify(meshesQuality);
        view.RainQuality?.SetValueWithoutNotify(rainQuality);
        view.LimitFps?.SetIsOnWithoutNotify(model.limit_fps);
        view.MaxFps?.SetTextWithoutNotify(model.max_fps.ToString("F0"));
        view.Vsync?.SetValueWithoutNotify(model.Vsync);
        view.Brightness?.SetValueWithoutNotify(model.brightness);
        view.RenderScale?.SetValueWithoutNotify(model.render_scale);
        view.CustomResolution?.SetIsOnWithoutNotify(model.custom_resolution);
        view.ResolutionWidth?.SetTextWithoutNotify(((int)model.resolution[0]).ToString());
        view.ResolutionHeight?.SetTextWithoutNotify(((int)model.resolution[1]).ToString());
        view.ScreenMode?.SetValueWithoutNotify(screenMode);
        view.InfantryFov?.SetValueWithoutNotify(model.infantary_fov);
        view.JetFov?.SetValueWithoutNotify(model.jet_fov);
        view.TankFov?.SetValueWithoutNotify(model.tank_fov);
        view.HelicopterFov?.SetValueWithoutNotify(model.helicopter_fov);
        view.CameraShakeIntensity?.SetValueWithoutNotify(model.camera_shake_intensity);
        view.Vignette?.SetIsOnWithoutNotify(model.vignette);
        view.MotionBlur?.SetValueWithoutNotify(model.motion_blur);
    }

    private void SetFloat(float value, System.Action<float> updateModel, string key)
    {
        updateModel(value);
        store.SetFloat(key, value);
    }

    private void SetResolution(string value, int index, string key)
    {
        if (!float.TryParse(value, out float resolution)) return;
        

        EnsureResolutionArray();
        model.resolution[index] = resolution;
        store.SetInt(key, (int)resolution);
    }

    private void EnsureResolutionArray()
    {
        if (model.resolution == null || model.resolution.Length < 2)  model.resolution = new float[2];
        
    }

    private float GetFloat(string key, float defaultValue, bool useStoredValues) =>
        useStoredValues ? store.GetFloat(key, defaultValue) : defaultValue;

    private int GetInt(string key, int defaultValue, bool useStoredValues) =>
        useStoredValues ? store.GetInt(key, defaultValue) : defaultValue;

    private bool GetBool(string key, bool defaultValue, bool useStoredValues) =>
        GetInt(key, defaultValue ? 1 : 0, useStoredValues) == 1;

    private readonly struct Defaults
    {
        public readonly int GraphicPreset;
        public readonly float RenderDistance;
        public readonly bool EnableShadows;
        public readonly int ShadowsQuality;
        public readonly int MeshesQuality;
        public readonly int RainQuality;
        public readonly bool LimitFps;
        public readonly float MaxFps;
        public readonly float Vsync;
        public readonly float Brightness;
        public readonly float RenderScale;
        public readonly bool CustomResolution;
        public readonly int ResolutionWidth;
        public readonly int ResolutionHeight;
        public readonly int ScreenMode;
        public readonly float InfantryFov;
        public readonly float JetFov;
        public readonly float TankFov;
        public readonly float HelicopterFov;
        public readonly float CameraShakeIntensity;
        public readonly bool Vignette;
        public readonly float MotionBlur;

        public Defaults(Video model)
        {
            GraphicPreset = (int)model.graphic_preset;
            RenderDistance = model.render_distance;
            EnableShadows = model.enable_shadows;
            ShadowsQuality = (int)model.shadows;
            MeshesQuality = (int)model.meshes;
            RainQuality = (int)model.rain_quality;
            LimitFps = model.limit_fps;
            MaxFps = model.max_fps;
            Vsync = model.Vsync;
            Brightness = model.brightness;
            RenderScale = model.render_scale;
            CustomResolution = model.custom_resolution;
            ResolutionWidth = model.resolution != null && model.resolution.Length >= 2 && model.resolution[0] > 0
                ? (int)model.resolution[0]
                : 1920;
            ResolutionHeight = model.resolution != null && model.resolution.Length >= 2 && model.resolution[1] > 0
                ? (int)model.resolution[1]
                : 1080;
            ScreenMode = 0;
            InfantryFov = model.infantary_fov;
            JetFov = model.jet_fov;
            TankFov = model.tank_fov;
            HelicopterFov = model.helicopter_fov;
            CameraShakeIntensity = model.camera_shake_intensity;
            Vignette = model.vignette;
            MotionBlur = model.motion_blur;
        }
    }
}
