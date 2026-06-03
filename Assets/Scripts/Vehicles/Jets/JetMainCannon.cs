using FishNet.Object;
using UnityEngine;

public class JetMainCannon : NetworkBehaviour, IVehicleArmory
{
    [SerializeField] private DummyBullet dummyBullet;

    [Header("Bullet Config")]
    public float muzzle_velocity;
    public float bullet_drop;
    public float minimum_damage;
    public GameObject bullet_hit_effect;

    [Header("MainCannon Stats")]
    public Transform shootPosition;
    public float infantary_damage;
    public float vehicle_damage;
    public Sprite hud_icon;
    public Transform bulletPref;
    public float fire_rate;
    public float zoom;
    public float overheat_time;
    public float damage_dropoff;
    public float damage_dropoff_timer;
    public float destruction_force;

    [Header("Spread Config")]
    public float spread_increase_per_shot;
    public float max_spread;
    public float spread_recovery_speed = 5f;

    [Header("Audio")]
    public AudioSource shoot_sound;
    public AudioSource stop_shooting_sound;

    [Header("Runtime State")]
    private float _nextFireTime = 0;
    private float _currentSpread;
    private float _overheatAmount;
    private bool _isOverheated;
    private bool _hasPlayedShootSound;
    private float _shootDelayTimer;
    private float _rotationValue;
    private float _interval;

    private bool isActive = true;

    void Start()
    {
        _interval = 60f / fire_rate;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (!isActive) StopFire(Time.deltaTime);
    }


    public void Shoot()
    {
        float deltaTime = Time.deltaTime;
        // Verifica se a tecla está pressionada (usando o sistema de Settings do seu projeto)
        bool isInputPressed = InputManager.GetKey(Settings.Instance._keybinds.JET_shootVehicleKey);
        bool canShoot = !_isOverheated && isInputPressed;

        // Timer de segurança para evitar disparos acidentais (cliques ultra rápidos)
        _shootDelayTimer = canShoot ? _shootDelayTimer + deltaTime : 0;

        if (canShoot && _shootDelayTimer >= 0.05f)
        {
            ExecuteFire(deltaTime);
        }
        else
        {
            StopFire(deltaTime);
        }

        // Rotação visual do canhão (Efeito Gatling) baseado na cadência
        transform.Rotate(Vector3.left * _rotationValue * deltaTime);
    }

    private void ExecuteFire(float deltaTime)
    {
        ManageAudio(true);

        if (Time.time >= _nextFireTime)
        {

            Quaternion finalRotation = Spread.CalculateSpreadRotation(shootPosition, _currentSpread);
            if(_currentSpread < max_spread)
            {
                _currentSpread = Spread.AddSpread(_currentSpread, spread_increase_per_shot);
            }

            Bullet.BulletData data = new Bullet.BulletData
            {
                position = shootPosition.position,
                rotation = finalRotation,
                direction = finalRotation * Vector3.forward,
                speed = muzzle_velocity,
                dropMultiplier = bullet_drop,
                infantaryDamage = infantary_damage,
                damageDropoff = damage_dropoff,
                damageDropoffTimer = damage_dropoff_timer,
                destructionForce = destruction_force,
                minimumDamage = minimum_damage,
                hsMultiplier = 2,
                size = 1,
                canDamageVehicles = true,
                vehicleDamage = vehicle_damage
            };

            DummyBullet instantiatedDummyBullet = Instantiate(dummyBullet, data.position, data.rotation);
            instantiatedDummyBullet.CreateBullet(data);

            CmdFire(data);

            _nextFireTime = Time.time + _interval;
        }

        _overheatAmount += deltaTime;
        _rotationValue = fire_rate;

        if (_overheatAmount >= overheat_time) _isOverheated = true;
    }

    private void StopFire(float deltaTime)
    {
        _currentSpread = Spread.ResetSpread(_currentSpread, 0f, spread_recovery_speed);

        _rotationValue = Mathf.Lerp(_rotationValue, 0f, deltaTime * 3f);

        float coolSpeed = _isOverheated ? (deltaTime / 2f) : deltaTime;
        _overheatAmount = Mathf.MoveTowards(_overheatAmount, 0f, coolSpeed);

        if (_overheatAmount <= 0) _isOverheated = false;

        ManageAudio(false);
    }

    private void ManageAudio(bool isShooting)
    {
        if (isShooting && !_hasPlayedShootSound)
        {
            CmdPlaySound(true);
            _hasPlayedShootSound = true;
        }
        else if (!isShooting && _hasPlayedShootSound)
        {
            CmdPlaySound(false);
            _hasPlayedShootSound = false;
        }
    }

    [ServerRpc]
    private void CmdPlaySound(bool play) => RpcPlaySound(play);

    [ObserversRpc]
    private void RpcPlaySound(bool play)
    {
        if (play) shoot_sound.Play();
        else
        {
            shoot_sound.Stop();
            if (stop_shooting_sound != null) stop_shooting_sound.PlayOneShot(stop_shooting_sound.clip);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdFire(Bullet.BulletData data)
    {
        Transform bulletObj = Instantiate(bulletPref, data.position, data.rotation);
        Spawn(bulletObj.gameObject, Owner);
        bulletObj.GetComponent<Bullet>()?.CreateBullet(data, transform.root);
    }

    // Implementação IVehicleArmory
    public void ActivateArmory() => isActive = true;
    public void DeactivateArmory() => isActive = false;
    public Sprite GetArmoryIcon() => hud_icon;
    public string GetCurrentAmmo() => null;
    public float GetHeatingLevel() => _overheatAmount;

    public float GetMaxOverheat() => overheat_time;
}