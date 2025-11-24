
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [HideInInspector] public bool is_active;

    [Header("Instances")]
    [SerializeField] private GameObject left_hand;
    [SerializeField] private GameObject right_hand;
    [SerializeField] private KeyBinds keyBinds;
    [SerializeField] private Camera player_camera;
    [SerializeField] private GameObject ads_position;
    [SerializeField] private GameObject screen_center;
    [SerializeField] private SwitchWeapon switchWeapon;

    [Header("Sounds")]
    public AudioSource switch_fire_mode_sound;

    //variables
    [HideInInspector] public bool can_aim = true;
    [HideInInspector] public bool can_shoot = true;
    bool can_reload;
    float reload_cooldown;
    float next_time_to_fire = 0f;
    [HideInInspector] public bool did_shoot = false;
    private Vector3 attatchment_change_ads_position;
    private float minFov;
    private int current_fire_mode = 0;
    float burst_timer = 0f;
    int bullets_shot_in_current_burst = 0;
    bool is_bursting;
    bool is_last_bullet;
    bool is_first_shot;
    int recoil_position_in_array = 0;
    Vector3 original_ads_position;
    private WeaponSounds weaponSounds;
    private WeaponProperties weaponProperties;
    private AudioSource shoot_sound;
    private PlayerController playerController;
    private PlayerProperties playerProperties;
    private WeaponAnimation weaponAnimation;
    private Sight sight_attatchment;
    private Shell Shell;
    float current_spread;
    [HideInInspector] public bool dot_position;
    bool restarted;

    Vector3 original_barrel_pos;

    void Awake()
    {
        SetHandsOnWeapon();
        restarted = false;
    }

    public void Restart()
    {

        weaponProperties = GetComponentInChildren<WeaponProperties>();
        weaponProperties.weapon.transform.localPosition = weaponProperties.initial_potiion;
        weaponProperties.weapon.transform.localRotation = weaponProperties.inicial_rotation;
        weaponAnimation = GetComponent<WeaponAnimation>();
        playerController = GetComponentInParent<PlayerController>();
        playerProperties = transform.root.GetComponent<PlayerProperties>();
        sight_attatchment = GetComponentInChildren<Sight>();
        Shell = GetComponentInChildren<Shell>();

        weaponSounds = GetComponentInChildren<WeaponSounds>();
        shoot_sound = weaponSounds.shoot_sound;
        minFov = player_camera.fieldOfView;

        weaponProperties.muzzle_lightinig.enabled = false;
        if (sight_attatchment == null)
        {
            attatchment_change_ads_position = new Vector3(0f, 0f, 0f);
        }
        else
        {
            attatchment_change_ads_position = sight_attatchment.change_ads_position;
        }

        current_fire_mode = 0;

        next_time_to_fire = 0f;

        if (keyBinds.aimKey == KeyCode.Alpha1)
        {
            keyBinds.aimKey = KeyCode.Mouse1;
        }
        if (keyBinds.shootKey == KeyCode.Alpha0)
        {
            keyBinds.shootKey = KeyCode.Mouse0;
        }

        original_barrel_pos = weaponProperties.barrel.transform.localPosition;

        weaponProperties.weapon.transform.localRotation = weaponProperties.inicial_rotation;
        can_reload = true;
        playerProperties.is_reloading = false;
        restarted = true;

    }

    void Update()
    {


        if (!restarted || !is_active)
        {
            return;
        }

        if (Input.GetKeyDown(keyBinds.reloadKey))
        {
            weaponAnimation.StartReloadAnimation();

            int mags_empty = 0;

            for (int i = 0; i < weaponProperties.mag_count; i++)
            {
                if (weaponProperties.mags[i] == 0)
                {
                    mags_empty += 1;
                }
            }

            if (mags_empty == weaponProperties.mag_count)
            {
                can_reload = false;
            }
            else
            {
                if (weaponAnimation.fireClip != null)
                {
                    if (!weaponAnimation.is_in_fire_animation)
                    {
                        reload_cooldown = weaponAnimation.reload_animation_timer / weaponProperties.reload_time;
                        playerProperties.is_reloading = true;
                        can_reload = true;

                    }
                }
                else
                {

                    reload_cooldown = weaponAnimation.reload_animation_timer / weaponProperties.reload_time;
                    playerProperties.is_reloading = true;
                    can_reload = true;

                }

            }

        }


        aim();

        try
        {
            if (can_shoot && !playerProperties.is_reloading && weaponProperties.mags[^1] > 0)
            {
                shoot();
            }

        }
        catch (System.Exception)
        {

        }


        Reload();

        if (Input.GetKeyDown(keyBinds.switchFireModeKey))
        {
            SwitchFireMode();
        }


    }

    void SetHandsOnWeapon()
    {
        WeaponHolder[] weaponHolders = GetComponentsInChildren<WeaponHolder>(true);
        foreach (WeaponHolder holder in weaponHolders)
        {

            holder.leftHand_origin = left_hand;
            holder.rightHand_origin = right_hand;
        }
    }



    void SwitchFireMode()
    {

        switch_fire_mode_sound.Play();

        if (current_fire_mode < weaponProperties.fire_modes.Count - 1)
        {
            current_fire_mode += 1;
        }
        else
        {
            current_fire_mode = 0;
        }

        //current_fire_mode = (current_fire_mode + 1) % weaponProperties.fire_modes.Count;

        //weaponProperties.can_hold_trigger = (weaponProperties.fire_modes[current_fire_mode] == "auto");

        if (weaponProperties.fire_modes[current_fire_mode] == "auto")
        {
            weaponProperties.can_hold_trigger = true;
            weaponProperties.CalculateRecoilSpeed(false);
        }
        else if (weaponProperties.fire_modes[current_fire_mode] == "burst")
        {
            weaponProperties.can_hold_trigger = false;
            weaponProperties.CalculateRecoilSpeed(true);
        }
        else
        {
            weaponProperties.can_hold_trigger = false;
            weaponProperties.CalculateRecoilSpeed(false);
        }


    }


    void Reload()
    {

        if (!weaponProperties.single_reload)
        {

            if (playerProperties.is_reloading && can_reload)
            {
                is_bursting = false;

                if (reload_cooldown >= 0)
                {
                    reload_cooldown -= Time.deltaTime;
                    can_shoot = false;
                    playerProperties.is_reloading = true;
                }
                else
                {
                    int max = 0;
                    int index = 0;
                    for (int i = 0; i < weaponProperties.mag_count; i++)
                    {
                        if (weaponProperties.mags[i] > max)
                        {   //a   //b
                            max = weaponProperties.mags[i];
                            index = i;
                        }
                    }

                    if (weaponProperties.mags[^1] == 0)
                    {
                        is_last_bullet = true;
                    }
                    else
                    {
                        is_last_bullet = false;
                    }

                    int temp = weaponProperties.mags[^1];
                    weaponProperties.mags[^1] = max; //Ultima posição
                    if (!is_last_bullet)
                    {
                        weaponProperties.mags[^1]++;

                    }
                    weaponProperties.mags[index] = temp;
                    can_shoot = true;
                    playerProperties.is_reloading = false;
                    reload_cooldown = 0;

                    weaponAnimation.FinishReloadAnimation();

                }

            }
        }
        else
        {

            if (playerProperties.is_reloading == true && can_reload == true && weaponProperties.mags[^1] != weaponProperties.bullets_per_mag && !playerProperties.is_firing && weaponProperties.shells > 0)
            {
                Shell.Reload(weaponProperties);
                //shotgun_shells = -1;

            }
            else
            {
                Shell.ReturnHand();
                playerProperties.is_reloading = false;
            }

        }

    }

    IEnumerator ApplyVisualRecoilOffset()
    {

        float vr = weaponProperties.vertical_recoil[recoil_position_in_array];
        float hr = weaponProperties.horizontal_recoil[recoil_position_in_array];

        if (is_first_shot == false || weaponProperties.mags[^1] == 1)
        {
            playerController.ApplyCameraRecoil(vr * weaponProperties.first_shoot_increaser, hr * weaponProperties.first_shoot_increaser);
            is_first_shot = true;
        }
        else
        {
            playerController.ApplyCameraRecoil(vr, hr);
        }

        if (hr < 0)
        {
            hr = Random.Range(hr, hr * -1);
        }
        else
        {
            hr = Random.Range(-hr, hr);
        }
        vr = Random.Range(0, vr);

        Vector3 recoilOffset = playerProperties.is_aiming == false ? new Vector3(0f, 0f, weaponProperties.visual_recoil.z) : new Vector3(0f, 0f, weaponProperties.visual_recoil.z / 2);
        //Vector3 start = weaponProperties.ads_position;
        Vector3 start = weaponProperties.initial_potiion;
        Vector3 target = start + recoilOffset;

        Quaternion weaponRotation = new Quaternion(weaponProperties.weapon.transform.localRotation.x + Random.Range(-0.02f, 0.02f),
                                                   weaponProperties.weapon.transform.localRotation.y + Random.Range(-0.02f, 0.02f),
                                                   weaponProperties.weapon.transform.localRotation.z + Random.Range(-0.02f, 0.02f),
                                                   weaponProperties.weapon.transform.localRotation.w);



        if (playerProperties.is_aiming)
        {

            weaponRotation = new Quaternion(weaponProperties.weapon.transform.localRotation.x + Random.Range(hr / -weaponProperties.weapon_stability, 0),
                                            weaponProperties.weapon.transform.localRotation.y + Random.Range(vr / -weaponProperties.weapon_stability, vr / weaponProperties.weapon_stability),
                                            weaponProperties.weapon.transform.localRotation.z + Random.Range((weaponProperties.horizontal_recoil_media / 50 + weaponProperties.vertical_recoil_media / 50) / -2, 0),
                                            weaponProperties.weapon.transform.localRotation.w);
        }


        float elapsed = 0f;

        while (elapsed < weaponProperties.apply_recoil_speed)
        {
            elapsed += Time.deltaTime;
            weaponProperties.weapon.transform.localPosition = Vector3.Lerp(start, target, elapsed / weaponProperties.apply_recoil_speed);
            weaponProperties.weapon.transform.localRotation = Quaternion.Lerp(weaponProperties.weapon.transform.localRotation, weaponRotation, elapsed / weaponProperties.apply_recoil_speed);

            yield return null;
        }

        elapsed = 0f;
        Vector3 backTarget = start;

        while (elapsed < weaponProperties.reset_recoil_speed)
        {
            elapsed += Time.deltaTime;
            weaponProperties.weapon.transform.localPosition = Vector3.Lerp(target, backTarget, elapsed / weaponProperties.apply_recoil_speed);
            weaponProperties.weapon.transform.localRotation = Quaternion.Lerp(weaponRotation, weaponProperties.inicial_rotation, elapsed / weaponProperties.reset_recoil_speed);
            yield return null;
        }

        weaponProperties.weapon.transform.localPosition = start;
    }


    void CreateBullet()
    {

        if (recoil_position_in_array >= weaponProperties.horizontal_recoil.Length - 1)
        {
            recoil_position_in_array = 0;
        }
        else
        {
            recoil_position_in_array += 1;
        }

        weaponProperties.barrel.transform.localRotation = new Quaternion(Random.Range(-current_spread, current_spread) / 1000, Random.Range(-current_spread, current_spread) / 1000, Random.Range(-current_spread, current_spread) / 1000, weaponProperties.barrel.transform.localRotation.w);


        Transform bulletObj = Instantiate(weaponProperties.bulletPref, weaponProperties.barrel.transform.position, weaponProperties.barrel.transform.rotation);

        Destroy(bulletObj.gameObject, 10f);


        if (weaponProperties.bullet_hit_effect != null)
        {
            bulletObj.GetComponent<Bullet>().CreateBullet(weaponProperties.barrel.transform.forward, weaponProperties.muzzle_velocity, weaponProperties.bullet_drop, weaponProperties.damage, weaponProperties.damage_dropoff, weaponProperties.damage_dropoff_timer, weaponProperties.destruction_force, weaponProperties.minimum_damage, weaponProperties.headshot_multiplier, weaponProperties.bullet_hit_effect);
        }
        else
        {
            bulletObj.GetComponent<Bullet>().CreateBullet(weaponProperties.barrel.transform.forward, weaponProperties.muzzle_velocity, weaponProperties.bullet_drop, weaponProperties.damage, weaponProperties.damage_dropoff, weaponProperties.damage_dropoff_timer, weaponProperties.destruction_force, weaponProperties.minimum_damage, weaponProperties.headshot_multiplier);
        
        }

        if (playerProperties.is_aiming)
        {
            current_spread += weaponProperties.spread_increaser;
        }
        else
        {
            current_spread += weaponProperties.spread_increaser * 1.5f;
        }

        current_spread = Mathf.Clamp(current_spread, 0, weaponProperties.max_spread);


    }


    void shoot()
    {


        did_shoot = false; // reset

        if (weaponProperties.fire_modes[current_fire_mode] == "auto")
        {

            if (Input.GetKey(keyBinds.shootKey))
            {

                weaponAnimation.StartFireAnimation();

                playerProperties.sprinting = false;
                playerProperties.is_firing = true;


                if (next_time_to_fire <= 0f)
                {


                    did_shoot = true;

                    if (weaponAnimation.fireClip == null)
                    {
                        weaponProperties.CreateBulletExtractor();
                    }

                    for (int i = 0; i < weaponProperties.bullets_per_shot; i++)
                    {
                        CreateBullet();

                    }

                    StartCoroutine(ApplyVisualRecoilOffset());

                    weaponProperties.muzzle_lightinig.enabled = true;
                    if (shoot_sound != null)
                    {
                        shoot_sound.PlayOneShot(shoot_sound.clip);

                    }

                    weaponProperties.mags[^1] -= 1;
                    next_time_to_fire = weaponProperties.interval;
                }
                else
                {
                    weaponProperties.muzzle_lightinig.enabled = false;

                }
            }
            else
            {

                recoil_position_in_array = 0;
                weaponProperties.muzzle_lightinig.enabled = false;
                playerProperties.is_firing = false;
                is_first_shot = false;
                current_spread = 0;
            }

            next_time_to_fire -= Time.deltaTime;
        }
        else if (weaponProperties.fire_modes[current_fire_mode] == "single")
        {

            if (Input.GetKeyDown(keyBinds.shootKey))
            {

                playerProperties.is_firing = true;
                playerProperties.sprinting = false;


                if (next_time_to_fire <= 0f)
                {

                    weaponAnimation.StartFireAnimation();

                    did_shoot = true;

                    if (weaponAnimation.fireClip == null)
                    {
                        weaponProperties.CreateBulletExtractor();
                    }
                    for (int i = 0; i < weaponProperties.bullets_per_shot; i++)
                    {
                        CreateBullet();
                    }

                    StartCoroutine(ApplyVisualRecoilOffset());

                    weaponProperties.muzzle_lightinig.enabled = true;
                    if (shoot_sound != null)
                    {
                        shoot_sound.PlayOneShot(shoot_sound.clip);

                    }

                    weaponProperties.mags[^1] -= 1;
                    next_time_to_fire = weaponProperties.interval;
                }
            }
            else
            {


                current_spread = 0;
                recoil_position_in_array = 0;
                weaponProperties.muzzle_lightinig.enabled = false;
                playerProperties.is_firing = false;
                is_first_shot = false;

            }
            next_time_to_fire -= Time.deltaTime;

        }
        else if (weaponProperties.fire_modes[current_fire_mode] == "burst")
        {
            if (!is_bursting)
            {

                if (Input.GetKeyDown(keyBinds.shootKey))
                {

                    if (next_time_to_fire <= 0f)
                    {
                        weaponAnimation.StartFireAnimation();

                        playerProperties.is_firing = true;
                        playerProperties.sprinting = false;

                        is_bursting = true;
                        bullets_shot_in_current_burst = 0;
                        burst_timer = 0f;
                    }
                }
                else
                {


                    recoil_position_in_array = 0;
                    weaponProperties.muzzle_lightinig.enabled = false;
                    playerProperties.is_firing = false;
                    is_first_shot = false;
                    current_spread = 0;
                }
            }

            if (is_bursting)
            {
                burst_timer -= Time.deltaTime;

                if (burst_timer <= 0f && bullets_shot_in_current_burst < weaponProperties.bullets_per_tap)
                {
                    if (weaponProperties.mags[^1] > 0)
                    {
                        did_shoot = true;

                        weaponProperties.muzzle_lightinig.enabled = true;

                        weaponProperties.mags[^1] -= 1;
                        bullets_shot_in_current_burst++;

                        // Reseta o tempo até o próximo disparo da rajada
                        burst_timer = weaponProperties.time_between_shots_in_burst;

                        if (shoot_sound != null)
                        {
                            shoot_sound.PlayOneShot(shoot_sound.clip);
                        }

                        if (weaponAnimation.fireClip == null)
                        {
                            weaponProperties.CreateBulletExtractor();
                        }
                        for (int i = 0; i < weaponProperties.bullets_per_shot; i++)
                        {
                            CreateBullet();
                        }

                        StartCoroutine(ApplyVisualRecoilOffset());

                    }
                }
                else
                {
                    weaponProperties.muzzle_lightinig.enabled = false;
                }

                // Finaliza a rajada após disparar todos os tiros
                if (bullets_shot_in_current_burst >= weaponProperties.bullets_per_tap)
                {
                    is_bursting = false;
                    next_time_to_fire = weaponProperties.time_between_bursts; // Tempo até poder iniciar nova rajada
                }

            }

            // Tempo geral para atirar novamente
            next_time_to_fire -= Time.deltaTime;

        }


        if (weaponProperties.mags[^1] <= 0)
        {
            weaponProperties.muzzle_lightinig.enabled = false;
            playerProperties.is_firing = false;
        }


    }


    void aim()
    {

        if (Input.GetKey(keyBinds.aimKey) && !playerProperties.is_reloading && !switchWeapon._switch)
        {

            // AIMING
            playerProperties.sprinting = false;
            playerProperties.is_aiming = true;

            float targetFov = minFov / weaponProperties.zoom;

            // POSIÇÃO
            ads_position.transform.localPosition = Vector3.MoveTowards(
                                                            ads_position.transform.localPosition,
                                                            weaponProperties.ads_position + attatchment_change_ads_position,
                                                            weaponProperties.ads_speed * Time.deltaTime
                                                            );

            if (ads_position.transform.localPosition == weaponProperties.ads_position + attatchment_change_ads_position)
            {
                weaponProperties.barrel.transform.position = screen_center.transform.position;
                dot_position = true;
            }

            player_camera.fieldOfView = Mathf.Lerp(player_camera.fieldOfView, targetFov, 10 * Time.deltaTime);

        }
        else
        {
            weaponProperties.barrel.transform.localPosition = original_barrel_pos;

            dot_position = false;
            player_camera.fieldOfView = Mathf.Lerp(
            player_camera.fieldOfView,
            minFov,
            10 * Time.deltaTime);

            playerProperties.is_aiming = false;

            ads_position.transform.localPosition = Vector3.Lerp(
                ads_position.transform.localPosition,
                original_ads_position,
                5 * Time.deltaTime
            );

        }

    }

}