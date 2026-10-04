using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class VehicleArmory : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shootPos;
    [SerializeField] protected Vehicle vehicle;
    [SerializeField] protected VehicleArmoryProperties properties;
    
    [Header("Armory Camera")]
    [SerializeField] protected Camera armoryCamera;
    [Tooltip("Pivot rotated by mouse input. Defaults to the armory camera transform.")]
    [SerializeField] private Transform cameraRotationPivot;
    [Tooltip("Allows mouse rotation only while the armory camera is active.")]
    [SerializeField] private bool canRotateCamera;

    public Camera ArmoryCamera => armoryCamera;
    public Transform CameraRotationPivot => cameraRotationPivot != null
        ? cameraRotationPivot : armoryCamera != null ? armoryCamera.transform : null;
    public virtual bool CanRotateCamera => canRotateCamera;
    public bool CanUseCamera => isActive && isActiveAndEnabled &&
        armoryCamera != null && armoryCamera.gameObject.activeInHierarchy;

    public virtual void SetCameraActive(bool active)
    {
        if (armoryCamera == null) return;
        armoryCamera.enabled = active;
        AudioListener listener = armoryCamera.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = active;
        if (!active) CameraRotationPivot.localRotation = Quaternion.identity;
    }

    [Header("Rotation Settings")]
    [SerializeField] private bool canRotate;
    [SerializeField] private Transform xRotationPivot;
    [SerializeField] private Transform yRotationPivot;

    [Header("Interpolated rotation pivots")]
    [SerializeField] private Transform[] xRotationFollowers;
    [SerializeField] private Transform[] yRotationFollowers;
    [SerializeField, Min(0f)] private float followerRotationSpeed = 8f;

    [Header("Rotational Clamps")]
    [SerializeField] private bool hasXAxisClamp;
    [SerializeField] private bool hasYAxisClamp;
    [SerializeField] private Vector2 minRotationClamp;
    [SerializeField] private Vector2 maxRotationClamp;

    private MuzzleController muzzleController;
    private Vehicle parentVehicle;
    private VehicleArmoryFireAudio fireAudio;
    private VehicleArmoryVisualRecoil visualRecoil;
    protected bool wasOverheatedLastFrame = false;
    private bool isReloading;
    protected float reloadTimer;

    private bool UsesHeat => properties != null && properties.useHeatValues && properties.heatValues != null;
    private bool UsesReload => properties != null && properties.useReloadValues && properties.reloadValues != null;

    // Estado Interno - Visual
    private float current_spread;

    protected bool isActive = true;

    // Rotação acumulada para cada pivô
    private float verticalRotation;
    private float horizontalRotation;
    private bool hasInitializedRotations = false;

    // Estado Interno (Recuo)
    private float recoilVerticalTarget;
    private float recoilVerticalCurrent;
    private float recoilVerticalVelocity;

    private float horizontalRecoilTarget;
    private float horizontalRecoilCurrent;
    private float horizontalRecoilVelocity;

    private int recoil_position_in_array = 0;
    private bool is_first_shot = false;

    protected virtual void Awake()
    {
        SetCameraActive(false);
        if (xRotationPivot == null) xRotationPivot = transform;
        if (yRotationPivot == null) yRotationPivot = transform;

        // Armamentos especializados podem usar outro tipo de propriedades.
        if (properties == null) return;

        fireAudio = new VehicleArmoryFireAudio(properties, transform);
        visualRecoil = new VehicleArmoryVisualRecoil(this, properties);
        // ATUALIZADO: sem stateId, apenas reseta o estado
        Firing.ResetState(properties.firing.fireModes);
        // Garante que o estado de superaquecimento comece falso
        if (UsesHeat) properties.heatValues.heatState.isOverheated = false;
        if (UsesReload)
        {
            if (properties.reloadValues.mags == null)
                properties.reloadValues.mags = new List<int>();
            if (properties.reloadValues.mags.Count == 0)
                properties.reloadValues.PopulateMags();
            while (properties.reloadValues.mags.Count < properties.reloadValues.magCount)
                properties.reloadValues.mags.Add(properties.reloadValues.bulletsPerMag);
        }
        properties.recoilValues.CalculateRecoilSpeed(properties.firing.interval);

        muzzleController = GetComponent<MuzzleController>();
        if (muzzleController != null)
        {
            muzzleController.RequestClearMuzzles();
            muzzleController.RequestSetupMuzzle(shootPos);
            muzzleController.RequestSetMuzzleLifetime(properties.firing.interval);
        }
    }


    protected virtual void Update()
    {
        if (!IsOwner)
        {
            fireAudio?.Stop();
            visualRecoil?.Reset();
            return;
        }

        if (properties == null)
        {
            if (isActive && canRotate) RotateGun();
            return;
        }

        if (UsesReload)
            UpdateReload(Time.deltaTime);

        if (!isActive)
        {
            fireAudio.Stop();
            visualRecoil.Reset();
            StopFire(Time.deltaTime);
            return;
        }

        if (UsesReload && InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_reloadKey))
            TryStartReload(false);

        // Se estiver superaquecido, força o resfriamento no Update também
        if (UsesHeat && Heating.isOverheated(properties.heatValues))
        {
            // Se acabou de superaquecer, para o som
            if (!wasOverheatedLastFrame)
            {
                if (properties.shootingSoundMode == VehicleArmoryProperties.ShootingSoundMode.PerBullet && properties.shootSound?.clip != null)
                    SoundManager.Instance.RequestPlay3dSound(properties.shootSound.clip.name, properties.shootSound.properties, transform.position, false);
                wasOverheatedLastFrame = true;
            }
            fireAudio.Stop();
            StopFire(Time.deltaTime);
        }
        else
        {
            wasOverheatedLastFrame = false;
        }

        if (canRotate) RotateGun();
    }

    protected virtual void LateUpdate()
    {
        if (!canRotate) return;

        float interpolation = Mathf.Clamp01(followerRotationSpeed * Time.deltaTime);
        if (interpolation <= 0f) return;

        RotateFollowers(xRotationFollowers, xRotationPivot.localEulerAngles.x, interpolation, true);
        RotateFollowers(yRotationFollowers, yRotationPivot.localEulerAngles.y, interpolation, false);
    }

    private void RotateFollowers(Transform[] followers, float targetAngle, float interpolation, bool rotateX)
    {
        if (followers == null) return;

        foreach (Transform follower in followers)
        {
            if (follower == null || follower == xRotationPivot || follower == yRotationPivot) continue;

            Vector3 angles = follower.localEulerAngles;
            if (rotateX)
                angles.x = Mathf.LerpAngle(angles.x, targetAngle, interpolation);
            else
                angles.y = Mathf.LerpAngle(angles.y, targetAngle, interpolation);

            follower.localEulerAngles = angles;
        }
    }

    public void RotateGun()
    {
        // Inicializa as variáveis baseadas na rotação atual assim que a arma começa a ser rotacionada
        if (!hasInitializedRotations)
        {
            verticalRotation = (xRotationPivot.localEulerAngles.x > 180) ? xRotationPivot.localEulerAngles.x - 360 : xRotationPivot.localEulerAngles.x;
            horizontalRotation = (yRotationPivot.localEulerAngles.y > 180) ? yRotationPivot.localEulerAngles.y - 360 : yRotationPivot.localEulerAngles.y;
            hasInitializedRotations = true;
        }

        if (parentVehicle == null) parentVehicle = GetComponentInParent<Vehicle>();
        bool blockMouseRotation = parentVehicle != null && parentVehicle.ShouldBlockMouseRotationForFreeLook();
        float mouseX = blockMouseRotation ? 0f : InputManager.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;
        float mouseY = blockMouseRotation ? 0f : InputManager.GetAxis("Mouse Y") * Settings.Instance._controls.helicopter_sensibility;

        float applySpeed = properties?.recoilValues != null
            ? Mathf.Max(properties.recoilValues.applyRecoilSpeed, 0.01f)
            : 0.1f;

        horizontalRecoilCurrent = Mathf.SmoothDamp(horizontalRecoilCurrent, horizontalRecoilTarget, ref horizontalRecoilVelocity, applySpeed);
        recoilVerticalCurrent = Mathf.SmoothDamp(recoilVerticalCurrent, recoilVerticalTarget, ref recoilVerticalVelocity, applySpeed);

        // Aplica o input + recuo de forma aditiva nas variáveis isoladas, idêntico ao PlayerController
        horizontalRotation += mouseX + horizontalRecoilCurrent;
        verticalRotation -= mouseY + recoilVerticalCurrent;

        if (hasXAxisClamp) verticalRotation = Mathf.Clamp(verticalRotation, minRotationClamp.x, maxRotationClamp.x);
        if (hasYAxisClamp) horizontalRotation = Mathf.Clamp(horizontalRotation, minRotationClamp.y, maxRotationClamp.y);

        if (yRotationPivot == xRotationPivot)
        {
            xRotationPivot.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
        }
        else
        {
            xRotationPivot.localRotation = Quaternion.Euler(verticalRotation, 0, 0f);
            yRotationPivot.localRotation = Quaternion.Euler(0, horizontalRotation, 0f);
        }

        horizontalRecoilTarget = 0f;
        recoilVerticalTarget = 0f;
    }

    private void ApplyGunnerRecoil()
    {
        if (properties.recoilValues.recoilPattern == null || properties.recoilValues.recoilPattern.Length == 0) return;

        if (recoil_position_in_array >= properties.recoilValues.recoilPattern.Length)
        {
            recoil_position_in_array = 0;
        }

        float vr = Recoil.GetVerticalRecoilDirection(properties.recoilValues.recoilPattern[recoil_position_in_array].verticalRecoil);
        float hr = Recoil.GetHorizontalRecoilDirection(properties.recoilValues.recoilPattern[recoil_position_in_array].horizontalRecoil);

        var recoil = Recoil.CalculateCameraRecoil(
            vr,
            hr,
            properties.recoilValues.firstShootRecoilMultiplier,
            !is_first_shot,
            2
        );

        recoilVerticalTarget += recoil.vertical;
        horizontalRecoilTarget += recoil.horizontal;

        is_first_shot = true;
        recoil_position_in_array++;
    }

    private void ExecuteFire()
    {
        PlayShotEffects();

        Quaternion finalRotation = Spread.CalculateSpreadRotation(shootPos, current_spread);

        current_spread = Spread.AddSpread(current_spread, properties.spreadValues.spreadIncreaser, properties.spreadValues.maxSpread);

        ApplyGunnerRecoil();

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = shootPos.position,
            rotation = finalRotation,
            ignoredObject = transform.root,
            root = gameObject
        };
        if (muzzleController != null) muzzleController.RequestCreateMuzzle();

        if (ProjectileSpawner.Instance != null) ProjectileSpawner.Instance.CreateProjectile(properties.bulletPref, properties.dummyBullet.gameObject, prop, properties.projectileValues);
    }

    protected void PlayShotAudio() => fireAudio?.OnShot();
    protected void PlayShotEffects()
    {
        PlayShotAudio();
        visualRecoil?.Play();
    }

    protected void UpdateFireAudio(bool shouldBeFiring) => fireAudio?.Update(shouldBeFiring);

    private void StopFire(float deltaTime)
    {
        recoil_position_in_array = 0;
        is_first_shot = false;

        current_spread = Spread.ResetSpread(current_spread, properties.spreadValues.baseSpread, properties.spreadValues.spreadRecovery);

        if (UsesHeat)
            properties.heatValues.heatState.currentHeat = Heating.HandleCooling(properties.heatValues, deltaTime);
    }

    private void TryStartReload(bool automatic)
    {
        if (!UsesReload) return;

        var values = properties.reloadValues;
        if (!ProcessReload.Reload.ReloadLogic.CanStartReload(
            values,
            !automatic && InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_shoot_key),
            isReloading, false, values.GetTotalReserveAmmo()))
            return;

        reloadTimer = ProcessReload.Reload.ReloadLogic.CalculateReloadTime(values, values.IsMagazineEmpty());
        isReloading = true;
        fireAudio.Stop();
    }

    private void UpdateReload(float deltaTime)
    {
        if (!isReloading) return;

        var values = properties.reloadValues;
        if (values.GetTotalReserveAmmo() <= 0 || values.IsMagazineFull())
        {
            isReloading = false;
            return;
        }

        if (values.isSingleReload)
        {
            reloadTimer -= deltaTime;
            if (reloadTimer > 0f) return;

            ProcessReload.Reload.ReloadLogic.TransferMagazineAmmoSingleReload(values);
            if (values.IsMagazineFull() || values.GetTotalReserveAmmo() <= 0)
            {
                isReloading = false;
                if (isActive) Firing.ResetState();
            }
            else
            {
                reloadTimer = Mathf.Max(ProcessReload.Reload.ReloadLogic.MIN_RELOAD_TIME, values.reloadTime);
            }
            return;
        }

        var result = ProcessReload.Reload.ReloadLogic.ProcessStandardReload(
            values, reloadTimer, deltaTime, values.IsMagazineEmpty());
        reloadTimer = result.remainingCooldown;
        isReloading = result.isReloading;
        if (result.shouldFinishReload && isActive) Firing.ResetState();
    }

    public virtual void Shoot()
    {
        float deltaTime = Time.deltaTime;

        // Obtém inputs
        bool isInputHeld = InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_shoot_key);
        bool isInputPressed = InputManager.GetKeyDown(Settings.Instance._keybinds.HELICOPTER_shoot_key);

        if (UsesReload && isReloading && properties.reloadValues.isSingleReload &&
            isInputPressed && properties.reloadValues.GetCurrentMagAmmo() > 0)
            isReloading = false;

        int currentAmmo = UsesReload ? properties.reloadValues.GetCurrentMagAmmo() : 1;

        // Verifica se está superaquecido (usando o estado persistente)
        if (UsesHeat && Heating.isOverheated(properties.heatValues))
        {
            // Força o resfriamento
            fireAudio.Stop();
            StopFire(deltaTime);
            return;
        }

        // ATUALIZADO: sem stateId
        Firing.UpdateTimeToFire(deltaTime);

        // Processa o tiro usando o sistema Firing (sem stateId)
        var shootResult = Firing.ProcessShooting(
            properties.firing,
            isInputHeld,
            isInputPressed,
            isReloading: isReloading,
            isRolling: false,
            isDead: false,
            currentAmmo: currentAmmo,
            deltaTime
        );

        // Obtém o estado atual de disparo (sem stateId)
        Firing.FireMode currentMode = Firing.GetCurrentFireMode();
        bool shouldHeat = UsesHeat && !isReloading && currentAmmo > 0 && Heating.ShouldHeat(currentMode, isInputHeld);
        fireAudio.Update(!isReloading && currentAmmo > 0 && (currentMode == Firing.FireMode.Auto ? isInputHeld
            : currentMode == Firing.FireMode.Burst ? Firing.IsBursting() || shootResult.shouldShoot
            : shootResult.shouldShoot));

        if (shootResult.shouldShoot)
        {
            ExecuteFire();
            if (UsesReload)
            {
                properties.reloadValues.mags[^1]--;
                if (properties.reloadValues.IsMagazineEmpty()) TryStartReload(true);
            }
        }

        // ATUALIZADO: Heating.ShouldHeat agora recebe o currentMode diretamente
        if (shouldHeat)
        {
            // Aplica aquecimento
            properties.heatValues.heatState.currentHeat = Heating.HandleHeating(properties.heatValues, deltaTime);

            // Verifica se atingiu o limite de superaquecimento
            if (properties.heatValues.heatState.currentHeat >= properties.heatValues.maxHeat)
            {
                // Marca como superaquecido
                properties.heatValues.heatState.isOverheated = true;

                // Para o som ao superaquecer
                fireAudio.Stop();
                if (properties.shootingSoundMode == VehicleArmoryProperties.ShootingSoundMode.PerBullet && properties.shootSound?.clip != null)
                    SoundManager.Instance.RequestPlay3dSound(properties.shootSound.clip.name, properties.shootSound.properties, transform.position, false);

                // Aplica um pequeno resfriamento para começar a esfriar
                properties.heatValues.heatState.currentHeat = Heating.HandleCooling(properties.heatValues, deltaTime);
            }
        }
        else
        {
            // RESFRIAMENTO: parou de atirar ou soltou o botão
            StopFire(deltaTime);
        }
    }

    public virtual float GetHeatingLevel() => UsesHeat ? properties.heatValues.heatState.currentHeat : 0f;
    public virtual float GetMaxHeat() => UsesHeat ? properties.heatValues.maxHeat : 100f;
    public virtual float GetMaxOverheat() => UsesHeat ? properties.heatValues.maxHeat : 100f;
    public virtual Sprite GetArmoryIcon() => properties != null ? properties.hudIcon : null;

    public virtual void ActivateArmory()
    {
        isActive = true;
        SetupFiringSystem();
    }

    public virtual void SetupFiringSystem()
    {
        Firing.ResetState(properties?.firing.fireModes);

        // Garante que o modo de tiro estático atual é válido para este armamento
        if (properties != null && properties.firing.fireModes != null && properties.firing.fireModes.Count > 0)
        {
            if (!properties.firing.fireModes.Contains(Firing.GetCurrentFireMode()))
            {
                Firing.SwitchFireMode(properties.firing);
            }
        }
    }

    public virtual void DeactivateArmory()
    {
        SetCameraActive(false);

        fireAudio?.Stop();
        visualRecoil?.Reset();
        isActive = false;
    }

    public virtual string GetCurrentAmmo()
    {
        if (!UsesReload) return "";
        var values = properties.reloadValues;
        if (values.mags == null || values.mags.Count == 0) return "0 / 0";
        return $"{values.GetCurrentMagAmmo()} / {values.GetTotalReserveAmmo()}";
    }

    private void OnDisable()
    {
        SetCameraActive(false);
        fireAudio?.Stop();
        visualRecoil?.Reset();
    }

    public void EnableRotation() => canRotate = true;
    public void DisableRotation() => canRotate = false;
}
