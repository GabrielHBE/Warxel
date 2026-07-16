using UnityEngine;
using FishNet.Object;

public class AttackHelicopterGunner : NetworkBehaviour, IVehicleArmory
{
    [SerializeField] private AttackHelicopter helicopter;
    [SerializeField] private Camera gunnerGunCamera;
    public AttackHelicopterGunnerProperties properties;
    public Transform shootPos;

    // REMOVIDO: private int firingStateId;
    private bool wasOverheatedLastFrame = false;

    // Estado Interno - Visual
    private float current_spread;
    private Vector3 shakeOffset;
    private float current_camera = 1;
    private bool isActive = true;

    // Rotação acumulada (Para espelhar o PlayerController e evitar bugs com shakeOffset)
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

    void Awake()
    {
        // ATUALIZADO: sem stateId, apenas reseta o estado
        Firing.ResetState();
        // Garante que o estado de superaquecimento comece falso
        properties.heatValues.heatState.isOverheated = false;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (!isActive)
        {
            StopFire(Time.deltaTime);
            return;
        }

        HandleSwitchCamera();

        // Se estiver superaquecido, força o resfriamento no Update também
        if (Heating.isOverheated(properties.heatValues))
        {
            // Se acabou de superaquecer, para o som
            if (!wasOverheatedLastFrame)
            {
                SoundManager.Instance.RequestPlay3dSound(properties.shootSound.clip.name, properties.shootSound.properties, transform.position, false);
                wasOverheatedLastFrame = true;
            }
            StopFire(Time.deltaTime);
        }
        else
        {
            wasOverheatedLastFrame = false;
        }

        if (gunnerGunCamera.enabled)
        {
            RotateGun(transform);

            // O shakeOffset agora é aplicado por cima de uma rotação limpa,
            // garantindo que não seja "assimilado" permanentemente pela câmera no próximo frame.
            Vector3 finalRotation = new Vector3(verticalRotation, horizontalRotation, 0f) + shakeOffset;
            transform.localEulerAngles = finalRotation;
        }
        else
        {
            ApplyFreeLookRotation();
        }
    }

    private void HandleSwitchCamera()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.HELICOPTER_switch_camera_key))
        {
            current_camera += 1;
            if (current_camera > 2)
            {
                current_camera = 1;
            }

            if (current_camera == 1)
            {
                helicopter.currentSeat.playerController.playerCamera.enabled = true;
                gunnerGunCamera.enabled = false;
            }
            else
            {
                helicopter.currentSeat.playerController.playerCamera.enabled = false;
                gunnerGunCamera.enabled = true;
            }
        }
    }

    private void ApplyFreeLookRotation()
    {
        float mouseY_freelook = InputManager.GetAxis("Mouse Y") * -Settings.Instance._controls.helicopter_sensibility;
        float mouseX_freelook = InputManager.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;

        Vector3 currentEuler = helicopter.currentSeat.activeCamera.transform.localEulerAngles;

        float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
        float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

        currentX += mouseY_freelook;
        currentY += mouseX_freelook;

        currentX = Mathf.Clamp(currentX, -80f, 20f);
        currentY = Mathf.Clamp(currentY, -90f, 90f);

        helicopter.currentSeat.activeCamera.transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    public void RotateGun(Transform gunTransform)
    {
        // Inicializa as variáveis baseadas na rotação atual assim que a arma começa a ser rotacionada
        if (!hasInitializedRotations)
        {
            Vector3 currentEuler = gunTransform.localEulerAngles;
            verticalRotation = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
            horizontalRotation = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;
            hasInitializedRotations = true;
        }

        float mouseX = InputManager.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;
        float mouseY = InputManager.GetAxis("Mouse Y") * Settings.Instance._controls.helicopter_sensibility;

        float applySpeed = Mathf.Max(properties.recoilValues.applyRecoilSpeed, 0.01f);

        horizontalRecoilCurrent = Mathf.SmoothDamp(horizontalRecoilCurrent, horizontalRecoilTarget, ref horizontalRecoilVelocity, applySpeed);
        recoilVerticalCurrent = Mathf.SmoothDamp(recoilVerticalCurrent, recoilVerticalTarget, ref recoilVerticalVelocity, applySpeed);

        // Aplica o input + recuo de forma aditiva nas variáveis isoladas, idêntico ao PlayerController
        horizontalRotation += mouseX + horizontalRecoilCurrent;
        verticalRotation -= mouseY + recoilVerticalCurrent;

        verticalRotation = Mathf.Clamp(verticalRotation, -5f, 80f);
        horizontalRotation = Mathf.Clamp(horizontalRotation, -90f, 90f);

        gunTransform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);

        // Zera os alvos logo após aplicá-los para não acumular forças irreais
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

        float vr = properties.recoilValues.recoilPattern[recoil_position_in_array].verticalRecoil;
        float hr = properties.recoilValues.recoilPattern[recoil_position_in_array].horizontalRecoil;

        var recoil = Recoil.CalculateCameraRecoil(
            vr,
            hr,
            properties.recoilValues.firstShootRecoilMultiplier,
            is_first_shot,
            2
        );

        recoilVerticalTarget += recoil.vertical;
        horizontalRecoilTarget += recoil.horizontal;

        is_first_shot = true;
        recoil_position_in_array++;
    }

    private void ExecuteFire()
    {
        SoundManager.Instance.RequestPlay3dSound(properties.shootSound.clip.name, properties.shootSound.properties, transform.position, false);
        SoundManager.Play2dSoundLocal(properties.shootSound.clip, properties.shootSound.properties);

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

        if (ProjectileSpawner.Instance != null) ProjectileSpawner.Instance.CreateProjectile(properties.bulletPref, properties.dummyBullet.gameObject, prop, properties.projectileValues);
    }

    private void StopFire(float deltaTime)
    {
        recoil_position_in_array = 0;
        is_first_shot = false;

        current_spread = Spread.ResetSpread(current_spread, properties.spreadValues.baseSpread, properties.spreadValues.spreadRecovery);

        // APENAS RESFRIAMENTO
        properties.heatValues.heatState.currentHeat = Heating.HandleCooling(properties.heatValues, deltaTime);
    }

    public void Shoot()
    {
        float deltaTime = Time.deltaTime;

        // Obtém inputs
        bool isInputHeld = InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_shoot_key);
        bool isInputPressed = InputManager.GetKeyDown(Settings.Instance._keybinds.HELICOPTER_shoot_key);

        // Verifica se está superaquecido (usando o estado persistente)
        if (Heating.isOverheated(properties.heatValues))
        {
            // Força o resfriamento
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
            isReloading: false,
            isRolling: false,
            isDead: false,
            currentAmmo: 1,
            deltaTime
        );

        // Obtém o estado atual de disparo (sem stateId)
        Firing.FireMode currentMode = Firing.GetCurrentFireMode();

        if (shootResult.shouldShoot)
        {
            ExecuteFire();
        }

        // ATUALIZADO: Heating.ShouldHeat agora recebe o currentMode diretamente
        if (Heating.ShouldHeat(currentMode, isInputHeld))
        {
            // Aplica aquecimento
            properties.heatValues.heatState.currentHeat = Heating.HandleHeating(properties.heatValues, deltaTime);

            // Verifica se atingiu o limite de superaquecimento
            if (properties.heatValues.heatState.currentHeat >= properties.heatValues.maxHeat)
            {
                // Marca como superaquecido
                properties.heatValues.heatState.isOverheated = true;

                // Para o som ao superaquecer
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

    public float GetHeatingLevel() => properties.heatValues.heatState.currentHeat;
    public float GetMaxOverheat() => properties.heatValues.maxHeat;
    public Sprite GetArmoryIcon() => properties.hudIcon;

    public void ActivateArmory()
    {
        isActive = true;
        SetupFiringSystem();
    }

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

    public void DeactivateArmory()
    {
        current_camera = 2;
        gunnerGunCamera.enabled = false;
        isActive = false;
        helicopter.currentSeat.playerController.playerCamera.enabled = true;
    }

    public string GetCurrentAmmo() => "";
}