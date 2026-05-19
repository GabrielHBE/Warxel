using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;

public class ScoutHelicopterMainMinigun : NetworkBehaviour, IVehicleArmory
{   
    private bool isActive = true;
    public ScoutHelicopterMainMinigunProperties properties;

    [Header("Minigun Rotation Settings")]
    public float maxRotationSpeed = 5000f;
    public float rotationAcceleration = 2000f;
    public float rotationDeceleration = 2000f;

    [Header("Miniguns Configuration")]
    [SerializeField]
    private List<MinigunDictionaryWrapper> miniguns = new List<MinigunDictionaryWrapper>();

    // Weapon State Variables
    private float _shootDelayTimer = 0;
    private bool _isOverheated;
    [SerializeField] private float _overheatAmount;
    private float _nextFireTime;
    private float _currentSpread;
    private float _currentRotationSpeed = 0f;

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
        }

        UpdateWeapon(Time.deltaTime, isActive && Input.GetKey(Settings.Instance._keybinds.HELICOPTER_shoot_key));
    }

    public void UpdateWeapon(float deltaTime, bool isShooting)
    {
        UpdateBarrelRotation(deltaTime, isShooting);
        HandleShootDelay(isShooting, deltaTime);
    }


    private void HandleContinuousFire(float deltaTime)
    {
        if (_nextFireTime <= 0f)
        {
            FireAllMiniguns();
        }

        ApplyHeat(deltaTime);
    }

    private void UpdateBarrelRotation(float deltaTime, bool isShooting)
    {
        float targetSpeed = isShooting && !_isOverheated ? maxRotationSpeed : 0f;
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

    private void FireAllMiniguns()
    {
        foreach (var minigunDict in miniguns)
        {
            foreach (var kvp in minigunDict.pairs)
            {
                Transform shootPosition = kvp.value;

                if (shootPosition != null)
                {
                    Quaternion finalRotation = CalculateSpreadRotation(shootPosition);
                    ShootBullet(shootPosition.position, finalRotation);
                }
            }
        }

        RequestPlayShootSound();
        _currentSpread += properties.spread;
        _currentSpread = Mathf.Clamp(_currentSpread, 0, properties.max_spread);
        _nextFireTime = properties.interval;

        if (properties.shoot_sound != null && IsOwner)
            properties.shoot_sound.PlayOneShot(properties.shoot_sound.clip);
    }

    private Quaternion CalculateSpreadRotation(Transform shootPosition)
    {
        float spreadX = Random.Range(-_currentSpread, _currentSpread) / 10f;
        float spreadY = Random.Range(-_currentSpread, _currentSpread) / 10f;
        float spreadZ = Random.Range(-_currentSpread, _currentSpread) / 10f;

        Quaternion spreadRotation = Quaternion.Euler(spreadX, spreadY, spreadZ);
        return shootPosition.rotation * spreadRotation;
    }

    private void ApplyHeat(float deltaTime)
    {
        _overheatAmount += deltaTime;

        if (_overheatAmount >= properties.overheat_time)
        {
            _isOverheated = true;
        }
    }

    private void CoolDownCannon(float deltaTime)
    {
        _currentSpread = 0;

        float coolSpeed = _isOverheated ? (deltaTime / 2f) : deltaTime;
        _overheatAmount = Mathf.MoveTowards(_overheatAmount, 0f, coolSpeed);

        if (_overheatAmount <= 0)
            _isOverheated = false;
    }

    private void HandleShootDelay(bool canShoot, float deltaTime)
    {
        _shootDelayTimer = canShoot ? _shootDelayTimer + deltaTime : 0;
    }

    public void StopShoot()
    {
        _shootDelayTimer = 0;
    }

    [ServerRpc(RequireOwnership = true)]
    private void ShootBullet(Vector3 position, Quaternion rotation)
    {
        if (properties.bulletPref == null)
            return;

        Transform bulletObj = Instantiate(properties.bulletPref, position, rotation);
        bulletObj.localScale *= 2;

        Bullet.BulletData data = new Bullet.BulletData
        {
            position = position,
            rotation = rotation,
            direction = rotation * Vector3.forward,
            speed = properties.muzzle_velocity,
            dropMultiplier = properties.bullet_drop,
            infantaryDamage = properties.infantary_damage,
            damageDropoff = properties.damage_dropoff,
            damageDropoffTimer = properties.damage_dropoff_timer,
            destructionForce = properties.destruction_force,
            minimumDamage = properties.minimum_damage,
            hsMultiplier = 2,
            size = 1,
            canDamageVehicles = true,
            vehicleDamage = properties.vehicle_damage,
            delaytoEnableForNonOwner = 0,
        };

        Spawn(bulletObj.gameObject, Owner);
        bulletObj.GetComponent<Bullet>()?.CreateBullet(data, transform.root);
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestPlayShootSound()
    {
        PlayShootSoundClientRpc();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void PlayShootSoundClientRpc()
    {
        if (properties.shoot_sound != null)
            properties.shoot_sound.PlayOneShot(properties.shoot_sound.clip);
    }

    public float GetMaxHeat()
    {
        return properties.overheat_time;
    }

    public float GetCurrentHeat()
    {
        return _overheatAmount;
    }

    public bool IsOverheated()
    {
        return _isOverheated;
    }

    #region Interface Methods
    public void Shoot()
    {
        float deltaTime = Time.deltaTime;
        // Verifica se a tecla está pressionada (usando o sistema de Settings do seu projeto)
        bool isInputPressed = Input.GetKey(Settings.Instance._keybinds.HELICOPTER_shoot_key);
        bool canShoot = !_isOverheated && isInputPressed;

        // Timer de segurança para evitar disparos acidentais (cliques ultra rápidos)
        _shootDelayTimer = canShoot ? _shootDelayTimer + deltaTime : 0;

        if (_nextFireTime > 0) _nextFireTime -= deltaTime;

        if (canShoot && _shootDelayTimer >= 0.05f)
        {
            HandleContinuousFire(deltaTime);
        }
        else
        {
            CoolDownCannon(deltaTime);
        }
    }

    public Sprite GetArmoryIcon() => properties.hudIcon;
    public void ActivateArmory() => isActive = true;
    public void DeactivateArmory() => isActive = false;
    public string GetCurrentAmmo() => null;
    public float GetHeatingLevel() => _overheatAmount;
    public float GetMaxOverheat() => properties.overheat_time;

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