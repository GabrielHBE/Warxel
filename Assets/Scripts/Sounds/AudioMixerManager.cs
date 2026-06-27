using UnityEngine;

public static class AudioMixerManager
{
    public const float VOLUME_COMPENSATOR = 100;

    public static void SetMasterVolume(float value)
    {
        SetMixerVolume("MasterVolume", value);
    }

    public static void SetMusicVolume(float value)
    {
        SetMixerVolume("MusicVolume", value);
    }

    public static void SetEnvironmentVolume(float value)
    {
        SetMixerVolume("EnviromentVolume", value);
    }

    public static void SetInWorldVoipVolume(float value)
    {
        SetMixerVolume("InWorldVoipVolume", value);
    }

    public static void SetRadioVoipVolume(float value)
    {
        SetMixerVolume("RadioVoipVolume", value);
    }

    public static void SetWorldVolume(float value)
    {
        SetMixerVolume("WorldVolume", value);
    }

    public static void SetHitVolume(float value)
    {
        SetMixerVolume("HitVolume", value);
    }
    public static void SetKillVolume(float value)
    {
        SetMixerVolume("KillVolume", value);
    }

    #region Auxiliar
    private static void SetMixerVolume(string parameterName, float value)
    {
        float safeValue = Mathf.Max(value / VOLUME_COMPENSATOR, 0.0001f);
        SoundManager.staticMainMixer.SetFloat(parameterName, Mathf.Log10(safeValue) * 20);
    }
    #endregion
}
