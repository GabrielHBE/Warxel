using UnityEngine;

internal interface ISettingsStore
{
    bool HasKey(string key);
    float GetFloat(string key, float defaultValue);
    int GetInt(string key, int defaultValue);
    string GetString(string key, string defaultValue = "");
    void SetFloat(string key, float value);
    void SetInt(string key, int value);
    void SetString(string key, string value);
    void DeleteKey(string key);
    void Save();
}

internal interface ISettingsSection
{
    void Load();
    void ResetToDefaults();
}

internal sealed class PlayerPrefsSettingsStore : ISettingsStore
{
    public bool HasKey(string key) => PlayerPrefs.HasKey(key);
    public float GetFloat(string key, float defaultValue) => PlayerPrefs.GetFloat(key, defaultValue);
    public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);
    public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
    public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
    public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
    public void Save() => PlayerPrefs.Save();
}
