using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WeaponProperties : MonoBehaviour
{
    public int level_to_unlock;

    [Header("Weapon properties")]
    public bool manual_calculate_recoil;
    public bool is_shotgun;
    public bool single_reload;
    public bool can_hold_trigger;
    public string weapon_name;
    public float rate_of_fire;
    public float ads_speed;
    public float speed_change;
    public float zoom;
    public Light muzzle_lightinig;
    public Vector3 ads_position;
    public List<string> fire_modes = new List<string>();

    [Header("Destruction")]
    public float destruction_force;

    [Header("Bullet")]
    public int bullets_per_shot;
    public Transform bulletPref;
    public float muzzle_velocity;
    public float bullet_drop;
    public GameObject bullet_hit_effect;
    

    [Header("Shotgun")]
    public int shells;

    [Header("Damage")]
    public float damage;
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
    public float reset_recoil_speed;
    public float apply_recoil_speed;
    [HideInInspector] public float interval;
    public Vector3 visual_recoil;
    public float horizontal_recoil_media;
    public float vertical_recoil_media;

    [Header("Magazine / Reload")]
    public float reload_time;
    public int mag_count;
    public int bullets_per_mag;
    [HideInInspector] public List<int> mags = new List<int>();
    public Vector3 weapon_reload_position;
    public Quaternion weapon_reload_rotation;

    [Header("Shoot")]
    public float delay_to_shoot_animation;

    [Header("Objects")]
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
    public float jump_offset_vector;
    public float jump_offset_x;
    public float jump_offset_y;
    public float[] vector3Values = new float[3];
    public float[] quaternionValues = new float[3];
    public Vector3 initial_potiion;
    public Quaternion inicial_rotation;
    private Sight sight;
    private Grip grip;
    private Mag mag;
    private Barrel _barrel;
    bool do_once = true;
    private BulletExtractor bulletExtractor;

    void Start()
    {
        inicial_rotation = transform.localRotation;
        initial_potiion = transform.localPosition;
        FillMags();
        Restart();
    }

    public void Restart()
    {
        bulletExtractor = GetComponentInChildren<BulletExtractor>();
        if (do_once)
        {
            MagAttatchment();
            GripAttatchment();
            SightAttatchment();
            BarrelAttatchment();
            do_once = false;

        }


        CalculateMedia();
        interval = 60f / rate_of_fire;

        if (!manual_calculate_recoil)
        {
            reset_recoil_speed = interval / 2;
            apply_recoil_speed = interval / 2;
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


    void CalculateMedia()
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

    void SightAttatchment()
    {
        sight = GetComponentInChildren<Sight>();
        if (sight == null)
        {
            return;
        }

        zoom += sight.zoom_change;
        ads_speed += sight.ads_speed_change;
    }

    void GripAttatchment()
    {
        grip = GetComponentInChildren<Grip>();
        if (grip == null)
        {
            return;
        }


        for (int i = 0; i < vertical_recoil.Length; i++)
        {
            vertical_recoil[i] += grip.vertical_recoil_change;
            horizontal_recoil[i] += grip.horizontal_recoil_change;
        }

        first_shoot_increaser += grip.first_shoot_change;
        reload_time += grip.reload_speed_change;
        ads_speed += grip.ads_speed_change;
        weapon_stability += grip.weapon_stability_change;
    }


    void MagAttatchment()
    {
        mag = GetComponentInChildren<Mag>();
        if (mag == null)
        {
            return;
        }

        ads_speed += mag.ads_speed_change;
        bullets_per_mag += mag.bullet_counter_change;
        weapon_stability += mag.stability_change;

        reload_time += mag.reload_speed_changer;
    }


    void BarrelAttatchment()
    {

        WeaponSounds weapon_sound = GetComponent<WeaponSounds>();
        _barrel = GetComponentInChildren<Barrel>();
        if (_barrel == null)
        {
            return;
        }

        first_shoot_increaser += _barrel.first_shoot_recoil_change;
        muzzle_velocity += _barrel.muzzle_velocity_change;
        muzzle_lightinig.intensity += _barrel.muzzle_lightning_change;

        weapon_sound.shoot_sound.pitch += _barrel.shoot_pith_change;
        weapon_sound.shoot_sound.volume += _barrel.volume_changer;
        spread_increaser += _barrel.spread_change;
    }

    public void CreateBulletExtractor()
    {
        bulletExtractor.CreateBullet();
    }

}
