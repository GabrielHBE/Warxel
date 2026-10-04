using System.Collections;
using UnityEngine;

public class RocketLauncher : Gadget
{
    [Header("Missiles")]
    [SerializeField] private Transform shootPos;

    [Header("Hand Positions")]
    [SerializeField] private Transform reloadLefthandPosition;

    [Header("Animations")]
    [SerializeField] private Animator anim;

    [Header("Properties")]
    [SerializeField] private MissileProperties properties;

    [Header("ADS")]
    [SerializeField] private float adsSpeed;
    [SerializeField] private Transform adsPosition;
    [SerializeField] private float zoom = 1.5f;
    [SerializeField] private bool canReloadAiming = false;

    [Header("Required Instances")]
    [SerializeField] private MeshRenderer missileModel;
    [SerializeField] private Transform visualRecoilApplierTransform;

    [Header("Variables")]
    [HideInInspector] public bool can_aim = true;
    [HideInInspector] public bool can_shoot = true;
    private Coroutine applyRotationRecoilCoroutine;
    private float reload_cooldown;
    [HideInInspector] public bool did_shoot = false;
    private ProcessCameraRecoil processCameraRecoil;
    private Vector3 initialPosition;
    private float current_spread;
    private bool restarted;
    private int reserve_ammo;
    private float time_to_contatenate = 0;
    private string ammo;

    public override void Initialize()
    {
        base.Initialize();
        
        processCameraRecoil = GetComponentInParent<ProcessCameraRecoil>();
        properties.reloadValues.PopulateMags();

        properties.recoilValues.CalculateRecoilSpeed(properties.firing.interval);
        equippableItemAnimator.Setup(0, properties.reloadValues, false, properties.firing);
    }

    public override void Restart()
    {
        base.Restart();

        if (visualRecoilApplierTransform != null)
        {
            initialPosition = visualRecoilApplierTransform.localPosition;
            visualRecoilApplierTransform.localPosition = initialPosition;
        }

        if (playerController.playerProperties != null) playerController.playerProperties.reloading = false;

        if (AdsBehaviour.Instance != null) AdsBehaviour.Instance.Setup(adsPosition, adsSpeed, zoom, canReloadAiming, equippableItemAudio);

        restarted = true;
        current_spread = properties.spreadValues.baseSpread;
        SetupFiringSystem();

    }

    void Update()
    {
        if (!restarted || !is_active) return;

        Reload();

        ConcatenateBullets();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_reloadKey)) HandleReload();

        UpdateAmmoHUD();

        Firing.UpdateTimeToFire(Time.deltaTime);

        if (can_shoot && !playerController.playerProperties.reloading) ProcessShooting();

        if (!playerController.playerProperties.firing) HandleRecoilReset();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_switchFireModeKey)) HandleFireModeSwitch();

    }
    private void SetupFiringSystem()
    {
        Firing.ResetState(properties.firing.fireModes);

        if (properties.firing.fireModes != null && properties.firing.fireModes.Count > 0 && !properties.firing.fireModes.Contains(Firing.GetCurrentFireMode())) Firing.SwitchFireMode(properties.firing);

    }

    private void UpdateAmmoHUD()
    {
        ammo = properties.reloadValues.mags[^1].ToString("F0") + " / ";
        int ammoLeft = 0;
        for (int i = 0; i < properties.reloadValues.mags.Count - 1; i++)
        {
            ammoLeft += properties.reloadValues.mags[i];
        }
        ammo += ammoLeft.ToString();
        soldierHudManager.SetCurrentAmmo(ammo);
    }

    #region Fire Mode
    private void HandleFireModeSwitch()
    {
        if (!Firing.CanSwitchFireMode(properties.firing.fireModes)) return;

        Firing.FireMode newMode = Firing.SwitchFireMode(properties.firing);
        UpdateFireModeHUD(newMode);
    }

    private void UpdateFireModeHUD(Firing.FireMode mode) => soldierHudManager.fire_mode_hud.SetFireMode(mode);
    #endregion

    #region Reload
    void HandleReload()
    {
        int reserveAmmo = properties.reloadValues.GetTotalReserveAmmo();

        if (!ProcessReload.Reload.ReloadLogic.CanStartReload(
            properties.reloadValues,
            playerController.playerProperties.firing,
            playerController.playerProperties.reloading,
            playerController.playerProperties.roll,
            reserveAmmo))
        {
            if (reserveAmmo == 0) AlertMessages.Instance.CreateMessage("Cant reload", 2);
            return;
        }

        equippableItemAnimator.StartReloadAnimation();

        bool isEmpty = properties.reloadValues.IsMagazineEmpty();
        float totalReloadTime = ProcessReload.Reload.ReloadLogic.CalculateReloadTime(properties.reloadValues, isEmpty);

        if (equippableItemAnimator.fireClip != null)
        {
            if (!equippableItemAnimator.isInFireAnimation)
            {
                reload_cooldown = totalReloadTime;
                playerController.playerProperties.reloading = true;
            }
        }
        else
        {
            reload_cooldown = totalReloadTime;
            playerController.playerProperties.reloading = true;
        }
    }

    void Reload()
    {
        CalculateReserveAmmo();

        if (reserve_ammo == 0 || !playerController.playerProperties.reloading) return;
        

        if (!properties.reloadValues.isSingleReload) HandleStandardReload();
    }

    private void CalculateReserveAmmo() => reserve_ammo = properties.reloadValues.GetTotalReserveAmmo();

    private void HandleStandardReload()
    {
        bool isEmpty = properties.reloadValues.IsMagazineEmpty();

        var result = ProcessReload.Reload.ReloadLogic.ProcessStandardReload(
            properties.reloadValues,
            reload_cooldown,
            Time.deltaTime,
            isEmpty
        );

        reload_cooldown = result.remainingCooldown;
        can_shoot = result.canShoot;
        playerController.playerProperties.reloading = result.isReloading;

        if (result.shouldFinishReload)
        {
            equippableItemAnimator.FinishReloadAnimation();
            Firing.ResetState();
        }
    }
    #endregion

    #region Shooting
    private void ProcessShooting()
    {
        bool holdShoot = InputManager.GetKey(Settings.Instance._keybinds.WEAPON_shootKey);
        bool pressShoot = InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_shootKey);

        // Check if ammo is empty for alert
        if (pressShoot && properties.reloadValues.mags[^1] == 0)
        {
            AlertMessages.Instance.CreateMessage("Not enough ammo", 2);
            return;
        }

        // ATUALIZADO: Process shooting through Firing system (sem stateId)
        var result = Firing.ProcessShooting(
            properties.firing,
            holdShoot,
            pressShoot,
            playerController.playerProperties.reloading,
            playerController.playerProperties.roll,
            playerController.playerProperties.isDead.Value,
            properties.reloadValues.mags[^1],
            Time.deltaTime
        );

        // Handle the result
        if (result.shouldResetShotState) ResetShotState();
        if (result.shouldShoot) ExecuteShot(result.isFirstShot);

        // ATUALIZADO: sem stateId
        playerController.playerProperties.firing = Firing.IsFiring();

        if (properties.reloadValues.mags[^1] <= 0) playerController.playerProperties.firing = false;

    }

    private void ExecuteShot(bool isFirstShot)
    {
        did_shoot = true;
        DisableMissileModelRenderer();

        if (equippableItemAnimator != null) equippableItemAnimator.StartFireAnimation();

        int patternLength = properties.recoilValues.recoilPattern.Length;
        if (patternLength > 0)
        {
            int recoilIndex = Firing.GetNextRecoilIndex(patternLength);

            if (recoilIndex >= 0 && recoilIndex < patternLength) StartCoroutine(ApplyVisualRecoilOffset(recoilIndex, isFirstShot));
            else StartCoroutine(ApplyVisualRecoilOffset(0, isFirstShot));
        }

        CreateBullet();

        properties.reloadValues.mags[^1] -= 1;
    }

    private void ResetShotState()
    {
        // ATUALIZADO: sem stateId
        Firing.ResetRecoilIndex();
        Firing.ResetState();
        playerController.playerProperties.firing = false;
    }

    private void HandleRecoilReset()
    {
        if (applyRotationRecoilCoroutine != null)
        {
            StopCoroutine(applyRotationRecoilCoroutine);
            applyRotationRecoilCoroutine = null;
        }

        current_spread = Spread.ResetSpread(current_spread, properties.spreadValues.baseSpread, properties.spreadValues.spreadRecovery);
    }

    void CreateBullet()
    {
        for (int i = 0; i < properties.firing.bulletsPerShot; i++)
        {
            SpawnBullet();
        }

        if (equippableItemAudio != null) equippableItemAudio.ShootSound();
    }

    private void SpawnBullet()
    {
        Quaternion finalRotation = Spread.CalculateSpreadRotation(shootPos.transform, current_spread);

        current_spread = Spread.AddSpread(current_spread, properties.spreadValues.spreadIncreaser, properties.spreadValues.maxSpread);

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = shootPos.transform.position,
            rotation = finalRotation,
            ignoredObject = transform.root
        };

        if (ProjectileSpawner.Instance != null) ProjectileSpawner.Instance.CreateProjectile(properties.missilePrefab, properties.dummyMissilePrefab.gameObject, prop, properties.projectileValues);

    }


    IEnumerator ApplyVisualRecoilOffset(int recoilIndex, bool isFirstShot)
    {
        // Safety check
        if (recoilIndex < 0 || recoilIndex >= properties.recoilValues.recoilPattern.Length) recoilIndex = 0;

        ApplyRecoilToCamera(
            Recoil.GetVerticalRecoilDirection(properties.recoilValues.recoilPattern[recoilIndex].verticalRecoil),
            Recoil.GetHorizontalRecoilDirection(properties.recoilValues.recoilPattern[recoilIndex].horizontalRecoil),
            isFirstShot
        );

        Vector3 recoilOffset = GetRecoilOffset();
        Vector3 start = initialPosition;
        Vector3 target = start + recoilOffset;

        yield return StartCoroutine(Recoil.ApplyPositionRecoilAnimation(
            start,
            target,
            properties.recoilValues.applyRecoilSpeed,
            properties.recoilValues.resetRecoilSpeed,
            properties.recoilValues.applyCurve,
            properties.recoilValues.resetCurve,
            visualRecoilApplierTransform.transform
        ));
    }

    private void ApplyRecoilToCamera(float vr, float hr, bool isFirstShot)
    {
        var recoil = Recoil.CalculateCameraRecoil(
            vr,
            hr,
            properties.recoilValues.firstShootRecoilMultiplier,
            isFirstShot,
            properties.reloadValues.mags[^1]
        );

        // Chamada direta para o ProcessCameraRecoil
        if (processCameraRecoil != null) processCameraRecoil.ApplyRecoil(recoil.vertical, recoil.horizontal);

    }
    private Vector3 GetRecoilOffset() => Recoil.CalculateVisualRecoilOffset(properties.recoilValues.visualPositionRecoil, playerController.playerProperties.aiming);
    #endregion

    #region Bullet Concatenation
    private void ConcatenateBullets()
    {
        if (properties.reloadValues.mags == null || properties.reloadValues.mags.Count == 0) return;

        if (InputManager.GetKey(Settings.Instance._keybinds.WEAPON_composeBulletsKey) &&
            !playerController.playerProperties.firing &&
            !playerController.playerProperties.reloading) ProcessBulletConcatenation();
        else ResetConcatenation();

    }

    private void ProcessBulletConcatenation()
    {
        playerController.playerProperties.isComposingBullets = true;
        time_to_contatenate -= Time.deltaTime;

        if (time_to_contatenate <= 0)
        {
            TransferBulletsBetweenMags();
            time_to_contatenate = properties.reloadValues.timeToTransferAmmo;
        }
    }

    private void TransferBulletsBetweenMags() => ProcessReload.Reload.ReloadLogic.TransferBulletBetweenMags(properties.reloadValues);
    private void ResetConcatenation()
    {
        playerController.playerProperties.isComposingBullets = false;
        time_to_contatenate = properties.reloadValues.timeToTransferAmmo;
    }
    #endregion

    #region Animation Events
    public void DisableMissileModelRenderer() => missileModel.enabled = false;
    public void EnableMissileModelRenderer() => missileModel.enabled = true;
    #endregion
}
