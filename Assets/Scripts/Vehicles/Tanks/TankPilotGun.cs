using System.Collections;
using FishNet.Object;
using UnityEngine;

public class TankPilotGun : NetworkBehaviour, IVehicleArmory
{
    [Header("Properties")]
    [SerializeField] private TankPilotGunProperties properties;

    [Header("Instances")]
    [SerializeField] private Transform shootPos;
    [SerializeField] private Transform tankGunnerGun;
    [SerializeField] private Tank tankContext;

    // Weapon State Variables
    private float _currentSpread;
    private bool wasOverheatedLastFrame = false;
    private bool isActive = true;

    private Vector3 gunnerGunOriginalLocalPosition;
    private Quaternion gunnerGunOriginalLocalRotation;
    private bool isGunnerGunRecoiling = false;
    private Coroutine gunnerGunRecoilCoroutine;
    private bool wasFiringThisFrame = false;

    void Awake()
    {
        // Reseta o estado de disparo
        Firing.ResetState();
        // Garante que o estado de superaquecimento comece falso
        properties.heatValues.heatState.isOverheated = false;
        // Reseta o spread
        _currentSpread = properties.spreadValues.baseSpread;
    }

    void Start()
    {
        if (tankGunnerGun != null)
        {
            gunnerGunOriginalLocalPosition = tankGunnerGun.localPosition;
            gunnerGunOriginalLocalRotation = tankGunnerGun.localRotation;
        }
    }

    void Update()
    {
        if (!IsOwner)
        {
            CoolDownGun(Time.deltaTime);
            return;
        }

        if (!isActive)
        {
            CoolDownGun(Time.deltaTime);
            return;
        }

        // Se estiver superaquecido, força o resfriamento
        if (Heating.isOverheated(properties.heatValues))
        {
            if (!wasOverheatedLastFrame)
            {
                wasOverheatedLastFrame = true;
            }
            CoolDownGun(Time.deltaTime);
        }
        else
        {
            wasOverheatedLastFrame = false;
        }

        UpdateWeapon(Time.deltaTime, isActive && InputManager.GetKey(Settings.Instance._keybinds.TANK_shoot_key));
    }

    public void UpdateWeapon(float deltaTime, bool isShooting)
    {
        // Atualiza o tempo para o próximo tiro
        Firing.UpdateTimeToFire(deltaTime);
    }

    public void Shoot()
    {
        if (!IsOwner) return;

        float deltaTime = Time.deltaTime;

        // Obtém inputs
        bool isInputHeld = InputManager.GetKey(Settings.Instance._keybinds.TANK_shoot_key);
        bool isInputPressed = InputManager.GetKeyDown(Settings.Instance._keybinds.TANK_shoot_key);

        // Verifica se está superaquecido
        if (Heating.isOverheated(properties.heatValues))
        {
            // Força o resfriamento
            CoolDownGun(deltaTime);
            return;
        }

        // Atualiza o tempo para o próximo tiro
        Firing.UpdateTimeToFire(deltaTime);

        // Processa o tiro usando o sistema Firing
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

        // Obtém o estado atual de disparo
        Firing.FireMode currentMode = Firing.GetCurrentFireMode();

        if (shootResult.shouldShoot)
        {
            ExecuteFire();
        }

        // Gerencia o aquecimento
        if (Heating.ShouldHeat(currentMode, isInputHeld))
        {
            // Aplica aquecimento
            properties.heatValues.heatState.currentHeat = Heating.HandleHeating(properties.heatValues, deltaTime);

            // Verifica se atingiu o limite de superaquecimento
            if (properties.heatValues.heatState.currentHeat >= properties.heatValues.maxHeat)
            {
                // Marca como superaquecido
                properties.heatValues.heatState.isOverheated = true;

                // Aplica um pequeno resfriamento para começar a esfriar
                properties.heatValues.heatState.currentHeat = Heating.HandleCooling(properties.heatValues, deltaTime);
            }
        }
        else
        {
            // RESFRIAMENTO: parou de atirar ou soltou o botão
            CoolDownGun(deltaTime);
        }
    }

    private void ExecuteFire()
    {
        // Toca o som
        SoundManager.Play3dSoundLocal(properties.shootSound.clip, properties.shootSound.properties, transform.position);

        Quaternion finalRotation = Spread.CalculateSpreadRotation(shootPos, _currentSpread);

        _currentSpread = Spread.AddSpread(_currentSpread, properties.spreadValues.spreadIncreaser, properties.spreadValues.maxSpread);

        ApplyMachineGunRecoil();

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = shootPos.position,
            rotation = finalRotation,
            ignoredObject = transform.root,
            root = transform.root.gameObject
        };

        if (ProjectileSpawner.Instance != null) ProjectileSpawner.Instance.CreateProjectile(properties.bulletPref, properties.dummyBullet.gameObject, prop, properties.projectileValues);
    }

    private void ApplyMachineGunRecoil()
    {
        if (tankGunnerGun == null || isGunnerGunRecoiling) return;

        if (gunnerGunRecoilCoroutine != null)
            StopCoroutine(gunnerGunRecoilCoroutine);

        gunnerGunRecoilCoroutine = StartCoroutine(MachineGunRecoilRoutine());
    }

    private IEnumerator MachineGunRecoilRoutine()
    {
        isGunnerGunRecoiling = true;

        float recoilDistance = 0.4f;

        Vector3 localRecoilDirection = -Vector3.forward;
        Vector3 recoilPosition = gunnerGunOriginalLocalPosition + (localRecoilDirection * recoilDistance);

        float recoilTimer = 0f;
        while (recoilTimer < properties.recoilValues.applyRecoilSpeed)
        {
            recoilTimer += Time.deltaTime;
            float t = recoilTimer / properties.recoilValues.applyRecoilSpeed;
            tankGunnerGun.localPosition = Vector3.Lerp(
                gunnerGunOriginalLocalPosition,
                recoilPosition,
                t
            );
            yield return null;
        }

        float returnTimer = 0f;
        while (returnTimer < properties.recoilValues.resetRecoilSpeed)
        {
            returnTimer += Time.deltaTime;
            float t = returnTimer / properties.recoilValues.resetRecoilSpeed;
            tankGunnerGun.localPosition = Vector3.Lerp(
                recoilPosition,
                gunnerGunOriginalLocalPosition,
                t
            );
            yield return null;
        }

        tankGunnerGun.localPosition = gunnerGunOriginalLocalPosition;
        tankGunnerGun.localRotation = gunnerGunOriginalLocalRotation;

        isGunnerGunRecoiling = false;
        gunnerGunRecoilCoroutine = null;
    }

    private void CoolDownGun(float deltaTime)
    {
        // Resfria o spread
        _currentSpread = Spread.ResetSpread(_currentSpread, properties.spreadValues.baseSpread, properties.spreadValues.spreadRecovery);

        // Resfria o calor
        properties.heatValues.heatState.currentHeat = Heating.HandleCooling(properties.heatValues, deltaTime);
    }

    #region IVehicleArmory Implementation
    public void ActivateArmory()
    {
        isActive = true;
        SetupFiringSystem();
    }

    public void DeactivateArmory()
    {
        isActive = false;
    }

    public Sprite GetArmoryIcon() => properties.hudIcon;

    public string GetCurrentAmmo() => "";

    public float GetHeatingLevel() => properties.heatValues.heatState.currentHeat;

    public float GetMaxOverheat() => properties.heatValues.maxHeat;

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
    #endregion
}