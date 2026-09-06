using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Unity-facing facade for the settings menu.
/// Domain-specific behavior lives in the section controllers.
/// </summary>
public partial class SettingsHUD : PersistentLocalSingleton<SettingsHUD>
{
    public bool is_menu_settings_active;

    [Header("Slider Value Input")]
    [SerializeField] private TMP_InputField sliderValueInputTemplate;

    private ISettingsStore _store;
    private ISettingsSection[] _settingsSections = System.Array.Empty<ISettingsSection>();
    private SettingsMenuController _menuController;
    private AudioSettingsController _audioController;
    private ControlsSettingsController _controlsController;
    private GameplaySettingsController _gameplayController;
    private VideoSettingsController _videoController;
    private KeybindSettingsController _keybindController;
    private readonly Dictionary<Slider, TMP_InputField> _sliderValueInputs = new Dictionary<Slider, TMP_InputField>();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        _store = new PlayerPrefsSettingsStore();
        _menuController = CreateMenuController();
        _menuController.SetOpen(false);
        is_menu_settings_active = false;

        if (error_image != null) error_image.SetActive(false);

        SelectControlsTab();
        StartCoroutine(WaitForSettingsAndInitialize());
    }

    private IEnumerator WaitForSettingsAndInitialize()
    {
        yield return new WaitUntil(() => Settings.Instance != null);

        Settings settings = Settings.Instance;
        if (settings._audio == null ||
            settings._controls == null ||
            settings._gameplay == null ||
            settings._video == null ||
            settings._keybinds == null)
        {
            Debug.LogError("Settings is missing one or more required section components.", settings);
            yield break;
        }

        CreateSettingsControllers(settings);

        for (int i = 0; i < _settingsSections.Length; i++)
        {
            _settingsSections[i].Load();
        }

        _keybindController.Load();
        InitializeSliderValueInputs();
        RefreshSliderValueControls();
        RefreshColorPreviews();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (colorPickerPopup != null && colorPickerPopup.IsOpen)
            {
                colorPickerPopup.CloseWithoutApply();
                return;
            }

            _keybindController?.Cancel();
            ToggleSettingsMenu();
        }

        if (!is_menu_settings_active) return;


        _keybindController?.Tick();
        if (colorPickerPopup == null || !colorPickerPopup.IsOpen)
        {
            _menuController.HandleScroll();
        }
    }

    public void ToggleSettingsMenu()
    {
        if (is_menu_settings_active)
        {
            CloseSettingsMenu();
        }
        else
        {
            OpenSettingsMenu();
        }
    }

    public void OpenSettingsMenu()
    {
        is_menu_settings_active = true;
        _menuController.SetOpen(true);
        RefreshSliderValueControls();
    }

    public void CloseSettingsMenu()
    {
        colorPickerPopup?.CloseWithoutApply();
        _keybindController?.Cancel();
        is_menu_settings_active = false;
        _menuController.SetOpen(false);
        SaveAllSettings();
    }

    public void CloseErrorMessage()
    {
        _keybindController?.Cancel();
        if (error_image != null) error_image.SetActive(false);

    }

    public void SelectAudioTab() => _menuController.SelectTab("Audio", audio_tab, false);

    public void SelectControlsTab() => _menuController.SelectTab("Controls", controls_tab, false);

    public void SelectGameplayTab() => _menuController.SelectTab("Gameplay", gameplay_tab, false);

    public void SelectKeyBindsTab() =>  _menuController.SelectTab("Key Binds", keybinds_tab, true);

    public void SelectVideoTab() => _menuController.SelectTab("Video", video_tab, false);

    public void SelectReturnToMenuTab()
    {
        if (InstanceFinder.IsServerStarted)  InstanceFinder.ServerManager.StopConnection(true);

        if (InstanceFinder.IsClientStarted) InstanceFinder.ClientManager.StopConnection();

        SceneManager.LoadScene("Menu");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SelectQuitGameTab() => Application.Quit();

    public void SaveAllSettings()
    {
        _keybindController?.SaveAll();
        _store?.Save();
        Debug.Log("All settings saved successfully!");
    }

    public void ResetAllSettingsToDefault()
    {
        if (_keybindController == null)
        {
            Debug.LogWarning("Settings cannot be reset before initialization finishes.");
            return;
        }

        _keybindController.ResetToDefaults();
        for (int i = 0; i < _settingsSections.Length; i++)
        {
            _settingsSections[i].ResetToDefaults();
        }

        RefreshColorPreviews();
        RefreshSliderValueControls();
        _store.Save();
        Debug.Log("All settings reset to default!");
    }

    public void ResetAllKeybindsToDefault()
    {
        if (_keybindController == null)
        {
            Debug.LogWarning("Keybinds cannot be reset before initialization finishes.");
            return;
        }

        _keybindController.ResetToDefaults();
    }

    public void SaveAllKeybinds()
    {
        _keybindController?.SaveAll();
        _store?.Save();
    }

    private void InitializeSliderValueInputs()
    {
        if (_sliderValueInputs.Count > 0 || sliderValueInputTemplate == null)
        {
            return;
        }

        Slider[] sliders = GetSettingsSliders();
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider == null) continue;

            TMP_InputField input = Instantiate(sliderValueInputTemplate, slider.transform);
            input.name = "Value Input";
            input.gameObject.SetActive(true);
            input.contentType = TMP_InputField.ContentType.Standard;
            input.inputType = TMP_InputField.InputType.Standard;
            input.keyboardType = TouchScreenKeyboardType.Default;
            input.characterValidation = TMP_InputField.CharacterValidation.None;
            input.characterLimit = 16;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onEndEdit = new TMP_InputField.SubmitEvent();
            input.onSubmit = new TMP_InputField.SubmitEvent();
            input.onValueChanged = new TMP_InputField.OnChangeEvent();

            RectTransform inputRect = input.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.72f, 0.53f);
            inputRect.anchorMax = new Vector2(0.83f, 0.97f);
            inputRect.anchoredPosition = Vector2.zero;
            inputRect.sizeDelta = new Vector2(-6f, -4f);

            if (input.textComponent != null)
            {
                input.textComponent.alignment = TextAlignmentOptions.Center;
                input.textComponent.fontSize = 16f;
            }

            if (input.placeholder != null)
            {
                input.placeholder.gameObject.SetActive(false);
            }

            Transform range = slider.transform.Find("Range");
            if (range is RectTransform rangeRect)
            {
                rangeRect.anchorMin = new Vector2(0.84f, 0.5f);
                rangeRect.anchorMax = new Vector2(1f, 1f);
                rangeRect.anchoredPosition = new Vector2(-7f, 0f);
                rangeRect.sizeDelta = new Vector2(-14f, -2f);
            }

            input.SetTextWithoutNotify(FormatSliderValue(slider, slider.value));
            slider.onValueChanged.AddListener(value =>
                input.SetTextWithoutNotify(FormatSliderValue(slider, value)));
            input.onEndEdit.AddListener(text => SetSliderFromInput(slider, input, text));
            _sliderValueInputs.Add(slider, input);
        }
    }

    private void RefreshSliderValueControls()
    {
        foreach (KeyValuePair<Slider, TMP_InputField> binding in _sliderValueInputs)
        {
            if (binding.Key != null && binding.Value != null)
            {
                binding.Value.SetTextWithoutNotify(FormatSliderValue(binding.Key, binding.Key.value));
            }
        }

        RefreshSliderLabel(infantrySensibilitySlider, OnInfantrySensibilityChanged);
        RefreshSliderLabel(infantryAimSensibilitySlider, OnInfantryAimSensibilityChanged);
        RefreshSliderLabel(tankSensibilitySlider, OnTankSensibilityChanged);
        RefreshSliderLabel(tankAimSensibilitySlider, OnTankAimSensibilityChanged);
        RefreshSliderLabel(jetSensibilitySlider, OnJetSensibilityChanged);
        RefreshSliderLabel(jetAimSensibilitySlider, OnJetAimSensibilityChanged);
        RefreshSliderLabel(helicopterSensibilitySlider, OnHelicopterSensibilityChanged);
        RefreshSliderLabel(helicopterAimSensibilitySlider, OnHelicopterAimSensibilityChanged);
        RefreshSliderLabel(hitMarkerOpacitySlider, OnHitMarkerOpacityChanged);
        RefreshSliderLabel(hitMarkerSizeSlider, OnHitMarkerSizeChanged);
        RefreshSliderLabel(sightReticleSizeSlider, OnSightReticleSizeChanged);
    }

    private static void RefreshSliderLabel(Slider slider, Action<TextMeshProUGUI> refresh)
    {
        if (slider == null || refresh == null) return;

        Transform labelTransform = slider.transform.Find("Label");
        TextMeshProUGUI label = labelTransform != null
            ? labelTransform.GetComponent<TextMeshProUGUI>()
            : null;
        refresh(label);
    }

    private static void SetSliderFromInput(Slider slider, TMP_InputField input, string text)
    {
        if (!TryParseSliderValue(text, out float value))
        {
            input.SetTextWithoutNotify(FormatSliderValue(slider, slider.value));
            return;
        }

        value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
        if (slider.wholeNumbers)
        {
            value = Mathf.Round(value);
        }
        else
        {
            value = Mathf.Round(value * 10f) / 10f;
        }

        slider.value = value;
        input.SetTextWithoutNotify(FormatSliderValue(slider, slider.value));
    }

    private static bool TryParseSliderValue(string text, out float value)
    {
        string normalized = string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Trim().Replace(',', '.');
        return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatSliderValue(Slider slider, float value) => slider.wholeNumbers
        ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
        : value.ToString("0.0", CultureInfo.InvariantCulture);

    private Slider[] GetSettingsSliders() => new[]
    {
        generalVolumeSlider,
        voipVolumeSlider,
        musicVolumeSlider,
        worldVolumeSlider,
        environmentVolumeSlider,
        hitVolumeSlider,
        killVolumeSlider,
        radioVoipVolumeSlider,
        infantrySensibilitySlider,
        infantryAimSensibilitySlider,
        tankSensibilitySlider,
        tankAimSensibilitySlider,
        jetSensibilitySlider,
        jetAimSensibilitySlider,
        helicopterSensibilitySlider,
        helicopterAimSensibilitySlider,
        hitMarkerOpacitySlider,
        hitMarkerSizeSlider,
        sightReticleSizeSlider,
        enemyIndicatorOpacitySlider,
        allyIndicatorOpacitySlider,
        squadIndicatorOpacitySlider,
        enemyIndicatorAimOpacitySlider,
        allyIndicatorAimOpacitySlider,
        squadIndicatorAimOpacitySlider,
        neutralIndicatorOpacitySlider,
        neutralIndicatorAimOpacitySlider,
        chatOpacitySlider,
        chatSizeSlider,
        renderDistanceSlider,
        vsyncSlider,
        brightnessSlider,
        renderScaleSlider,
        infantryFovSlider,
        jetFovSlider,
        tankFovSlider,
        helicopterFovSlider,
        cameraShakeIntensitySlider,
        motionBlurSlider
    };
}
