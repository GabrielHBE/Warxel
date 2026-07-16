using System.Collections;
using FishNet.Object;
using UnityEngine;

public class Weapon : MonoBehaviour, ICurrentSpreadUIValues
{
    public const float LAST_MAG_RELOAD_TIMER_INCREASER = 1;

    [Header("State")]
    public bool is_active;
    [HideInInspector] public bool is_side_grip_activated;

    [Header("HUD")]
    [SerializeField] private SoldierHudManager soldierHudManager;

    [Header("Bullets")]
    [SerializeField] private DummyProjectile dummyBullet;
    [SerializeField] private GameObject bullet;

    [Header("Instances")]
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
    private Coroutine applyRotationRecoilCoroutine;
    private AdsBehaviour adsBehaviour;
    private bool can_reload;
    private float reload_cooldown;
    [HideInInspector] public bool did_shoot = false;
    private WeaponSounds weaponSounds;
    [HideInInspector] public WeaponProperties weaponProperties;
    private PlayerController playerController;
    private Shell shell;
    [HideInInspector] public WeaponAnimation weaponAnimation;
    private Sight sight_attatchment;
    private float current_spread;
    private bool restarted;
    private int reserve_ammo;
    private float time_to_contatenate = 0;
    private string ammo;
    private Quaternion initialRotation;

    // REMOVIDO: private int firingStateId;

    #region Unity Lifecycle Methods
    void Awake()
    {
        initialRotation = transform.localRotation;
        restarted = false;
        adsBehaviour = GetComponent<AdsBehaviour>();
    }

    void Update()
    {
        if (weaponProperties != null)
            Reload();

        if (!restarted || !is_active)
        {
            playerProperties.is_firing = false;
            return;
        }

        if (adsBehaviour == null) adsBehaviour = GetComponent<AdsBehaviour>();

        ConcatenateBullets();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_reloadKey))
        {
            HandleReload();
        }

        if (weaponProperties != null)
        {
            UpdateAmmoHUD();
        }

        // ATUALIZADO: sem stateId
        Firing.UpdateTimeToFire(Time.deltaTime);

        if (can_shoot && !playerProperties.is_reloading)
        {
            ProcessShooting();
        }

        if (!playerProperties.is_firing)
        {
            HandleRecoilReset();
        }

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_switchFireModeKey))
        {
            HandleFireModeSwitch();
        }
    }
    #endregion

    #region Initialization
    public void Restart()
    {
        transform.localRotation = initialRotation;
        weaponProperties = GetComponentInChildren<WeaponProperties>();
        weaponAnimation = GetComponent<WeaponAnimation>();
        playerController = GetComponentInParent<PlayerController>();
        sight_attatchment = GetComponentInChildren<Sight>();
        weaponSounds = GetComponentInChildren<WeaponSounds>();

        shell = weaponProperties.GetComponentInChildren<Shell>();
        time_to_contatenate = weaponProperties.reloadValues.timeToTransferAmmo;

        can_reload = true;
        playerProperties.is_reloading = false;
        restarted = true;

        weaponProperties.weapon.transform.localPosition = weaponProperties.initial_potiion;
        weaponProperties.weapon.transform.localRotation = weaponProperties.initial_rotation;

        if (sight_attatchment != null)
        {
            AdsBehaviour.Instance.Setup(sight_attatchment.adsPosition, weaponProperties.ads_speed, weaponProperties.zoom);
        }
        else
        {
            AdsBehaviour.Instance.Setup(null, weaponProperties.ads_speed, weaponProperties.zoom);
        }

        current_spread = weaponProperties.spreadValues.baseSpread;

        // Initialize firing system
        SetupFiringSystem();
    }

    private void SetupFiringSystem()
    {
        // ATUALIZADO: sem stateId, apenas reseta o estado
        Firing.ResetState();
        // Garante que o modo de tiro estático atual é válido para este armamento
        if (weaponProperties != null && weaponProperties.firing.fireModes != null && weaponProperties.firing.fireModes.Count > 0)
        {
            if (!weaponProperties.firing.fireModes.Contains(Firing.GetCurrentFireMode()))
            {
                Firing.SwitchFireMode(weaponProperties.firing.fireModes);
            }
        }

        // Update HUD with current fire mode
        UpdateFireModeHUD(Firing.GetCurrentFireMode());
    }

    private void UpdateAmmoHUD()
    {
        ammo = weaponProperties.reloadValues.mags[^1].ToString("F0") + " / ";
        for (int i = 0; i < weaponProperties.reloadValues.mags.Count - 1; i++)
        {
            ammo += weaponProperties.reloadValues.mags[i].ToString("F0") + " ";
        }
        soldierHudManager.SetCurrentAmmo(ammo);
    }
    #endregion

    #region Fire Mode
    private void HandleFireModeSwitch()
    {
        if (!Firing.CanSwitchFireMode(weaponProperties.firing.fireModes)) return;

        switch_fire_mode_sound.Play();

        // ATUALIZADO: sem stateId
        Firing.FireMode newMode = Firing.SwitchFireMode(weaponProperties.firing.fireModes);
        UpdateFireModeHUD(newMode);
    }

    private void UpdateFireModeHUD(Firing.FireMode mode)
    {
        soldierHudManager.fire_mode_hud.SetFireMode(mode);
    }
    #endregion

    #region Reload
    void HandleReload()
    {
        int reserveAmmo = weaponProperties.reloadValues.GetTotalReserveAmmo();

        if (!ProcessReload.Reload.ReloadLogic.CanStartReload(
            weaponProperties.reloadValues,
            playerProperties.is_firing,
            playerProperties.is_reloading,
            playerProperties.roll,
            reserveAmmo))
        {
            if (reserveAmmo == 0)
                GeneralHudAlertMessages.Instance.CreateMessage("Cant reload", 2);
            return;
        }

        weaponAnimation.StartReloadAnimation();

        bool isEmpty = weaponProperties.reloadValues.IsMagazineEmpty();
        float totalReloadTime = ProcessReload.Reload.ReloadLogic.CalculateReloadTime(weaponProperties.reloadValues, isEmpty);

        if (weaponAnimation.fireClip != null)
        {
            if (!weaponAnimation.is_in_fire_animation)
            {
                reload_cooldown = totalReloadTime;
                playerProperties.is_reloading = true;
                can_reload = true;
            }
        }
        else
        {
            reload_cooldown = totalReloadTime;
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

        if (!weaponProperties.reloadValues.isSingleReload)
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
        reserve_ammo = weaponProperties.reloadValues.GetTotalReserveAmmo();
    }

    private void HandleStandardReload()
    {
        bool isEmpty = weaponProperties.reloadValues.IsMagazineEmpty();

        var result = ProcessReload.Reload.ReloadLogic.ProcessStandardReload(
            weaponProperties.reloadValues,
            reload_cooldown,
            Time.deltaTime,
            isEmpty
        );

        reload_cooldown = result.remainingCooldown;
        can_shoot = result.canShoot;
        playerProperties.is_reloading = result.isReloading;

        if (result.shouldFinishReload)
        {
            weaponAnimation.FinishReloadAnimation();
            // ATUALIZADO: sem stateId
            Firing.ResetState();
        }
    }

    public void ApplyMagAmmo(int amount)
    {
        weaponProperties.reloadValues.mags[^1] = amount;
    }

    public void RemoveMagAmmo(int amount, int index)
    {
        weaponProperties.reloadValues.mags[index] -= amount;
    }

    private void HandleSingleReload()
    {
        bool shouldContinue = ProcessReload.Reload.ReloadLogic.ProcessSingleReload(
            weaponProperties.reloadValues,
            playerProperties.is_reloading,
            can_reload,
            playerProperties.is_firing,
            out bool shouldContinueReloading
        );

        if (shouldContinueReloading)
        {
            shell.Reload();
        }
        else if (!shouldContinue)
        {
            playerProperties.is_reloading = false;
        }
    }
    #endregion

    #region Shooting
    private void ProcessShooting()
    {
        bool holdShoot = InputManager.GetKey(Settings.Instance._keybinds.WEAPON_shootKey);
        bool pressShoot = InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_shootKey);

        // Check if ammo is empty for alert
        if (pressShoot && weaponProperties.reloadValues.mags[^1] == 0)
        {
            GeneralHudAlertMessages.Instance.CreateMessage("Not enough ammo", 2);
            return;
        }

        // ATUALIZADO: Process shooting through Firing system (sem stateId)
        var result = Firing.ProcessShooting(
            weaponProperties.firing,
            holdShoot,
            pressShoot,
            playerProperties.is_reloading,
            playerProperties.roll,
            playerProperties.is_dead.Value,
            weaponProperties.reloadValues.mags[^1],
            Time.deltaTime
        );

        // Handle the result
        if (result.shouldResetShotState)
        {
            ResetShotState();
        }

        if (result.shouldShoot)
        {
            ExecuteShot(result.isFirstShot);
        }

        // ATUALIZADO: sem stateId
        playerProperties.is_firing = Firing.IsFiring();

        if (weaponProperties.reloadValues.mags[^1] <= 0)
        {
            playerProperties.is_firing = false;
        }
    }

    private void ExecuteShot(bool isFirstShot)
    {
        did_shoot = true;

        if (weaponAnimation != null)
        {
            weaponAnimation.StartFireAnimation();
        }

        if (weaponAnimation.fireClip == null)
        {
            weaponProperties.CreateBulletExtractor();
        }

        // Apply recoil using the next recoil index
        int patternLength = weaponProperties.recoilValues.recoilPattern.Length;
        if (patternLength > 0)
        {
            // ATUALIZADO: sem stateId
            int recoilIndex = Firing.GetNextRecoilIndex(patternLength);

            // Extra safety check to ensure index is within bounds
            if (recoilIndex >= 0 && recoilIndex < patternLength)
            {
                StartCoroutine(ApplyVisualRecoilOffset(recoilIndex, isFirstShot));
            }
            else
            {
                Debug.LogWarning($"Recoil index {recoilIndex} out of range for pattern length {patternLength}");
                // Fallback to index 0
                StartCoroutine(ApplyVisualRecoilOffset(0, isFirstShot));
            }
        }

        // Create bullet
        CreateBullet();

        // Remove ammo
        weaponProperties.reloadValues.mags[^1] -= 1;
    }

    private void ResetShotState()
    {
        // ATUALIZADO: sem stateId
        Firing.ResetRecoilIndex();
        Firing.ResetState();
        playerProperties.is_firing = false;
    }

    private void HandleRecoilReset()
    {
        if (applyRotationRecoilCoroutine != null)
        {
            StopCoroutine(applyRotationRecoilCoroutine);
            applyRotationRecoilCoroutine = null;
        }

        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            initialRotation,
            Time.deltaTime * 5
        );

        current_spread = Spread.ResetSpread(current_spread, weaponProperties.spreadValues.baseSpread, weaponProperties.spreadValues.spreadRecovery);
    }

    void CreateBullet()
    {
        for (int i = 0; i < weaponProperties.firing.bulletsPerShot; i++)
        {
            SpawnBullet();
        }

        if (weaponSounds != null) weaponSounds.ShootSound();
    }

    private void SpawnBullet()
    {
        Quaternion finalRotation = Spread.CalculateSpreadRotation(weaponProperties.barrel.transform, current_spread);

        current_spread = Spread.AddSpread(current_spread, weaponProperties.spreadValues.spreadIncreaser, weaponProperties.spreadValues.maxSpread);

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = weaponProperties.barrel.transform.position,
            rotation = finalRotation,
            ignoredObject = transform.root
        };

        if(ProjectileSpawner.Instance!=null) ProjectileSpawner.Instance.CreateProjectile(bullet, dummyBullet.gameObject, prop, weaponProperties.projectileValues);

        /*
        LocalObjectPooling.Instance.GetPooledItem(dummyBullet.gameObject).GetComponent<DummyProjectile>().CreateBullet(prop, weaponProperties.projectileValues);
        playerNetworkObjectSpawner.ServerSpawnBullet(serverBullet, prop, weaponProperties.projectileValues, weaponProperties.gameObject.name);
        */
    }

    // ATUALIZADO: recebe isFirstShot como parâmetro
    IEnumerator ApplyVisualRecoilOffset(int recoilIndex, bool isFirstShot)
    {
        // Safety check
        if (recoilIndex < 0 || recoilIndex >= weaponProperties.recoilValues.recoilPattern.Length)
        {
            recoilIndex = 0;
        }

        ApplyRecoilToCamera(
            weaponProperties.recoilValues.recoilPattern[recoilIndex].verticalRecoil,
            weaponProperties.recoilValues.recoilPattern[recoilIndex].horizontalRecoil,
            isFirstShot
        );

        Vector3 recoilOffset = GetRecoilOffset();
        Vector3 start = weaponProperties.initial_potiion;
        Vector3 target = start + recoilOffset;

        yield return StartCoroutine(ApplyPositionRecoilAnimation(start, target));
    }

    private void ApplyRecoilToCamera(float vr, float hr, bool isFirstShot)
    {
        var recoil = Recoil.CalculateCameraRecoil(
            vr,
            hr,
            weaponProperties.recoilValues.firstShootRecoilMultiplier,
            isFirstShot,
            weaponProperties.reloadValues.mags[^1]
        );

        playerController.ApplyCameraRecoil(recoil.vertical, recoil.horizontal);
    }

    private Vector3 GetRecoilOffset()
    {
        return Recoil.CalculateVisualRecoilOffset(
            weaponProperties.recoilValues.visual_recoil,
            playerProperties.is_aiming
        );
    }

    private IEnumerator ApplyPositionRecoilAnimation(Vector3 start, Vector3 target)
    {
        float elapsed = 0f;
        float originalFOV = playerController.playerCamera.fieldOfView;
        while (elapsed < weaponProperties.recoilValues.applyRecoilSpeed)
        {
            elapsed += Time.deltaTime;
            weaponProperties.weapon.transform.localPosition = Vector3.Lerp(start, target, elapsed / weaponProperties.recoilValues.applyRecoilSpeed);
            playerController.playerCamera.fieldOfView = Mathf.Lerp(playerController.playerCamera.fieldOfView, playerController.playerCamera.fieldOfView + 0.3f, elapsed / weaponProperties.recoilValues.applyRecoilSpeed);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < weaponProperties.recoilValues.resetRecoilSpeed)
        {
            elapsed += Time.deltaTime;
            weaponProperties.weapon.transform.localPosition = Vector3.Lerp(
                weaponProperties.weapon.transform.localPosition,
                start,
                elapsed / weaponProperties.recoilValues.applyRecoilSpeed
            );
            playerController.playerCamera.fieldOfView = Mathf.Lerp(playerController.playerCamera.fieldOfView, originalFOV, elapsed / weaponProperties.recoilValues.applyRecoilSpeed);

            yield return null;
        }
    }
    #endregion

    #region Bullet Concatenation
    private void ConcatenateBullets()
    {
        if (weaponProperties.reloadValues.mags == null || weaponProperties.reloadValues.mags.Count == 0)
        {
            return;
        }

        if (InputManager.GetKey(Settings.Instance._keybinds.WEAPON_composeBulletsKey) &&
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
            time_to_contatenate = weaponProperties.reloadValues.timeToTransferAmmo;
        }
    }

    private void TransferBulletsBetweenMags()
    {
        ProcessReload.Reload.ReloadLogic.TransferBulletBetweenMags(weaponProperties.reloadValues);
    }


    private void ResetConcatenation()
    {
        playerProperties.is_composing_bullets = false;
        time_to_contatenate = weaponProperties.reloadValues.timeToTransferAmmo;
    }
    #endregion

    #region Interface Implementations
    public float GetCurrentSpread() => current_spread;
    public float GetMaxSpread() => weaponProperties.spreadValues.maxSpread;
    #endregion

    // REMOVIDO: OnDestroy não é mais necessário pois não há stateId para limpar
}