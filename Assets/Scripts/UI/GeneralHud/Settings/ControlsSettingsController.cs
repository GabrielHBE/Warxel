using TMPro;
using UnityEngine.UI;

internal sealed class ControlsSettingsView
{
    public Toggle AimHold;
    public Toggle SprintHold;
    public Toggle CrouchHold;
    public Toggle ProneHold;
    public Toggle VehicleBoostHold;
    public Toggle InvertVerticalInfantry;
    public Slider InfantrySensitivity;
    public Slider InfantryAimSensitivity;
    public Toggle InvertVerticalTank;
    public Slider TankSensitivity;
    public Slider TankAimSensitivity;
    public Toggle InvertVerticalJet;
    public Slider JetSensitivity;
    public Slider JetAimSensitivity;
    public Toggle InvertVerticalHelicopter;
    public Slider HelicopterSensitivity;
    public Slider HelicopterAimSensitivity;
}

internal sealed class ControlsSettingsController : ISettingsSection
{
    private static readonly string[] Keys =
    {
        SettingsKeys.AIM_HOLD,
        SettingsKeys.SPRINT_HOLD,
        SettingsKeys.CROUCH_HOLD,
        SettingsKeys.PRONE_HOLD,
        SettingsKeys.VEHICLE_BOOST_HOLD,
        SettingsKeys.INVERT_VERTICAL_INFANTRY,
        SettingsKeys.INFANTRY_SENSIBILITY,
        SettingsKeys.INFANTRY_AIM_SENSIBILITY,
        SettingsKeys.INVERT_VERTICAL_TANK,
        SettingsKeys.TANK_SENSIBILITY,
        SettingsKeys.TANK_AIM_SENSIBILITY,
        SettingsKeys.INVERT_VERTICAL_JET,
        SettingsKeys.JET_SENSIBILITY,
        SettingsKeys.JET_AIM_SENSIBILITY,
        SettingsKeys.INVERT_VERTICAL_HELI,
        SettingsKeys.HELICOPTER_SENSIBILITY,
        SettingsKeys.HELICOPTER_AIM_SENSIBILITY
    };

    private readonly Controls model;
    private readonly ControlsSettingsView view;
    private readonly ISettingsStore store;
    private readonly Defaults defaults;

    public ControlsSettingsController(Controls model, ControlsSettingsView view, ISettingsStore store)
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

    public void SetAimHold() => SetToggle(view.AimHold, value => model.is_aim_on_hold = value, SettingsKeys.AIM_HOLD);
    public void SetSprintHold() => SetToggle(view.SprintHold, value => model.is_sprint_on_hold = value, SettingsKeys.SPRINT_HOLD);
    public void SetCrouchHold() => SetToggle(view.CrouchHold, value => model.is_crouch_on_hold = value, SettingsKeys.CROUCH_HOLD);
    public void SetProneHold() => SetToggle(view.ProneHold, value => model.is_prone_on_hold = value, SettingsKeys.PRONE_HOLD);
    public void SetVehicleBoostHold() => SetToggle(view.VehicleBoostHold, value => model.is_vehicle_boost_on_hold = value, SettingsKeys.VEHICLE_BOOST_HOLD);
    public void SetInvertVerticalInfantry() => SetToggle(view.InvertVerticalInfantry, value => model.invert_vertical_infantary_mouse = value, SettingsKeys.INVERT_VERTICAL_INFANTRY);
    public void SetInvertVerticalTank() => SetToggle(view.InvertVerticalTank, value => model.invert_vertical_tank_mouse = value, SettingsKeys.INVERT_VERTICAL_TANK);
    public void SetInvertVerticalJet() => SetToggle(view.InvertVerticalJet, value => model.invert_vertical_jet_mouse = value, SettingsKeys.INVERT_VERTICAL_JET);
    public void SetInvertVerticalHelicopter() => SetToggle(view.InvertVerticalHelicopter, value => model.invert_vertical_heli_mouse = value, SettingsKeys.INVERT_VERTICAL_HELI);

    public void SetInfantrySensitivity(TextMeshProUGUI label) => SetSlider(
        view.InfantrySensitivity,
        label,
        "Infantaty Mouse Sensibility",
        value => model.infantary_sensibility = value,
        SettingsKeys.INFANTRY_SENSIBILITY);

    public void SetInfantryAimSensitivity(TextMeshProUGUI label) => SetSlider(
        view.InfantryAimSensitivity,
        label,
        "Infantaty Aim Mouse Sensibility",
        value => model.infantary_aim_sensibility = value,
        SettingsKeys.INFANTRY_AIM_SENSIBILITY);

    public void SetTankSensitivity(TextMeshProUGUI label) => SetSlider(
        view.TankSensitivity,
        label,
        "Tank Mouse Sensibility",
        value => model.tank_sensibility = value,
        SettingsKeys.TANK_SENSIBILITY);

    public void SetTankAimSensitivity(TextMeshProUGUI label) => SetSlider(
        view.TankAimSensitivity,
        label,
        "Tank Aim Mouse Sensibility",
        value => model.tank_aim_sensibility = value,
        SettingsKeys.TANK_AIM_SENSIBILITY);

    public void SetJetSensitivity(TextMeshProUGUI label) => SetSlider(
        view.JetSensitivity,
        label,
        "Jet Mouse Sensibility",
        value => model.jet_sensibility = value,
        SettingsKeys.JET_SENSIBILITY);

    public void SetJetAimSensitivity(TextMeshProUGUI label) => SetSlider(
        view.JetAimSensitivity,
        label,
        "Jet Aim Mouse Sensibility",
        value => model.jet_aim_sensibility = value,
        SettingsKeys.JET_AIM_SENSIBILITY);

    public void SetHelicopterSensitivity(TextMeshProUGUI label) => SetSlider(
        view.HelicopterSensitivity,
        label,
        "Helicopter Mouse Sensibility",
        value => model.helicopter_sensibility = value,
        SettingsKeys.HELICOPTER_SENSIBILITY);

    public void SetHelicopterAimSensitivity(TextMeshProUGUI label) => SetSlider(
        view.HelicopterAimSensitivity,
        label,
        "Helicopter Aim Mouse Sensibility",
        value => model.helicopter_aim_sensibility = value,
        SettingsKeys.HELICOPTER_AIM_SENSIBILITY);

    private void ApplyValues(bool useStoredValues)
    {
        model.is_aim_on_hold = GetBool(SettingsKeys.AIM_HOLD, defaults.AimHold, useStoredValues);
        model.is_sprint_on_hold = GetBool(SettingsKeys.SPRINT_HOLD, defaults.SprintHold, useStoredValues);
        model.is_crouch_on_hold = GetBool(SettingsKeys.CROUCH_HOLD, defaults.CrouchHold, useStoredValues);
        model.is_prone_on_hold = GetBool(SettingsKeys.PRONE_HOLD, defaults.ProneHold, useStoredValues);
        model.is_vehicle_boost_on_hold = GetBool(SettingsKeys.VEHICLE_BOOST_HOLD, defaults.VehicleBoostHold, useStoredValues);
        model.invert_vertical_infantary_mouse = GetBool(SettingsKeys.INVERT_VERTICAL_INFANTRY, defaults.InvertInfantry, useStoredValues);
        model.infantary_sensibility = GetFloat(SettingsKeys.INFANTRY_SENSIBILITY, defaults.InfantrySensitivity, useStoredValues);
        model.infantary_aim_sensibility = GetFloat(SettingsKeys.INFANTRY_AIM_SENSIBILITY, defaults.InfantryAimSensitivity, useStoredValues);
        model.invert_vertical_tank_mouse = GetBool(SettingsKeys.INVERT_VERTICAL_TANK, defaults.InvertTank, useStoredValues);
        model.tank_sensibility = GetFloat(SettingsKeys.TANK_SENSIBILITY, defaults.TankSensitivity, useStoredValues);
        model.tank_aim_sensibility = GetFloat(SettingsKeys.TANK_AIM_SENSIBILITY, defaults.TankAimSensitivity, useStoredValues);
        model.invert_vertical_jet_mouse = GetBool(SettingsKeys.INVERT_VERTICAL_JET, defaults.InvertJet, useStoredValues);
        model.jet_sensibility = GetFloat(SettingsKeys.JET_SENSIBILITY, defaults.JetSensitivity, useStoredValues);
        model.jet_aim_sensibility = GetFloat(SettingsKeys.JET_AIM_SENSIBILITY, defaults.JetAimSensitivity, useStoredValues);
        model.invert_vertical_heli_mouse = GetBool(SettingsKeys.INVERT_VERTICAL_HELI, defaults.InvertHelicopter, useStoredValues);
        model.helicopter_sensibility = GetFloat(SettingsKeys.HELICOPTER_SENSIBILITY, defaults.HelicopterSensitivity, useStoredValues);
        model.helicopter_aim_sensibility = GetFloat(SettingsKeys.HELICOPTER_AIM_SENSIBILITY, defaults.HelicopterAimSensitivity, useStoredValues);

        view.AimHold?.SetIsOnWithoutNotify(model.is_aim_on_hold);
        view.SprintHold?.SetIsOnWithoutNotify(model.is_sprint_on_hold);
        view.CrouchHold?.SetIsOnWithoutNotify(model.is_crouch_on_hold);
        view.ProneHold?.SetIsOnWithoutNotify(model.is_prone_on_hold);
        view.VehicleBoostHold?.SetIsOnWithoutNotify(model.is_vehicle_boost_on_hold);
        view.InvertVerticalInfantry?.SetIsOnWithoutNotify(model.invert_vertical_infantary_mouse);
        view.InfantrySensitivity?.SetValueWithoutNotify(model.infantary_sensibility);
        view.InfantryAimSensitivity?.SetValueWithoutNotify(model.infantary_aim_sensibility);
        view.InvertVerticalTank?.SetIsOnWithoutNotify(model.invert_vertical_tank_mouse);
        view.TankSensitivity?.SetValueWithoutNotify(model.tank_sensibility);
        view.TankAimSensitivity?.SetValueWithoutNotify(model.tank_aim_sensibility);
        view.InvertVerticalJet?.SetIsOnWithoutNotify(model.invert_vertical_jet_mouse);
        view.JetSensitivity?.SetValueWithoutNotify(model.jet_sensibility);
        view.JetAimSensitivity?.SetValueWithoutNotify(model.jet_aim_sensibility);
        view.InvertVerticalHelicopter?.SetIsOnWithoutNotify(model.invert_vertical_heli_mouse);
        view.HelicopterSensitivity?.SetValueWithoutNotify(model.helicopter_sensibility);
        view.HelicopterAimSensitivity?.SetValueWithoutNotify(model.helicopter_aim_sensibility);
    }

    private void SetToggle(Toggle toggle, System.Action<bool> updateModel, string key)
    {
        if (toggle == null) return;
        
        updateModel(toggle.isOn);
        store.SetInt(key, toggle.isOn ? 1 : 0);
    }

    private void SetSlider(
        Slider slider,
        TextMeshProUGUI label,
        string labelText,
        System.Action<float> updateModel,
        string key)
    {
        if (slider == null) return;
        

        if (label != null) label.text = $"[{slider.value:F1}] {labelText}";
        

        updateModel(slider.value);
        store.SetFloat(key, slider.value);
    }

    private float GetFloat(string key, float defaultValue, bool useStoredValues) => useStoredValues ? store.GetFloat(key, defaultValue) : defaultValue;

    private bool GetBool(string key, bool defaultValue, bool useStoredValues) => (!useStoredValues && defaultValue) || (useStoredValues && store.GetInt(key, defaultValue ? 1 : 0) == 1);

    private readonly struct Defaults
    {
        public readonly bool AimHold;
        public readonly bool SprintHold;
        public readonly bool CrouchHold;
        public readonly bool ProneHold;
        public readonly bool VehicleBoostHold;
        public readonly bool InvertInfantry;
        public readonly float InfantrySensitivity;
        public readonly float InfantryAimSensitivity;
        public readonly bool InvertTank;
        public readonly float TankSensitivity;
        public readonly float TankAimSensitivity;
        public readonly bool InvertJet;
        public readonly float JetSensitivity;
        public readonly float JetAimSensitivity;
        public readonly bool InvertHelicopter;
        public readonly float HelicopterSensitivity;
        public readonly float HelicopterAimSensitivity;

        public Defaults(Controls model)
        {
            AimHold = model.is_aim_on_hold;
            SprintHold = model.is_sprint_on_hold;
            CrouchHold = model.is_crouch_on_hold;
            ProneHold = model.is_prone_on_hold;
            VehicleBoostHold = model.is_vehicle_boost_on_hold;
            InvertInfantry = model.invert_vertical_infantary_mouse;
            InfantrySensitivity = model.infantary_sensibility;
            InfantryAimSensitivity = model.infantary_aim_sensibility;
            InvertTank = model.invert_vertical_tank_mouse;
            TankSensitivity = model.tank_sensibility;
            TankAimSensitivity = model.tank_aim_sensibility;
            InvertJet = model.invert_vertical_jet_mouse;
            JetSensitivity = model.jet_sensibility;
            JetAimSensitivity = model.jet_aim_sensibility;
            InvertHelicopter = model.invert_vertical_heli_mouse;
            HelicopterSensitivity = model.helicopter_sensibility;
            HelicopterAimSensitivity = model.helicopter_aim_sensibility;
        }
    }
}
