using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("State")]
    public bool is_active;
    [HideInInspector] public bool is_side_grip_activated;

    [Header("HUD")]
    [SerializeField] private SoldierHudManager soldierHudManager;

    [Header("Instances")]
    [SerializeField] private DummyBullet dummyBullet;
    [SerializeField] private PlayerNetworkObjectSpawner playerNetworkObjectSpawner;
    [SerializeField] private ThirdPersonWeaponController thirdPersonWeapon;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private Camera player_camera;
    [SerializeField] private SwitchWeapon switchWeapon;

    [Header("Sounds")]
    public AudioSource switch_fire_mode_sound;

    [Header("Variables")]
    [HideInInspector] public bool can_aim = true;
    [HideInInspector] public bool can_shoot = true;

    private AdsBehaviour adsBehaviour;
    private bool can_reload;
    private float reload_cooldown;
    private float next_time_to_fire = 0f;
    [HideInInspector] public bool did_shoot = false;
    private int current_fire_mode = 0;
    private float burst_timer = 0f;
    private int bullets_shot_in_current_burst = 0;
    private bool is_bursting;
    private bool is_last_bullet;
    private bool is_first_shot;
    private int recoil_position_in_array = 0;
    private WeaponSounds weaponSounds;
    [HideInInspector] public WeaponProperties weaponProperties;
    private PlayerController playerController;
    private Shell shell;
    [HideInInspector] public WeaponAnimation weaponAnimation;
    private Sight sight_attatchment;
    private float current_spread;
    private bool restarted;
    private int reserve_ammo;
    private float crouch_recoil_multiplier = 0.8f;
    private float time_to_contatenate = 0;

    #region Unity Lifecycle Methods
    void Awake()
    {
        restarted = false;
        adsBehaviour = GetComponent<AdsBehaviour>();
    }

    void Update()
    {

        if (weaponProperties != null)
            Reload();

        if (!restarted || !is_active || SettingsHUD.Instance.is_menu_settings_active)
        {
            playerProperties.is_firing = false;
            return;
        }

        if (adsBehaviour == null) adsBehaviour = GetComponent<AdsBehaviour>();

        ConcatenateBullets();

        if (Input.GetKeyDown(Settings.Instance._keybinds.WEAPON_reloadKey))
        {
            HandleReload();
        }

        if (soldierHudManager.mag_counter_hud != null && weaponProperties != null) soldierHudManager.mag_counter_hud.UpdateMagCount(0, weaponProperties.mags);

        if (can_shoot && !playerProperties.is_reloading)
        {
            shoot();
        }

        if (!playerProperties.is_firing)
        {
            current_spread = Spread.ResetSpread(current_spread, weaponProperties.base_spread, weaponProperties.spread_recovery);
        }

        if (Input.GetKeyDown(Settings.Instance._keybinds.WEAPON_switchFireModeKey))
        {
            HandleFireModeSwitch();
        }
    }


    #endregion

    #region Initialization

    public void Restart()
    {

        weaponProperties = GetComponentInChildren<WeaponProperties>();
        weaponAnimation = GetComponent<WeaponAnimation>();
        playerController = GetComponentInParent<PlayerController>();
        sight_attatchment = GetComponentInChildren<Sight>();
        weaponSounds = GetComponentInChildren<WeaponSounds>();

        shell = weaponProperties.GetComponentInChildren<Shell>();
        time_to_contatenate = weaponProperties.time_to_transfer_bullets;

        current_fire_mode = 0;
        next_time_to_fire = 0f;
        can_reload = true;

        playerProperties.is_reloading = false;
        restarted = true;

        weaponProperties.weapon.transform.localPosition = weaponProperties.initial_potiion;
        weaponProperties.weapon.transform.localRotation = weaponProperties.inicial_rotation;

        if (sight_attatchment != null)
        {
            AdsBehaviour.Instance.Setup(sight_attatchment.adsPosition, weaponProperties.ads_speed, weaponProperties.zoom);
        }
        else
        {
            AdsBehaviour.Instance.Setup(null, weaponProperties.ads_speed, weaponProperties.zoom);
        }


        SwitchFireMode();
    }

    #endregion

    #region Fire Mode

    private void HandleFireModeSwitch()
    {
        if (weaponProperties.fire_modes.Count == 1) return;

        switch_fire_mode_sound.Play();

        if (current_fire_mode < weaponProperties.fire_modes.Count - 1)
        {
            current_fire_mode += 1;
        }
        else
        {
            current_fire_mode = 0;
        }

        SwitchFireMode();
    }

    void SwitchFireMode()
    {
        WeaponProperties.FireMode currentMode = weaponProperties.fire_modes[current_fire_mode];

        if (currentMode == WeaponProperties.FireMode.Auto)
        {
            soldierHudManager.fire_mode_hud.SetFireMode(WeaponProperties.FireMode.Auto);
            weaponProperties.can_hold_trigger = true;
            weaponProperties.CalculateRecoilSpeed(false);
        }
        else if (currentMode == WeaponProperties.FireMode.Burst)
        {
            soldierHudManager.fire_mode_hud.SetFireMode(WeaponProperties.FireMode.Burst);
            weaponProperties.can_hold_trigger = false;
            weaponProperties.CalculateRecoilSpeed(true);
        }
        else
        {
            soldierHudManager.fire_mode_hud.SetFireMode(WeaponProperties.FireMode.Single);
            weaponProperties.can_hold_trigger = false;
            weaponProperties.CalculateRecoilSpeed(false);
        }
    }

    #endregion

    #region Reload

    void HandleReload()
    {
        if (playerProperties.is_firing || playerProperties.is_reloading || playerProperties.roll || reserve_ammo == 0)
        {
            if (reserve_ammo == 0)
                GeneralHudAlertMessages.Instance.CreateMessage("Cant reload", 2);
            return;
        }

        weaponAnimation.StartReloadAnimation();

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

    void Reload()
    {
        CalculateReserveAmmo();

        if (reserve_ammo == 0 || !playerProperties.is_reloading)
        {
            can_reload = false;
            return;
        }

        if (!weaponProperties.single_reload)
        {
            HandleStandardReload();
        }
        else
        {
            HandleSingleReload();
        }
    }

    private void CalculateReserveAmmo()
    {
        reserve_ammo = 0;
        for (int i = 0; i < weaponProperties.mags.Count - 1; i++)
        {
            reserve_ammo += weaponProperties.mags[i];
        }
    }

    private void HandleStandardReload()
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
            TransferMagazineAmmo();
            can_shoot = true;
            playerProperties.is_reloading = false;
            reload_cooldown = 0;

            weaponAnimation.FinishReloadAnimation();
        }
    }

    private void TransferMagazineAmmo()
    {
        int max = 0;
        int index = 0;

        for (int i = 0; i < weaponProperties.mag_count; i++)
        {
            if (weaponProperties.mags[i] > max)
            {
                max = weaponProperties.mags[i];
                index = i;
            }
        }

        is_last_bullet = weaponProperties.mags[^1] == 0;

        int temp = weaponProperties.mags[^1];
        ApplyMagAmmo(max);

        if (!is_last_bullet)
        {
            weaponProperties.mags[^1]++;
        }

        weaponProperties.mags[index] = temp;
    }

    public void ApplyMagAmmo(int amount)
    {
        weaponProperties.mags[^1] = amount;
    }
    public void RemoveMagAmmo(int amount, int index)
    {
        weaponProperties.mags[index] -= amount;
    }

    private void HandleSingleReload()
    {
        if (playerProperties.is_reloading &&
            can_reload &&
            weaponProperties.mags[^1] != weaponProperties.bullets_per_mag &&
            !playerProperties.is_firing &&
            weaponProperties.shells > 0)
        {
            shell.Reload();
        }
        else
        {

            playerProperties.is_reloading = false;
        }
    }

    #endregion

    #region Shooting

    void shoot()
    {
        bool hold_shoot = Input.GetKey(Settings.Instance._keybinds.WEAPON_shootKey);
        bool press_shoot = Input.GetKeyDown(Settings.Instance._keybinds.WEAPON_shootKey);


        if (ShouldBlockShooting())
        {
            if (Input.GetKeyDown(Settings.Instance._keybinds.WEAPON_shootKey) && weaponProperties.mags[^1] == 0)
            {
                GeneralHudAlertMessages.Instance.CreateMessage("Not enought ammo", 2);
            }
            return;
        }

        did_shoot = false;
        WeaponProperties.FireMode currentMode = weaponProperties.fire_modes[current_fire_mode];

        switch (currentMode)
        {
            case WeaponProperties.FireMode.Auto:
                HandleAutoFire(hold_shoot);
                break;
            case WeaponProperties.FireMode.Single:
                HandleSingleFire(press_shoot);
                break;
            case WeaponProperties.FireMode.Burst:
                HandleBurstFire(press_shoot);
                break;
        }

        if (weaponProperties.mags[^1] <= 0)
        {
            playerProperties.is_firing = false;
        }
    }

    private bool ShouldBlockShooting()
    {
        return playerProperties.is_reloading ||
               playerProperties.roll ||
               playerProperties.is_dead.Value ||
               weaponProperties.mags[^1] == 0;
    }

    private void HandleAutoFire(bool hold_shoot)
    {
        if (hold_shoot)
        {
            PrepareForShot();

            if (next_time_to_fire <= 0f)
            {
                ExecuteShot();
                next_time_to_fire = weaponProperties.interval;
            }
        }
        else
        {
            ResetShotState();
        }

        next_time_to_fire -= Time.deltaTime;
    }

    private void HandleSingleFire(bool press_shoot)
    {
        if (press_shoot)
        {
            playerProperties.is_firing = true;
            playerProperties.sprinting = false;

            if (next_time_to_fire <= 0f)
            {
                PrepareForShot();
                ExecuteShot();
                next_time_to_fire = weaponProperties.interval;
            }
        }
        else
        {
            ResetShotState();
        }

        next_time_to_fire -= Time.deltaTime;
    }

    private void HandleBurstFire(bool press_shoot)
    {
        if (!is_bursting && press_shoot && next_time_to_fire <= 0f)
        {
            StartBurst();
        }
        else if (!is_bursting)
        {
            ResetShotState();
        }

        if (is_bursting)
        {
            UpdateBurst();
        }

        next_time_to_fire -= Time.deltaTime;
    }

    private void PrepareForShot()
    {
        weaponAnimation.StartFireAnimation();
        playerProperties.sprinting = false;
        playerProperties.is_firing = true;
    }

    private void ExecuteShot()
    {
        did_shoot = true;

        if (weaponAnimation.fireClip == null)
        {
            weaponProperties.CreateBulletExtractor();
        }

        CreateBullet();
        StartCoroutine(ApplyVisualRecoilOffset());
        weaponProperties.mags[^1] -= 1;
    }

    private void ResetShotState()
    {
        recoil_position_in_array = 0;
        playerProperties.is_firing = false;
        is_first_shot = false;
    }

    private void StartBurst()
    {
        weaponAnimation.StartFireAnimation();
        playerProperties.is_firing = true;
        playerProperties.sprinting = false;

        is_bursting = true;
        bullets_shot_in_current_burst = 0;
        burst_timer = 0f;
    }

    private void UpdateBurst()
    {
        burst_timer -= Time.deltaTime;

        if (burst_timer <= 0f && bullets_shot_in_current_burst < weaponProperties.bullets_per_tap)
        {
            if (weaponProperties.mags[^1] > 0)
            {
                ExecuteBurstShot();
            }
        }

        if (bullets_shot_in_current_burst >= weaponProperties.bullets_per_tap)
        {
            EndBurst();
        }
    }

    private void ExecuteBurstShot()
    {
        did_shoot = true;
        weaponProperties.mags[^1] -= 1;
        bullets_shot_in_current_burst++;
        burst_timer = weaponProperties.time_between_shots_in_burst;

        if (weaponAnimation.fireClip == null)
        {
            weaponProperties.CreateBulletExtractor();
        }

        CreateBullet();
        StartCoroutine(ApplyVisualRecoilOffset());
    }

    private void EndBurst()
    {
        is_bursting = false;
        next_time_to_fire = weaponProperties.time_between_bursts;
    }

    void CreateBullet()
    {

        for (int i = 0; i < weaponProperties.bullets_per_shot; i++)
        {
            UpdateRecoilPosition();
            SpawnBullet();
        }

        if (weaponSounds != null)
            weaponSounds.ShootSound();
    }

    private void UpdateRecoilPosition()
    {
        if (recoil_position_in_array >= weaponProperties.horizontal_recoil.Length - 1)
        {
            recoil_position_in_array = 0;
        }
        else
        {
            recoil_position_in_array += 1;
        }
    }

    private void SpawnBullet()
    {
        Quaternion finalRotation = Spread.CalculateSpreadRotation(weaponProperties.barrel.transform, current_spread);
        if (current_spread < weaponProperties.max_spread)
        {
            current_spread = Spread.AddSpread(current_spread, weaponProperties.spread_increaser);
        }
        Bullet.BulletData data = new Bullet.BulletData
        {
            position = weaponProperties.barrel.transform.position,
            rotation = finalRotation,
            direction = finalRotation * Vector3.forward,
            speed = weaponProperties.muzzle_velocity,
            dropMultiplier = weaponProperties.bullet_drop,
            infantaryDamage = weaponProperties.infantry_damage,
            damageDropoff = weaponProperties.damage_dropoff,
            damageDropoffTimer = weaponProperties.damage_dropoff_timer,
            destructionForce = weaponProperties.destruction_force,
            minimumDamage = weaponProperties.minimum_damage,
            hsMultiplier = weaponProperties.headshot_multiplier,
            size = weaponProperties.bullet_size,
            canDamageVehicles = weaponProperties.can_damage_vehicles,
            vehicleDamage = weaponProperties.vehicle_damage,
            delaytoEnableForNonOwner = 0.2f,
        };

        DummyBullet instantiatedDummyBullet = Instantiate(dummyBullet, data.position, data.rotation);
        instantiatedDummyBullet.CreateBullet(data, playerController.transform);

        NetworkObject shooterNetObj = playerController.GetComponent<NetworkObject>();
        playerNetworkObjectSpawner.ServerSpawnBullet(weaponProperties.bulletPref.gameObject, data, shooterNetObj, weaponProperties.gameObject.name);
    }



    IEnumerator ApplyVisualRecoilOffset()
    {
        float vr = weaponProperties.vertical_recoil[recoil_position_in_array];
        float hr = weaponProperties.horizontal_recoil[recoil_position_in_array];

        ApplyRecoilToCamera(vr, hr);

        float randomizedHr = RandomizeHorizontalRecoil(hr);
        float randomizedVr = UnityEngine.Random.Range(0, vr);

        Vector3 recoilOffset = GetRecoilOffset();
        Vector3 start = weaponProperties.initial_potiion;
        Vector3 target = start + recoilOffset;

        Quaternion weaponRotation = CalculateWeaponRotation(randomizedHr, randomizedVr);

        yield return StartCoroutine(ApplyRecoilAnimation(start, target, weaponRotation));
    }

    private void ApplyRecoilToCamera(float vr, float hr)
    {
        if (!is_first_shot || weaponProperties.mags[^1] == 1)
        {
            if (playerProperties.crouched || playerProperties.is_proned)
            {
                playerController.ApplyCameraRecoil(
                    (vr * weaponProperties.first_shoot_increaser) * crouch_recoil_multiplier,
                    (hr * weaponProperties.first_shoot_increaser) / 1.3f
                );
            }
            else
            {
                playerController.ApplyCameraRecoil(
                    vr * weaponProperties.first_shoot_increaser,
                    hr * weaponProperties.first_shoot_increaser
                );
            }
            is_first_shot = true;
        }
        else
        {
            if (playerProperties.crouched || playerProperties.is_proned)
            {
                playerController.ApplyCameraRecoil(vr * crouch_recoil_multiplier, hr * crouch_recoil_multiplier);
            }
            else
            {
                playerController.ApplyCameraRecoil(vr, hr);
            }
        }
    }

    private float RandomizeHorizontalRecoil(float hr)
    {
        if (hr < 0)
        {
            return UnityEngine.Random.Range(hr, hr * -1);
        }
        else
        {
            return UnityEngine.Random.Range(-hr, hr);
        }
    }

    private Vector3 GetRecoilOffset()
    {
        return playerProperties.is_aiming == false
            ? weaponProperties.visual_recoil
            : new Vector3(
                weaponProperties.visual_recoil.x / 2,
                weaponProperties.visual_recoil.y / 2,
                weaponProperties.visual_recoil.z / 2
            );
    }

    private Quaternion CalculateWeaponRotation(float hr, float vr)
    {
        if (playerProperties.is_aiming)
        {
            return new Quaternion(
                weaponProperties.weapon.transform.localRotation.x + UnityEngine.Random.Range(hr / -weaponProperties.weapon_stability, 0),
                weaponProperties.weapon.transform.localRotation.y + UnityEngine.Random.Range(vr / -weaponProperties.weapon_stability, vr / weaponProperties.weapon_stability),
                weaponProperties.weapon.transform.localRotation.z + UnityEngine.Random.Range((weaponProperties.horizontal_recoil_media / 40 + weaponProperties.vertical_recoil_media / 40) / -2, 0),
                weaponProperties.weapon.transform.localRotation.w
            );
        }

        return new Quaternion(
            weaponProperties.weapon.transform.localRotation.x + UnityEngine.Random.Range(-0.02f, 0.02f),
            weaponProperties.weapon.transform.localRotation.y + UnityEngine.Random.Range(-0.02f, 0.02f),
            weaponProperties.weapon.transform.localRotation.z + UnityEngine.Random.Range(-0.02f, 0.02f),
            weaponProperties.weapon.transform.localRotation.w
        );
    }

    private IEnumerator ApplyRecoilAnimation(Vector3 start, Vector3 target, Quaternion weaponRotation)
    {
        float elapsed = 0f;

        while (elapsed < weaponProperties.weapon_apply_recoil_speed)
        {
            elapsed += Time.deltaTime;
            weaponProperties.weapon.transform.localPosition = Vector3.Lerp(start, target, elapsed / weaponProperties.weapon_apply_recoil_speed);
            weaponProperties.weapon.transform.localRotation = Quaternion.Lerp(
                weaponProperties.weapon.transform.localRotation,
                weaponRotation,
                elapsed / weaponProperties.weapon_apply_recoil_speed
            );
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < weaponProperties.weapon_reset_recoil_speed)
        {
            elapsed += Time.deltaTime;
            weaponProperties.weapon.transform.localPosition = Vector3.Lerp(
                weaponProperties.weapon.transform.localPosition,
                start,
                elapsed / weaponProperties.weapon_apply_recoil_speed
            );
            weaponProperties.weapon.transform.localRotation = Quaternion.Lerp(
                weaponProperties.weapon.transform.localRotation,
                weaponProperties.inicial_rotation,
                elapsed / weaponProperties.weapon_reset_recoil_speed
            );
            yield return null;
        }
    }

    #endregion

    #region Bullet Concatenation

    private void ConcatenateBullets()
    {
        if (weaponProperties.mags == null || weaponProperties.mags.Count == 0)
        {
            return;
        }

        if (Input.GetKey(Settings.Instance._keybinds.WEAPON_composeBulletsKey) &&
            !playerProperties.is_firing &&
            !playerProperties.is_reloading)
        {
            ProcessBulletConcatenation();
        }
        else
        {
            ResetConcatenation();
        }
    }

    private void ProcessBulletConcatenation()
    {
        playerProperties.is_composing_bullets = true;
        time_to_contatenate -= Time.deltaTime;

        if (time_to_contatenate <= 0)
        {
            TransferBulletsBetweenMags();
            time_to_contatenate = weaponProperties.time_to_transfer_bullets;
        }
    }

    private void TransferBulletsBetweenMags()
    {
        int minPosition = FindMinMagazinePosition();
        int maxPosition = FindMaxMagazinePosition();

        if (minPosition == -1 || maxPosition == -1 || minPosition == maxPosition)
        {
            Debug.Log("Não é possível transferir munição");
            time_to_contatenate = 2;
            return;
        }

        int min = weaponProperties.mags[minPosition];
        int max = weaponProperties.mags[maxPosition];

        int transfer_amount = Mathf.Min(
            min,
            weaponProperties.bullets_per_mag - max
        );

        if (transfer_amount > 0)
        {
            ExecuteBulletTransfer(minPosition, maxPosition, transfer_amount);
        }
        else
        {
            Debug.Log("Nada para transferir");
        }
    }

    private int FindMinMagazinePosition()
    {
        int min = weaponProperties.mags[0];
        int min_position = 0;

        for (int i = 1; i < weaponProperties.mags.Count; i++)
        {
            if (weaponProperties.mags[i] < min)
            {
                min = weaponProperties.mags[i];
                min_position = i;
            }
        }

        if (min == 0)
        {
            min = int.MaxValue;
            min_position = -1;

            for (int i = 0; i < weaponProperties.mags.Count; i++)
            {
                int mag = weaponProperties.mags[i];
                if (mag > 0 && mag < min)
                {
                    min = mag;
                    min_position = i;
                }
            }

            if (min_position == -1)
            {
                Debug.Log("Todos os magazines estão vazios!");
                time_to_contatenate = 2;
                return -1;
            }
        }

        return min_position;
    }

    private int FindMaxMagazinePosition()
    {
        int max = weaponProperties.mags[0];
        int max_position = 0;

        for (int i = 1; i < weaponProperties.mags.Count; i++)
        {
            int mag = weaponProperties.mags[i];
            if (mag > max && mag < weaponProperties.bullets_per_mag)
            {
                max = mag;
                max_position = i;
            }
        }

        return max >= weaponProperties.bullets_per_mag ? -1 : max_position;
    }

    private void ExecuteBulletTransfer(int fromPosition, int toPosition, int amount)
    {

        weaponProperties.mags[fromPosition] -= amount;
        weaponProperties.mags[toPosition] += amount;

    }

    private void ResetConcatenation()
    {
        playerProperties.is_composing_bullets = false;
        time_to_contatenate = weaponProperties.time_to_transfer_bullets;
    }

    #endregion
}