using TMPro;
using UnityEngine.UI;

internal sealed class AudioSettingsView
{
    public Slider GeneralVolume;
    public Slider VoipVolume;
    public Slider MusicVolume;
    public Slider WorldVolume;
    public Slider EnvironmentVolume;
    public Slider HitVolume;
    public Slider KillVolume;
    public Slider RadioVoipVolume;
    public Toggle EnableDeathVoip;
    public TMP_Dropdown InWorldVoipMode;
    public TMP_Dropdown RadioVoipMode;
}

internal sealed class AudioSettingsController : ISettingsSection
{
    private static readonly string[] Keys =
    {
        SettingsKeys.GENERAL_VOLUME,
        SettingsKeys.VOIP_VOLUME,
        SettingsKeys.MUSIC_VOLUME,
        SettingsKeys.WORLD_VOLUME,
        SettingsKeys.HIT_VOLUME,
        SettingsKeys.KILL_VOLUME,
        SettingsKeys.ENVIRONMENT_VOLUME,
        SettingsKeys.RADIO_VOIP_VOLUME,
        SettingsKeys.ENABLE_DEATH_VOIP,
        SettingsKeys.IN_WORLD_VOIP_MODE,
        SettingsKeys.RADIO_VOIP_MODE
    };

    private readonly Audio model;
    private readonly AudioSettingsView view;
    private readonly ISettingsStore store;
    private readonly Defaults defaults;

    public AudioSettingsController(Audio model, AudioSettingsView view, ISettingsStore store)
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

    public void SetGeneralVolume(float value)
    {
        model.general_volume = value;
        AudioMixerManager.SetMasterVolume(value);
        store.SetFloat(SettingsKeys.GENERAL_VOLUME, value);
    }

    public void SetVoipVolume(float value)
    {
        model.in_world_voip_volume = value;
        AudioMixerManager.SetInWorldVoipVolume(value);
        store.SetFloat(SettingsKeys.VOIP_VOLUME, value);
    }

    public void SetMusicVolume(float value)
    {
        model.music_volume = value;
        AudioMixerManager.SetMusicVolume(value);
        store.SetFloat(SettingsKeys.MUSIC_VOLUME, value);
    }

    public void SetWorldVolume(float value)
    {
        model.world_volume = value;
        AudioMixerManager.SetWorldVolume(value);
        store.SetFloat(SettingsKeys.WORLD_VOLUME, value);
    }

    public void SetEnvironmentVolume(float value)
    {
        model.enviroment_volume = value;
        AudioMixerManager.SetEnvironmentVolume(value);
        store.SetFloat(SettingsKeys.ENVIRONMENT_VOLUME, value);
    }

    public void SetHitVolume(float value)
    {
        model.hit_volume = value;
        AudioMixerManager.SetHitVolume(value);
        store.SetFloat(SettingsKeys.HIT_VOLUME, value);
    }

    public void SetKillVolume(float value)
    {
        model.kill_volume = value;
        AudioMixerManager.SetKillVolume(value);
        store.SetFloat(SettingsKeys.KILL_VOLUME, value);
    }

    public void SetRadioVoipVolume(float value)
    {
        model.voip_radio_volume = value;
        AudioMixerManager.SetRadioVoipVolume(value);
        store.SetFloat(SettingsKeys.RADIO_VOIP_VOLUME, value);
    }

    public void SetDeathVoipEnabled()
    {
        if (view.EnableDeathVoip == null)
        {
            return;
        }

        model.enable_deth_voip = view.EnableDeathVoip.isOn;
        store.SetInt(SettingsKeys.ENABLE_DEATH_VOIP, model.enable_deth_voip ? 1 : 0);
    }

    public void SetInWorldVoipMode(int index)
    {
        model.selected_in_world_voip_mode = (Audio.VoipModes)index;
        store.SetInt(SettingsKeys.IN_WORLD_VOIP_MODE, index);
    }

    public void SetRadioVoipMode(int index)
    {
        model.selected_radio_world_voip_mode = (Audio.VoipModes)index;
        store.SetInt(SettingsKeys.RADIO_VOIP_MODE, index);
    }

    private void ApplyValues(bool useStoredValues)
    {
        model.general_volume = GetFloat(SettingsKeys.GENERAL_VOLUME, defaults.GeneralVolume, useStoredValues);
        model.in_world_voip_volume = GetFloat(SettingsKeys.VOIP_VOLUME, defaults.VoipVolume, useStoredValues);
        model.music_volume = GetFloat(SettingsKeys.MUSIC_VOLUME, defaults.MusicVolume, useStoredValues);
        model.world_volume = GetFloat(SettingsKeys.WORLD_VOLUME, defaults.WorldVolume, useStoredValues);
        model.enviroment_volume = GetFloat(SettingsKeys.ENVIRONMENT_VOLUME, defaults.EnvironmentVolume, useStoredValues);
        model.hit_volume = GetFloat(SettingsKeys.HIT_VOLUME, defaults.HitVolume, useStoredValues);
        model.kill_volume = GetFloat(SettingsKeys.KILL_VOLUME, defaults.KillVolume, useStoredValues);
        model.voip_radio_volume = GetFloat(SettingsKeys.RADIO_VOIP_VOLUME, defaults.RadioVoipVolume, useStoredValues);
        model.enable_deth_voip = GetBool(SettingsKeys.ENABLE_DEATH_VOIP, defaults.EnableDeathVoip, useStoredValues);

        int inWorldMode = GetInt(SettingsKeys.IN_WORLD_VOIP_MODE, defaults.InWorldVoipMode, useStoredValues);
        int radioMode = GetInt(SettingsKeys.RADIO_VOIP_MODE, defaults.RadioVoipMode, useStoredValues);
        model.selected_in_world_voip_mode = (Audio.VoipModes)inWorldMode;
        model.selected_radio_world_voip_mode = (Audio.VoipModes)radioMode;

        view.GeneralVolume?.SetValueWithoutNotify(model.general_volume);
        view.VoipVolume?.SetValueWithoutNotify(model.in_world_voip_volume);
        view.MusicVolume?.SetValueWithoutNotify(model.music_volume);
        view.WorldVolume?.SetValueWithoutNotify(model.world_volume);
        view.EnvironmentVolume?.SetValueWithoutNotify(model.enviroment_volume);
        view.HitVolume?.SetValueWithoutNotify(model.hit_volume);
        view.KillVolume?.SetValueWithoutNotify(model.kill_volume);
        view.RadioVoipVolume?.SetValueWithoutNotify(model.voip_radio_volume);
        view.EnableDeathVoip?.SetIsOnWithoutNotify(model.enable_deth_voip);
        view.InWorldVoipMode?.SetValueWithoutNotify(inWorldMode);
        view.RadioVoipMode?.SetValueWithoutNotify(radioMode);

        AudioMixerManager.SetMasterVolume(model.general_volume);
        AudioMixerManager.SetInWorldVoipVolume(model.in_world_voip_volume);
        AudioMixerManager.SetMusicVolume(model.music_volume);
        AudioMixerManager.SetWorldVolume(model.world_volume);
        AudioMixerManager.SetEnvironmentVolume(model.enviroment_volume);
        AudioMixerManager.SetHitVolume(model.hit_volume);
        AudioMixerManager.SetKillVolume(model.kill_volume);
        AudioMixerManager.SetRadioVoipVolume(model.voip_radio_volume);
    }

    private float GetFloat(string key, float defaultValue, bool useStoredValues) => useStoredValues ? store.GetFloat(key, defaultValue) : defaultValue;

    private int GetInt(string key, int defaultValue, bool useStoredValues) => useStoredValues ? store.GetInt(key, defaultValue) : defaultValue;

    private bool GetBool(string key, bool defaultValue, bool useStoredValues) => GetInt(key, defaultValue ? 1 : 0, useStoredValues) == 1;

    private readonly struct Defaults
    {
        public readonly float GeneralVolume;
        public readonly float VoipVolume;
        public readonly float MusicVolume;
        public readonly float WorldVolume;
        public readonly float EnvironmentVolume;
        public readonly float HitVolume;
        public readonly float KillVolume;
        public readonly float RadioVoipVolume;
        public readonly bool EnableDeathVoip;
        public readonly int InWorldVoipMode;
        public readonly int RadioVoipMode;

        public Defaults(Audio model)
        {
            GeneralVolume = model.general_volume;
            VoipVolume = model.in_world_voip_volume;
            MusicVolume = model.music_volume;
            WorldVolume = model.world_volume;
            EnvironmentVolume = model.enviroment_volume;
            HitVolume = model.hit_volume;
            KillVolume = model.kill_volume;
            RadioVoipVolume = model.voip_radio_volume;
            EnableDeathVoip = model.enable_deth_voip;
            InWorldVoipMode = (int)model.selected_in_world_voip_mode;
            RadioVoipMode = (int)model.selected_radio_world_voip_mode;
        }
    }
}
