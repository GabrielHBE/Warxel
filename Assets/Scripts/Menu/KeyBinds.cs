using UnityEngine;
using UnityEngine.InputSystem;

public class KeyBinds : MonoBehaviour
{

    public static KeyBinds Instance; // Singleton global

    private void Awake()
    {
        // Se já existe uma instância, destrói a nova
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Define como global
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }



    [Header("Player")]
    public KeyCode PLAYER_moveFowardKey = KeyCode.W;
    public KeyCode PLAYER_moveBackwardsdKey = KeyCode.S;
    public KeyCode PLAYER_moveLeftKey = KeyCode.A;
    public KeyCode PLAYER_moveRightKey = KeyCode.D;
    public KeyCode PLAYER_jumpKey = KeyCode.Space;
    public KeyCode PLAYER_interactKey = KeyCode.F;
    public KeyCode PLAYER_sprintKey = KeyCode.LeftControl;
    public KeyCode PLAYER_crouchKey = KeyCode.LeftShift;
    public KeyCode PLAYER_proneKey = KeyCode.C;
    public KeyCode PLAYER_leanLeftKey = KeyCode.Q;
    public KeyCode PLAYER_leanRightKey = KeyCode.E;
    public KeyCode PLAYER_rollKey = KeyCode.Z;
    public KeyCode PLAYER_activateNightNision = KeyCode.N;


    [Header("Weapons")]
    public KeyCode WEAPON_composeBulletsKey = KeyCode.P;
    public KeyCode WEAPON_activateSideGrip = KeyCode.T;
    public KeyCode WEAPON_shootKey = KeyCode.Mouse0;
    public KeyCode WEAPON_reloadKey = KeyCode.R;
    public KeyCode WEAPON_aimKey = KeyCode.Mouse1;
    public KeyCode WEAPON_switchFireModeKey = KeyCode.X;
    public KeyCode WEAPON_weapon1Key = KeyCode.Alpha1;
    public KeyCode WEAPON_weapon2Key = KeyCode.Alpha2;


    [Header("Gadget")]
    public KeyCode GADGET_gadget1Key = KeyCode.Alpha3;
    public KeyCode GADGET_gadget2Key = KeyCode.Alpha3;
    public KeyCode GADGET_throwGrenadeKey = KeyCode.G;
    public KeyCode GADGET_throwC4Key = KeyCode.Mouse1;
    public KeyCode GADGET_detonateC4Key = KeyCode.Mouse0;



    [Header("Vehicle")]
    public KeyCode VEHICLE_startEngineKey = KeyCode.E;
    public KeyCode VEHICLE_freeLookKey = KeyCode.Mouse2;
    public KeyCode VEHICLE_countermeasureKey = KeyCode.X;
    public KeyCode VEHICLE_switchSeatKey = KeyCode.LeftControl;
    public KeyCode VEHICLE_weapon1 = KeyCode.Alpha1;
    public KeyCode VEHICLE_weapon2 = KeyCode.Alpha2;


    [Header("Jet")]
    public KeyCode JET_boostKey = KeyCode.LeftControl;
    public KeyCode JET_shootVehicleKey = KeyCode.Mouse0;
    public KeyCode JET_pitchUpKey = KeyCode.Space;
    public KeyCode JET_pitchDownKey = KeyCode.LeftShift;
    public KeyCode JET_yawLeftKey = KeyCode.A;
    public KeyCode JET_yawRightKey = KeyCode.D;
    public KeyCode JET_speedUpKey = KeyCode.W;
    public KeyCode JET_speedDownKey = KeyCode.S;

    [Header("Helicopter")]
    public KeyCode HELICOPTER_increase_throtlle = KeyCode.W;
    public KeyCode HELICOPTER_decrease_throtlle = KeyCode.S;
    public KeyCode HELICOPTER_switch_camera_key = KeyCode.C;
    public KeyCode HELICOPTER_main_cannon_key = KeyCode.Alpha1;
    public KeyCode HELICOPTER_upgrade_gun_key = KeyCode.Alpha2;
    public KeyCode HELICOPTER_shoot_key = KeyCode.Mouse0;
    public KeyCode HELICOPTER_zoom_key = KeyCode.Mouse1;
    public KeyCode HELICOPTER_pitch_up_key = KeyCode.Space;
    public KeyCode HELICOPTER_pitch_down_key = KeyCode.LeftShift;
    public KeyCode HELICOPTER_lean_left_key = KeyCode.A;
    public KeyCode HELICOPTER_lean_right_key = KeyCode.D;
    public KeyCode HELICOPTER_gunner_seat_key = KeyCode.F1;
    public KeyCode HELICOPTER_pilot_seat_key = KeyCode.F2;

    [Header("Tank")]
    public KeyCode TANK_increase_throtlle = KeyCode.W;
    public KeyCode TANK_decrease_throtlle = KeyCode.S;
    public KeyCode TANK_turn_left_key = KeyCode.A;
    public KeyCode TANK_turn_right_key = KeyCode.D;
    public KeyCode TANK_shoot_key = KeyCode.Mouse0;
    public KeyCode TANK_zoom_key = KeyCode.Mouse1;
    public KeyCode TANK_boostKey = KeyCode.LeftControl;
    public KeyCode TANK_gunner_seat_key = KeyCode.F1;
    public KeyCode TANK_pilot_seat_key = KeyCode.F2;



}
