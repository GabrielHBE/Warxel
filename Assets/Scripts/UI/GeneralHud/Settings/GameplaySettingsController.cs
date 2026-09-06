using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class GameplaySettingsView
{
    public Toggle ShowHitMarker;
    public Slider HitMarkerOpacity;
    public Slider HitMarkerSize;
    public Toggle ShowFps;
    public Toggle ShowNetworkStatus;
    public Toggle ShowLevelProgression;
    public Toggle ShowKillFeed;
    public Slider SightReticleSize;
    public Slider EnemyIndicatorOpacity;
    public Slider AllyIndicatorOpacity;
    public Slider SquadIndicatorOpacity;
    public Slider EnemyIndicatorAimOpacity;
    public Slider AllyIndicatorAimOpacity;
    public Slider SquadIndicatorAimOpacity;
    public Slider NeutralIndicatorOpacity;
    public Slider NeutralIndicatorAimOpacity;
    public Toggle ShowChat;
    public Slider ChatOpacity;
    public Slider ChatSize;
    public TMP_InputField BodyShotColor;
    public TMP_InputField HeadShotColor;
    public TMP_InputField VehicleMarkerColor;
    public TMP_InputField SightReticleColor;
    public TMP_InputField EnemyColor;
    public TMP_InputField AllyColor;
    public TMP_InputField SquadColor;
    public TMP_InputField NeutralColor;
}

internal sealed class GameplaySettingsController : ISettingsSection
{
    private static readonly string[] Keys =
    {
        SettingsKeys.SHOW_HIT_MARKER,
        SettingsKeys.HIT_MARKER_OPACITY,
        SettingsKeys.HIT_MARKER_SIZE,
        SettingsKeys.SHOW_FPS,
        SettingsKeys.SHOW_NETWORK_STATUS,
        SettingsKeys.SHOW_LEVEL_PROGRESSION,
        SettingsKeys.SHOW_KILL_FEED,
        SettingsKeys.SIGHT_RETICLE_SIZE,
        SettingsKeys.ENEMY_INDICATOR_OPACITY,
        SettingsKeys.ALLY_INDICATOR_OPACITY,
        SettingsKeys.SQUAD_INDICATOR_OPACITY,
        SettingsKeys.ENEMY_INDICATOR_AIM_OPACITY,
        SettingsKeys.ALLY_INDICATOR_AIM_OPACITY,
        SettingsKeys.SQUAD_INDICATOR_AIM_OPACITY,
        SettingsKeys.NEUTRAL_INDICATOR_OPACITY,
        SettingsKeys.NEUTRAL_INDICATOR_AIM_OPACITY,
        SettingsKeys.SHOW_CHAT,
        SettingsKeys.CHAT_OPACITY,
        SettingsKeys.CHAT_SIZE,
        SettingsKeys.BODY_SHOT_COLOR,
        SettingsKeys.HEAD_SHOT_COLOR,
        SettingsKeys.VEHICLE_MARKER_COLOR,
        SettingsKeys.SIGHT_RETICLE_COLOR,
        SettingsKeys.ENEMY_COLOR,
        SettingsKeys.ALLY_COLOR,
        SettingsKeys.SQUAD_COLOR,
        SettingsKeys.NEUTRAL_COLOR
    };

    private readonly Gameplay model;
    private readonly GameplaySettingsView view;
    private readonly ISettingsStore store;
    private readonly Defaults defaults;

    public GameplaySettingsController(Gameplay model, GameplaySettingsView view, ISettingsStore store)
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

    public void SetShowHitMarker() => SetToggle(view.ShowHitMarker, value => model.show_hit_marker = value, SettingsKeys.SHOW_HIT_MARKER);
    public void SetShowFps() => SetToggle(view.ShowFps, value => model.show_fps = value, SettingsKeys.SHOW_FPS);
    public void SetShowNetworkStatus() => SetToggle(view.ShowNetworkStatus, value => model.show_network_status = value, SettingsKeys.SHOW_NETWORK_STATUS);
    public void SetShowLevelProgression() => SetToggle(view.ShowLevelProgression, value => model.show_level_progression = value, SettingsKeys.SHOW_LEVEL_PROGRESSION);
    public void SetShowKillFeed() => SetToggle(view.ShowKillFeed, value => model.show_kill_feed = value, SettingsKeys.SHOW_KILL_FEED);
    public void SetShowChat() => SetToggle(view.ShowChat, value => model.show_chat = value, SettingsKeys.SHOW_CHAT);

    public void SetHitMarkerOpacity(TextMeshProUGUI label) => SetLabeledSlider(
        view.HitMarkerOpacity,
        label,
        "Hit Marker Opacity",
        value => model.hit_marker_opacity = value,
        SettingsKeys.HIT_MARKER_OPACITY);

    public void SetHitMarkerSize(TextMeshProUGUI label) => SetLabeledSlider(
        view.HitMarkerSize,
        label,
        "Hit Marker Size",
        value => model.hit_marker_size = value,
        SettingsKeys.HIT_MARKER_SIZE);

    public void SetSightReticleSize(TextMeshProUGUI label) => SetLabeledSlider(
        view.SightReticleSize,
        label,
        "Sight Reticle Size",
        value => model.sight_reticle_size = value,
        SettingsKeys.SIGHT_RETICLE_SIZE);

    public void SetEnemyIndicatorOpacity(float value) => SetFloat(value, newValue => model.enemy_indicator_opacity = newValue, SettingsKeys.ENEMY_INDICATOR_OPACITY);
    public void SetAllyIndicatorOpacity(float value) => SetFloat(value, newValue => model.ally_indicator_opacity = newValue, SettingsKeys.ALLY_INDICATOR_OPACITY);
    public void SetSquadIndicatorOpacity(float value) => SetFloat(value, newValue => model.squad_indicator_opacity = newValue, SettingsKeys.SQUAD_INDICATOR_OPACITY);
    public void SetEnemyIndicatorAimOpacity(float value) => SetFloat(value, newValue => model.enemy_indicator_aim_opacity = newValue, SettingsKeys.ENEMY_INDICATOR_AIM_OPACITY);
    public void SetAllyIndicatorAimOpacity(float value) => SetFloat(value, newValue => model.ally_indicator_aim_opacity = newValue, SettingsKeys.ALLY_INDICATOR_AIM_OPACITY);
    public void SetSquadIndicatorAimOpacity(float value) => SetFloat(value, newValue => model.squad_indicator_aim_opacity = newValue, SettingsKeys.SQUAD_INDICATOR_AIM_OPACITY);
    public void SetNeutralIndicatorOpacity(float value) => SetFloat(value, newValue => model.neutral_indicator_opacity = newValue, SettingsKeys.NEUTRAL_INDICATOR_OPACITY);
    public void SetNeutralIndicatorAimOpacity(float value) => SetFloat(value, newValue => model.neutral_indicator_aim_opacity = newValue, SettingsKeys.NEUTRAL_INDICATOR_AIM_OPACITY);
    public void SetChatOpacity(float value) => SetFloat(value, newValue => model.chat_opacity = newValue, SettingsKeys.CHAT_OPACITY);
    public void SetChatSize(float value) => SetFloat(value, newValue => model.chat_size = newValue, SettingsKeys.CHAT_SIZE);
    public void SetBodyShotColor(Color color) => SetColor(color, value => model.body_shot_marker_colour = value, SettingsKeys.BODY_SHOT_COLOR);
    public void SetHeadShotColor(Color color) => SetColor(color, value => model.head_shot_marker_colour = value, SettingsKeys.HEAD_SHOT_COLOR);
    public void SetVehicleMarkerColor(Color color) => SetColor(color, value => model.vehicle_marker_colour = value, SettingsKeys.VEHICLE_MARKER_COLOR);
    public void SetSightReticleColor(Color color) => SetColor(color, value => model.sight_reticle_collor = value, SettingsKeys.SIGHT_RETICLE_COLOR);
    public void SetEnemyColor(Color color) => SetColor(color, value => model.enemy_color = value, SettingsKeys.ENEMY_COLOR);
    public void SetAllyColor(Color color) => SetColor(color, value => model.ally_color = value, SettingsKeys.ALLY_COLOR);
    public void SetSquadColor(Color color) => SetColor(color, value => model.squad_color = value, SettingsKeys.SQUAD_COLOR);
    public void SetNeutralColor(Color color) => SetColor(color, value => model.neutral_color = value, SettingsKeys.NEUTRAL_COLOR);
    public void SetBodyShotColor(string value) => SetColor(value, view.BodyShotColor, color => model.body_shot_marker_colour = color, SettingsKeys.BODY_SHOT_COLOR);
    public void SetHeadShotColor(string value) => SetColor(value, view.HeadShotColor, color => model.head_shot_marker_colour = color, SettingsKeys.HEAD_SHOT_COLOR);
    public void SetVehicleMarkerColor(string value) => SetColor(value, view.VehicleMarkerColor, color => model.vehicle_marker_colour = color, SettingsKeys.VEHICLE_MARKER_COLOR);
    public void SetSightReticleColor(string value) => SetColor(value, view.SightReticleColor, color => model.sight_reticle_collor = color, SettingsKeys.SIGHT_RETICLE_COLOR);
    public void SetEnemyColor(string value) => SetColor(value, view.EnemyColor, color => model.enemy_color = color, SettingsKeys.ENEMY_COLOR);
    public void SetAllyColor(string value) => SetColor(value, view.AllyColor, color => model.ally_color = color, SettingsKeys.ALLY_COLOR);
    public void SetSquadColor(string value) => SetColor(value, view.SquadColor, color => model.squad_color = color, SettingsKeys.SQUAD_COLOR);
    public void SetNeutralColor(string value) => SetColor(value, view.NeutralColor, color => model.neutral_color = color, SettingsKeys.NEUTRAL_COLOR);

    private void ApplyValues(bool useStoredValues)
    {
        model.show_hit_marker = GetBool(SettingsKeys.SHOW_HIT_MARKER, defaults.ShowHitMarker, useStoredValues);
        model.hit_marker_opacity = GetFloat(SettingsKeys.HIT_MARKER_OPACITY, defaults.HitMarkerOpacity, useStoredValues);
        model.hit_marker_size = GetFloat(SettingsKeys.HIT_MARKER_SIZE, defaults.HitMarkerSize, useStoredValues);
        model.show_fps = GetBool(SettingsKeys.SHOW_FPS, defaults.ShowFps, useStoredValues);
        model.show_network_status = GetBool(SettingsKeys.SHOW_NETWORK_STATUS, defaults.ShowNetworkStatus, useStoredValues);
        model.show_level_progression = GetBool(SettingsKeys.SHOW_LEVEL_PROGRESSION, defaults.ShowLevelProgression, useStoredValues);
        model.show_kill_feed = GetBool(SettingsKeys.SHOW_KILL_FEED, defaults.ShowKillFeed, useStoredValues);
        model.sight_reticle_size = GetFloat(SettingsKeys.SIGHT_RETICLE_SIZE, defaults.SightReticleSize, useStoredValues);
        model.enemy_indicator_opacity = GetFloat(SettingsKeys.ENEMY_INDICATOR_OPACITY, defaults.EnemyIndicatorOpacity, useStoredValues);
        model.ally_indicator_opacity = GetFloat(SettingsKeys.ALLY_INDICATOR_OPACITY, defaults.AllyIndicatorOpacity, useStoredValues);
        model.squad_indicator_opacity = GetFloat(SettingsKeys.SQUAD_INDICATOR_OPACITY, defaults.SquadIndicatorOpacity, useStoredValues);
        model.enemy_indicator_aim_opacity = GetFloat(SettingsKeys.ENEMY_INDICATOR_AIM_OPACITY, defaults.EnemyIndicatorAimOpacity, useStoredValues);
        model.ally_indicator_aim_opacity = GetFloat(SettingsKeys.ALLY_INDICATOR_AIM_OPACITY, defaults.AllyIndicatorAimOpacity, useStoredValues);
        model.squad_indicator_aim_opacity = GetFloat(SettingsKeys.SQUAD_INDICATOR_AIM_OPACITY, defaults.SquadIndicatorAimOpacity, useStoredValues);
        model.neutral_indicator_opacity = GetFloat(SettingsKeys.NEUTRAL_INDICATOR_OPACITY, defaults.NeutralIndicatorOpacity, useStoredValues);
        model.neutral_indicator_aim_opacity = GetFloat(SettingsKeys.NEUTRAL_INDICATOR_AIM_OPACITY, defaults.NeutralIndicatorAimOpacity, useStoredValues);
        model.show_chat = GetBool(SettingsKeys.SHOW_CHAT, defaults.ShowChat, useStoredValues);
        model.chat_opacity = GetFloat(SettingsKeys.CHAT_OPACITY, defaults.ChatOpacity, useStoredValues);
        model.chat_size = GetFloat(SettingsKeys.CHAT_SIZE, defaults.ChatSize, useStoredValues);

        model.body_shot_marker_colour = GetColor(SettingsKeys.BODY_SHOT_COLOR, defaults.BodyShotColor, useStoredValues);
        model.head_shot_marker_colour = GetColor(SettingsKeys.HEAD_SHOT_COLOR, defaults.HeadShotColor, useStoredValues);
        model.vehicle_marker_colour = GetColor(SettingsKeys.VEHICLE_MARKER_COLOR, defaults.VehicleMarkerColor, useStoredValues);
        model.sight_reticle_collor = GetColor(SettingsKeys.SIGHT_RETICLE_COLOR, defaults.SightReticleColor, useStoredValues);
        model.enemy_color = GetColor(SettingsKeys.ENEMY_COLOR, defaults.EnemyColor, useStoredValues);
        model.ally_color = GetColor(SettingsKeys.ALLY_COLOR, defaults.AllyColor, useStoredValues);
        model.squad_color = GetColor(SettingsKeys.SQUAD_COLOR, defaults.SquadColor, useStoredValues);
        model.neutral_color = GetColor(SettingsKeys.NEUTRAL_COLOR, defaults.NeutralColor, useStoredValues);

        view.ShowHitMarker?.SetIsOnWithoutNotify(model.show_hit_marker);
        view.HitMarkerOpacity?.SetValueWithoutNotify(model.hit_marker_opacity);
        view.HitMarkerSize?.SetValueWithoutNotify(model.hit_marker_size);
        view.ShowFps?.SetIsOnWithoutNotify(model.show_fps);
        view.ShowNetworkStatus?.SetIsOnWithoutNotify(model.show_network_status);
        view.ShowLevelProgression?.SetIsOnWithoutNotify(model.show_level_progression);
        view.ShowKillFeed?.SetIsOnWithoutNotify(model.show_kill_feed);
        view.SightReticleSize?.SetValueWithoutNotify(model.sight_reticle_size);
        view.EnemyIndicatorOpacity?.SetValueWithoutNotify(model.enemy_indicator_opacity);
        view.AllyIndicatorOpacity?.SetValueWithoutNotify(model.ally_indicator_opacity);
        view.SquadIndicatorOpacity?.SetValueWithoutNotify(model.squad_indicator_opacity);
        view.EnemyIndicatorAimOpacity?.SetValueWithoutNotify(model.enemy_indicator_aim_opacity);
        view.AllyIndicatorAimOpacity?.SetValueWithoutNotify(model.ally_indicator_aim_opacity);
        view.SquadIndicatorAimOpacity?.SetValueWithoutNotify(model.squad_indicator_aim_opacity);
        view.NeutralIndicatorOpacity?.SetValueWithoutNotify(model.neutral_indicator_opacity);
        view.NeutralIndicatorAimOpacity?.SetValueWithoutNotify(model.neutral_indicator_aim_opacity);
        view.ShowChat?.SetIsOnWithoutNotify(model.show_chat);
        view.ChatOpacity?.SetValueWithoutNotify(model.chat_opacity);
        view.ChatSize?.SetValueWithoutNotify(model.chat_size);
        SetColorField(view.BodyShotColor, model.body_shot_marker_colour);
        SetColorField(view.HeadShotColor, model.head_shot_marker_colour);
        SetColorField(view.VehicleMarkerColor, model.vehicle_marker_colour);
        SetColorField(view.SightReticleColor, model.sight_reticle_collor);
        SetColorField(view.EnemyColor, model.enemy_color);
        SetColorField(view.AllyColor, model.ally_color);
        SetColorField(view.SquadColor, model.squad_color);
        SetColorField(view.NeutralColor, model.neutral_color);
    }

    private void SetToggle(Toggle toggle, System.Action<bool> updateModel, string key)
    {
        if (toggle == null) return;
        

        updateModel(toggle.isOn);
        store.SetInt(key, toggle.isOn ? 1 : 0);
    }

    private void SetLabeledSlider(
        Slider slider,
        TextMeshProUGUI label,
        string labelText,
        System.Action<float> updateModel,
        string key)
    {
        if (slider == null) return;
        
        if (label != null) label.text = $"[{slider.value:F1}] {labelText}";
        
        SetFloat(slider.value, updateModel, key);
    }

    private void SetFloat(float value, System.Action<float> updateModel, string key)
    {
        updateModel(value);
        store.SetFloat(key, value);
    }

    private void SetColor(Color color, System.Action<Color> updateModel, string key)
    {
        updateModel(color);
        store.SetString(key, ColorUtility.ToHtmlStringRGBA(color));
    }

    private void SetColor(string html, TMP_InputField input, System.Action<Color> updateModel, string key)
    {
        string normalized = string.IsNullOrWhiteSpace(html) ? string.Empty : html.Trim();
        if (!normalized.StartsWith("#")) normalized = "#" + normalized;

        if (!ColorUtility.TryParseHtmlString(normalized, out Color color))
        {
            if (input != null) input.SetTextWithoutNotify("INVALID");
            return;
        }

        updateModel(color);
        store.SetString(key, ColorUtility.ToHtmlStringRGBA(color));
        SetColorField(input, color);
    }

    private static void SetColorField(TMP_InputField input, Color color)
    {
        input?.SetTextWithoutNotify("#" + ColorUtility.ToHtmlStringRGBA(color));
    }

    private float GetFloat(string key, float defaultValue, bool useStoredValues) => useStoredValues ? store.GetFloat(key, defaultValue) : defaultValue;

    private bool GetBool(string key, bool defaultValue, bool useStoredValues) => useStoredValues ? store.GetInt(key, defaultValue ? 1 : 0) == 1 : defaultValue;

    private Color GetColor(string key, Color defaultValue, bool useStoredValues)
    {
        if (!useStoredValues)
        {
            return defaultValue;
        }

        string htmlColor = store.GetString(key);
        return !string.IsNullOrEmpty(htmlColor) &&
               ColorUtility.TryParseHtmlString("#" + htmlColor, out Color color)
            ? color
            : defaultValue;
    }

    private readonly struct Defaults
    {
        public readonly bool ShowHitMarker;
        public readonly float HitMarkerOpacity;
        public readonly float HitMarkerSize;
        public readonly bool ShowFps;
        public readonly bool ShowNetworkStatus;
        public readonly bool ShowLevelProgression;
        public readonly bool ShowKillFeed;
        public readonly float SightReticleSize;
        public readonly float EnemyIndicatorOpacity;
        public readonly float AllyIndicatorOpacity;
        public readonly float SquadIndicatorOpacity;
        public readonly float EnemyIndicatorAimOpacity;
        public readonly float AllyIndicatorAimOpacity;
        public readonly float SquadIndicatorAimOpacity;
        public readonly float NeutralIndicatorOpacity;
        public readonly float NeutralIndicatorAimOpacity;
        public readonly bool ShowChat;
        public readonly float ChatOpacity;
        public readonly float ChatSize;
        public readonly Color BodyShotColor;
        public readonly Color HeadShotColor;
        public readonly Color VehicleMarkerColor;
        public readonly Color SightReticleColor;
        public readonly Color EnemyColor;
        public readonly Color AllyColor;
        public readonly Color SquadColor;
        public readonly Color NeutralColor;

        public Defaults(Gameplay model)
        {
            ShowHitMarker = model.show_hit_marker;
            HitMarkerOpacity = model.hit_marker_opacity;
            HitMarkerSize = model.hit_marker_size;
            ShowFps = model.show_fps;
            ShowNetworkStatus = model.show_network_status;
            ShowLevelProgression = model.show_level_progression;
            ShowKillFeed = model.show_kill_feed;
            SightReticleSize = model.sight_reticle_size;
            EnemyIndicatorOpacity = model.enemy_indicator_opacity;
            AllyIndicatorOpacity = model.ally_indicator_opacity;
            SquadIndicatorOpacity = model.squad_indicator_opacity;
            EnemyIndicatorAimOpacity = model.enemy_indicator_aim_opacity;
            AllyIndicatorAimOpacity = model.ally_indicator_aim_opacity;
            SquadIndicatorAimOpacity = model.squad_indicator_aim_opacity;
            NeutralIndicatorOpacity = model.neutral_indicator_opacity;
            NeutralIndicatorAimOpacity = model.neutral_indicator_aim_opacity;
            ShowChat = model.show_chat;
            ChatOpacity = model.chat_opacity;
            ChatSize = model.chat_size;
            BodyShotColor = model.body_shot_marker_colour;
            HeadShotColor = model.head_shot_marker_colour;
            VehicleMarkerColor = model.vehicle_marker_colour;
            SightReticleColor = model.sight_reticle_collor;
            EnemyColor = model.enemy_color;
            AllyColor = model.ally_color;
            SquadColor = model.squad_color;
            NeutralColor = model.neutral_color;
        }
    }
}
