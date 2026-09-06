using TMPro;
using UnityEngine;

// Compatibility adapter for UnityEvents already configured in scenes and prefabs.
public partial class SettingsHUD
{
    #region Controls
    public void OnAimHoldChanged() => _controlsController?.SetAimHold();
    public void OnSprintHoldChanged() => _controlsController?.SetSprintHold();
    public void OnCrouchHoldChanged() => _controlsController?.SetCrouchHold();
    public void OnProneHoldChanged() => _controlsController?.SetProneHold();
    public void OnVehicleBoostHoldChanged() => _controlsController?.SetVehicleBoostHold();
    public void OnInvertVerticalInfantryChanged() => _controlsController?.SetInvertVerticalInfantry();
    public void OnInfantrySensibilityChanged(TextMeshProUGUI text) => _controlsController?.SetInfantrySensitivity(text);
    public void OnInfantryAimSensibilityChanged(TextMeshProUGUI text) => _controlsController?.SetInfantryAimSensitivity(text);
    public void OnInvertVerticalTankChanged() => _controlsController?.SetInvertVerticalTank();
    public void OnTankSensibilityChanged(TextMeshProUGUI text) => _controlsController?.SetTankSensitivity(text);
    public void OnTankAimSensibilityChanged(TextMeshProUGUI text) => _controlsController?.SetTankAimSensitivity(text);
    public void OnInvertVerticalJetChanged() => _controlsController?.SetInvertVerticalJet();
    public void OnJetSensibilityChanged(TextMeshProUGUI text) => _controlsController?.SetJetSensitivity(text);
    public void OnJetAimSensibilityChanged(TextMeshProUGUI text) => _controlsController?.SetJetAimSensitivity(text);
    public void OnInvertVerticalHeliChanged() => _controlsController?.SetInvertVerticalHelicopter();
    public void OnHelicopterSensibilityChanged(TextMeshProUGUI text) => _controlsController?.SetHelicopterSensitivity(text);
    public void OnHelicopterAimSensibilityChanged(TextMeshProUGUI text) => _controlsController?.SetHelicopterAimSensitivity(text);
    #endregion

    #region Audio
    public void OnGeneralVolumeChanged(float value) => _audioController?.SetGeneralVolume(value);
    public void OnVoipVolumeChanged(float value) => _audioController?.SetVoipVolume(value);
    public void OnMusicVolumeChanged(float value) => _audioController?.SetMusicVolume(value);
    public void OnWorldVolumeChanged(float value) => _audioController?.SetWorldVolume(value);
    public void OnEnvironmentVolumeChanged(float value) => _audioController?.SetEnvironmentVolume(value);
    public void OnHitVolumeChanged(float value) => _audioController?.SetHitVolume(value);
    public void OnKillVolumeChanged(float value) => _audioController?.SetKillVolume(value);
    public void OnRadioVoipVolumeChanged(float value) => _audioController?.SetRadioVoipVolume(value);
    public void OnEnableDeathVoipChanged() => _audioController?.SetDeathVoipEnabled();
    public void OnInWorldVoipModeChanged(int index) => _audioController?.SetInWorldVoipMode(index);
    public void OnRadioVoipModeChanged(int index) => _audioController?.SetRadioVoipMode(index);
    #endregion

    #region Gameplay
    public void OnShowHitMarkerChanged() => _gameplayController?.SetShowHitMarker();
    public void OnHitMarkerOpacityChanged(TextMeshProUGUI text) => _gameplayController?.SetHitMarkerOpacity(text);
    public void OnHitMarkerSizeChanged(TextMeshProUGUI text) => _gameplayController?.SetHitMarkerSize(text);
    public void OnShowFpsChanged() => _gameplayController?.SetShowFps();
    public void OnShowNetworkStatusChanged() => _gameplayController?.SetShowNetworkStatus();
    public void OnShowLevelProgressionChanged() => _gameplayController?.SetShowLevelProgression();
    public void OnShowKillFeedChanged() => _gameplayController?.SetShowKillFeed();
    public void OnSightReticleSizeChanged(TextMeshProUGUI text) => _gameplayController?.SetSightReticleSize(text);
    public void OnEnemyIndicatorOpacityChanged(float value) => _gameplayController?.SetEnemyIndicatorOpacity(value);
    public void OnAllyIndicatorOpacityChanged(float value) => _gameplayController?.SetAllyIndicatorOpacity(value);
    public void OnSquadIndicatorOpacityChanged(float value) => _gameplayController?.SetSquadIndicatorOpacity(value);
    public void OnEnemyIndicatorAimOpacityChanged(float value) => _gameplayController?.SetEnemyIndicatorAimOpacity(value);
    public void OnAllyIndicatorAimOpacityChanged(float value) => _gameplayController?.SetAllyIndicatorAimOpacity(value);
    public void OnSquadIndicatorAimOpacityChanged(float value) => _gameplayController?.SetSquadIndicatorAimOpacity(value);
    public void OnNeutralIndicatorOpacityChanged(float value) => _gameplayController?.SetNeutralIndicatorOpacity(value);
    public void OnNeutralIndicatorAimOpacityChanged(float value) => _gameplayController?.SetNeutralIndicatorAimOpacity(value);
    public void OnShowChatChanged() => _gameplayController?.SetShowChat();
    public void OnChatOpacityChanged(float value) => _gameplayController?.SetChatOpacity(value);
    public void OnChatSizeChanged(float value) => _gameplayController?.SetChatSize(value);
    public void OnBodyShotColorSelected(Color color) => ApplyPickedColor(bodyShotColorInput, bodyShotColorPreview, color, value => _gameplayController?.SetBodyShotColor(value));
    public void OnHeadShotColorSelected(Color color) => ApplyPickedColor(headShotColorInput, headShotColorPreview, color, value => _gameplayController?.SetHeadShotColor(value));
    public void OnVehicleMarkerColorSelected(Color color) => ApplyPickedColor(vehicleMarkerColorInput, vehicleMarkerColorPreview, color, value => _gameplayController?.SetVehicleMarkerColor(value));
    public void OnSightReticleColorSelected(Color color) => ApplyPickedColor(sightReticleColorInput, sightReticleColorPreview, color, value => _gameplayController?.SetSightReticleColor(value));
    public void OnEnemyColorSelected(Color color) => ApplyPickedColor(enemyColorInput, enemyColorPreview, color, value => _gameplayController?.SetEnemyColor(value));
    public void OnAllyColorSelected(Color color) => ApplyPickedColor(allyColorInput, allyColorPreview, color, value => _gameplayController?.SetAllyColor(value));
    public void OnSquadColorSelected(Color color) => ApplyPickedColor(squadColorInput, squadColorPreview, color, value => _gameplayController?.SetSquadColor(value));
    public void OnNeutralColorSelected(Color color) => ApplyPickedColor(neutralColorInput, neutralColorPreview, color, value => _gameplayController?.SetNeutralColor(value));
    public void OnBodyShotColorHexChanged(string value) => ApplyHexColor(value, bodyShotColorInput, bodyShotColorPreview, html => _gameplayController?.SetBodyShotColor(html));
    public void OnHeadShotColorHexChanged(string value) => ApplyHexColor(value, headShotColorInput, headShotColorPreview, html => _gameplayController?.SetHeadShotColor(html));
    public void OnVehicleMarkerColorHexChanged(string value) => ApplyHexColor(value, vehicleMarkerColorInput, vehicleMarkerColorPreview, html => _gameplayController?.SetVehicleMarkerColor(html));
    public void OnSightReticleColorHexChanged(string value) => ApplyHexColor(value, sightReticleColorInput, sightReticleColorPreview, html => _gameplayController?.SetSightReticleColor(html));
    public void OnEnemyColorHexChanged(string value) => ApplyHexColor(value, enemyColorInput, enemyColorPreview, html => _gameplayController?.SetEnemyColor(html));
    public void OnAllyColorHexChanged(string value) => ApplyHexColor(value, allyColorInput, allyColorPreview, html => _gameplayController?.SetAllyColor(html));
    public void OnSquadColorHexChanged(string value) => ApplyHexColor(value, squadColorInput, squadColorPreview, html => _gameplayController?.SetSquadColor(html));
    public void OnNeutralColorHexChanged(string value) => ApplyHexColor(value, neutralColorInput, neutralColorPreview, html => _gameplayController?.SetNeutralColor(html));
    public void OpenBodyShotColorPicker() => OpenColorPicker(bodyShotColorInput, bodyShotColorPreview, OnBodyShotColorSelected);
    public void OpenHeadShotColorPicker() => OpenColorPicker(headShotColorInput, headShotColorPreview, OnHeadShotColorSelected);
    public void OpenVehicleMarkerColorPicker() => OpenColorPicker(vehicleMarkerColorInput, vehicleMarkerColorPreview, OnVehicleMarkerColorSelected);
    public void OpenSightReticleColorPicker() => OpenColorPicker(sightReticleColorInput, sightReticleColorPreview, OnSightReticleColorSelected);
    public void OpenEnemyColorPicker() => OpenColorPicker(enemyColorInput, enemyColorPreview, OnEnemyColorSelected);
    public void OpenAllyColorPicker() => OpenColorPicker(allyColorInput, allyColorPreview, OnAllyColorSelected);
    public void OpenSquadColorPicker() => OpenColorPicker(squadColorInput, squadColorPreview, OnSquadColorSelected);
    public void OpenNeutralColorPicker() => OpenColorPicker(neutralColorInput, neutralColorPreview, OnNeutralColorSelected);
    #endregion

    #region Video
    public void OnGraphicPresetChanged(int index) => _videoController?.SetGraphicPreset(index);
    public void OnRenderDistanceChanged(float value) => _videoController?.SetRenderDistance(value);
    public void OnEnableShadowsChanged() => _videoController?.SetShadowsEnabled();
    public void OnShadowsQualityChanged(int index) => _videoController?.SetShadowsQuality(index);
    public void OnMeshesQualityChanged(int index) => _videoController?.SetMeshesQuality(index);
    public void OnRainQualityChanged(int index) => _videoController?.SetRainQuality(index);
    public void OnLimitFpsChanged() => _videoController?.SetLimitFps();
    public void OnMaxFpsChanged() => _videoController?.SetMaxFps();
    public void OnVsyncChanged(float value) => _videoController?.SetVsync(value);
    public void OnBrightnessChanged(float value) => _videoController?.SetBrightness(value);
    public void OnRenderScaleChanged(float value) => _videoController?.SetRenderScale(value);
    public void OnCustomResolutionChanged() => _videoController?.SetCustomResolution();
    public void OnResolutionWidthChanged(string value) => _videoController?.SetResolutionWidth(value);
    public void OnResolutionHeightChanged(string value) => _videoController?.SetResolutionHeight(value);
    public void OnScreenModeChanged(int index) => _videoController?.SetScreenMode(index);
    public void OnInfantryFovChanged(float value) => _videoController?.SetInfantryFov(value);
    public void OnJetFovChanged(float value) => _videoController?.SetJetFov(value);
    public void OnTankFovChanged(float value) => _videoController?.SetTankFov(value);
    public void OnHelicopterFovChanged(float value) => _videoController?.SetHelicopterFov(value);
    public void OnCameraShakeIntensityChanged(float value) => _videoController?.SetCameraShakeIntensity(value);
    public void OnVignetteChanged() => _videoController?.SetVignette();
    public void OnMotionBlurChanged(float value) => _videoController?.SetMotionBlur(value);
    #endregion

    #region Keybinds
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
    public void StartRebindPlayerNightVision() => StartRebinding("PLAYER_activateNightNision");
    public void StartRebindPlayerSpot() => StartRebinding("PLAYER_spotKey");
    public void StartRebindPlayerHoldBreath() => StartRebinding("PLAYER_holdBreathKey");
    public void StartRebindWeaponComposeBullets() => StartRebinding("WEAPON_composeBulletsKey");
    public void StartRebindWeaponActivateSideGrip() => StartRebinding("WEAPON_activateSideGrip");
    public void StartRebindWeaponShoot() => StartRebinding("WEAPON_shootKey");
    public void StartRebindWeaponReload() => StartRebinding("WEAPON_reloadKey");
    public void StartRebindWeaponAim() => StartRebinding("WEAPON_aimKey");
    public void StartRebindWeaponSwitchFireMode() => StartRebinding("WEAPON_switchFireModeKey");
    public void StartRebindWeapon1() => StartRebinding("WEAPON_weapon1Key");
    public void StartRebindWeapon2() => StartRebinding("WEAPON_weapon2Key");
    public void StartRebindWeaponZoomChange() => StartRebinding("WEAPON_zoomChangeKey");
    public void StartRebindWeaponSwitchSight() => StartRebinding("WEAPON_switchSightKey");
    public void StartRebindGadget1() => StartRebinding("GADGET_gadget1Key");
    public void StartRebindGadget2() => StartRebinding("GADGET_gadget2Key");
    public void StartRebindGadgetThrowGrenade() => StartRebinding("GADGET_throwGrenadeKey");
    public void StartRebindGadgetThrowC4() => StartRebinding("GADGET_throwC4Key");
    public void StartRebindGadgetDetonateC4() => StartRebinding("GADGET_detonateC4Key");
    public void StartRebindVehicleStartEngine() => StartRebinding("VEHICLE_startEngineKey");
    public void StartRebindVehicleFreeLook() => StartRebinding("VEHICLE_freeLookKey");
    public void StartRebindVehicleCountermeasure() => StartRebinding("VEHICLE_countermeasureKey");
    public void StartRebindVehicleSwitchFireMode() => StartRebinding("VEHICLE_switchFireModeKey");
    public void StartRebindVehicleSwitchSeat() => StartRebinding("VEHICLE_switchSeatKey");
    public void StartRebindVehicleWeapon1() => StartRebinding("VEHICLE_weapon1");
    public void StartRebindVehicleWeapon2() => StartRebinding("VEHICLE_weapon2");
    public void StartRebindVehicleWeapon3() => StartRebinding("VEHICLE_weapon3");
    public void StartRebindVehicleWeapon4() => StartRebinding("VEHICLE_weapon4");
    public void StartRebindVehicleWeapon5() => StartRebinding("VEHICLE_weapon5");
    public void StartRebindVehicleWeapon6() => StartRebinding("VEHICLE_weapon6");
    public void StartRebindVehicleWeapon7() => StartRebinding("VEHICLE_weapon7");
    public void StartRebindVehicleWeapon8() => StartRebinding("VEHICLE_weapon8");
    public void StartRebindVehicleWeapon9() => StartRebinding("VEHICLE_weapon9");
    public void StartRebindJetBoost() => StartRebinding("JET_boostKey");
    public void StartRebindJetShootVehicle() => StartRebinding("JET_shootVehicleKey");
    public void StartRebindJetPitchUp() => StartRebinding("JET_pitchUpKey");
    public void StartRebindJetPitchDown() => StartRebinding("JET_pitchDownKey");
    public void StartRebindJetYawLeft() => StartRebinding("JET_yawLeftKey");
    public void StartRebindJetYawRight() => StartRebinding("JET_yawRightKey");
    public void StartRebindJetSpeedUp() => StartRebinding("JET_speedUpKey");
    public void StartRebindJetSpeedDown() => StartRebinding("JET_speedDownKey");
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
    public void StartRebindTankIncreaseThrottle() => StartRebinding("TANK_increase_throtlle");
    public void StartRebindTankDecreaseThrottle() => StartRebinding("TANK_decrease_throtlle");
    public void StartRebindTankTurnLeft() => StartRebinding("TANK_turn_left_key");
    public void StartRebindTankTurnRight() => StartRebinding("TANK_turn_right_key");
    public void StartRebindTankShoot() => StartRebinding("TANK_shoot_key");
    public void StartRebindTankZoom() => StartRebinding("TANK_zoom_key");
    public void StartRebindTankBoost() => StartRebinding("TANK_boostKey");
    public void StartRebindTankGunnerSeat() => StartRebinding("TANK_gunner_seat_key");
    public void StartRebindTankPilotSeat() => StartRebinding("TANK_pilot_seat_key");

    private void StartRebinding(string actionName) => _keybindController?.Start(actionName);
    #endregion
}
