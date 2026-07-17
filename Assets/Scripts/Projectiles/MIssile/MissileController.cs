using FishNet.Object;
using UnityEngine;
using FishNet.Object.Synchronizing;

public class MissileController : NetworkBehaviour, IVehicleArmory
{
    [SerializeField] protected MissileControllerProperties properties;
    [SerializeField] protected Transform[] spawnPoints;
    [SerializeField] protected bool initializeDummyMissiles;

    protected bool isActive;

    // Sincronização da munição - todos os clients verão os mesmos valores
    protected readonly SyncList<int> syncMags = new SyncList<int>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    protected readonly SyncVar<int> currentMagIndex = new SyncVar<int>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    protected readonly SyncVar<int> currentSpawnPointShootIndex = new SyncVar<int>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    protected readonly SyncVar<bool> isReloading = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    private float reloadTimer = 0f;

    #region Unity Lifecycle
    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeMagazines();
    }
    protected virtual void Update()
    {
        if (!IsOwner) return;

        // Process reload logic
        if (properties != null)
        {
            Reload();
        }

        if (!isActive) return;

        // Check manual reload input
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_reloadKey))
        {
            HandleReload();
        }

        // NOVO: Permitir a troca de modo de tiro igual a Weapon.cs
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_switchFireModeKey))
        {
            HandleFireModeSwitch();
        }

        // Update firing timers
        Firing.UpdateTimeToFire(Time.deltaTime);

        // Update spread recovery
        properties.spreadValues.spreadState.currentSpread = Spread.ResetSpread(
            properties.spreadValues.spreadState.currentSpread,
            properties.spreadValues.baseSpread,
            properties.spreadValues.spreadRecovery
        );
    }
    #endregion

    #region Fire Mode & Setup
    public void SetupFiringSystem()
    {
        Firing.ResetState();

        // Garante que o modo de tiro estático atual é válido para este armamento
        if (properties != null && properties.firing.fireModes != null && properties.firing.fireModes.Count > 0)
        {
            if (!properties.firing.fireModes.Contains(Firing.GetCurrentFireMode()))
            {
                Firing.SwitchFireMode(properties.firing.fireModes);
            }
        }
    }

    private void HandleFireModeSwitch()
    {
        if (properties == null || !Firing.CanSwitchFireMode(properties.firing.fireModes)) return;

        Firing.SwitchFireMode(properties.firing.fireModes);

    }
    #endregion

    #region Magazine Management
    protected void InitializeMagazines()
    {
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
    public void Shoot() => ProcessShooting();

    protected void ProcessShooting()
    {
        bool holdShoot = InputManager.GetKey(Settings.Instance._keybinds.WEAPON_shootKey);
        bool pressShoot = InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_shootKey);

        // Check if ammo is empty for alert
        if (pressShoot && GetCurrentMagAmmo() == 0)
        {
            // Optional: Show alert message
            return;
        }

        // Process shooting through Firing system (same as Weapon.cs)
        var shootResult = Firing.ProcessShooting(
            properties.firing,
            holdShoot,
            pressShoot,
            isReloading.Value,
            false,  // isRolling
            false,  // isDead
            GetCurrentMagAmmo(),
            Time.deltaTime
        );

        // Handle the result
        if (shootResult.shouldResetShotState)
        {
            ResetShotState();
        }

        if (shootResult.shouldShoot)
        {
            ExecuteShot();
        }
    }

    protected void ResetShotState()
    {
        Firing.ResetRecoilIndex();
        Firing.ResetState();
    }

    protected virtual void ExecuteShot()
    {
        // Calcula o spread
        Quaternion finalRotation = Spread.CalculateSpreadRotation(
            spawnPoints[currentSpawnPointShootIndex.Value].transform,
            properties.spreadValues.spreadState.currentSpread
        );

        // Atualiza o spread
        properties.spreadValues.spreadState.currentSpread = Spread.AddSpread(
            properties.spreadValues.spreadState.currentSpread,
            properties.spreadValues.spreadIncreaser,
            properties.spreadValues.maxSpread
        );

        UpdateCurrentSpawnPointShootIndex();

        if (initializeDummyMissiles) RequestActivateDummyMissile(false);

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = spawnPoints[currentSpawnPointShootIndex.Value].position,
            rotation = finalRotation,
            ignoredObject = transform.root,
            root = transform.root.gameObject
        };
        // Dispara o míssil
        if (ProjectileSpawner.Instance != null) ProjectileSpawner.Instance.CreateProjectile(properties.missilePrefab, properties.dummyMissilePrefab, prop, properties.projectileValues);


        // Atualiza munição
        UpdateAmmoAfterShot();
    }

    protected void UpdateAmmoAfterShot()
    {
        int currentAmmo = syncMags[currentMagIndex.Value];
        syncMags[currentMagIndex.Value] = currentAmmo - 1;

        // Se o magazine está vazio, tenta recarregar automaticamente usando a nova lógica
        if (syncMags[currentMagIndex.Value] <= 0)
        {
            HandleReload();
        }
    }

    protected void UpdateCurrentSpawnPointShootIndex()
    {
        currentSpawnPointShootIndex.Value += 1;
        if (currentSpawnPointShootIndex.Value >= spawnPoints.Length)
        {
            currentSpawnPointShootIndex.Value = 0;
        }
    }
    #endregion

    #region Reload Logic
    private void HandleReload()
    {
        SyncSyncMagsToProperties();
        int reserveAmmo = GetTotalReserveAmmo();

        if (!ProcessReload.Reload.ReloadLogic.CanStartReload(
            properties.reloadValues,
            Firing.IsFiring(),
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

        if (reserveAmmo == 0 || !isReloading.Value)
        {
            return;
        }

        if (!properties.reloadValues.isSingleReload)
        {
            HandleStandardReload();
        }
        else
        {
            HandleSingleReload();
        }
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
            Firing.ResetState();
        }
    }

    private void HandleSingleReload()
    {
        SyncSyncMagsToProperties();

        bool shouldContinue = ProcessReload.Reload.ReloadLogic.ProcessSingleReload(
            properties.reloadValues,
            isReloading.Value,
            true,
            Firing.IsFiring(),
            out bool shouldContinueReloading
        );

        SyncPropertiesToSyncMags();

        if (!shouldContinue)
        {
            isReloading.Value = false;
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
    protected void RequestActivateDummyMissile(bool active) => CmdActivateDummyMissile(active);

    [ObserversRpc]
    private void CmdActivateDummyMissile(bool active) => ActivateDummyMissile(active);

    private void ActivateDummyMissile(bool active)
    {
        spawnPoints[currentSpawnPointShootIndex.Value].GetComponentInChildren<GameObject>().SetActive(active);
    }
    #endregion

    #region Interface Implementations
    public Sprite GetArmoryIcon() => properties.hudIcon;
    public void ActivateArmory()
    {
        isActive = true;
        SetupFiringSystem(); // Reinicia e sincroniza o Firing state ao assumir a arma
    }
    public void DeactivateArmory() => isActive = false;

    public string GetCurrentAmmo()
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

    public float GetHeatingLevel() => 0;
    public float GetMaxOverheat() => 100;
    #endregion
}