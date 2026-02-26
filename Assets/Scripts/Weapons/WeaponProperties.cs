using System.Collections.Generic;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine;

public class WeaponProperties : MonoBehaviour
{
    [Header("Progression / Category")]
    public ClassManager.Class[] class_weapon;
    public int weapon_level;
    public float points_to_up_level;
    public float weapon_level_progression;
    public float current_attachment_points;
    public string category;

    [Header("Weapon properties")]
    public bool can_damage_vehicles;
    public bool manual_calculate_recoil;
    public bool is_shotgun;
    public bool single_reload;
    public bool can_hold_trigger;
    public string weapon_name;
    public float rate_of_fire;
    public float ads_speed;
    public float speed_change;
    public float zoom;
    public float time_to_transfer_bullets;

    public Vector3 ads_position;
    public List<string> fire_modes = new List<string>();

    [Header("HUD")]
    public Sprite icon_hud;


    [Header("Destruction")]
    public float destruction_force;

    [Header("Bullet")]
    [SerializeField] public float bullet_size = 1; // ✅ Agora aparecerá no Inspector
    public int bullets_per_shot;
    public Transform bulletPref;
    public float muzzle_velocity;
    public float bullet_drop;
    public GameObject bullet_hit_effect;


    [Header("Shotgun")]
    public int shells;

    [Header("Damage")]
    public float damage;
    public float vehicle_damage;
    public float minimum_damage;
    public float headshot_multiplier;
    public float damage_dropoff;
    public float damage_dropoff_timer;

    [Header("Switch Weapon")]
    public float switch_weapon_timer;

    [Header("Burst Mode")]
    public int bullets_per_tap;
    public float time_between_shots_in_burst;
    public float time_between_bursts;

    [Header("Spread")]
    public float spread_increaser;
    public float max_spread;

    [Header("Recoil")]
    public float[] vertical_recoil = new float[10];
    public float[] horizontal_recoil = new float[10];
    public float first_shoot_increaser;
    public float weapon_stability;
    public float screen_reset_recoil_speed;
    public float weapon_reset_recoil_speed;
    public float weapon_apply_recoil_speed;
    [HideInInspector] public float interval;
    public Vector3 visual_recoil;
    public float horizontal_recoil_media;
    public float vertical_recoil_media;

    [Header("Magazine / Reload")]
    public float reload_time;
    public int mag_count;
    public int bullets_per_mag;
    [HideInInspector] public List<int> mags = new List<int>();

    [Header("Shoot")]
    public float delay_to_shoot_animation;

    [Header("Objects")]
    public WeaponSounds weapon_sound;
    public GameObject barrel;
    public GameObject weapon;

    [Header("Sway and Bob")]
    public float bob_walk_exageration;
    public float bob_sprint_exageration;
    public float bob_crouch_exageration;
    public float bob_aim_exageration;
    public Vector3 walk_multiplier;
    public Vector3 sprint_multiplier;
    public Vector3 aim_multiplier;
    public Vector3 shoot_multiplier;
    public Vector3 crouch_multiplier;
    public float[] vector3Values = new float[3];
    public float[] quaternionValues = new float[3];
    public Vector3 initial_potiion;
    public Quaternion inicial_rotation;
    private BulletExtractor bulletExtractor;

    void Awake()
    {
        bulletExtractor = GetComponentInChildren<BulletExtractor>();
        FillMags();
        Restart();
    }

    public void Restart()
    {

        CalculateMedia();
        interval = 60f / rate_of_fire;

        if (!manual_calculate_recoil)
        {
            weapon_reset_recoil_speed = interval / 2;
            weapon_apply_recoil_speed = interval / 2;
        }

        //if (reset_recoil_speed == 0) auto_reset_recoil_speed = interval / 2;
        //if (apply_recoil_speed == 0) auto_apply_recoil_speed = interval / 2;

    }

    void FillMags()
    {
        for (int i = 0; i < mag_count; i++)
        {
            mags.Add(bullets_per_mag);
        }
    }

    public void CalculateRecoilSpeed(bool is_burst)
    {
        /*
        if (fire_modes.Count != 1)
        {
            if (!is_burst)
            {

                //reset_recoil_speed = auto_reset_recoil_speed;
                //apply_recoil_speed = auto_apply_recoil_speed;

            }
            else
            {
                reset_recoil_speed = time_between_shots_in_burst / 2;
                apply_recoil_speed = time_between_shots_in_burst / 2;
            }
            
        }
        */

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
        bulletExtractor.CreateBullet();
    }

    public void UpgradeWeaponLevel(float points)
    {
        weapon_level_progression += points;

        if (weapon_level_progression >= points_to_up_level)
        {
            weapon_level += 1;
            weapon_level_progression = 0;
        }

    }

}