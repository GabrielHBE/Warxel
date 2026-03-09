using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using System;
using UnityEngine.EventSystems;

public class SettingsHUD : MonoBehaviour
{
    public static SettingsHUD Instance { get; private set; }

    [SerializeField] private GameObject reset_keyBind_button;
    [SerializeField] private GameObject error_image;
    [SerializeField] private UnityEngine.UI.Button close_image_error_button;
    [SerializeField] private GameObject settings_menu;
    [SerializeField] private Image background_image;
    [SerializeField] private TextMeshProUGUI tab_title;
    [HideInInspector] public bool is_menu_settings_active = false;

    [Header("Tabs")]
    [SerializeField] private GameObject audio_tab;
    [SerializeField] private GameObject controls_tab;
    [SerializeField] private GameObject gameplay_tab;
    [SerializeField] private GameObject keybinds_tab;
    [SerializeField] private GameObject video_tab;

    // Componentes de UI de cada tab
    [Header("UI Controls - Audio")]
    [SerializeField] private Slider generalVolumeSlider;
    [SerializeField] private Slider voipVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider worldVolumeSlider;
    [SerializeField] private Slider hitVolumeSlider;
    [SerializeField] private Slider killVolumeSlider;
    [SerializeField] private Slider vehicleVolumeSlider;
    [SerializeField] private Slider infantryVolumeSlider;
    [SerializeField] private Slider microphoneVolumeSlider;
    [SerializeField] private Toggle enableDeathVoipToggle;
    [SerializeField] private TMP_Dropdown inWorldVoipDropdown;
    [SerializeField] private TMP_Dropdown radioVoipDropdown;

    [Header("UI Controls - Controls")]
    [SerializeField] private Toggle aimHoldToggle;
    [SerializeField] private Toggle sprintHoldToggle;
    [SerializeField] private Toggle crouchHoldToggle;
    [SerializeField] private Toggle proneHoldToggle;
    [SerializeField] private Toggle vehicleBoostHoldToggle;

    // Mouse Infantry
    [SerializeField] private Toggle invertVerticalInfantryToggle;
    [SerializeField] private Slider infantrySensibilitySlider;
    [SerializeField] private Slider infantryAimSensibilitySlider;

    // Mouse Tank
    [SerializeField] private Toggle invertVerticalTankToggle;
    [SerializeField] private Slider tankSensibilitySlider;
    [SerializeField] private Slider tankAimSensibilitySlider;

    // Mouse Jet
    [SerializeField] private Toggle invertVerticalJetToggle;
    [SerializeField] private Slider jetSensibilitySlider;
    [SerializeField] private Slider jetAimSensibilitySlider;

    // Mouse Helicopter
    [SerializeField] private Toggle invertVerticalHeliToggle;
    [SerializeField] private Slider helicopterSensibilitySlider;
    [SerializeField] private Slider helicopterAimSensibilitySlider;

    [Header("UI Controls - Gameplay")]
    // HitMarkers
    [SerializeField] private Toggle showHitMarkerToggle;
    [SerializeField] private Slider hitMarkerOpacitySlider;
    [SerializeField] private Slider hitMarkerSizeSlider;

    // User Interface
    [SerializeField] private Toggle showFpsToggle;
    [SerializeField] private Toggle showNetworkStatusToggle;
    [SerializeField] private Toggle showLevelProgressionToggle;
    [SerializeField] private Toggle showKillFeedToggle;
    [SerializeField] private Slider sightReticleSizeSlider;

    // Indicators
    [SerializeField] private Slider enemyIndicatorOpacitySlider;
    [SerializeField] private Slider allyIndicatorOpacitySlider;
    [SerializeField] private Slider squadIndicatorOpacitySlider;
    [SerializeField] private Slider enemyIndicatorAimOpacitySlider;
    [SerializeField] private Slider allyIndicatorAimOpacitySlider;
    [SerializeField] private Slider squadIndicatorAimOpacitySlider;

    // Flags
    [SerializeField] private Slider enemyFlagOpacitySlider;
    [SerializeField] private Slider allyFlagOpacitySlider;
    [SerializeField] private Slider enemyFlagAimOpacitySlider;
    [SerializeField] private Slider allyFlagAimOpacitySlider;

    // Chat
    [SerializeField] private Toggle showChatToggle;
    [SerializeField] private Slider chatOpacitySlider;
    [SerializeField] private Slider chatSizeSlider;

    [Header("UI Controls - Video")]
    // Graphics
    [SerializeField] private TMP_Dropdown graphicPresetsDropdown;
    [SerializeField] private Slider renderDistanceSlider;
    [SerializeField] private Toggle enableShadowsToggle;
    [SerializeField] private TMP_Dropdown shadowsDropdown;
    [SerializeField] private TMP_Dropdown meshesDropdown;
    [SerializeField] private TMP_Dropdown rainQualityDropdown;

    // Screen
    [SerializeField] private Toggle limitFpsToggle;
    [SerializeField] private TMP_InputField maxFpsInput;
    [SerializeField] private Slider vsyncSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider renderScaleSlider;
    [SerializeField] private Toggle customResolutionToggle;
    [SerializeField] private TMP_InputField resolutionWidthInput;
    [SerializeField] private TMP_InputField resolutionHeightInput;
    [SerializeField] private TMP_Dropdown screenModeDropdown;

    // Camera
    [SerializeField] private Slider infantryFovSlider;
    [SerializeField] private Slider jetFovSlider;
    [SerializeField] private Slider tankFovSlider;
    [SerializeField] private Slider helicopterFovSlider;
    [SerializeField] private Slider cameraShakeIntensitySlider;
    [SerializeField] private Toggle vignetteToggle;
    [SerializeField] private Slider motionBlurSlider;


    [Header("UI Controls - Key Binds")]
    [Header("Key Bind UI Elements - Player")]
    [SerializeField] private TextMeshProUGUI PLAYER_moveFowardButton;
    [SerializeField] private TextMeshProUGUI PLAYER_moveBackwardsButton;
    [SerializeField] private TextMeshProUGUI PLAYER_moveLeftButton;
    [SerializeField] private TextMeshProUGUI PLAYER_moveRightButton;
    [SerializeField] private TextMeshProUGUI PLAYER_jumpButton;
    [SerializeField] private TextMeshProUGUI PLAYER_interactButton;
    [SerializeField] private TextMeshProUGUI PLAYER_sprintButton;
    [SerializeField] private TextMeshProUGUI PLAYER_crouchButton;
    [SerializeField] private TextMeshProUGUI PLAYER_proneButton;
    [SerializeField] private TextMeshProUGUI PLAYER_leanLeftButton;
    [SerializeField] private TextMeshProUGUI PLAYER_leanRightButton;
    [SerializeField] private TextMeshProUGUI PLAYER_rollButton;

    [Header("Key Bind UI Elements - Weapons")]
    [SerializeField] private TextMeshProUGUI WEAPON_composeBulletsButton;
    public TextMeshProUGUI WEAPON_activateSideGripButton;
    [SerializeField] private TextMeshProUGUI WEAPON_shootButton;
    [SerializeField] private TextMeshProUGUI WEAPON_reloadButton;
    [SerializeField] private TextMeshProUGUI WEAPON_aimButton;
    public TextMeshProUGUI WEAPON_switchFireModeButton;
    [SerializeField] private TextMeshProUGUI WEAPON_weapon1Button;
    [SerializeField] private TextMeshProUGUI WEAPON_weapon2Button;

    [Header("Key Bind UI Elements - Gadget")]
    [SerializeField] private TextMeshProUGUI GADGET_gadget1Button;
    [SerializeField] private TextMeshProUGUI GADGET_gadget2Button;
    [SerializeField] private TextMeshProUGUI GADGET_throwGrenadeButton;
    [SerializeField] private TextMeshProUGUI GADGET_throwC4Button;
    [SerializeField] private TextMeshProUGUI GADGET_detonateC4Button;

    [Header("Key Bind UI Elements - Vehicle")]
    [SerializeField] private TextMeshProUGUI VEHICLE_startEngineButton;
    [SerializeField] private TextMeshProUGUI VEHICLE_freeLookButton;

    [Header("Key Bind UI Elements - Jet")]
    [SerializeField] private TextMeshProUGUI JET_boostButton;
    [SerializeField] private TextMeshProUGUI JET_shootVehicleButton;
    [SerializeField] private TextMeshProUGUI JET_pitchUpButton;
    [SerializeField] private TextMeshProUGUI JET_pitchDownButton;
    [SerializeField] private TextMeshProUGUI JET_yawLeftButton;
    [SerializeField] private TextMeshProUGUI JET_yawRightButton;
    [SerializeField] private TextMeshProUGUI JET_speedUpButton;
    [SerializeField] private TextMeshProUGUI JET_speedDownButton;

    [Header("Key Bind UI Elements - Helicopter")]
    [SerializeField] private TextMeshProUGUI HELICOPTER_increaseThrottleButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_decreaseThrottleButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_switchCameraButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_mainCannonButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_upgradeGunButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_shootButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_pitchUpButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_pitchDownButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_leanLeftButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_leanRightButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_zoomButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_gunnerSeatButton;
    [SerializeField] private TextMeshProUGUI HELICOPTER_pilotSeatButton;


    private bool is_mouse_over_close_error_message_button;
    private bool show_message_error = false;
    private string message_error;
    TextMeshProUGUI text_error;
    void Start()
    {
        Instance = this;
        text_error = error_image.GetComponentInChildren<TextMeshProUGUI>();

        if (settings_menu != null)
            settings_menu.SetActive(false);

        // Inicializa todos os valores da UI
        InitializeUIValues();


        SelectControlsTab();

        // Inicializar o mapeamento de botões
        InitializeKeyButtonMap();

        // Carregar keybinds salvos
        LoadAllKeybindsToUI();

        CloseErrorMessage();
    }

    private void ShowErrorMessage()
    {
        error_image.SetActive(true);
        text_error.text = message_error;

    }

    public void CloseErrorMessage()
    {
        // Se fechar a mensagem de erro, cancelar o rebind e restaurar tecla anterior
        CancelRebind();
        error_image.SetActive(false);
    }


    float scrollSpeed = 3000f;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRebind();
            ToggleSettingsMenu();
        }

        if (!is_menu_settings_active) return;

        if (show_message_error)
        {
            IsMouseOverButton();
            ShowErrorMessage();
        }

        if (isWaitingForKey)
        {
            ProcessKeyInput();
        }

        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();

        // Só processa se houve scroll
        if (scrollDelta.y == 0) return;

        if (audio_tab.activeSelf)
        {
            // Apenas adiciona o valor do scroll à posição atual
            Vector3 currentPos = audio_tab.transform.localPosition;
            audio_tab.transform.localPosition = new Vector3(
                currentPos.x,
                currentPos.y + scrollDelta.y * scrollSpeed * Time.deltaTime,
                currentPos.z
            );
        }
        else if (controls_tab.activeSelf)
        {
            Vector3 currentPos = controls_tab.transform.localPosition;
            controls_tab.transform.localPosition = new Vector3(
                currentPos.x,
                currentPos.y + scrollDelta.y * scrollSpeed * Time.deltaTime,
                currentPos.z
            );
        }
        else if (gameplay_tab.activeSelf)
        {
            Vector3 currentPos = gameplay_tab.transform.localPosition;
            gameplay_tab.transform.localPosition = new Vector3(
                currentPos.x,
                currentPos.y + scrollDelta.y * scrollSpeed * Time.deltaTime,
                currentPos.z
            );
        }
        else if (keybinds_tab.activeSelf)
        {
            Vector3 currentPos = keybinds_tab.transform.localPosition;
            keybinds_tab.transform.localPosition = new Vector3(
                currentPos.x,
                currentPos.y + scrollDelta.y * scrollSpeed * Time.deltaTime,
                currentPos.z
            );
        }
        else if (video_tab.activeSelf)
        {
            Vector3 currentPos = video_tab.transform.localPosition;
            video_tab.transform.localPosition = new Vector3(
                currentPos.x,
                currentPos.y + scrollDelta.y * scrollSpeed * Time.deltaTime,
                currentPos.z
            );
        }
    }

    public void ToggleSettingsMenu()
    {
        is_menu_settings_active = !is_menu_settings_active;

        if (settings_menu != null)
            settings_menu.SetActive(is_menu_settings_active);


    }

    public void OpenSettingsMenu()
    {
        is_menu_settings_active = true;

        if (settings_menu != null)
        {

            settings_menu.SetActive(true);
        }


    }

    public void CloseSettingsMenu()
    {
        is_menu_settings_active = false;

        if (settings_menu != null)
            settings_menu.SetActive(false);

        // Salvar automaticamente ao fechar
        SaveAllSettings();

    }



    #region Select tabs
    public void SelectAudioTab()
    {
        tab_title.text = "Audio";
        audio_tab.SetActive(true);
        controls_tab.SetActive(false);
        gameplay_tab.SetActive(false);
        keybinds_tab.SetActive(false);
        video_tab.SetActive(false);

        reset_keyBind_button.SetActive(false);
    }

    public void SelectControlsTab()
    {
        tab_title.text = "Controls";
        audio_tab.SetActive(false);
        controls_tab.SetActive(true);
        gameplay_tab.SetActive(false);
        keybinds_tab.SetActive(false);
        video_tab.SetActive(false);

        reset_keyBind_button.SetActive(false);
    }

    public void SelectGameplayTab()
    {
        tab_title.text = "Gameplay";
        audio_tab.SetActive(false);
        controls_tab.SetActive(false);
        gameplay_tab.SetActive(true);
        keybinds_tab.SetActive(false);
        video_tab.SetActive(false);

        reset_keyBind_button.SetActive(false);
    }

    public void SelectKeyBindsTab()
    {
        tab_title.text = "Key Binds";
        audio_tab.SetActive(false);
        controls_tab.SetActive(false);
        gameplay_tab.SetActive(false);
        keybinds_tab.SetActive(true);
        video_tab.SetActive(false);

        reset_keyBind_button.SetActive(true);
    }

    public void SelectVideoTab()
    {
        tab_title.text = "Video";
        audio_tab.SetActive(false);
        controls_tab.SetActive(false);
        gameplay_tab.SetActive(false);
        keybinds_tab.SetActive(false);
        video_tab.SetActive(true);

        reset_keyBind_button.SetActive(false);
    }

    public void SelectReturnToMenuTab()
    {

    }

    public void SelectQuitGameTab()
    {
        Application.Quit();
    }

    #endregion

    #region Inicialização
    private void InitializeUIValues()
    {

        // VERIFICAR SE Settings.Instance EXISTE
        if (Settings.Instance == null)
        {
            Debug.LogError("Settings.Instance is null! Make sure Settings object exists in the scene.");
            return;
        }

        //LoadColors();

        // Áudio - Carregar valores salvos ou usar padrão
        if (generalVolumeSlider != null) generalVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.GENERAL_VOLUME, Settings.Instance._audio.general_volume);
        if (voipVolumeSlider != null) voipVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.VOIP_VOLUME, Settings.Instance._audio.in_world_voip_volume);
        if (musicVolumeSlider != null) musicVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.MUSIC_VOLUME, Settings.Instance._audio.music_volume);
        if (worldVolumeSlider != null) worldVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.WORLD_VOLUME, Settings.Instance._audio.world_volume);
        if (hitVolumeSlider != null) hitVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.HIT_VOLUME, Settings.Instance._audio.hit_volume);
        if (killVolumeSlider != null) killVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.KILL_VOLUME, Settings.Instance._audio.kill_volume);
        if (vehicleVolumeSlider != null) vehicleVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.VEHICLE_VOLUME, Settings.Instance._audio.vehicle_volume);
        if (infantryVolumeSlider != null) infantryVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.INFANTRY_VOLUME, Settings.Instance._audio.infantary_volume);
        if (microphoneVolumeSlider != null) microphoneVolumeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.MICROPHONE_VOLUME, Settings.Instance._audio.microphone_volume);
        if (enableDeathVoipToggle != null) enableDeathVoipToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.ENABLE_DEATH_VOIP, Settings.Instance._audio.enable_deth_voip ? 1 : 0) == 1;

        // Configurar dropdowns
        if (inWorldVoipDropdown != null)
        {
            inWorldVoipDropdown.ClearOptions();
            inWorldVoipDropdown.AddOptions(Settings.Instance._audio.in_world_voip_modes);
            inWorldVoipDropdown.value = PlayerPrefs.GetInt(SettingsKeys.IN_WORLD_VOIP_MODE, 0);
        }

        if (radioVoipDropdown != null)
        {
            radioVoipDropdown.ClearOptions();
            radioVoipDropdown.AddOptions(Settings.Instance._audio.radio_world_voip_modes);
            radioVoipDropdown.value = PlayerPrefs.GetInt(SettingsKeys.RADIO_VOIP_MODE, 0);
        }

        // Controls - Carregar valores salvos
        if (aimHoldToggle != null) aimHoldToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.AIM_HOLD, Settings.Instance._controls.is_aim_on_hold ? 1 : 0) == 1;
        if (sprintHoldToggle != null) sprintHoldToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.SPRINT_HOLD, Settings.Instance._controls.is_sprint_on_hold ? 1 : 0) == 1;
        if (crouchHoldToggle != null) crouchHoldToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.CROUCH_HOLD, Settings.Instance._controls.is_crouch_on_hold ? 1 : 0) == 1;
        if (proneHoldToggle != null) proneHoldToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.PRONE_HOLD, Settings.Instance._controls.is_prone_on_hold ? 1 : 0) == 1;
        if (vehicleBoostHoldToggle != null) vehicleBoostHoldToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.VEHICLE_BOOST_HOLD, Settings.Instance._controls.is_vehicle_boost_on_hold ? 1 : 0) == 1;

        // Mouse Infantry
        if (invertVerticalInfantryToggle != null) invertVerticalInfantryToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.INVERT_VERTICAL_INFANTRY, Settings.Instance._controls.invert_vertical_infantary_mouse ? 1 : 0) == 1;
        if (infantrySensibilitySlider != null) infantrySensibilitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.INFANTRY_SENSIBILITY, Settings.Instance._controls.infantary_sensibility);
        if (infantryAimSensibilitySlider != null) infantryAimSensibilitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.INFANTRY_AIM_SENSIBILITY, Settings.Instance._controls.infantary_aim_sensibility);

        // Mouse Tank
        if (invertVerticalTankToggle != null) invertVerticalTankToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.INVERT_VERTICAL_TANK, Settings.Instance._controls.invert_vertical_tank_mouse ? 1 : 0) == 1;
        if (tankSensibilitySlider != null) tankSensibilitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.TANK_SENSIBILITY, Settings.Instance._controls.tank_sensibility);
        if (tankAimSensibilitySlider != null) tankAimSensibilitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.TANK_AIM_SENSIBILITY, Settings.Instance._controls.tank_aim_sensibility);

        // Mouse Jet
        if (invertVerticalJetToggle != null) invertVerticalJetToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.INVERT_VERTICAL_JET, Settings.Instance._controls.invert_vertical_jet_mouse ? 1 : 0) == 1;
        if (jetSensibilitySlider != null) jetSensibilitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.JET_SENSIBILITY, Settings.Instance._controls.jet_sensibility);
        if (jetAimSensibilitySlider != null) jetAimSensibilitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.JET_AIM_SENSIBILITY, Settings.Instance._controls.jet_aim_sensibility);

        // Mouse Helicopter
        if (invertVerticalHeliToggle != null) invertVerticalHeliToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.INVERT_VERTICAL_HELI, Settings.Instance._controls.invert_vertical_heli_mouse ? 1 : 0) == 1;
        if (helicopterSensibilitySlider != null) helicopterSensibilitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.HELICOPTER_SENSIBILITY, Settings.Instance._controls.helicopter_sensibility);
        if (helicopterAimSensibilitySlider != null) helicopterAimSensibilitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.HELICOPTER_AIM_SENSIBILITY, Settings.Instance._controls.helicopter_aim_sensibility);

        // Gameplay - Carregar valores salvos
        if (showHitMarkerToggle != null) showHitMarkerToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.SHOW_HIT_MARKER, Settings.Instance._gameplay.show_hit_marker ? 1 : 0) == 1;
        if (hitMarkerOpacitySlider != null) hitMarkerOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.HIT_MARKER_OPACITY, Settings.Instance._gameplay.hit_marker_opacity);
        if (hitMarkerSizeSlider != null) hitMarkerSizeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.HIT_MARKER_SIZE, Settings.Instance._gameplay.hit_marker_size);

        if (showFpsToggle != null) showFpsToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.SHOW_FPS, Settings.Instance._gameplay.show_fps ? 1 : 0) == 1;
        if (showNetworkStatusToggle != null) showNetworkStatusToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.SHOW_NETWORK_STATUS, Settings.Instance._gameplay.show_network_status ? 1 : 0) == 1;
        if (showLevelProgressionToggle != null) showLevelProgressionToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.SHOW_LEVEL_PROGRESSION, Settings.Instance._gameplay.show_level_progression ? 1 : 0) == 1;
        if (showKillFeedToggle != null) showKillFeedToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.SHOW_KILL_FEED, Settings.Instance._gameplay.show_kill_feed ? 1 : 0) == 1;
        if (sightReticleSizeSlider != null) sightReticleSizeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.SIGHT_RETICLE_SIZE, Settings.Instance._gameplay.sight_reticle_size);

        // Indicators
        if (enemyIndicatorOpacitySlider != null) enemyIndicatorOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ENEMY_INDICATOR_OPACITY, Settings.Instance._gameplay.enemy_indicator_opacity);
        if (allyIndicatorOpacitySlider != null) allyIndicatorOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ALLY_INDICATOR_OPACITY, Settings.Instance._gameplay.ally_indicator_opacity);
        if (squadIndicatorOpacitySlider != null) squadIndicatorOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.SQUAD_INDICATOR_OPACITY, Settings.Instance._gameplay.squad_indicator_opacity);
        if (enemyIndicatorAimOpacitySlider != null) enemyIndicatorAimOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ENEMY_INDICATOR_AIM_OPACITY, Settings.Instance._gameplay.enemy_indicator_aim_opacity);
        if (allyIndicatorAimOpacitySlider != null) allyIndicatorAimOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ALLY_INDICATOR_AIM_OPACITY, Settings.Instance._gameplay.ally_indicator_aim_opacity);
        if (squadIndicatorAimOpacitySlider != null) squadIndicatorAimOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.SQUAD_INDICATOR_AIM_OPACITY, Settings.Instance._gameplay.squad_indicator_aim_opacity);

        // Flags
        if (enemyFlagOpacitySlider != null) enemyFlagOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ENEMY_FLAG_OPACITY, Settings.Instance._gameplay.enemy_flag_opacity);
        if (allyFlagOpacitySlider != null) allyFlagOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ALLY_FLAG_OPACITY, Settings.Instance._gameplay.ally_flag_opacity);
        if (enemyFlagAimOpacitySlider != null) enemyFlagAimOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ENEMY_FLAG_AIM_OPACITY, Settings.Instance._gameplay.enemy_flag_aim_opacity);
        if (allyFlagAimOpacitySlider != null) allyFlagAimOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ALLY_FLAG_AIM_OPACITY, Settings.Instance._gameplay.ally_flag_aim_opacity);

        // Chat
        if (showChatToggle != null) showChatToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.SHOW_CHAT, Settings.Instance._gameplay.show_chat ? 1 : 0) == 1;
        if (chatOpacitySlider != null) chatOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.CHAT_OPACITY, Settings.Instance._gameplay.chat_opacity);
        if (chatSizeSlider != null) chatSizeSlider.value = PlayerPrefs.GetFloat(SettingsKeys.CHAT_SIZE, Settings.Instance._gameplay.chat_size);

        // Video - Carregar valores salvos
        if (graphicPresetsDropdown != null)
        {
            graphicPresetsDropdown.ClearOptions();
            graphicPresetsDropdown.AddOptions(Settings.Instance._video.graphic_presets);
            graphicPresetsDropdown.value = PlayerPrefs.GetInt(SettingsKeys.GRAPHIC_PRESET, 0);
        }

        if (renderDistanceSlider != null) renderDistanceSlider.value = PlayerPrefs.GetFloat(SettingsKeys.RENDER_DISTANCE, Settings.Instance._video.render_distance);
        if (enableShadowsToggle != null) enableShadowsToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.ENABLE_SHADOWS, Settings.Instance._video.enable_shadows ? 1 : 0) == 1;

        if (shadowsDropdown != null)
        {
            shadowsDropdown.ClearOptions();
            shadowsDropdown.AddOptions(Settings.Instance._video.shadows);
            shadowsDropdown.value = PlayerPrefs.GetInt(SettingsKeys.SHADOWS_QUALITY, 0);
        }

        if (meshesDropdown != null)
        {
            meshesDropdown.ClearOptions();
            meshesDropdown.AddOptions(Settings.Instance._video.meshes);
            meshesDropdown.value = PlayerPrefs.GetInt(SettingsKeys.MESHES_QUALITY, 0);
        }

        if (rainQualityDropdown != null)
        {
            rainQualityDropdown.ClearOptions();
            rainQualityDropdown.AddOptions(Settings.Instance._video.rain_quality);
            rainQualityDropdown.value = PlayerPrefs.GetInt(SettingsKeys.RAIN_QUALITY, 0);
        }

        // Screen
        if (limitFpsToggle != null) limitFpsToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.LIMIT_FPS, Settings.Instance._video.limit_fps ? 1 : 0) == 1;
        if (maxFpsInput != null) maxFpsInput.text = PlayerPrefs.GetFloat(SettingsKeys.MAX_FPS, Settings.Instance._video.max_fps).ToString("F0");
        if (vsyncSlider != null) vsyncSlider.value = PlayerPrefs.GetFloat(SettingsKeys.VSYNC, Settings.Instance._video.Vsync);
        if (brightnessSlider != null) brightnessSlider.value = PlayerPrefs.GetFloat(SettingsKeys.BRIGHTNESS, Settings.Instance._video.brightness);
        if (renderScaleSlider != null) renderScaleSlider.value = PlayerPrefs.GetFloat(SettingsKeys.RENDER_SCALE, Settings.Instance._video.render_scale);
        if (customResolutionToggle != null) customResolutionToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.CUSTOM_RESOLUTION, Settings.Instance._video.custom_resolution ? 1 : 0) == 1;

        if (resolutionWidthInput != null && resolutionHeightInput != null)
        {
            resolutionWidthInput.text = PlayerPrefs.GetInt(SettingsKeys.RESOLUTION_WIDTH, 1920).ToString();
            resolutionHeightInput.text = PlayerPrefs.GetInt(SettingsKeys.RESOLUTION_HEIGHT, 1080).ToString();
        }

        if (screenModeDropdown != null)
        {
            screenModeDropdown.ClearOptions();
            screenModeDropdown.AddOptions(Settings.Instance._video.screen_mode);
            screenModeDropdown.value = PlayerPrefs.GetInt(SettingsKeys.SCREEN_MODE, 0);
        }

        // Camera
        if (infantryFovSlider != null) infantryFovSlider.value = PlayerPrefs.GetFloat(SettingsKeys.INFANTRY_FOV, Settings.Instance._video.infantary_fov);
        if (jetFovSlider != null) jetFovSlider.value = PlayerPrefs.GetFloat(SettingsKeys.JET_FOV, Settings.Instance._video.jet_fov);
        if (tankFovSlider != null) tankFovSlider.value = PlayerPrefs.GetFloat(SettingsKeys.TANK_FOV, Settings.Instance._video.tank_fov);
        if (helicopterFovSlider != null) helicopterFovSlider.value = PlayerPrefs.GetFloat(SettingsKeys.HELICOPTER_FOV, Settings.Instance._video.helicopter_fov);
        if (cameraShakeIntensitySlider != null) cameraShakeIntensitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.CAMERA_SHAKE_INTENSITY, Settings.Instance._video.camera_shake_intensity);
        if (vignetteToggle != null) vignetteToggle.isOn = PlayerPrefs.GetInt(SettingsKeys.VIGNETTE, Settings.Instance._video.vignette ? 1 : 0) == 1;
        if (motionBlurSlider != null) motionBlurSlider.value = PlayerPrefs.GetFloat(SettingsKeys.MOTION_BLUR, Settings.Instance._video.motion_blur);
    }

    #endregion

    #region Controls
    // Gameplay Controls
    public void OnAimHoldChanged()
    {
        Settings.Instance._controls.is_aim_on_hold = aimHoldToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.AIM_HOLD, aimHoldToggle.isOn ? 1 : 0);
    }

    public void OnSprintHoldChanged()
    {
        Settings.Instance._controls.is_sprint_on_hold = sprintHoldToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.SPRINT_HOLD, sprintHoldToggle.isOn ? 1 : 0);
    }

    public void OnCrouchHoldChanged()
    {
        Settings.Instance._controls.is_crouch_on_hold = crouchHoldToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.CROUCH_HOLD, crouchHoldToggle.isOn ? 1 : 0);
    }

    public void OnProneHoldChanged()
    {
        Settings.Instance._controls.is_prone_on_hold = proneHoldToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.PRONE_HOLD, proneHoldToggle.isOn ? 1 : 0);
    }

    public void OnVehicleBoostHoldChanged()
    {
        Settings.Instance._controls.is_vehicle_boost_on_hold = vehicleBoostHoldToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.VEHICLE_BOOST_HOLD, vehicleBoostHoldToggle.isOn ? 1 : 0);
    }

    // Mouse Infantry
    public void OnInvertVerticalInfantryChanged()
    {
        Settings.Instance._controls.invert_vertical_infantary_mouse = invertVerticalInfantryToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.INVERT_VERTICAL_INFANTRY, invertVerticalInfantryToggle.isOn ? 1 : 0);
    }

    public void OnInfantrySensibilityChanged(TextMeshProUGUI text)
    {
        text.text = "[" + infantrySensibilitySlider.value.ToString("F1") + "] Infantaty Mouse Sensibility";
        Settings.Instance._controls.infantary_sensibility = infantrySensibilitySlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.INFANTRY_SENSIBILITY, infantrySensibilitySlider.value);
    }

    public void OnInfantryAimSensibilityChanged(TextMeshProUGUI text)
    {
        text.text = "[" + infantryAimSensibilitySlider.value.ToString("F1") + "] Infantaty Aim Mouse Sensibility";
        Settings.Instance._controls.infantary_aim_sensibility = infantryAimSensibilitySlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.INFANTRY_AIM_SENSIBILITY, infantryAimSensibilitySlider.value);
    }

    // Mouse Tank
    public void OnInvertVerticalTankChanged()
    {
        Settings.Instance._controls.invert_vertical_tank_mouse = invertVerticalTankToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.INVERT_VERTICAL_TANK, invertVerticalTankToggle.isOn ? 1 : 0);
    }

    public void OnTankSensibilityChanged(TextMeshProUGUI text)
    {
        text.text = "[" + tankSensibilitySlider.value.ToString("F1") + "] Tank Mouse Sensibility";
        Settings.Instance._controls.tank_sensibility = tankSensibilitySlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.TANK_SENSIBILITY, tankSensibilitySlider.value);
    }

    public void OnTankAimSensibilityChanged(TextMeshProUGUI text)
    {
        text.text = "[" + tankAimSensibilitySlider.value.ToString("F1") + "] Tank Aim Mouse Sensibility";
        Settings.Instance._controls.tank_aim_sensibility = tankAimSensibilitySlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.TANK_AIM_SENSIBILITY, tankAimSensibilitySlider.value);
    }

    // Mouse Jet
    public void OnInvertVerticalJetChanged()
    {
        Settings.Instance._controls.invert_vertical_jet_mouse = invertVerticalJetToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.INVERT_VERTICAL_JET, invertVerticalJetToggle.isOn ? 1 : 0);
    }

    public void OnJetSensibilityChanged(TextMeshProUGUI text)
    {
        text.text = "[" + jetSensibilitySlider.value.ToString("F1") + "] Jet Mouse Sensibility";
        Settings.Instance._controls.jet_sensibility = jetSensibilitySlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.JET_SENSIBILITY, jetSensibilitySlider.value);
    }

    public void OnJetAimSensibilityChanged(TextMeshProUGUI text)
    {
        text.text = "[" + jetAimSensibilitySlider.value.ToString("F1") + "] Jet Aim Mouse Sensibility";
        Settings.Instance._controls.jet_aim_sensibility = jetAimSensibilitySlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.JET_AIM_SENSIBILITY, jetAimSensibilitySlider.value);
    }

    // Mouse Helicopter
    public void OnInvertVerticalHeliChanged()
    {
        Settings.Instance._controls.invert_vertical_heli_mouse = invertVerticalHeliToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.INVERT_VERTICAL_HELI, invertVerticalHeliToggle.isOn ? 1 : 0);
    }

    public void OnHelicopterSensibilityChanged(TextMeshProUGUI text)
    {
        text.text = "[" + helicopterSensibilitySlider.value.ToString("F1") + "] Helicopter Mouse Sensibility";
        Settings.Instance._controls.helicopter_sensibility = helicopterSensibilitySlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.HELICOPTER_SENSIBILITY, helicopterSensibilitySlider.value);
    }

    public void OnHelicopterAimSensibilityChanged(TextMeshProUGUI text)
    {
        text.text = "[" + helicopterAimSensibilitySlider.value.ToString("F1") + "] Helicopter Aim Mouse Sensibility";
        Settings.Instance._controls.helicopter_aim_sensibility = helicopterAimSensibilitySlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.HELICOPTER_AIM_SENSIBILITY, helicopterAimSensibilitySlider.value);
    }
    #endregion

    #region Audio
    public void OnGeneralVolumeChanged(float value)
    {
        Settings.Instance._audio.general_volume = value;
        PlayerPrefs.SetFloat(SettingsKeys.GENERAL_VOLUME, value);
    }

    public void OnVoipVolumeChanged(float value)
    {
        Settings.Instance._audio.in_world_voip_volume = value;
        PlayerPrefs.SetFloat(SettingsKeys.VOIP_VOLUME, value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        Settings.Instance._audio.music_volume = value;
        PlayerPrefs.SetFloat(SettingsKeys.MUSIC_VOLUME, value);
    }

    public void OnWorldVolumeChanged(float value)
    {
        Settings.Instance._audio.world_volume = value;
        PlayerPrefs.SetFloat(SettingsKeys.WORLD_VOLUME, value);
    }

    public void OnHitVolumeChanged(float value)
    {
        Settings.Instance._audio.hit_volume = value;
        PlayerPrefs.SetFloat(SettingsKeys.HIT_VOLUME, value);
    }

    public void OnKillVolumeChanged(float value)
    {
        Settings.Instance._audio.kill_volume = value;
        PlayerPrefs.SetFloat(SettingsKeys.KILL_VOLUME, value);
    }

    public void OnVehicleVolumeChanged(float value)
    {
        Settings.Instance._audio.vehicle_volume = value;
        PlayerPrefs.SetFloat(SettingsKeys.VEHICLE_VOLUME, value);
    }

    public void OnInfantryVolumeChanged(float value)
    {
        Settings.Instance._audio.infantary_volume = value;
        PlayerPrefs.SetFloat(SettingsKeys.INFANTRY_VOLUME, value);
    }

    public void OnMicrophoneVolumeChanged(float value)
    {
        Settings.Instance._audio.microphone_volume = value;
        PlayerPrefs.SetFloat(SettingsKeys.MICROPHONE_VOLUME, value);
    }

    public void OnEnableDeathVoipChanged()
    {
        Settings.Instance._audio.enable_deth_voip = enableDeathVoipToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.ENABLE_DEATH_VOIP, enableDeathVoipToggle.isOn ? 1 : 0);
    }

    public void OnInWorldVoipModeChanged(int index)
    {
        Settings.Instance._audio.in_world_voip_key = GetKeyForMode(Settings.Instance._audio.in_world_voip_modes[index]);
        PlayerPrefs.SetInt(SettingsKeys.IN_WORLD_VOIP_MODE, index);
    }

    public void OnRadioVoipModeChanged(int index)
    {
        Settings.Instance._audio.radio_voip_key = GetKeyForMode(Settings.Instance._audio.radio_world_voip_modes[index]);
        PlayerPrefs.SetInt(SettingsKeys.RADIO_VOIP_MODE, index);
    }

    private KeyCode GetKeyForMode(string mode)
    {
        switch (mode.ToLower())
        {
            case "push": return KeyCode.V;
            case "enabled": return KeyCode.B;
            default: return KeyCode.None;
        }
    }
    #endregion

    #region Gameplay
    // HitMarkers
    public void OnShowHitMarkerChanged()
    {
        Settings.Instance._gameplay.show_hit_marker = showHitMarkerToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.SHOW_HIT_MARKER, showHitMarkerToggle.isOn ? 1 : 0);
    }

    public void OnHitMarkerOpacityChanged(TextMeshProUGUI text)
    {
        text.text = "[" + hitMarkerOpacitySlider.value.ToString("F1") + "] Hit Marker Opacity";
        Settings.Instance._gameplay.hit_marker_opacity = hitMarkerOpacitySlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.HIT_MARKER_OPACITY, hitMarkerOpacitySlider.value);
    }

    public void OnHitMarkerSizeChanged(TextMeshProUGUI text)
    {
        text.text = "[" + hitMarkerSizeSlider.value.ToString("F1") + "] Hit Marker Size";
        Settings.Instance._gameplay.hit_marker_size = hitMarkerSizeSlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.HIT_MARKER_SIZE, hitMarkerSizeSlider.value);
    }

    // User Interface
    public void OnShowFpsChanged()
    {
        Settings.Instance._gameplay.show_fps = showFpsToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.SHOW_FPS, showFpsToggle.isOn ? 1 : 0);
    }

    public void OnShowNetworkStatusChanged()
    {
        Settings.Instance._gameplay.show_network_status = showNetworkStatusToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.SHOW_NETWORK_STATUS, showNetworkStatusToggle.isOn ? 1 : 0);
    }

    public void OnShowLevelProgressionChanged()
    {
        Settings.Instance._gameplay.show_level_progression = showLevelProgressionToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.SHOW_LEVEL_PROGRESSION, showLevelProgressionToggle.isOn ? 1 : 0);
    }

    public void OnShowKillFeedChanged()
    {
        Settings.Instance._gameplay.show_kill_feed = showKillFeedToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.SHOW_KILL_FEED, showKillFeedToggle.isOn ? 1 : 0);
    }

    public void OnSightReticleSizeChanged(TextMeshProUGUI text)
    {
        text.text = "[" + sightReticleSizeSlider.value.ToString("F1") + "] Sight Reticle Size";
        Settings.Instance._gameplay.sight_reticle_size = sightReticleSizeSlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.SIGHT_RETICLE_SIZE, sightReticleSizeSlider.value);
    }

    // Indicators
    public void OnEnemyIndicatorOpacityChanged(float value)
    {
        Settings.Instance._gameplay.enemy_indicator_opacity = value;
        PlayerPrefs.SetFloat(SettingsKeys.ENEMY_INDICATOR_OPACITY, value);
    }

    public void OnAllyIndicatorOpacityChanged(float value)
    {
        Settings.Instance._gameplay.ally_indicator_opacity = value;
        PlayerPrefs.SetFloat(SettingsKeys.ALLY_INDICATOR_OPACITY, value);
    }

    public void OnSquadIndicatorOpacityChanged(float value)
    {
        Settings.Instance._gameplay.squad_indicator_opacity = value;
        PlayerPrefs.SetFloat(SettingsKeys.SQUAD_INDICATOR_OPACITY, value);
    }

    public void OnEnemyIndicatorAimOpacityChanged(float value)
    {
        Settings.Instance._gameplay.enemy_indicator_aim_opacity = value;
        PlayerPrefs.SetFloat(SettingsKeys.ENEMY_INDICATOR_AIM_OPACITY, value);
    }

    public void OnAllyIndicatorAimOpacityChanged(float value)
    {
        Settings.Instance._gameplay.ally_indicator_aim_opacity = value;
        PlayerPrefs.SetFloat(SettingsKeys.ALLY_INDICATOR_AIM_OPACITY, value);
    }

    public void OnSquadIndicatorAimOpacityChanged(float value)
    {
        Settings.Instance._gameplay.squad_indicator_aim_opacity = value;
        PlayerPrefs.SetFloat(SettingsKeys.SQUAD_INDICATOR_AIM_OPACITY, value);
    }

    // Flags
    public void OnEnemyFlagOpacityChanged(float value)
    {
        Settings.Instance._gameplay.enemy_flag_opacity = value;
        PlayerPrefs.SetFloat(SettingsKeys.ENEMY_FLAG_OPACITY, value);
    }

    public void OnAllyFlagOpacityChanged(float value)
    {
        Settings.Instance._gameplay.ally_flag_opacity = value;
        PlayerPrefs.SetFloat(SettingsKeys.ALLY_FLAG_OPACITY, value);
    }

    public void OnEnemyFlagAimOpacityChanged(float value)
    {
        Settings.Instance._gameplay.enemy_flag_aim_opacity = value;
        PlayerPrefs.SetFloat(SettingsKeys.ENEMY_FLAG_AIM_OPACITY, value);
    }

    public void OnAllyFlagAimOpacityChanged(float value)
    {
        Settings.Instance._gameplay.ally_flag_aim_opacity = value;
        PlayerPrefs.SetFloat(SettingsKeys.ALLY_FLAG_AIM_OPACITY, value);
    }

    // Chat
    public void OnShowChatChanged()
    {
        Settings.Instance._gameplay.show_chat = showChatToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.SHOW_CHAT, showChatToggle.isOn ? 1 : 0);
    }

    public void OnChatOpacityChanged(float value)
    {
        Settings.Instance._gameplay.chat_opacity = value;
        PlayerPrefs.SetFloat(SettingsKeys.CHAT_OPACITY, value);
    }

    public void OnChatSizeChanged(float value)
    {
        Settings.Instance._gameplay.chat_size = value;
        PlayerPrefs.SetFloat(SettingsKeys.CHAT_SIZE, value);
    }

    // Funções para selecionar cores (usando ColorPicker ou botões)
    public void OnBodyShotColorSelected(Color color)
    {
        Settings.Instance._gameplay.body_shot_marker_colour = color;
        PlayerPrefs.SetString("BodyShotColor", ColorUtility.ToHtmlStringRGBA(color));
    }

    public void OnHeadShotColorSelected(Color color)
    {
        Settings.Instance._gameplay.head_shot_marker_colour = color;
        PlayerPrefs.SetString("HeadShotColor", ColorUtility.ToHtmlStringRGBA(color));
    }

    public void OnVehicleMarkerColorSelected(Color color)
    {
        Settings.Instance._gameplay.vehicle_marker_colour = color;
        PlayerPrefs.SetString("VehicleMarkerColor", ColorUtility.ToHtmlStringRGBA(color));
    }

    public void OnSightReticleColorSelected(Color color)
    {
        Settings.Instance._gameplay.sight_reticle_collor = color;
        PlayerPrefs.SetString("SightReticleColor", ColorUtility.ToHtmlStringRGBA(color));
    }

    public void OnEnemyColorSelected(Color color)
    {
        Settings.Instance._gameplay.enemy_color = color;
        PlayerPrefs.SetString("EnemyColor", ColorUtility.ToHtmlStringRGBA(color));
    }

    public void OnAllyColorSelected(Color color)
    {
        Settings.Instance._gameplay.ally_color = color;
        PlayerPrefs.SetString("AllyColor", ColorUtility.ToHtmlStringRGBA(color));
    }

    public void OnSquadColorSelected(Color color)
    {
        Settings.Instance._gameplay.squad_color = color;
        PlayerPrefs.SetString("SquadColor", ColorUtility.ToHtmlStringRGBA(color));
    }

    public void OnEnemyFlagColorSelected(Color color)
    {
        Settings.Instance._gameplay.enemy_color_flag = color;
        PlayerPrefs.SetString("EnemyFlagColor", ColorUtility.ToHtmlStringRGBA(color));
    }

    public void OnAllyFlagColorSelected(Color color)
    {
        Settings.Instance._gameplay.ally_color_flag = color;
        PlayerPrefs.SetString("AllyFlagColor", ColorUtility.ToHtmlStringRGBA(color));
    }

    public void OnSquadFlagColorSelected(Color color)
    {
        Settings.Instance._gameplay.squad_color_flag = color;
        PlayerPrefs.SetString("SquadFlagColor", ColorUtility.ToHtmlStringRGBA(color));
    }
    #endregion


    #region Video
    // Graphics
    // Graphics
    public void OnGraphicPresetChanged(int index)
    {
        PlayerPrefs.SetInt(SettingsKeys.GRAPHIC_PRESET, index);
        // Implemente mudança de preset
    }

    public void OnRenderDistanceChanged(float value)
    {
        Settings.Instance._video.render_distance = value;
        PlayerPrefs.SetFloat(SettingsKeys.RENDER_DISTANCE, value);
    }

    public void OnEnableShadowsChanged()
    {
        Settings.Instance._video.enable_shadows = enableShadowsToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.ENABLE_SHADOWS, enableShadowsToggle.isOn ? 1 : 0);
    }

    public void OnShadowsQualityChanged(int index)
    {
        PlayerPrefs.SetInt(SettingsKeys.SHADOWS_QUALITY, index);
        // Implemente mudança de qualidade de sombras
    }

    public void OnMeshesQualityChanged(int index)
    {
        PlayerPrefs.SetInt(SettingsKeys.MESHES_QUALITY, index);
        // Implemente mudança de qualidade de meshes
    }

    public void OnRainQualityChanged(int index)
    {
        PlayerPrefs.SetInt(SettingsKeys.RAIN_QUALITY, index);
        // Implemente mudança de qualidade de chuva
    }

    // Screen
    public void OnLimitFpsChanged()
    {
        Settings.Instance._video.limit_fps = limitFpsToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.LIMIT_FPS, limitFpsToggle.isOn ? 1 : 0);
    }

    public void OnMaxFpsChanged()
    {

        int numero;

        if (int.TryParse(maxFpsInput.text, out numero))
        {
            Settings.Instance._video.max_fps = numero;
            PlayerPrefs.SetFloat(SettingsKeys.MAX_FPS, numero);
            Application.targetFrameRate = numero;
            QualitySettings.vSyncCount = 0;
        }
        else
        {
            Debug.Log("Entrada inválida");
        }

    }

    public void OnVsyncChanged(float value)
    {
        Settings.Instance._video.Vsync = value;
        PlayerPrefs.SetFloat(SettingsKeys.VSYNC, value);
    }

    public void OnBrightnessChanged(float value)
    {
        Settings.Instance._video.brightness = value;
        PlayerPrefs.SetFloat(SettingsKeys.BRIGHTNESS, value);
    }

    public void OnRenderScaleChanged(float value)
    {
        Settings.Instance._video.render_scale = value;
        PlayerPrefs.SetFloat(SettingsKeys.RENDER_SCALE, value);
    }

    public void OnCustomResolutionChanged()
    {
        Settings.Instance._video.custom_resolution = customResolutionToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.CUSTOM_RESOLUTION, customResolutionToggle.isOn ? 1 : 0);
    }

    public void OnResolutionWidthChanged(string value)
    {
        if (float.TryParse(value, out float width) && Settings.Instance._video.resolution.Length >= 2)
        {
            Settings.Instance._video.resolution[0] = width;
            PlayerPrefs.SetInt(SettingsKeys.RESOLUTION_WIDTH, (int)width);
        }
    }

    public void OnResolutionHeightChanged(string value)
    {
        if (float.TryParse(value, out float height) && Settings.Instance._video.resolution.Length >= 2)
        {
            Settings.Instance._video.resolution[1] = height;
            PlayerPrefs.SetInt(SettingsKeys.RESOLUTION_HEIGHT, (int)height);
        }
    }

    public void OnScreenModeChanged(int index)
    {
        PlayerPrefs.SetInt(SettingsKeys.SCREEN_MODE, index);
        // Implemente mudança de modo de tela
    }

    // Camera
    public void OnInfantryFovChanged(float value)
    {
        Settings.Instance._video.infantary_fov = value;
        PlayerPrefs.SetFloat(SettingsKeys.INFANTRY_FOV, value);
    }

    public void OnJetFovChanged(float value)
    {
        Settings.Instance._video.jet_fov = value;
        PlayerPrefs.SetFloat(SettingsKeys.JET_FOV, value);
    }

    public void OnTankFovChanged(float value)
    {
        Settings.Instance._video.tank_fov = value;
        PlayerPrefs.SetFloat(SettingsKeys.TANK_FOV, value);
    }

    public void OnHelicopterFovChanged(float value)
    {
        Settings.Instance._video.helicopter_fov = value;
        PlayerPrefs.SetFloat(SettingsKeys.HELICOPTER_FOV, value);
    }

    public void OnCameraShakeIntensityChanged(float value)
    {
        Settings.Instance._video.camera_shake_intensity = value;
        PlayerPrefs.SetFloat(SettingsKeys.CAMERA_SHAKE_INTENSITY, value);
    }

    public void OnVignetteChanged()
    {
        Settings.Instance._video.vignette = vignetteToggle.isOn;
        PlayerPrefs.SetInt(SettingsKeys.VIGNETTE, vignetteToggle.isOn ? 1 : 0);
    }

    public void OnMotionBlurChanged(float value)
    {
        Settings.Instance._video.motion_blur = value;
        PlayerPrefs.SetFloat(SettingsKeys.MOTION_BLUR, value);
    }
    #endregion


    #region Save/Load All Settings
    public void SaveAllSettings()
    {
        // Salvar keybinds primeiro (já implementado)
        SaveAllKeybinds();

        // Salvar todas as outras configurações
        PlayerPrefs.Save();
        Debug.Log("All settings saved successfully!");
    }

    public void ResetAllSettingsToDefault()
    {
        // Resetar keybinds (já implementado)
        ResetAllKeybindsToDefault();

        // Resetar outras configurações
        ResetAudioSettings();
        ResetControlsSettings();
        ResetGameplaySettings();
        ResetVideoSettings();

        // Recarregar UI
        InitializeUIValues();

        Debug.Log("All settings reset to default!");

        PlayerPrefs.Save();
    }

    private void ResetAudioSettings()
    {
        PlayerPrefs.DeleteKey(SettingsKeys.GENERAL_VOLUME);
        PlayerPrefs.DeleteKey(SettingsKeys.VOIP_VOLUME);
        PlayerPrefs.DeleteKey(SettingsKeys.MUSIC_VOLUME);
        PlayerPrefs.DeleteKey(SettingsKeys.WORLD_VOLUME);
        PlayerPrefs.DeleteKey(SettingsKeys.HIT_VOLUME);
        PlayerPrefs.DeleteKey(SettingsKeys.KILL_VOLUME);
        PlayerPrefs.DeleteKey(SettingsKeys.VEHICLE_VOLUME);
        PlayerPrefs.DeleteKey(SettingsKeys.INFANTRY_VOLUME);
        PlayerPrefs.DeleteKey(SettingsKeys.MICROPHONE_VOLUME);
        PlayerPrefs.DeleteKey(SettingsKeys.ENABLE_DEATH_VOIP);
        PlayerPrefs.DeleteKey(SettingsKeys.IN_WORLD_VOIP_MODE);
        PlayerPrefs.DeleteKey(SettingsKeys.RADIO_VOIP_MODE);


    }

    private void ResetControlsSettings()
    {
        PlayerPrefs.DeleteKey(SettingsKeys.AIM_HOLD);
        PlayerPrefs.DeleteKey(SettingsKeys.SPRINT_HOLD);
        PlayerPrefs.DeleteKey(SettingsKeys.CROUCH_HOLD);
        PlayerPrefs.DeleteKey(SettingsKeys.PRONE_HOLD);
        PlayerPrefs.DeleteKey(SettingsKeys.VEHICLE_BOOST_HOLD);

        PlayerPrefs.DeleteKey(SettingsKeys.INVERT_VERTICAL_INFANTRY);
        PlayerPrefs.DeleteKey(SettingsKeys.INFANTRY_SENSIBILITY);
        PlayerPrefs.DeleteKey(SettingsKeys.INFANTRY_AIM_SENSIBILITY);

        PlayerPrefs.DeleteKey(SettingsKeys.INVERT_VERTICAL_TANK);
        PlayerPrefs.DeleteKey(SettingsKeys.TANK_SENSIBILITY);
        PlayerPrefs.DeleteKey(SettingsKeys.TANK_AIM_SENSIBILITY);

        PlayerPrefs.DeleteKey(SettingsKeys.INVERT_VERTICAL_JET);
        PlayerPrefs.DeleteKey(SettingsKeys.JET_SENSIBILITY);
        PlayerPrefs.DeleteKey(SettingsKeys.JET_AIM_SENSIBILITY);

        PlayerPrefs.DeleteKey(SettingsKeys.INVERT_VERTICAL_HELI);
        PlayerPrefs.DeleteKey(SettingsKeys.HELICOPTER_SENSIBILITY);
        PlayerPrefs.DeleteKey(SettingsKeys.HELICOPTER_AIM_SENSIBILITY);
    }

    private void ResetGameplaySettings()
    {
        PlayerPrefs.DeleteKey(SettingsKeys.SHOW_HIT_MARKER);
        PlayerPrefs.DeleteKey(SettingsKeys.HIT_MARKER_OPACITY);
        PlayerPrefs.DeleteKey(SettingsKeys.HIT_MARKER_SIZE);

        PlayerPrefs.DeleteKey(SettingsKeys.SHOW_FPS);
        PlayerPrefs.DeleteKey(SettingsKeys.SHOW_NETWORK_STATUS);
        PlayerPrefs.DeleteKey(SettingsKeys.SHOW_LEVEL_PROGRESSION);
        PlayerPrefs.DeleteKey(SettingsKeys.SHOW_KILL_FEED);
        PlayerPrefs.DeleteKey(SettingsKeys.SIGHT_RETICLE_SIZE);

        PlayerPrefs.DeleteKey(SettingsKeys.ENEMY_INDICATOR_OPACITY);
        PlayerPrefs.DeleteKey(SettingsKeys.ALLY_INDICATOR_OPACITY);
        PlayerPrefs.DeleteKey(SettingsKeys.SQUAD_INDICATOR_OPACITY);
        PlayerPrefs.DeleteKey(SettingsKeys.ENEMY_INDICATOR_AIM_OPACITY);
        PlayerPrefs.DeleteKey(SettingsKeys.ALLY_INDICATOR_AIM_OPACITY);
        PlayerPrefs.DeleteKey(SettingsKeys.SQUAD_INDICATOR_AIM_OPACITY);

        PlayerPrefs.DeleteKey(SettingsKeys.ENEMY_FLAG_OPACITY);
        PlayerPrefs.DeleteKey(SettingsKeys.ALLY_FLAG_OPACITY);
        PlayerPrefs.DeleteKey(SettingsKeys.ENEMY_FLAG_AIM_OPACITY);
        PlayerPrefs.DeleteKey(SettingsKeys.ALLY_FLAG_AIM_OPACITY);

        PlayerPrefs.DeleteKey(SettingsKeys.SHOW_CHAT);
        PlayerPrefs.DeleteKey(SettingsKeys.CHAT_OPACITY);
        PlayerPrefs.DeleteKey(SettingsKeys.CHAT_SIZE);

        // Cores
        PlayerPrefs.DeleteKey("SightReticleColor");
        PlayerPrefs.DeleteKey("EnemyColor");
        PlayerPrefs.DeleteKey("AllyColor");
        PlayerPrefs.DeleteKey("SquadColor");
        PlayerPrefs.DeleteKey("EnemyFlagColor");
        PlayerPrefs.DeleteKey("AllyFlagColor");
        PlayerPrefs.DeleteKey("SquadFlagColor");
        PlayerPrefs.DeleteKey("BodyShotColor");
        PlayerPrefs.DeleteKey("HeadShotColor");
        PlayerPrefs.DeleteKey("VehicleMarkerColor");
    }

    private void LoadColors()
    {
        // Carregar cores salvas de Gameplay
        string bodyShotColor = PlayerPrefs.GetString("BodyShotColor", "");
        if (!string.IsNullOrEmpty(bodyShotColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + bodyShotColor, out Color color))
            {
                Settings.Instance._gameplay.body_shot_marker_colour = color;
            }
        }

        string headShotColor = PlayerPrefs.GetString("HeadShotColor", "");
        if (!string.IsNullOrEmpty(headShotColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + headShotColor, out Color color))
            {
                Settings.Instance._gameplay.head_shot_marker_colour = color;
            }
        }

        string vehicleMarkerColor = PlayerPrefs.GetString("VehicleMarkerColor", "");
        if (!string.IsNullOrEmpty(vehicleMarkerColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + vehicleMarkerColor, out Color color))
            {
                Settings.Instance._gameplay.vehicle_marker_colour = color;
            }
        }

        string sightReticleColor = PlayerPrefs.GetString("SightReticleColor", "");
        if (!string.IsNullOrEmpty(sightReticleColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + sightReticleColor, out Color color))
            {
                Settings.Instance._gameplay.sight_reticle_collor = color;
            }
        }

        string enemyColor = PlayerPrefs.GetString("EnemyColor", "");
        if (!string.IsNullOrEmpty(enemyColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + enemyColor, out Color color))
            {
                Settings.Instance._gameplay.enemy_color = color;
            }
        }

        string allyColor = PlayerPrefs.GetString("AllyColor", "");
        if (!string.IsNullOrEmpty(allyColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + allyColor, out Color color))
            {
                Settings.Instance._gameplay.ally_color = color;
            }
        }

        string squadColor = PlayerPrefs.GetString("SquadColor", "");
        if (!string.IsNullOrEmpty(squadColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + squadColor, out Color color))
            {
                Settings.Instance._gameplay.squad_color = color;
            }
        }

        string enemyFlagColor = PlayerPrefs.GetString("EnemyFlagColor", "");
        if (!string.IsNullOrEmpty(enemyFlagColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + enemyFlagColor, out Color color))
            {
                Settings.Instance._gameplay.enemy_color_flag = color;
            }
        }

        string allyFlagColor = PlayerPrefs.GetString("AllyFlagColor", "");
        if (!string.IsNullOrEmpty(allyFlagColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + allyFlagColor, out Color color))
            {
                Settings.Instance._gameplay.ally_color_flag = color;
            }
        }

        string squadFlagColor = PlayerPrefs.GetString("SquadFlagColor", "");
        if (!string.IsNullOrEmpty(squadFlagColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + squadFlagColor, out Color color))
            {
                Settings.Instance._gameplay.squad_color_flag = color;
            }
        }

        // Opcional: Carregar outras cores se necessário
        string uiTextColor = PlayerPrefs.GetString("UITextColor", "");
        if (!string.IsNullOrEmpty(uiTextColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + uiTextColor, out Color color))
            {
                // Se você tiver variável para cor de texto da UI
                // Settings.Instance._gameplay.ui_text_color = color;
            }
        }

        string uiBackgroundColor = PlayerPrefs.GetString("UIBackgroundColor", "");
        if (!string.IsNullOrEmpty(uiBackgroundColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + uiBackgroundColor, out Color color))
            {
                // Se você tiver variável para cor de fundo da UI
                // Settings.Instance._gameplay.ui_background_color = color;
            }
        }

        string crosshairColor = PlayerPrefs.GetString("CrosshairColor", "");
        if (!string.IsNullOrEmpty(crosshairColor))
        {
            if (ColorUtility.TryParseHtmlString("#" + crosshairColor, out Color color))
            {
                // Se você tiver variável para cor da mira
                // Settings.Instance._gameplay.crosshair_color = color;
            }
        }
    }

    private void ResetVideoSettings()
    {
        PlayerPrefs.DeleteKey(SettingsKeys.GRAPHIC_PRESET);
        PlayerPrefs.DeleteKey(SettingsKeys.RENDER_DISTANCE);
        PlayerPrefs.DeleteKey(SettingsKeys.ENABLE_SHADOWS);
        PlayerPrefs.DeleteKey(SettingsKeys.SHADOWS_QUALITY);
        PlayerPrefs.DeleteKey(SettingsKeys.MESHES_QUALITY);
        PlayerPrefs.DeleteKey(SettingsKeys.RAIN_QUALITY);

        PlayerPrefs.DeleteKey(SettingsKeys.LIMIT_FPS);
        PlayerPrefs.DeleteKey(SettingsKeys.MAX_FPS);
        PlayerPrefs.DeleteKey(SettingsKeys.VSYNC);
        PlayerPrefs.DeleteKey(SettingsKeys.BRIGHTNESS);
        PlayerPrefs.DeleteKey(SettingsKeys.RENDER_SCALE);
        PlayerPrefs.DeleteKey(SettingsKeys.CUSTOM_RESOLUTION);
        PlayerPrefs.DeleteKey(SettingsKeys.RESOLUTION_WIDTH);
        PlayerPrefs.DeleteKey(SettingsKeys.RESOLUTION_HEIGHT);
        PlayerPrefs.DeleteKey(SettingsKeys.SCREEN_MODE);

        PlayerPrefs.DeleteKey(SettingsKeys.INFANTRY_FOV);
        PlayerPrefs.DeleteKey(SettingsKeys.JET_FOV);
        PlayerPrefs.DeleteKey(SettingsKeys.TANK_FOV);
        PlayerPrefs.DeleteKey(SettingsKeys.HELICOPTER_FOV);
        PlayerPrefs.DeleteKey(SettingsKeys.CAMERA_SHAKE_INTENSITY);
        PlayerPrefs.DeleteKey(SettingsKeys.VIGNETTE);
        PlayerPrefs.DeleteKey(SettingsKeys.MOTION_BLUR);
    }
    #endregion


    #region Key Binds
    // Variáveis para controle do rebind
    private bool isWaitingForKey = false;
    private string currentRebindingAction = "";
    private KeyCode previousKeyCode; // Adicionar esta variável
    private Dictionary<string, TextMeshProUGUI> keyButtonMap;

    private void InitializeKeyButtonMap()
    {
        keyButtonMap = new Dictionary<string, TextMeshProUGUI>
        {
            // Player
            { "PLAYER_moveFowardKey", PLAYER_moveFowardButton },
            { "PLAYER_moveBackwardsdKey", PLAYER_moveBackwardsButton },
            { "PLAYER_moveLeftKey", PLAYER_moveLeftButton },
            { "PLAYER_moveRightKey", PLAYER_moveRightButton },
            { "PLAYER_jumpKey", PLAYER_jumpButton },
            { "PLAYER_interactKey", PLAYER_interactButton },
            { "PLAYER_sprintKey", PLAYER_sprintButton },
            { "PLAYER_crouchKey", PLAYER_crouchButton },
            { "PLAYER_proneKey", PLAYER_proneButton },
            { "PLAYER_leanLeftKey", PLAYER_leanLeftButton },
            { "PLAYER_leanRightKey", PLAYER_leanRightButton },
            { "PLAYER_rollKey", PLAYER_rollButton },

            // Weapons
            { "WEAPON_composeBulletsKey", WEAPON_composeBulletsButton },
            { "WEAPON_activateSideGrip", WEAPON_activateSideGripButton },
            { "WEAPON_shootKey", WEAPON_shootButton },
            { "WEAPON_reloadKey", WEAPON_reloadButton },
            { "WEAPON_aimKey", WEAPON_aimButton },
            { "WEAPON_switchFireModeKey", WEAPON_switchFireModeButton },
            { "WEAPON_weapon1Key", WEAPON_weapon1Button },
            { "WEAPON_weapon2Key", WEAPON_weapon2Button },

            // Gadget
            { "GADGET_gadget1Key", GADGET_gadget1Button },
            { "GADGET_gadget2Key", GADGET_gadget2Button },
            { "GADGET_throwGrenadeKey", GADGET_throwGrenadeButton },
            { "GADGET_throwC4Key", GADGET_throwC4Button },
            { "GADGET_detonateC4Key", GADGET_detonateC4Button },

            // Vehicle
            { "VEHICLE_startEngineKey", VEHICLE_startEngineButton },
            { "VEHICLE_freeLookKey", VEHICLE_freeLookButton },

            // Jet
            { "JET_boostKey", JET_boostButton },
            { "JET_shootVehicleKey", JET_shootVehicleButton },
            { "JET_pitchUpKey", JET_pitchUpButton },
            { "JET_pitchDownKey", JET_pitchDownButton },
            { "JET_yawLeftKey", JET_yawLeftButton },
            { "JET_yawRightKey", JET_yawRightButton },
            { "JET_speedUpKey", JET_speedUpButton },
            { "JET_speedDownKey", JET_speedDownButton },

            // Helicopter
            { "HELICOPTER_increase_throtlle", HELICOPTER_increaseThrottleButton },
            { "HELICOPTER_decrease_throtlle", HELICOPTER_decreaseThrottleButton },
            { "HELICOPTER_switch_camera_key", HELICOPTER_switchCameraButton },
            { "HELICOPTER_main_cannon_key", HELICOPTER_mainCannonButton },
            { "HELICOPTER_upgrade_gun_key", HELICOPTER_upgradeGunButton },
            { "HELICOPTER_shoot_key", HELICOPTER_shootButton },
            { "HELICOPTER_pitch_up_key", HELICOPTER_pitchUpButton },
            { "HELICOPTER_pitch_down_key", HELICOPTER_pitchDownButton },
            { "HELICOPTER_lean_left_key", HELICOPTER_leanLeftButton },
            { "HELICOPTER_lean_right_key", HELICOPTER_leanRightButton },
            { "HELICOPTER_zoom_key", HELICOPTER_zoomButton },
            { "HELICOPTER_gunner_seat_key", HELICOPTER_gunnerSeatButton },
            { "HELICOPTER_pilot_seat_key", HELICOPTER_pilotSeatButton }
        };
    }

    private void LoadAllKeybindsToUI()
    {
        var keyBindsType = Settings.Instance._keybinds.GetType();
        var fields = keyBindsType.GetFields();

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(KeyCode))
            {
                string actionName = field.Name;
                string prefKey = $"KeyBind_{actionName}";

                if (PlayerPrefs.HasKey(prefKey))
                {
                    string savedKey = PlayerPrefs.GetString(prefKey);
                    if (Enum.TryParse<KeyCode>(savedKey, out KeyCode loadedKey))
                    {
                        // Atualizar no script KeyBinds
                        field.SetValue(Settings.Instance._keybinds, loadedKey);

                        // Atualizar na UI
                        if (keyButtonMap.ContainsKey(actionName) && keyButtonMap[actionName] != null)
                        {
                            keyButtonMap[actionName].text = loadedKey.ToString();
                        }
                    }
                }
                else
                {
                    // Mostrar valor padrão na UI
                    KeyCode defaultKey = (KeyCode)field.GetValue(Settings.Instance._keybinds);
                    if (keyButtonMap.ContainsKey(actionName) && keyButtonMap[actionName] != null)
                    {
                        keyButtonMap[actionName].text = defaultKey.ToString();
                    }
                }
            }
        }
    }

    public void StartRebindPlayerMoveForward() => StartRebinding("PLAYER_moveFowardKey");
    public void StartRebindPlayerMoveBackwards() => StartRebinding("PLAYER_moveBackwardsdKey");
    public void StartRebindPlayerMoveLeft() => StartRebinding("PLAYER_moveLeftKey");
    public void StartRebindPlayerMoveRight() => StartRebinding("PLAYER_moveRightKey");
    public void StartRebindPlayerJump() => StartRebinding("PLAYER_jumpKey");
    public void StartRebindPlayerInteract() => StartRebinding("PLAYER_interactKey");
    public void StartRebindPlayerSprint() => StartRebinding("PLAYER_sprintKey");
    public void StartRebindPlayerCrouch() => StartRebinding("PLAYER_crouchKey");
    public void StartRebindPlayerProne() => StartRebinding("PLAYER_proneKey");
    public void StartRebindPlayerLeanLeft() => StartRebinding("PLAYER_leanLeftKey");
    public void StartRebindPlayerLeanRight() => StartRebinding("PLAYER_leanRightKey");
    public void StartRebindPlayerRoll() => StartRebinding("PLAYER_rollKey");

    // Weapons
    public void StartRebindWeaponComposeBullets() => StartRebinding("WEAPON_composeBulletsKey");
    public void StartRebindWeaponActivateSideGrip() => StartRebinding("WEAPON_activateSideGrip");
    public void StartRebindWeaponShoot() => StartRebinding("WEAPON_shootKey");
    public void StartRebindWeaponReload() => StartRebinding("WEAPON_reloadKey");
    public void StartRebindWeaponAim() => StartRebinding("WEAPON_aimKey");
    public void StartRebindWeaponSwitchFireMode() => StartRebinding("WEAPON_switchFireModeKey");
    public void StartRebindWeapon1() => StartRebinding("WEAPON_weapon1Key");
    public void StartRebindWeapon2() => StartRebinding("WEAPON_weapon2Key");

    // Gadget
    public void StartRebindGadget1() => StartRebinding("GADGET_gadget1Key");
    public void StartRebindGadget2() => StartRebinding("GADGET_gadget2Key");
    public void StartRebindGadgetThrowGrenade() => StartRebinding("GADGET_throwGrenadeKey");
    public void StartRebindGadgetThrowC4() => StartRebinding("GADGET_throwC4Key");
    public void StartRebindGadgetDetonateC4() => StartRebinding("GADGET_detonateC4Key");

    // Vehicle
    public void StartRebindVehicleStartEngine() => StartRebinding("VEHICLE_startEngineKey");
    public void StartRebindVehicleFreeLook() => StartRebinding("VEHICLE_freeLookKey");

    // Jet
    public void StartRebindJetBoost() => StartRebinding("JET_boostKey");
    public void StartRebindJetShootVehicle() => StartRebinding("JET_shootVehicleKey");
    public void StartRebindJetPitchUp() => StartRebinding("JET_pitchUpKey");
    public void StartRebindJetPitchDown() => StartRebinding("JET_pitchDownKey");
    public void StartRebindJetYawLeft() => StartRebinding("JET_yawLeftKey");
    public void StartRebindJetYawRight() => StartRebinding("JET_yawRightKey");
    public void StartRebindJetSpeedUp() => StartRebinding("JET_speedUpKey");
    public void StartRebindJetSpeedDown() => StartRebinding("JET_speedDownKey");

    // Helicopter
    public void StartRebindHelicopterIncreaseThrottle() => StartRebinding("HELICOPTER_increase_throtlle");
    public void StartRebindHelicopterDecreaseThrottle() => StartRebinding("HELICOPTER_decrease_throtlle");
    public void StartRebindHelicopterSwitchCamera() => StartRebinding("HELICOPTER_switch_camera_key");
    public void StartRebindHelicopterMainCannon() => StartRebinding("HELICOPTER_main_cannon_key");
    public void StartRebindHelicopterUpgradeGun() => StartRebinding("HELICOPTER_upgrade_gun_key");
    public void StartRebindHelicopterShoot() => StartRebinding("HELICOPTER_shoot_key");
    public void StartRebindHelicopterPitchUp() => StartRebinding("HELICOPTER_pitch_up_key");
    public void StartRebindHelicopterPitchDown() => StartRebinding("HELICOPTER_pitch_down_key");
    public void StartRebindHelicopterLeanLeft() => StartRebinding("HELICOPTER_lean_left_key");
    public void StartRebindHelicopterLeanRight() => StartRebinding("HELICOPTER_lean_right_key");
    public void StartRebindHelicopterZoom() => StartRebinding("HELICOPTER_zoom_key");
    public void StartRebindHelicopterGunnerSeat() => StartRebinding("HELICOPTER_gunner_seat_key");
    public void StartRebindHelicopterPilotSeat() => StartRebinding("HELICOPTER_pilot_seat_key");

    private void StartRebinding(string actionName)
    {
        if (isWaitingForKey) return;

        currentRebindingAction = actionName;
        isWaitingForKey = true;

        // Salvar a tecla atual como anterior
        previousKeyCode = GetCurrentKey(actionName);

        // Atualizar o texto do botão para indicar que está esperando entrada
        if (keyButtonMap.ContainsKey(actionName) && keyButtonMap[actionName] != null)
        {
            keyButtonMap[actionName].text = "Press any key";
        }
    }

    private bool IsKeyAlreadyUsed(KeyCode key, string currentAction)
    {

        // Obter todas as teclas atuais do KeyBinds
        var keyBindsType = Settings.Instance._keybinds.GetType();
        var fields = keyBindsType.GetFields();

        foreach (var field in fields)
        {
            // Ignorar a ação atual
            if (field.Name == currentAction) continue;

            if (field.FieldType == typeof(KeyCode))
            {
                KeyCode existingKey = (KeyCode)field.GetValue(Settings.Instance._keybinds);
                if (existingKey == key)
                {

                    return true;
                }
            }
        }

        return false;
    }

    private KeyCode GetCurrentKey(string actionName)
    {
        var field = Settings.Instance._keybinds.GetType().GetField(actionName);
        if (field != null && field.FieldType == typeof(KeyCode))
        {
            return (KeyCode)field.GetValue(Settings.Instance._keybinds);
        }
        return KeyCode.None;
    }

    private void SaveKeybind(string actionName, KeyCode keyCode)
    {
        PlayerPrefs.SetString($"KeyBind_{actionName}", keyCode.ToString());
        PlayerPrefs.Save();
    }

    public void ResetAllKeybindsToDefault()
    {
        // Definir valores padrão no KeyBinds
        var keyBindsType = Settings.Instance._keybinds.GetType();
        var fields = keyBindsType.GetFields();

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(KeyCode))
            {
                string actionName = field.Name;

                // Obter valor padrão baseado no nome
                KeyCode defaultValue = GetDefaultKey(actionName);
                field.SetValue(Settings.Instance._keybinds, defaultValue);

                // Atualizar na UI
                if (keyButtonMap.ContainsKey(actionName) && keyButtonMap[actionName] != null)
                {
                    keyButtonMap[actionName].text = defaultValue.ToString();
                }

                // Salvar
                SaveKeybind(actionName, defaultValue);
            }
        }

        //InitializeUIValues();
        //PlayerPrefs.Save();
        Debug.Log("All keybinds reset to default");
    }

    private KeyCode GetDefaultKey(string actionName)
    {
        // Retornar valores padrão baseados no seu KeyBinds.cs original
        switch (actionName)
        {
            // Player
            case "PLAYER_moveFowardKey": return KeyCode.W;
            case "PLAYER_moveBackwardsdKey": return KeyCode.S;
            case "PLAYER_moveLeftKey": return KeyCode.A;
            case "PLAYER_moveRightKey": return KeyCode.D;
            case "PLAYER_jumpKey": return KeyCode.Space;
            case "PLAYER_interactKey": return KeyCode.F;
            case "PLAYER_sprintKey": return KeyCode.LeftControl;
            case "PLAYER_crouchKey": return KeyCode.LeftShift;
            case "PLAYER_proneKey": return KeyCode.C;
            case "PLAYER_leanLeftKey": return KeyCode.Q;
            case "PLAYER_leanRightKey": return KeyCode.E;
            case "PLAYER_rollKey": return KeyCode.Z;

            // Weapons
            case "WEAPON_composeBulletsKey": return KeyCode.P;
            case "WEAPON_activateSideGrip": return KeyCode.T;
            case "WEAPON_shootKey": return KeyCode.Mouse0;
            case "WEAPON_reloadKey": return KeyCode.R;
            case "WEAPON_aimKey": return KeyCode.Mouse1;
            case "WEAPON_switchFireModeKey": return KeyCode.X;
            case "WEAPON_weapon1Key": return KeyCode.Alpha1;
            case "WEAPON_weapon2Key": return KeyCode.Alpha2;

            // Gadget
            case "GADGET_gadget1Key": return KeyCode.Alpha3;
            case "GADGET_gadget2Key": return KeyCode.Alpha3;
            case "GADGET_throwGrenadeKey": return KeyCode.G;
            case "GADGET_throwC4Key": return KeyCode.Mouse1;
            case "GADGET_detonateC4Key": return KeyCode.Mouse0;

            // Vehicle
            case "VEHICLE_startEngineKey": return KeyCode.E;
            case "VEHICLE_freeLookKey": return KeyCode.Mouse2;

            // Jet
            case "JET_boostKey": return KeyCode.LeftControl;
            case "JET_pitchUpKey": return KeyCode.Space;
            case "JET_pitchDownKey": return KeyCode.LeftShift;
            case "JET_speedUpKey": return KeyCode.W;
            case "JET_speedDownKey": return KeyCode.S;

            // Helicopter
            case "HELICOPTER_increase_throtlle": return KeyCode.W;
            case "HELICOPTER_decrease_throtlle": return KeyCode.S;
            case "HELICOPTER_switch_camera_key": return KeyCode.C;
            case "HELICOPTER_main_cannon_key": return KeyCode.Alpha1;
            case "HELICOPTER_upgrade_gun_key": return KeyCode.Alpha2;
            case "HELICOPTER_shoot_key": return KeyCode.Mouse0;
            case "HELICOPTER_pitch_up_key": return KeyCode.Space;
            case "HELICOPTER_pitch_down_key": return KeyCode.LeftShift;
            case "HELICOPTER_lean_left_key": return KeyCode.A;
            case "HELICOPTER_lean_right_key": return KeyCode.D;
            case "HELICOPTER_zoom_key": return KeyCode.Mouse1;
            case "HELICOPTER_gunner_seat_key": return KeyCode.F1;
            case "HELICOPTER_pilot_seat_key": return KeyCode.F2;

            default: return KeyCode.None;
        }
    }


    public void SaveAllKeybinds()
    {
        var keyBindsType = Settings.Instance._keybinds.GetType();
        var fields = keyBindsType.GetFields();

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(KeyCode))
            {
                KeyCode currentKey = (KeyCode)field.GetValue(Settings.Instance._keybinds);
                SaveKeybind(field.Name, currentKey);
            }
        }

        PlayerPrefs.Save();
        Debug.Log("All keybinds saved");
    }

    KeyCode confirmationKeyCode;

    private void ProcessKeyInput()
    {
        // Verificar todas as teclas
        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(keyCode))
            {
                if (keyCode == KeyCode.Mouse0 && is_mouse_over_close_error_message_button)
                {
                    CancelRebind();
                    return;
                }
                else
                {
                    // Ignorar ESC (já tratado)
                    if (keyCode == KeyCode.Escape) continue;

                    // Verificar se a tecla já está em uso
                    if (IsKeyAlreadyUsed(keyCode, currentRebindingAction))
                    {
                        if (keyCode == confirmationKeyCode)
                        {
                            CloseErrorMessage();
                        }
                        else
                        {
                            // Iniciar processo de confirmação
                            show_message_error = true;
                            message_error = $"Key {keyCode} is already in use!\nPress it again to confirm";
                            confirmationKeyCode = keyCode;

                            // Aqui NÃO aplicamos a tecla, apenas mostramos a mensagem
                            return;
                        }
                    }

                    // Aplicar a nova tecla
                    ApplyKeybind(currentRebindingAction, keyCode);
                    isWaitingForKey = false;
                    SaveKeybind(currentRebindingAction, keyCode);
                    return;
                }
            }
        }
    }

    private void CancelRebind()
    {
        // Se estava mostrando mensagem de erro, restaurar tecla anterior
        if (show_message_error)
        {
            RestorePreviousKey();
        }

        isWaitingForKey = false;
        show_message_error = false; // Garantir que a mensagem seja fechada

        // Restaurar o texto original do botão
        if (!string.IsNullOrEmpty(currentRebindingAction) &&
            keyButtonMap.ContainsKey(currentRebindingAction) &&
            keyButtonMap[currentRebindingAction] != null)
        {
            // Recarregar a tecla atual para mostrar
            KeyCode currentKey = GetCurrentKey(currentRebindingAction);
            keyButtonMap[currentRebindingAction].text = currentKey.ToString();
        }

        //Debug.Log("Key rebinding cancelled");
    }

    private void RestorePreviousKey()
    {
        if (string.IsNullOrEmpty(currentRebindingAction) || previousKeyCode == KeyCode.None)
            return;

        // Restaurar a tecla anterior
        ApplyKeybind(currentRebindingAction, previousKeyCode);

        // Atualizar a UI
        if (keyButtonMap.ContainsKey(currentRebindingAction) && keyButtonMap[currentRebindingAction] != null)
        {
            keyButtonMap[currentRebindingAction].text = previousKeyCode.ToString();
        }

        Debug.Log($"Restored previous key: {currentRebindingAction} = {previousKeyCode}");
    }


    private void ApplyKeybind(string actionName, KeyCode keyCode)
    {
        // Atualizar no script KeyBinds
        switch (actionName)
        {
            // Player
            case "PLAYER_moveFowardKey": Settings.Instance._keybinds.PLAYER_moveFowardKey = keyCode; break;
            case "PLAYER_moveBackwardsdKey": Settings.Instance._keybinds.PLAYER_moveBackwardsdKey = keyCode; break;
            case "PLAYER_moveLeftKey": Settings.Instance._keybinds.PLAYER_moveLeftKey = keyCode; break;
            case "PLAYER_moveRightKey": Settings.Instance._keybinds.PLAYER_moveRightKey = keyCode; break;
            case "PLAYER_jumpKey": Settings.Instance._keybinds.PLAYER_jumpKey = keyCode; break;
            case "PLAYER_interactKey": Settings.Instance._keybinds.PLAYER_interactKey = keyCode; break;
            case "PLAYER_sprintKey": Settings.Instance._keybinds.PLAYER_sprintKey = keyCode; break;
            case "PLAYER_crouchKey": Settings.Instance._keybinds.PLAYER_crouchKey = keyCode; break;
            case "PLAYER_proneKey": Settings.Instance._keybinds.PLAYER_proneKey = keyCode; break;
            case "PLAYER_leanLeftKey": Settings.Instance._keybinds.PLAYER_leanLeftKey = keyCode; break;
            case "PLAYER_leanRightKey": Settings.Instance._keybinds.PLAYER_leanRightKey = keyCode; break;
            case "PLAYER_rollKey": Settings.Instance._keybinds.PLAYER_rollKey = keyCode; break;

            // Weapons
            case "WEAPON_composeBulletsKey": Settings.Instance._keybinds.WEAPON_composeBulletsKey = keyCode; break;
            case "WEAPON_activateSideGrip": Settings.Instance._keybinds.WEAPON_activateSideGrip = keyCode; break;
            case "WEAPON_shootKey": Settings.Instance._keybinds.WEAPON_shootKey = keyCode; break;
            case "WEAPON_reloadKey": Settings.Instance._keybinds.WEAPON_reloadKey = keyCode; break;
            case "WEAPON_aimKey": Settings.Instance._keybinds.WEAPON_aimKey = keyCode; break;
            case "WEAPON_switchFireModeKey": Settings.Instance._keybinds.WEAPON_switchFireModeKey = keyCode; break;
            case "WEAPON_weapon1Key": Settings.Instance._keybinds.WEAPON_weapon1Key = keyCode; break;
            case "WEAPON_weapon2Key": Settings.Instance._keybinds.WEAPON_weapon2Key = keyCode; break;

            // Gadget
            case "GADGET_gadget1Key": Settings.Instance._keybinds.GADGET_gadget1Key = keyCode; break;
            case "GADGET_gadget2Key": Settings.Instance._keybinds.GADGET_gadget2Key = keyCode; break;
            case "GADGET_throwGrenadeKey": Settings.Instance._keybinds.GADGET_throwGrenadeKey = keyCode; break;
            case "GADGET_throwC4Key": Settings.Instance._keybinds.GADGET_throwC4Key = keyCode; break;
            case "GADGET_detonateC4Key": Settings.Instance._keybinds.GADGET_detonateC4Key = keyCode; break;

            // Vehicle
            case "VEHICLE_startEngineKey": Settings.Instance._keybinds.VEHICLE_startEngineKey = keyCode; break;
            case "VEHICLE_freeLookKey": Settings.Instance._keybinds.VEHICLE_freeLookKey = keyCode; break;

            // Jet
            case "JET_boostKey": Settings.Instance._keybinds.JET_boostKey = keyCode; break;
            case "JET_shootVehicleKey": Settings.Instance._keybinds.JET_shootVehicleKey = keyCode; break;
            case "JET_pitchUpKey": Settings.Instance._keybinds.JET_pitchUpKey = keyCode; break;
            case "JET_pitchDownKey": Settings.Instance._keybinds.JET_pitchDownKey = keyCode; break;
            case "JET_yawLeftKey": Settings.Instance._keybinds.JET_yawLeftKey = keyCode; break;
            case "JET_yawRightKey": Settings.Instance._keybinds.JET_yawRightKey = keyCode; break;
            case "JET_speedUpKey": Settings.Instance._keybinds.JET_speedUpKey = keyCode; break;
            case "JET_speedDownKey": Settings.Instance._keybinds.JET_speedDownKey = keyCode; break;

            // Helicopter
            case "HELICOPTER_increase_throtlle": Settings.Instance._keybinds.HELICOPTER_increase_throtlle = keyCode; break;
            case "HELICOPTER_decrease_throtlle": Settings.Instance._keybinds.HELICOPTER_decrease_throtlle = keyCode; break;
            case "HELICOPTER_switch_camera_key": Settings.Instance._keybinds.HELICOPTER_switch_camera_key = keyCode; break;
            case "HELICOPTER_main_cannon_key": Settings.Instance._keybinds.HELICOPTER_main_cannon_key = keyCode; break;
            case "HELICOPTER_upgrade_gun_key": Settings.Instance._keybinds.HELICOPTER_upgrade_gun_key = keyCode; break;
            case "HELICOPTER_shoot_key": Settings.Instance._keybinds.HELICOPTER_shoot_key = keyCode; break;
            case "HELICOPTER_pitch_up_key": Settings.Instance._keybinds.HELICOPTER_pitch_up_key = keyCode; break;
            case "HELICOPTER_pitch_down_key": Settings.Instance._keybinds.HELICOPTER_pitch_down_key = keyCode; break;
            case "HELICOPTER_lean_left_key": Settings.Instance._keybinds.HELICOPTER_lean_left_key = keyCode; break;
            case "HELICOPTER_lean_right_key": Settings.Instance._keybinds.HELICOPTER_lean_right_key = keyCode; break;
            case "HELICOPTER_zoom_key": Settings.Instance._keybinds.HELICOPTER_zoom_key = keyCode; break;
            case "HELICOPTER_gunner_seat_key": Settings.Instance._keybinds.HELICOPTER_gunner_seat_key = keyCode; break;
            case "HELICOPTER_pilot_seat_key": Settings.Instance._keybinds.HELICOPTER_pilot_seat_key = keyCode; break;
        }

        // Atualizar a UI
        if (keyButtonMap.ContainsKey(actionName) && keyButtonMap[actionName] != null)
        {
            keyButtonMap[actionName].text = keyCode.ToString();
        }

        Debug.Log($"Keybind updated: {actionName} = {keyCode}");
    }

    #endregion

    void IsMouseOverButton()
    {
        if (close_image_error_button == null)
        {
            is_mouse_over_close_error_message_button = false;
            return; // Adicione este return para sair do método
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Inicializar como false
        is_mouse_over_close_error_message_button = false;

        foreach (var result in results)
        {
            // Corrigir: acessar gameObject do button
            if (result.gameObject == close_image_error_button.gameObject)
            {
                is_mouse_over_close_error_message_button = true;
                break; // Se encontrou, pode sair do loop
            }
        }
    }

}

[Serializable]
public class SettingsKeys
{
    // Audio Keys
    public const string GENERAL_VOLUME = "GeneralVolume";
    public const string VOIP_VOLUME = "VoipVolume";
    public const string MUSIC_VOLUME = "MusicVolume";
    public const string WORLD_VOLUME = "WorldVolume";
    public const string HIT_VOLUME = "HitVolume";
    public const string KILL_VOLUME = "KillVolume";
    public const string VEHICLE_VOLUME = "VehicleVolume";
    public const string INFANTRY_VOLUME = "InfantryVolume";
    public const string MICROPHONE_VOLUME = "MicrophoneVolume";
    public const string ENABLE_DEATH_VOIP = "EnableDeathVoip";
    public const string IN_WORLD_VOIP_MODE = "InWorldVoipMode";
    public const string RADIO_VOIP_MODE = "RadioVoipMode";

    // Controls Keys
    public const string AIM_HOLD = "AimHold";
    public const string SPRINT_HOLD = "SprintHold";
    public const string CROUCH_HOLD = "CrouchHold";
    public const string PRONE_HOLD = "ProneHold";
    public const string VEHICLE_BOOST_HOLD = "VehicleBoostHold";

    public const string INVERT_VERTICAL_INFANTRY = "InvertVerticalInfantry";
    public const string INFANTRY_SENSIBILITY = "InfantrySensibility";
    public const string INFANTRY_AIM_SENSIBILITY = "InfantryAimSensibility";

    public const string INVERT_VERTICAL_TANK = "InvertVerticalTank";
    public const string TANK_SENSIBILITY = "TankSensibility";
    public const string TANK_AIM_SENSIBILITY = "TankAimSensibility";

    public const string INVERT_VERTICAL_JET = "InvertVerticalJet";
    public const string JET_SENSIBILITY = "JetSensibility";
    public const string JET_AIM_SENSIBILITY = "JetAimSensibility";

    public const string INVERT_VERTICAL_HELI = "InvertVerticalHeli";
    public const string HELICOPTER_SENSIBILITY = "HelicopterSensibility";
    public const string HELICOPTER_AIM_SENSIBILITY = "HelicopterAimSensibility";

    // Gameplay Keys
    public const string SHOW_HIT_MARKER = "ShowHitMarker";
    public const string HIT_MARKER_OPACITY = "HitMarkerOpacity";
    public const string HIT_MARKER_SIZE = "HitMarkerSize";

    public const string SHOW_FPS = "ShowFps";
    public const string SHOW_NETWORK_STATUS = "ShowNetworkStatus";
    public const string SHOW_LEVEL_PROGRESSION = "ShowLevelProgression";
    public const string SHOW_KILL_FEED = "ShowKillFeed";
    public const string SIGHT_RETICLE_SIZE = "SightReticleSize";

    public const string ENEMY_INDICATOR_OPACITY = "EnemyIndicatorOpacity";
    public const string ALLY_INDICATOR_OPACITY = "AllyIndicatorOpacity";
    public const string SQUAD_INDICATOR_OPACITY = "SquadIndicatorOpacity";
    public const string ENEMY_INDICATOR_AIM_OPACITY = "EnemyIndicatorAimOpacity";
    public const string ALLY_INDICATOR_AIM_OPACITY = "AllyIndicatorAimOpacity";
    public const string SQUAD_INDICATOR_AIM_OPACITY = "SquadIndicatorAimOpacity";

    public const string ENEMY_FLAG_OPACITY = "EnemyFlagOpacity";
    public const string ALLY_FLAG_OPACITY = "AllyFlagOpacity";
    public const string ENEMY_FLAG_AIM_OPACITY = "EnemyFlagAimOpacity";
    public const string ALLY_FLAG_AIM_OPACITY = "AllyFlagAimOpacity";

    public const string SHOW_CHAT = "ShowChat";
    public const string CHAT_OPACITY = "ChatOpacity";
    public const string CHAT_SIZE = "ChatSize";

    // Video Keys
    public const string GRAPHIC_PRESET = "GraphicPreset";
    public const string RENDER_DISTANCE = "RenderDistance";
    public const string ENABLE_SHADOWS = "EnableShadows";
    public const string SHADOWS_QUALITY = "ShadowsQuality";
    public const string MESHES_QUALITY = "MeshesQuality";
    public const string RAIN_QUALITY = "RainQuality";

    public const string LIMIT_FPS = "LimitFps";
    public const string MAX_FPS = "MaxFps";
    public const string VSYNC = "Vsync";
    public const string BRIGHTNESS = "Brightness";
    public const string RENDER_SCALE = "RenderScale";
    public const string CUSTOM_RESOLUTION = "CustomResolution";
    public const string RESOLUTION_WIDTH = "ResolutionWidth";
    public const string RESOLUTION_HEIGHT = "ResolutionHeight";
    public const string SCREEN_MODE = "ScreenMode";

    public const string INFANTRY_FOV = "InfantryFov";
    public const string JET_FOV = "JetFov";
    public const string TANK_FOV = "TankFov";
    public const string HELICOPTER_FOV = "HelicopterFov";
    public const string CAMERA_SHAKE_INTENSITY = "CameraShakeIntensity";
    public const string VIGNETTE = "Vignette";
    public const string MOTION_BLUR = "MotionBlur";
}