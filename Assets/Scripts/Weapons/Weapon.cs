using UnityEngine;

public class Weapon : MonoBehaviour, ICurrentSpreadUIValues, IReloadContext
{
    public const float LAST_MAG_RELOAD_TIMER_INCREASER = 1.5f;

    [Header("State")]
    public bool is_active;
    [HideInInspector] public bool is_side_grip_activated;

    [Header("HUD")]
    [SerializeField] private SoldierHudManager soldierHudManager;

    [Header("Bullets")]
    [SerializeField] private DummyProjectile dummyBullet;
    [SerializeField] private GameObject bullet;

    [Header("Instances")]
    [SerializeField] private MuzzleController muzzleController;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private ProcessCameraRecoil processCameraRecoil;

    [Header("Variables")]
    [HideInInspector] public bool can_aim = true;
    [HideInInspector] public bool can_shoot = true;
    private Coroutine visualRecoilCoroutine;
    private EquippableItemAudio equippableItemAudio;
    private WeaponProperties weaponProperties;
    [HideInInspector] public EquippableItemAnimator weaponAnimation;
    private Sight sight_attatchment;
    private CantedSight cantedSightAttatchment;
    private float current_spread;
    private bool restarted = false;
    private float time_to_contatenate = 0;
    private ReloadController reloadController;
    private SwayNBobScript swayAndBob;
    private Vector3 visualRecoilPositionOffset;
    private Quaternion visualRecoilRotationOffset = Quaternion.identity;
    private Vector3 fallbackInitialLocalPosition;
    private Quaternion fallbackInitialLocalRotation;

    #region Unity Lifecycle Methods
    private void Awake()
    {
        swayAndBob = GetComponent<SwayNBobScript>();
        fallbackInitialLocalPosition = transform.localPosition;
        fallbackInitialLocalRotation = transform.localRotation;
    }

    private void OnDisable() => ResetVisualRecoil();

    void Update()
    {
        if (!restarted || !is_active) return;

        ConcatenateBullets();

        if (weaponProperties != null) UpdateAmmoHUD();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_reloadKey) && reloadController != null) reloadController.TryStartReload();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_shootKey) && playerProperties.reloading && weaponProperties.reloadValues.isSingleReload) reloadController.StopSingleReload();

        Firing.UpdateTimeToFire(Time.deltaTime);

        if (can_shoot && !playerProperties.reloading) ProcessShooting();

        if (!playerProperties.firing) HandleRecoilReset();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_switchFireModeKey)) HandleFireModeSwitch();
    }
    #endregion

    #region Initialization
    public void Restart(WeaponProperties wp)
    {
        ResetVisualRecoil();
        swayAndBob ??= GetComponent<SwayNBobScript>();

        if (swayAndBob == null)
        {
            fallbackInitialLocalPosition = transform.localPosition;
            fallbackInitialLocalRotation = transform.localRotation;
        }

        weaponProperties = wp;
        reloadController = wp.GetComponent<ReloadController>();
        weaponAnimation = wp.weaponAnimation;
        FindActiveSights(wp);
        equippableItemAudio = wp.weaponSound;

        time_to_contatenate = weaponProperties.reloadValues.timeToTransferAmmo;

        reloadController.Setup(this);

        playerProperties.reloading = false;
        restarted = true;

        if (sight_attatchment != null) AdsBehaviour.Instance.Setup(sight_attatchment, cantedSightAttatchment, weaponProperties, weaponProperties.adsSpeed, weaponProperties.canReloadAiming, equippableItemAudio);
        else AdsBehaviour.Instance.Setup(null, weaponProperties.adsSpeed, weaponProperties.zoom, weaponProperties.canReloadAiming, equippableItemAudio);

        current_spread = weaponProperties.spreadValues.baseSpread;
        SetupFiringSystem();

        if (muzzleController != null)
        {
            muzzleController.RequestClearMuzzles();
            muzzleController.RequestSetupMuzzle(weaponProperties.shootPos);
            muzzleController.RequestSetMuzzleLifetime(weaponProperties.firing.interval);
        }

    }

    private void FindActiveSights(WeaponProperties wp)
    {
        sight_attatchment = null;
        cantedSightAttatchment = null;

        Sight[] sights = wp.GetComponentsInChildren<Sight>();
        foreach (Sight sight in sights)
        {
            if (sight is CantedSight cantedSight)
            {
                cantedSightAttatchment ??= cantedSight;
            }
            else
            {
                sight_attatchment ??= sight;
            }
        }

        // Uma CantedSight isolada continua funcionando como uma Sight comum.
        if (sight_attatchment == null && cantedSightAttatchment != null)
        {
            sight_attatchment = cantedSightAttatchment;
            cantedSightAttatchment = null;
        }
    }

    private void SetupFiringSystem()
    {
        Firing.ResetState(weaponProperties?.firing.fireModes);
        if (weaponProperties != null && weaponProperties.firing.fireModes != null && weaponProperties.firing.fireModes.Count > 0)
        {
            if (!weaponProperties.firing.fireModes.Contains(Firing.GetCurrentFireMode())) Firing.SwitchFireMode(weaponProperties.firing);
        }
        UpdateFireModeHUD(Firing.GetCurrentFireMode());
    }

    private void UpdateAmmoHUD()
    {
        string ammoLeft = "";

        if (weaponProperties.reloadValues.isSingleReload || weaponProperties.reloadValues.bulletsPerMag == 1)
        {
            int ammoCount = 0;
            for (int i = 0; i < weaponProperties.reloadValues.mags.Count - 1; i++)
            {
                ammoCount += weaponProperties.reloadValues.mags[i];
            }

            ammoLeft = ammoCount.ToString();
        }
        else
        {
            for (int i = 0; i < weaponProperties.reloadValues.mags.Count - 1; i++)
            {
                ammoLeft += weaponProperties.reloadValues.mags[i].ToString("F0") + " ";
            }
        }

        soldierHudManager.SetCurrentAmmo(weaponProperties.reloadValues.mags[^1].ToString("F0") + " / " + ammoLeft);

    }
    #endregion

    #region Fire Mode
    private void HandleFireModeSwitch()
    {
        if (!Firing.CanSwitchFireMode(weaponProperties.firing.fireModes)) return;

        Firing.FireMode newMode = Firing.SwitchFireMode(weaponProperties.firing);
        UpdateFireModeHUD(newMode);
    }

    private void UpdateFireModeHUD(Firing.FireMode mode) => soldierHudManager.fire_mode_hud.SetFireMode(mode);
    #endregion

    #region Shooting
    private void ProcessShooting()
    {
        bool holdShoot = InputManager.GetKey(Settings.Instance._keybinds.WEAPON_shootKey);
        bool pressShoot = InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_shootKey);

        if (pressShoot && weaponProperties.reloadValues.mags[^1] == 0)
        {
            AlertMessages.Instance.CreateMessage("Not enough ammo", 2);
            return;
        }

        var result = Firing.ProcessShooting(
            weaponProperties.firing,
            holdShoot,
            pressShoot,
            playerProperties.reloading,
            playerProperties.roll,
            playerProperties.isDead.Value,
            weaponProperties.reloadValues.mags[^1],
            Time.deltaTime
        );

        if (result.shouldResetShotState) ResetShotState();
        if (result.shouldShoot) ExecuteShot(result.isFirstShot);

        playerProperties.firing = Firing.IsFiring();

        if (weaponProperties.reloadValues.mags[^1] <= 0) playerProperties.firing = false;
    }

    private void ExecuteShot(bool isFirstShot)
    {
        if (weaponAnimation != null)
        {
            if (weaponAnimation != null) weaponAnimation.StartFireAnimation();
            if (weaponAnimation.fireClip == null) weaponProperties.CreateBulletExtractor();
        }

        int patternLength = weaponProperties.recoilValues.recoilPattern.Length;
        if (patternLength > 0)
        {
            int recoilIndex = Firing.GetNextRecoilIndex(patternLength);

            if (recoilIndex >= 0 && recoilIndex < patternLength) ApplyVisualRecoilOffset(recoilIndex, isFirstShot);
            else
            {
                Debug.LogWarning($"Recoil index {recoilIndex} out of range for pattern length {patternLength}");
                ApplyVisualRecoilOffset(0, isFirstShot);
            }
        }

        CreateBullet();
        weaponProperties.reloadValues.mags[^1] -= 1;
    }

    private void ResetShotState()
    {
        Firing.ResetRecoilIndex();
        Firing.ResetState();
        playerProperties.firing = false;
    }

    private void HandleRecoilReset()
    {
        current_spread = Spread.ResetSpread(current_spread, weaponProperties.spreadValues.baseSpread, weaponProperties.spreadValues.spreadRecovery);
    }

    void CreateBullet()
    {
        for (int i = 0; i < weaponProperties.firing.bulletsPerShot; i++)
        {
            SpawnBullet();
        }

        if (equippableItemAudio != null) equippableItemAudio.ShootSound();
        if (muzzleController != null) muzzleController.RequestCreateMuzzle();
    }

    private void SpawnBullet()
    {
        Quaternion finalRotation = Spread.CalculateSpreadRotation(weaponProperties.shootPos, current_spread);

        current_spread = Spread.AddSpread(current_spread, weaponProperties.spreadValues.spreadIncreaser, weaponProperties.spreadValues.maxSpread);

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = weaponProperties.shootPos.position,
            rotation = finalRotation,
            ignoredObject = transform.root
        };

        if (ProjectileSpawner.Instance != null) ProjectileSpawner.Instance.CreateProjectile(bullet, dummyBullet.gameObject, prop, weaponProperties.projectileValues);
    }

    private void ApplyVisualRecoilOffset(int recoilIndex, bool isFirstShot)
    {
        if (recoilIndex < 0 || recoilIndex >= weaponProperties.recoilValues.recoilPattern.Length) recoilIndex = 0;

        ApplyRecoilToCamera(
            Recoil.GetVerticalRecoilDirection(weaponProperties.recoilValues.recoilPattern[recoilIndex].verticalRecoil),
            Recoil.GetHorizontalRecoilDirection(weaponProperties.recoilValues.recoilPattern[recoilIndex].horizontalRecoil),
            isFirstShot
        );

        Vector3 recoilOffset = Recoil.CalculateVisualRecoilOffset(
            weaponProperties.recoilValues.visualPositionRecoil,
            playerProperties.aiming);
        Vector3 startPositionOffset = visualRecoilPositionOffset;
        Vector3 targetPositionOffset = startPositionOffset + recoilOffset;

        Vector3 recoilRotOffset = Recoil.CalculateVisualRotationOffset(weaponProperties.recoilValues.maxRotationRecoil, playerProperties.aiming);
        Quaternion startRotationOffset = visualRecoilRotationOffset;
        Quaternion targetRotationOffset = startRotationOffset * Quaternion.Euler(recoilRotOffset);

        if (visualRecoilCoroutine != null)
        {
            StopCoroutine(visualRecoilCoroutine);
        }

        visualRecoilCoroutine = StartCoroutine(ApplyVisualRecoilAnimation(
            startPositionOffset,
            targetPositionOffset,
            startRotationOffset,
            targetRotationOffset));
    }

    private System.Collections.IEnumerator ApplyVisualRecoilAnimation(
        Vector3 startPositionOffset,
        Vector3 targetPositionOffset,
        Quaternion startRotationOffset,
        Quaternion targetRotationOffset)
    {
        yield return Recoil.ApplyVisualRecoilAnimation(
            startPositionOffset,
            targetPositionOffset,
            startRotationOffset,
            targetRotationOffset,
            weaponProperties.recoilValues.applyRecoilSpeed,
            weaponProperties.recoilValues.resetRecoilSpeed,
            weaponProperties.recoilValues.applyCurve,
            weaponProperties.recoilValues.resetCurve,
            SetVisualRecoilOffset);

        visualRecoilCoroutine = null;
    }

    private void SetVisualRecoilOffset(Vector3 positionOffset, Quaternion rotationOffset)
    {
        visualRecoilPositionOffset = positionOffset;
        visualRecoilRotationOffset = rotationOffset;

        if (swayAndBob != null)
        {
            swayAndBob.SetVisualRecoilOffset(positionOffset, rotationOffset);
            return;
        }

        transform.localPosition = fallbackInitialLocalPosition + positionOffset;
        transform.localRotation = fallbackInitialLocalRotation * rotationOffset;
    }

    private void ResetVisualRecoil()
    {
        if (visualRecoilCoroutine != null)
        {
            StopCoroutine(visualRecoilCoroutine);
            visualRecoilCoroutine = null;
        }

        SetVisualRecoilOffset(Vector3.zero, Quaternion.identity);
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

        if (processCameraRecoil != null) processCameraRecoil.ApplyRecoil(recoil.vertical, recoil.horizontal);
    }
    #endregion

    #region Bullet Concatenation
    private void ConcatenateBullets()
    {
        if (weaponProperties.reloadValues.mags == null || weaponProperties.reloadValues.mags.Count == 0) return;

        if (InputManager.GetKey(Settings.Instance._keybinds.WEAPON_composeBulletsKey) &&
            !playerProperties.firing &&
            !playerProperties.reloading) ProcessBulletConcatenation();
        else ResetConcatenation();
    }

    private void ProcessBulletConcatenation()
    {
        playerProperties.isComposingBullets = true;
        time_to_contatenate -= Time.deltaTime;

        if (time_to_contatenate <= 0)
        {
            TransferBulletsBetweenMags();
            time_to_contatenate = weaponProperties.reloadValues.timeToTransferAmmo;
        }
    }

    private void TransferBulletsBetweenMags() => ProcessReload.Reload.ReloadLogic.TransferBulletBetweenMags(weaponProperties.reloadValues);
    private void ResetConcatenation()
    {
        playerProperties.isComposingBullets = false;
        time_to_contatenate = weaponProperties.reloadValues.timeToTransferAmmo;
    }
    #endregion

    #region Interface Implementations
    // ==========================================
    // INTERFACE ICurrentSpreadUIValues
    // ==========================================
    public float GetCurrentSpread() => current_spread;
    public float GetMaxSpread() => weaponProperties.spreadValues.maxSpread;

    // ==========================================
    // INTERFACE IReloadContext
    // ==========================================
    public ProcessReload.Reload.ReloadValues ReloadValues => weaponProperties.reloadValues;
    public bool IsActive => is_active && restarted;
    public bool IsFiring => playerProperties.firing;
    public bool IsRolling => playerProperties.roll;

    public bool IsReloading
    {
        get => playerProperties.reloading;
        set => playerProperties.reloading = value;
    }

    public bool HasFireClip => weaponAnimation != null && weaponAnimation.fireClip != null;
    public bool IsInFireAnimation => weaponAnimation != null && weaponAnimation.isInFireAnimation;

    public void SetCanShoot(bool canShoot) => this.can_shoot = canShoot;
    public void StartReloadAnimation() => weaponAnimation.StartReloadAnimation();
    public void FinishReloadAnimation() => weaponAnimation.FinishReloadAnimation();
    public void ResetFiringState() => Firing.ResetState();
    public void OnReloadFailedNoAmmo() => AlertMessages.Instance.CreateMessage("Cant reload", 2);
    #endregion
}
