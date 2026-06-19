using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponProperties : MonoBehaviour, UpgradeLevel
{
    #region Variables

    #region Progression and Meta
    [Header("Progression & Economy")]
    public string weapon_name;
    public ClassManager.Class[] class_weapon;
    public FactionManager.Faction[] faction;
    public WeaponCategory category;
    public int battle_coins_to_unlock;
    public int weapon_kills;
    public float current_attachment_points;
    #endregion

    #region Core Weapon Settings
    [Header("Core Settings")]
    public GameObject weapon;
    public GameObject third_person_prefab;
    public Sprite icon_hud;
    public List<FireMode> fire_modes = new List<FireMode>();
    public float rate_of_fire;
    public float ads_speed;
    public float speed_change;
    public float zoom = 1;
    public float switch_weapon_timer;
    [HideInInspector] public float interval => 60f / rate_of_fire;
    #endregion

    [Header("Handling")]
    public float pick_up_weapon_speed;
    public float store_weapon_speed;

    #region Shooting and Reload mechanics
    [Header("Shooting & Reloading")]
    public bool single_reload;
    public float delay_to_shoot_animation;
    public float time_to_transfer_bullets;
    public float reload_time;
    public int mag_count;
    [HideInInspector] public int bullets_per_mag;
    [HideInInspector] public List<int> mags = new List<int>();
    #endregion
    
    #region Damage and Ballistics
    [Header("Damage & Ballistics")]
    public bool can_damage_vehicles;
    public int bullets_per_shot;
    public float muzzle_velocity;
    public float bullet_drop;
    public float infantry_damage;
    public float vehicle_damage;
    public float minimum_damage;
    public float headshot_multiplier;
    public float damage_dropoff;
    public float damage_dropoff_timer;
    public float destruction_force;
    #endregion

    #region Burst Mode Settings
    [Header("Burst Mode")]
    public int bullets_per_tap;
    public float time_between_bursts;
    #endregion

    #region Accuracy and Spread
    [Header("Spread")]
    [Range(Spread.MIN_SPREAD_VALUE, Spread.MAX_SPREAD_VALUE)]
    public float base_spread;
    [Range(Spread.MIN_SPREAD_VALUE, Spread.MAX_SPREAD_VALUE)]
    public float spread_increaser;
    [Range(Spread.MIN_SPREAD_VALUE, Spread.MAX_SPREAD_VALUE)]
    public float max_spread;
    public float spread_recovery = 1;
    #endregion

    #region Recoil Mechanics
    [Header("Recoil Settings")]
    public bool manual_calculate_recoil;
    [Range(Recoil.MIN_FIRTSHOTINCREASER_VALUE, Recoil.MAX_FIRTSHOTINCREASER_VALUE)]
    public float first_shoot_increaser;
    public float weapon_reset_recoil_speed;
    public float weapon_apply_recoil_speed;
    public Vector3 visual_recoil;
    
    [Space(5)]
    [Range(Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE)]
    public float[] vertical_recoil = new float[10];
    [Range(Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE)]
    public float[] horizontal_recoil = new float[10];
    [HideInInspector] public float horizontal_recoil_media;
    [HideInInspector] public float vertical_recoil_media;
    #endregion

    #region View Kick, Sway and Bobbing
    [Header("Sway & Bobbing Exaggeration")]
    public float bob_walk_exageration;
    public float bob_sprint_exageration;
    public float bob_crouch_exageration;
    public float bob_aim_exageration;

    [Header("Sway Multipliers")]
    public Vector3 walk_multiplier;
    public Vector3 sprint_multiplier;
    public Vector3 aim_multiplier;
    public Vector3 shoot_multiplier;
    public Vector3 crouch_multiplier;

    [Header("Sway Transforms")]
    public Vector3 initial_potiion; // Mantido o erro de digitação original para não quebrar referências externamente
    public Quaternion initial_rotation;
    public float[] vector3Values = new float[3];
    public float[] quaternionValues = new float[3];
    #endregion

    #region Weapon References
    [Header("References & Effects")]
    public WeaponSounds weapon_sound;
    public GameObject barrel;
    private BulletExtractor bulletExtractor;
    #endregion

    #endregion

    #region Enums

    public enum WeaponCategory
    {
        AssaultRifle,
        Dmr,
        SniperRifle,
        SubmachineGun,
        LightMachineGun,
        Shotgun,
        Pistol,
        Launcher
    }

    [Serializable]
    public enum FireMode
    {
        Auto,
        Burst,
        Single
    }

    #endregion

    #region Unity Callbacks
    void Awake()
    {
        weapon = gameObject;
    }
    #endregion

    #region Initialization & Setup
    public void Initialize()
    {
        weapon_kills = PlayerPrefs.GetInt($"WeaponProperties_weapon_kills_{weapon_name}");
        SetClassBenefits();
        bulletExtractor = GetComponentInChildren<BulletExtractor>();
        FillMags();
        Restart();
    }

    private void SetClassBenefits()
    {
        if (AccountManager.Instance.selected_class == ClassManager.Class.Assault)
        {
            reload_time *= 1.2f;
            first_shoot_increaser *= 0.9f;

            for (int i = 0; i < vertical_recoil.Length; i++)
            {
                vertical_recoil[i] *= 0.9f;
            }

            for (int i = 0; i < horizontal_recoil.Length; i++)
            {
                horizontal_recoil[i] *= 0.9f;
            }
        }
    }

    public void Restart()
    {
        CalculateMedia();
        
        if (!manual_calculate_recoil)
        {
            weapon_reset_recoil_speed = interval / 2;
            weapon_apply_recoil_speed = interval / 2;
        }
    }

    void FillMags()
    {
        for (int i = 0; i < mag_count; i++)
        {
            mags.Add(bullets_per_mag);
        }
    }
    #endregion

    #region Logic & Calculations
    public void CalculateRecoilSpeed(bool is_burst)
    {
        // Espaço reservado para lógica futura de recuo por modo de tiro
    }

    public void CalculateMedia()
    {
        float media = 0;

        for (int i = 0; i < vertical_recoil.Length; i++)
        {
            media += vertical_recoil[i];
        }
        media /= vertical_recoil.Length;
        vertical_recoil_media = media;

        media = 0;

        for (int i = 0; i < horizontal_recoil.Length; i++)
        {
            media += horizontal_recoil[i];
        }
        media /= horizontal_recoil.Length;
        horizontal_recoil_media = media;
    }

    public void CreateBulletExtractor()
    {
        if (bulletExtractor != null)
        {
            bulletExtractor.CreateBullet();
        }
    }
    #endregion

    #region Progression Systems
    public void AddKill()
    {
        weapon_kills += 1;
    }

    public void ResetWeaponlevel()
    {
        PlayerPrefs.SetFloat($"WeaponProperties_weapon_level_progression_{weapon_name}", 0);
        PlayerPrefs.SetFloat($"WeaponProperties_weapon_level_{weapon_name}", 0);
        PlayerPrefs.Save();
    }
    #endregion
}