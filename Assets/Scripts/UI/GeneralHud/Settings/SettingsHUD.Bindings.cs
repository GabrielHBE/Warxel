using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class SettingsHUD
{
    [Header("Menu")]
    [SerializeField] private GameObject reset_keyBind_button;
    [SerializeField] private GameObject error_image;
    [SerializeField] private Button close_image_error_button;
    [SerializeField] private GameObject settings_menu;
    [SerializeField] private Image background_image;
    [SerializeField] private TextMeshProUGUI tab_title;

    [Header("Tabs")]
    [SerializeField] private GameObject audio_tab;
    [SerializeField] private GameObject controls_tab;
    [SerializeField] private GameObject gameplay_tab;
    [SerializeField] private GameObject keybinds_tab;
    [SerializeField] private GameObject video_tab;

    [Header("UI Controls - Audio")]
    [SerializeField] private Slider generalVolumeSlider;
    [SerializeField] private Slider voipVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider worldVolumeSlider;
    [SerializeField] private Slider environmentVolumeSlider;
    [SerializeField] private Slider hitVolumeSlider;
    [SerializeField] private Slider killVolumeSlider;
    [SerializeField] private Slider radioVoipVolumeSlider;
    [SerializeField] private Toggle enableDeathVoipToggle;
    [SerializeField] private TMP_Dropdown inWorldVoipDropdown;
    [SerializeField] private TMP_Dropdown radioVoipDropdown;

    [Header("UI Controls - Controls")]
    [SerializeField] private Toggle aimHoldToggle;
    [SerializeField] private Toggle sprintHoldToggle;
    [SerializeField] private Toggle crouchHoldToggle;
    [SerializeField] private Toggle proneHoldToggle;
    [SerializeField] private Toggle vehicleBoostHoldToggle;
    [SerializeField] private Toggle invertVerticalInfantryToggle;
    [SerializeField] private Slider infantrySensibilitySlider;
    [SerializeField] private Slider infantryAimSensibilitySlider;
    [SerializeField] private Toggle invertVerticalTankToggle;
    [SerializeField] private Slider tankSensibilitySlider;
    [SerializeField] private Slider tankAimSensibilitySlider;
    [SerializeField] private Toggle invertVerticalJetToggle;
    [SerializeField] private Slider jetSensibilitySlider;
    [SerializeField] private Slider jetAimSensibilitySlider;
    [SerializeField] private Toggle invertVerticalHeliToggle;
    [SerializeField] private Slider helicopterSensibilitySlider;
    [SerializeField] private Slider helicopterAimSensibilitySlider;

    [Header("UI Controls - Gameplay")]
    [SerializeField] private Toggle showHitMarkerToggle;
    [SerializeField] private Slider hitMarkerOpacitySlider;
    [SerializeField] private Slider hitMarkerSizeSlider;
    [SerializeField] private Toggle showFpsToggle;
    [SerializeField] private Toggle showNetworkStatusToggle;
    [SerializeField] private Toggle showLevelProgressionToggle;
    [SerializeField] private Toggle showKillFeedToggle;
    [SerializeField] private Slider sightReticleSizeSlider;
    [SerializeField] private Slider enemyIndicatorOpacitySlider;
    [SerializeField] private Slider allyIndicatorOpacitySlider;
    [SerializeField] private Slider squadIndicatorOpacitySlider;
    [SerializeField] private Slider enemyIndicatorAimOpacitySlider;
    [SerializeField] private Slider allyIndicatorAimOpacitySlider;
    [SerializeField] private Slider squadIndicatorAimOpacitySlider;
    [SerializeField] private Slider neutralIndicatorOpacitySlider;
    [SerializeField] private Slider neutralIndicatorAimOpacitySlider;
    [SerializeField] private Toggle showChatToggle;
    [SerializeField] private Slider chatOpacitySlider;
    [SerializeField] private Slider chatSizeSlider;
    [SerializeField] private TMP_InputField bodyShotColorInput;
    [SerializeField] private TMP_InputField headShotColorInput;
    [SerializeField] private TMP_InputField vehicleMarkerColorInput;
    [SerializeField] private TMP_InputField sightReticleColorInput;
    [SerializeField] private TMP_InputField enemyColorInput;
    [SerializeField] private TMP_InputField allyColorInput;
    [SerializeField] private TMP_InputField squadColorInput;
    [SerializeField] private TMP_InputField neutralColorInput;

    [Header("UI Controls - Color Picker")]
    [SerializeField] private SettingsColorPickerPopup colorPickerPopup;
    [SerializeField] private Image bodyShotColorPreview;
    [SerializeField] private Image headShotColorPreview;
    [SerializeField] private Image vehicleMarkerColorPreview;
    [SerializeField] private Image sightReticleColorPreview;
    [SerializeField] private Image enemyColorPreview;
    [SerializeField] private Image allyColorPreview;
    [SerializeField] private Image squadColorPreview;
    [SerializeField] private Image neutralColorPreview;

    [Header("UI Controls - Video")]
    [SerializeField] private TMP_Dropdown graphicPresetsDropdown;
    [SerializeField] private Slider renderDistanceSlider;
    [SerializeField] private Toggle enableShadowsToggle;
    [SerializeField] private TMP_Dropdown shadowsDropdown;
    [SerializeField] private TMP_Dropdown meshesDropdown;
    [SerializeField] private TMP_Dropdown rainQualityDropdown;
    [SerializeField] private Toggle limitFpsToggle;
    [SerializeField] private TMP_InputField maxFpsInput;
    [SerializeField] private Slider vsyncSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider renderScaleSlider;
    [SerializeField] private Toggle customResolutionToggle;
    [SerializeField] private TMP_InputField resolutionWidthInput;
    [SerializeField] private TMP_InputField resolutionHeightInput;
    [SerializeField] private TMP_Dropdown screenModeDropdown;
    [SerializeField] private Slider infantryFovSlider;
    [SerializeField] private Slider jetFovSlider;
    [SerializeField] private Slider tankFovSlider;
    [SerializeField] private Slider helicopterFovSlider;
    [SerializeField] private Slider cameraShakeIntensitySlider;
    [SerializeField] private Toggle vignetteToggle;
    [SerializeField] private Slider motionBlurSlider;

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
    [SerializeField] private TextMeshProUGUI PLAYER_activateNightVisionButton;
    [SerializeField] private TextMeshProUGUI PLAYER_spotButton;
    [SerializeField] private TextMeshProUGUI PLAYER_holdBreathButton;

    [Header("Key Bind UI Elements - Weapons")]
    [SerializeField] private TextMeshProUGUI WEAPON_composeBulletsButton;
    public TextMeshProUGUI WEAPON_activateSideGripButton;
    [SerializeField] private TextMeshProUGUI WEAPON_shootButton;
    [SerializeField] private TextMeshProUGUI WEAPON_reloadButton;
    [SerializeField] private TextMeshProUGUI WEAPON_aimButton;
    public TextMeshProUGUI WEAPON_switchFireModeButton;
    [SerializeField] private TextMeshProUGUI WEAPON_weapon1Button;
    [SerializeField] private TextMeshProUGUI WEAPON_weapon2Button;
    [SerializeField] private TextMeshProUGUI WEAPON_zoomChangeButton;
    [SerializeField] private TextMeshProUGUI WEAPON_switchSightButton;

    [Header("Key Bind UI Elements - Gadget")]
    [SerializeField] private TextMeshProUGUI GADGET_gadget1Button;
    [SerializeField] private TextMeshProUGUI GADGET_gadget2Button;
    [SerializeField] private TextMeshProUGUI GADGET_throwGrenadeButton;
    [SerializeField] private TextMeshProUGUI GADGET_throwC4Button;
    [SerializeField] private TextMeshProUGUI GADGET_detonateC4Button;

    [Header("Key Bind UI Elements - Vehicle")]
    [SerializeField] private TextMeshProUGUI VEHICLE_startEngineButton;
    [SerializeField] private TextMeshProUGUI VEHICLE_freeLookButton;
    [SerializeField] private TextMeshProUGUI VEHICLE_countermeasureButton;
    [SerializeField] private TextMeshProUGUI VEHICLE_switchFireModeButton;
    [SerializeField] private TextMeshProUGUI VEHICLE_switchSeatButton;
    [SerializeField] private TextMeshProUGUI VEHICLE_weapon1Button;
    [SerializeField] private TextMeshProUGUI VEHICLE_weapon2Button;
    [SerializeField] private TextMeshProUGUI VEHICLE_weapon3Button;
    [SerializeField] private TextMeshProUGUI VEHICLE_weapon4Button;
    [SerializeField] private TextMeshProUGUI VEHICLE_weapon5Button;
    [SerializeField] private TextMeshProUGUI VEHICLE_weapon6Button;
    [SerializeField] private TextMeshProUGUI VEHICLE_weapon7Button;
    [SerializeField] private TextMeshProUGUI VEHICLE_weapon8Button;
    [SerializeField] private TextMeshProUGUI VEHICLE_weapon9Button;

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

    [Header("Key Bind UI Elements - Tank")]
    [SerializeField] private TextMeshProUGUI TANK_increaseThrottleButton;
    [SerializeField] private TextMeshProUGUI TANK_decreaseThrottleButton;
    [SerializeField] private TextMeshProUGUI TANK_turnLeftButton;
    [SerializeField] private TextMeshProUGUI TANK_turnRightButton;
    [SerializeField] private TextMeshProUGUI TANK_shootButton;
    [SerializeField] private TextMeshProUGUI TANK_zoomButton;
    [SerializeField] private TextMeshProUGUI TANK_boostButton;
    [SerializeField] private TextMeshProUGUI TANK_gunnerSeatButton;
    [SerializeField] private TextMeshProUGUI TANK_pilotSeatButton;

    private SettingsMenuController CreateMenuController() => new SettingsMenuController(
        settings_menu,
        reset_keyBind_button,
        tab_title,
        new[] { audio_tab, controls_tab, gameplay_tab, keybinds_tab, video_tab },
        3000f);

    private void CreateSettingsControllers(Settings settings)
    {
        _audioController = new AudioSettingsController(settings._audio, CreateAudioView(), _store);
        _controlsController = new ControlsSettingsController(settings._controls, CreateControlsView(), _store);
        _gameplayController = new GameplaySettingsController(settings._gameplay, CreateGameplayView(), _store);
        _videoController = new VideoSettingsController(settings._video, CreateVideoView(), _store);
        _keybindController = new KeybindSettingsController(settings._keybinds, CreateKeybindView(), _store);

        _settingsSections = new ISettingsSection[]
        {
            _audioController,
            _controlsController,
            _gameplayController,
            _videoController
        };
    }

    private AudioSettingsView CreateAudioView() => new AudioSettingsView
    {
        GeneralVolume = generalVolumeSlider,
        VoipVolume = voipVolumeSlider,
        MusicVolume = musicVolumeSlider,
        WorldVolume = worldVolumeSlider,
        EnvironmentVolume = environmentVolumeSlider,
        HitVolume = hitVolumeSlider,
        KillVolume = killVolumeSlider,
        RadioVoipVolume = radioVoipVolumeSlider,
        EnableDeathVoip = enableDeathVoipToggle,
        InWorldVoipMode = inWorldVoipDropdown,
        RadioVoipMode = radioVoipDropdown
    };

    private ControlsSettingsView CreateControlsView() => new ControlsSettingsView
    {
        AimHold = aimHoldToggle,
        SprintHold = sprintHoldToggle,
        CrouchHold = crouchHoldToggle,
        ProneHold = proneHoldToggle,
        VehicleBoostHold = vehicleBoostHoldToggle,
        InvertVerticalInfantry = invertVerticalInfantryToggle,
        InfantrySensitivity = infantrySensibilitySlider,
        InfantryAimSensitivity = infantryAimSensibilitySlider,
        InvertVerticalTank = invertVerticalTankToggle,
        TankSensitivity = tankSensibilitySlider,
        TankAimSensitivity = tankAimSensibilitySlider,
        InvertVerticalJet = invertVerticalJetToggle,
        JetSensitivity = jetSensibilitySlider,
        JetAimSensitivity = jetAimSensibilitySlider,
        InvertVerticalHelicopter = invertVerticalHeliToggle,
        HelicopterSensitivity = helicopterSensibilitySlider,
        HelicopterAimSensitivity = helicopterAimSensibilitySlider
    };

    private GameplaySettingsView CreateGameplayView() => new GameplaySettingsView
    {
        ShowHitMarker = showHitMarkerToggle,
        HitMarkerOpacity = hitMarkerOpacitySlider,
        HitMarkerSize = hitMarkerSizeSlider,
        ShowFps = showFpsToggle,
        ShowNetworkStatus = showNetworkStatusToggle,
        ShowLevelProgression = showLevelProgressionToggle,
        ShowKillFeed = showKillFeedToggle,
        SightReticleSize = sightReticleSizeSlider,
        EnemyIndicatorOpacity = enemyIndicatorOpacitySlider,
        AllyIndicatorOpacity = allyIndicatorOpacitySlider,
        SquadIndicatorOpacity = squadIndicatorOpacitySlider,
        EnemyIndicatorAimOpacity = enemyIndicatorAimOpacitySlider,
        AllyIndicatorAimOpacity = allyIndicatorAimOpacitySlider,
        SquadIndicatorAimOpacity = squadIndicatorAimOpacitySlider,
        NeutralIndicatorOpacity = neutralIndicatorOpacitySlider,
        NeutralIndicatorAimOpacity = neutralIndicatorAimOpacitySlider,
        ShowChat = showChatToggle,
        ChatOpacity = chatOpacitySlider,
        ChatSize = chatSizeSlider,
        BodyShotColor = bodyShotColorInput,
        HeadShotColor = headShotColorInput,
        VehicleMarkerColor = vehicleMarkerColorInput,
        SightReticleColor = sightReticleColorInput,
        EnemyColor = enemyColorInput,
        AllyColor = allyColorInput,
        SquadColor = squadColorInput,
        NeutralColor = neutralColorInput
    };

    private VideoSettingsView CreateVideoView() => new VideoSettingsView
    {
        GraphicPreset = graphicPresetsDropdown,
        RenderDistance = renderDistanceSlider,
        EnableShadows = enableShadowsToggle,
        ShadowsQuality = shadowsDropdown,
        MeshesQuality = meshesDropdown,
        RainQuality = rainQualityDropdown,
        LimitFps = limitFpsToggle,
        MaxFps = maxFpsInput,
        Vsync = vsyncSlider,
        Brightness = brightnessSlider,
        RenderScale = renderScaleSlider,
        CustomResolution = customResolutionToggle,
        ResolutionWidth = resolutionWidthInput,
        ResolutionHeight = resolutionHeightInput,
        ScreenMode = screenModeDropdown,
        InfantryFov = infantryFovSlider,
        JetFov = jetFovSlider,
        TankFov = tankFovSlider,
        HelicopterFov = helicopterFovSlider,
        CameraShakeIntensity = cameraShakeIntensitySlider,
        Vignette = vignetteToggle,
        MotionBlur = motionBlurSlider
    };

    private KeybindSettingsView CreateKeybindView() => new KeybindSettingsView
    {
        Buttons = CreateKeyButtonMap(),
        ErrorPanel = error_image,
        CloseErrorButton = close_image_error_button,
        ErrorText = error_image != null
            ? error_image.GetComponentInChildren<TextMeshProUGUI>()
            : null
    };

    private Dictionary<string, TextMeshProUGUI> CreateKeyButtonMap() =>
        new Dictionary<string, TextMeshProUGUI>
        {
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
            { "PLAYER_activateNightNision", PLAYER_activateNightVisionButton },
            { "PLAYER_spotKey", PLAYER_spotButton },
            { "PLAYER_holdBreathKey", PLAYER_holdBreathButton },
            { "WEAPON_composeBulletsKey", WEAPON_composeBulletsButton },
            { "WEAPON_activateSideGrip", WEAPON_activateSideGripButton },
            { "WEAPON_shootKey", WEAPON_shootButton },
            { "WEAPON_reloadKey", WEAPON_reloadButton },
            { "WEAPON_aimKey", WEAPON_aimButton },
            { "WEAPON_switchFireModeKey", WEAPON_switchFireModeButton },
            { "WEAPON_weapon1Key", WEAPON_weapon1Button },
            { "WEAPON_weapon2Key", WEAPON_weapon2Button },
            { "WEAPON_zoomChangeKey", WEAPON_zoomChangeButton },
            { "WEAPON_switchSightKey", WEAPON_switchSightButton },
            { "GADGET_gadget1Key", GADGET_gadget1Button },
            { "GADGET_gadget2Key", GADGET_gadget2Button },
            { "GADGET_throwGrenadeKey", GADGET_throwGrenadeButton },
            { "GADGET_throwC4Key", GADGET_throwC4Button },
            { "GADGET_detonateC4Key", GADGET_detonateC4Button },
            { "VEHICLE_startEngineKey", VEHICLE_startEngineButton },
            { "VEHICLE_freeLookKey", VEHICLE_freeLookButton },
            { "VEHICLE_countermeasureKey", VEHICLE_countermeasureButton },
            { "VEHICLE_switchFireModeKey", VEHICLE_switchFireModeButton },
            { "VEHICLE_switchSeatKey", VEHICLE_switchSeatButton },
            { "VEHICLE_weapon1", VEHICLE_weapon1Button },
            { "VEHICLE_weapon2", VEHICLE_weapon2Button },
            { "VEHICLE_weapon3", VEHICLE_weapon3Button },
            { "VEHICLE_weapon4", VEHICLE_weapon4Button },
            { "VEHICLE_weapon5", VEHICLE_weapon5Button },
            { "VEHICLE_weapon6", VEHICLE_weapon6Button },
            { "VEHICLE_weapon7", VEHICLE_weapon7Button },
            { "VEHICLE_weapon8", VEHICLE_weapon8Button },
            { "VEHICLE_weapon9", VEHICLE_weapon9Button },
            { "JET_boostKey", JET_boostButton },
            { "JET_shootVehicleKey", JET_shootVehicleButton },
            { "JET_pitchUpKey", JET_pitchUpButton },
            { "JET_pitchDownKey", JET_pitchDownButton },
            { "JET_yawLeftKey", JET_yawLeftButton },
            { "JET_yawRightKey", JET_yawRightButton },
            { "JET_speedUpKey", JET_speedUpButton },
            { "JET_speedDownKey", JET_speedDownButton },
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
            { "HELICOPTER_pilot_seat_key", HELICOPTER_pilotSeatButton },
            { "TANK_increase_throtlle", TANK_increaseThrottleButton },
            { "TANK_decrease_throtlle", TANK_decreaseThrottleButton },
            { "TANK_turn_left_key", TANK_turnLeftButton },
            { "TANK_turn_right_key", TANK_turnRightButton },
            { "TANK_shoot_key", TANK_shootButton },
            { "TANK_zoom_key", TANK_zoomButton },
            { "TANK_boostKey", TANK_boostButton },
            { "TANK_gunner_seat_key", TANK_gunnerSeatButton },
            { "TANK_pilot_seat_key", TANK_pilotSeatButton }
        };
}
