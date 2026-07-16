using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;

public class ScoutHelicopterMainMinigun : NetworkBehaviour, IVehicleArmory
{
    private bool isActive = true;
    public ScoutHelicopterMainMinigunProperties properties;

    [Header("Minigun Rotation Settings")]
    [SerializeField] private float maxRotationSpeed = 5000f;
    [SerializeField] private float rotationAcceleration = 2000f;
    [SerializeField] private float rotationDeceleration = 2000f;

    [Header("Miniguns Configuration")]
    [SerializeField] private List<MinigunDictionaryWrapper> miniguns = new List<MinigunDictionaryWrapper>();

    // REMOVIDO: private int firingStateId;
    private bool wasOverheatedLastFrame = false;

    // Weapon State Variables
    private float _currentSpread;
    private float _currentRotationSpeed = 0f;

    void Awake()
    {
        // ATUALIZADO: sem stateId, apenas reseta o estado
        Firing.ResetState();
        // Garante que o estado de superaquecimento comece falso
        properties.heatValues.heatState.isOverheated = false;
    }

    void Update()
    {
        if (!IsOwner)
        {
            CoolDownCannon(Time.deltaTime);
            return;
        }

        if (!isActive)
        {
            CoolDownCannon(Time.deltaTime);
            return;
        }

        // Se estiver superaquecido, força o resfriamento
        if (Heating.isOverheated(properties.heatValues))
        {
            if (!wasOverheatedLastFrame)
            {
                wasOverheatedLastFrame = true;
            }
            CoolDownCannon(Time.deltaTime);
            // Para a rotação do cano
            UpdateBarrelRotation(Time.deltaTime, false);
        }
        else
        {
            wasOverheatedLastFrame = false;
        }

        UpdateWeapon(Time.deltaTime, isActive && InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_shoot_key));
    }

    public void UpdateWeapon(float deltaTime, bool isShooting)
    {
        UpdateBarrelRotation(deltaTime, isShooting);
    }

    private void UpdateBarrelRotation(float deltaTime, bool isShooting)
    {
        bool canShoot = isShooting && !Heating.isOverheated(properties.heatValues);
        float targetSpeed = canShoot ? maxRotationSpeed : 0f;
        float accelRate = isShooting ? rotationAcceleration : rotationDeceleration;

        _currentRotationSpeed = Mathf.MoveTowards(_currentRotationSpeed, targetSpeed, accelRate * deltaTime);

        if (_currentRotationSpeed <= 0.1f) return;

        foreach (var minigunDict in miniguns)
        {
            foreach (var pair in minigunDict.pairs)
            {
                if (pair.key != null)
                {
                    pair.key.Rotate(Vector3.right, _currentRotationSpeed * deltaTime);
                }
            }
        }
    }

    private void ExecuteFire()
    {
        SoundManager.Instance.RequestPlay3dSound(properties.shootSound.clip.name, properties.shootSound.properties, transform.position, false);
        SoundManager.Play2dSoundLocal(properties.shootSound.clip, properties.shootSound.properties);

        FireAllMiniguns();
    }

    private void FireAllMiniguns()
    {
        foreach (var minigunDict in miniguns)
        {
            foreach (var kvp in minigunDict.pairs)
            {
                Transform shootPosition = kvp.value;

                if (shootPosition != null)
                {
                    Quaternion finalRotation = Spread.CalculateSpreadRotation(shootPosition, _currentSpread);

                    _currentSpread = Spread.AddSpread(_currentSpread, properties.spreadValues.spreadIncreaser, properties.spreadValues.maxSpread);

                    Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
                    {
                        position = shootPosition.position,
                        rotation = finalRotation,
                        ignoredObject = transform.root,
                        root = gameObject
                    };

                    if (ProjectileSpawner.Instance != null) ProjectileSpawner.Instance.CreateProjectile(properties.bulletPref, properties.dummyBullet.gameObject, prop, properties.projectileValues);

                }
            }
        }
        _currentSpread += properties.spreadValues.spreadIncreaser;
        _currentSpread = Mathf.Clamp(_currentSpread, 0, properties.spreadValues.maxSpread);
    }

    private void CoolDownCannon(float deltaTime)
    {
        _currentSpread = Spread.ResetSpread(_currentSpread, properties.spreadValues.baseSpread, properties.spreadValues.spreadRecovery);

        properties.heatValues.heatState.currentHeat = Heating.HandleCooling(properties.heatValues, deltaTime);
    }


    public float GetMaxHeat()
    {
        return properties.heatValues.maxHeat;
    }

    #region Interface Methods
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
            CoolDownCannon(deltaTime);
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

                // Aplica um pequeno resfriamento para começar a esfriar
                properties.heatValues.heatState.currentHeat = Heating.HandleCooling(properties.heatValues, deltaTime);
            }
        }
        else
        {
            // RESFRIAMENTO: parou de atirar ou soltou o botão
            CoolDownCannon(deltaTime);
        }
    }

    public Sprite GetArmoryIcon() => properties.hudIcon;

    public void ActivateArmory()
    {
        isActive = true;
        // ATUALIZADO: sem stateId
        SetupFiringSystem();
    }

    public void DeactivateArmory()
    {
        isActive = false;
    }

    public string GetCurrentAmmo() => null;
    public float GetHeatingLevel() => properties.heatValues.heatState.currentHeat;
    public float GetMaxOverheat() => properties.heatValues.maxHeat;
    #endregion

    #region INNER CLASSES   
    [System.Serializable]
    public class MinigunPair
    {
        public Transform key;
        public Transform value;
    }

    [System.Serializable]
    public class MinigunDictionaryWrapper
    {
        public List<MinigunPair> pairs = new List<MinigunPair>();
    }
    #endregion
}