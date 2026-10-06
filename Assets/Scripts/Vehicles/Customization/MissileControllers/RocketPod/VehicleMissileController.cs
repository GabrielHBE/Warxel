using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using FishNet.Object.Synchronizing;

public class VehicleMissileController : VehicleArmory
{
    [SerializeField] protected Transform[] spawnPoints;
    [SerializeField] protected bool initializeDummyMissiles;

    // Sincronização da munição - todos os clients verão os mesmos valores
    protected readonly SyncList<int> syncMags = new SyncList<int>();
    protected readonly SyncVar<int> currentMagIndex = new SyncVar<int>();
    protected readonly SyncVar<int> currentSpawnPointShootIndex = new SyncVar<int>();
    protected readonly SyncVar<bool> isReloading = new SyncVar<bool>();
    private Firing.FireMode missileFireMode = Firing.FireMode.Auto;
    private float nextMissileFireTime;
    private int missileBurstShotsRemaining;
    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
        isActive = false;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeMagazines();
    }
    protected override void Update()
    {
        base.Update();
        if (!IsOwner) return;

        Reload();


        if (!isActive) return;

        // Check manual reload input
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_reloadKey))
        {
            HandleReload(false);
        }

        // NOVO: Permitir a troca de modo de tiro igual a Weapon.cs
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_switchFireModeKey))
        {
            HandleFireModeSwitch();
        }

        // Update spread recovery
        properties.spreadValues.spreadState.currentSpread = Spread.ResetSpread(
            properties.spreadValues.spreadState.currentSpread,
            properties.spreadValues.baseSpread,
            properties.spreadValues.spreadRecovery
        );
    }
    #endregion

    #region Fire Mode & Setup
    public override void SetupFiringSystem()
    {
        List<Firing.FireMode> modes = properties?.firing?.fireModes;
        missileFireMode = modes != null && modes.Count > 0 ? modes[0] : Firing.FireMode.Auto;
        nextMissileFireTime = 0f;
        ResetShotState();

    }

    private void HandleFireModeSwitch()
    {
        List<Firing.FireMode> modes = properties?.firing?.fireModes;
        if (modes == null || modes.Count < 2) return;

        int currentIndex = modes.IndexOf(missileFireMode);
        missileFireMode = modes[(currentIndex + 1) % modes.Count];
        ResetShotState();
        nextMissileFireTime = 0f;

        SoundManager.SoundComponents sound = properties.firing.switchFireModeSound;
        if (sound?.clip != null)
            SoundManager.Play2dSoundLocal(sound.clip, sound.properties);

    }
    #endregion

    #region Magazine Management
    protected void InitializeMagazines()
    {
        if (properties == null) return;

        syncMags.Clear();
        properties.reloadValues.PopulateMags();

        for (int i = 0; i < properties.reloadValues.magCount; i++)
        {
            syncMags.Add(properties.reloadValues.bulletsPerMag);
        }
    }

    protected int GetCurrentMagAmmo()
    {
        if (syncMags.Count == 0 || currentMagIndex.Value >= syncMags.Count) return 0;
        return syncMags[currentMagIndex.Value];
    }

    private int GetTotalReserveAmmo()
    {
        int total = 0;
        for (int i = 0; i < syncMags.Count; i++)
        {
            if (i != currentMagIndex.Value)
                total += syncMags[i];
        }
        return total;
    }
    #endregion

    #region Firing Logic
    public override void Shoot()
    {
        if (!IsOwner || !isActive || properties == null || spawnPoints == null || spawnPoints.Length == 0) return;
        ProcessShooting();
    }

    protected void ProcessShooting()
    {
        KeyCode shootKey = GetShootKey();
        bool holdShoot = InputManager.GetKey(shootKey);
        bool pressShoot = InputManager.GetKeyDown(shootKey);

        // Check if ammo is empty for alert
        if (pressShoot && GetCurrentMagAmmo() == 0)
        {
            ResetShotState();
            AlertMessages.Instance.CreateMessage("Not enought Ammo");
            return;
        }

        if (isReloading.Value || GetCurrentMagAmmo() <= 0)
        {
            ResetShotState();
            return;
        }

        bool ready = Time.time >= nextMissileFireTime;
        if (missileFireMode == Firing.FireMode.Burst && missileBurstShotsRemaining == 0 && pressShoot && ready)
            missileBurstShotsRemaining = Mathf.Max(1, properties.firing.BurstModeBulletsPerTap);

        bool shouldShoot = ready && (missileFireMode == Firing.FireMode.Auto && holdShoot ||
                                     missileFireMode == Firing.FireMode.Single && pressShoot ||
                                     missileFireMode == Firing.FireMode.Burst && missileBurstShotsRemaining > 0);
        if (shouldShoot)
        {
            ExecuteShot();
            int rateOfFire = properties.firing.rateOfFire;
            nextMissileFireTime = Time.time + (rateOfFire > 0 ? 60f / rateOfFire : 1f);
            if (missileFireMode == Firing.FireMode.Burst) missileBurstShotsRemaining--;
        }

        UpdateFireAudio(CanPlayFireAudio() && !isReloading.Value && GetCurrentMagAmmo() > 0 &&
            (missileFireMode == Firing.FireMode.Auto ? holdShoot
                : missileFireMode == Firing.FireMode.Burst ? missileBurstShotsRemaining > 0 || shouldShoot
                : shouldShoot));
    }

    protected void ResetShotState()
    {
        missileBurstShotsRemaining = 0;
        UpdateFireAudio(false);
    }

    protected virtual KeyCode GetShootKey()
    {
        KeyBinds keybinds = Settings.Instance._keybinds;
        Vehicle vehicle = GetComponentInParent<Vehicle>();
        if (vehicle == null) return keybinds.WEAPON_shootKey;

        switch (vehicle.vehicleCategory)
        {
            case Vehicle.VehicleCategory.Helicopter: return keybinds.HELICOPTER_shoot_key;
            case Vehicle.VehicleCategory.Plane: return keybinds.JET_shootVehicleKey;
            case Vehicle.VehicleCategory.Tank: return keybinds.TANK_shoot_key;
            default: return keybinds.WEAPON_shootKey;
        }
    }

    protected virtual bool CanPlayFireAudio() => true;

    protected virtual void ExecuteShot()
    {

        // Calcula o spread
        int spawnIndex = currentSpawnPointShootIndex.Value;
        Transform spawnPoint = spawnPoints[spawnIndex];
        Quaternion finalRotation = Spread.CalculateSpreadRotation(
            spawnPoint,
            properties.spreadValues.spreadState.currentSpread
        );

        // Atualiza o spread
        properties.spreadValues.spreadState.currentSpread = Spread.AddSpread(
            properties.spreadValues.spreadState.currentSpread,
            properties.spreadValues.spreadIncreaser,
            properties.spreadValues.maxSpread
        );

        if (initializeDummyMissiles) RequestActivateDummyMissile(spawnIndex, false);

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = spawnPoint.position,
            rotation = finalRotation,
            ignoredObject = transform.root,
            root = transform.root.gameObject
        };

        // Dispara o míssil
        if (ProjectileSpawner.Instance != null)
        {
            Vehicle firingVehicle = vehicle != null ? vehicle : GetComponentInParent<Vehicle>();
            float initialSpeed = properties.projectileValues.muzzleVelocity;
            if (firingVehicle != null && firingVehicle.rb != null) initialSpeed += firingVehicle.rb.linearVelocity.magnitude;

            Projectile.ProjectileValues shotValues = properties.projectileValues.WithMuzzleVelocity(initialSpeed);
            ProjectileSpawner.Instance.CreateProjectile(properties.bulletPref, properties.dummyBullet.gameObject, prop, shotValues);
        }

        // Atualiza munição
        PlayShotEffects();
        UpdateAmmoAfterShot();
        UpdateCurrentSpawnPointShootIndex();
    }

    protected void UpdateAmmoAfterShot()
    {
        if (syncMags.Count == 0 || currentMagIndex.Value >= syncMags.Count) return;
        int currentAmmo = syncMags[currentMagIndex.Value];
        syncMags[currentMagIndex.Value] = currentAmmo - 1;

        // Se o magazine está vazio, tenta recarregar automaticamente usando a nova lógica
        if (syncMags[currentMagIndex.Value] <= 0)
        {
            HandleReload(true);
        }
    }

    protected void UpdateCurrentSpawnPointShootIndex()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        currentSpawnPointShootIndex.Value += 1;
        if (currentSpawnPointShootIndex.Value >= spawnPoints.Length)
        {
            currentSpawnPointShootIndex.Value = 0;
        }
    }
    #endregion

    #region Reload Logic
    private void HandleReload(bool automatic)
    {
        SyncSyncMagsToProperties();
        int reserveAmmo = GetTotalReserveAmmo();

        if (!ProcessReload.Reload.ReloadLogic.CanStartReload(
            properties.reloadValues,
            !automatic && InputManager.GetKey(GetShootKey()),
            isReloading.Value,
            false, // Roll desativado por padrão para veículos
            reserveAmmo))
        {
            return;
        }

        bool isEmpty = GetCurrentMagAmmo() == 0;
        float totalReloadTime = ProcessReload.Reload.ReloadLogic.CalculateReloadTime(properties.reloadValues, isEmpty);

        reloadTimer = totalReloadTime;
        isReloading.Value = true;

        SyncPropertiesToSyncMags();
    }

    private void Reload()
    {
        int reserveAmmo = GetTotalReserveAmmo();

        if (reserveAmmo == 0 || !isReloading.Value) return;

        if (!properties.reloadValues.isSingleReload) HandleStandardReload();

    }

    private void HandleStandardReload()
    {
        SyncSyncMagsToProperties();
        bool isEmpty = GetCurrentMagAmmo() == 0;

        var result = ProcessReload.Reload.ReloadLogic.ProcessStandardReload(
            properties.reloadValues,
            reloadTimer,
            Time.deltaTime,
            isEmpty
        );

        reloadTimer = result.remainingCooldown;
        isReloading.Value = result.isReloading;

        SyncPropertiesToSyncMags();

        if (result.shouldFinishReload)
        {
            ResetShotState();
        }
    }


    /// <summary>
    /// Mapeia os dados do SyncList da rede para a lista interna que o ProcessStandardReload/ProcessSingleReload esperam (mags[^1] sendo o pente atual).
    /// </summary>
    private void SyncSyncMagsToProperties()
    {
        if (properties.reloadValues.mags == null) return;

        while (properties.reloadValues.mags.Count < syncMags.Count)
            properties.reloadValues.mags.Add(0);
        while (properties.reloadValues.mags.Count > syncMags.Count)
            properties.reloadValues.mags.RemoveAt(properties.reloadValues.mags.Count - 1);

        int reserveCount = 0;
        for (int i = 0; i < syncMags.Count; i++)
        {
            if (i == currentMagIndex.Value)
            {
                properties.reloadValues.mags[^1] = syncMags[i];
            }
            else
            {
                properties.reloadValues.mags[reserveCount] = syncMags[i];
                reserveCount++;
            }
        }
    }

    /// <summary>
    /// Retorna as alterações calculadas pela biblioteca compartilhada de volta para o SyncList replicado da rede.
    /// </summary>
    private void SyncPropertiesToSyncMags()
    {
        if (properties.reloadValues.mags == null) return;

        int reserveCount = 0;
        for (int i = 0; i < syncMags.Count; i++)
        {
            if (i == currentMagIndex.Value)
            {
                syncMags[i] = properties.reloadValues.mags[^1];
            }
            else
            {
                syncMags[i] = properties.reloadValues.mags[reserveCount];
                reserveCount++;
            }
        }
    }
    #endregion

    #region Dummy missiles
    [ServerRpc]
    protected void RequestActivateDummyMissile(int spawnIndex, bool active) => CmdActivateDummyMissile(spawnIndex, active);

    [ObserversRpc]
    private void CmdActivateDummyMissile(int spawnIndex, bool active) => ActivateDummyMissile(spawnIndex, active);

    private void ActivateDummyMissile(int spawnIndex, bool active)
    {
        if (spawnPoints == null || spawnIndex < 0 || spawnIndex >= spawnPoints.Length) return;
        Transform spawnPoint = spawnPoints[spawnIndex];
        if (spawnPoint.childCount > 0) spawnPoint.GetChild(0).gameObject.SetActive(active);
    }
    #endregion

    #region Interface Implementations
    public override Sprite GetArmoryIcon() => properties != null ? properties.hudIcon : null;
    public override void ActivateArmory()
    {
        base.ActivateArmory();
    }
    public override void DeactivateArmory()
    {
        ResetShotState();
        base.DeactivateArmory();
    }

    public override string GetCurrentAmmo()
    {
        if (syncMags.Count == 0) return "0 / ";

        string ammo = syncMags[currentMagIndex.Value].ToString("F0") + " / ";

        for (int i = 0; i < syncMags.Count; i++)
        {
            if (i != currentMagIndex.Value)
            {
                ammo += syncMags[i].ToString("F0") + " ";
            }
        }

        return ammo;
    }

    public override float GetHeatingLevel() => 0f;
    public override float GetMaxOverheat() => 100f;
    #endregion
}
