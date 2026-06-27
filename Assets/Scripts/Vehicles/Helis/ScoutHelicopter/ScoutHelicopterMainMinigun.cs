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

        UpdateWeapon(Time.deltaTime, isActive && InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_shoot_key));
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
            SoundManager.Instance.RequestPlay3dSound(properties.shoot_sound.name, properties.shootSoundProperties, transform.position, false);
            SoundManager.Play2dSoundLocal(properties.shoot_sound, properties.shootSoundProperties);
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
                    Quaternion finalRotation = Spread.CalculateSpreadRotation(shootPosition, _currentSpread);

                    _currentSpread = Spread.AddSpread(_currentSpread, properties.spread, properties.max_spread);

                    Bullet.BulletData data = new Bullet.BulletData
                    {
                        position = shootPosition.position,
                        rotation = finalRotation,
                        direction = finalRotation * Vector3.forward,
                        speed = properties.muzzle_velocity,
                        dropMultiplier = properties.bullet_drop,
                        infantaryDamage = properties.infantary_damage,
                        damageDropoff = properties.damage_dropoff,
                        damageDropoffTimer = properties.damage_dropoff_timer,
                        destructionForce = properties.destruction_force,
                        minimumDamage = properties.minimum_damage,
                        hsMultiplier = 2,
                        canDamageVehicles = true,
                        vehicleDamage = properties.vehicle_damage,
                        delaytoEnableForNonOwner = 0,
                    };

                    DummyBullet instantiatedDummyBullet = LocalObjectPooling.Instance.GetPooledItem(properties.dummyBullet.gameObject).GetComponent<DummyBullet>();
                    if (instantiatedDummyBullet != null) instantiatedDummyBullet.CreateBullet(data, transform.root);

                    //DummyBullet instantiatedDummyBullet = Instantiate(dummyBullet, data.position, data.rotation);
                    //instantiatedDummyBullet.CreateBullet(data);

                    CmdShootBullet(data);
                }
            }
        }
        _currentSpread += properties.spread;
        _currentSpread = Mathf.Clamp(_currentSpread, 0, properties.max_spread);
        _nextFireTime = properties.interval;

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
        _currentSpread = Spread.ResetSpread(_currentSpread);

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

    [ServerRpc]
    private void CmdShootBullet(Bullet.BulletData data)
    {
        if (properties.bulletPref == null)
            return;


        NetworkObject pooledNetworkObj = NetworkManager.GetPooledInstantiated(properties.bulletPref, IsServerInitialized);

        Bullet bullet = pooledNetworkObj.GetComponent<Bullet>();

        Spawn(pooledNetworkObj, Owner);

        bullet.CreateBullet(data, transform.root, gameObject);
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
        bool isInputPressed = InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_shoot_key);
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