using System.Collections;
using FishNet.Object;
using UnityEngine;

public class TankPilotGun : NetworkBehaviour, IVehicleArmory
{
    [Header("Properties")]
    [SerializeField] private TankPilotGunProperties tankPilotGunProperties;

    [Header("Instances")]
    [SerializeField] private DummyBullet dummyBullet;
    [SerializeField] private Transform pilotGunShootPos;
    [SerializeField] private Transform tankGunnerGun;

    [SerializeField] private Tank tankContext;
    private float pilot_gun_overheat_amount;
    private bool is_pilot_gun_overheated = false;
    private float pilot_gun_next_time_to_fire;

    private Vector3 gunnerGunOriginalLocalPosition;
    private Quaternion gunnerGunOriginalLocalRotation;
    private bool isGunnerGunRecoiling = false;
    private Coroutine gunnerGunRecoilCoroutine;
    private bool wasFiringThisFrame = false;

    void Start()
    {
        if (tankGunnerGun != null)
        {
            gunnerGunOriginalLocalPosition = tankGunnerGun.localPosition;
            gunnerGunOriginalLocalRotation = tankGunnerGun.localRotation;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Gerencia o resfriamento da arma de forma independente sempre que não estiver atirando
        if (!wasFiringThisFrame)
        {
            CoolDownGun();
        }

        if (pilot_gun_next_time_to_fire > 0f)
        {
            pilot_gun_next_time_to_fire -= Time.deltaTime;
        }

        wasFiringThisFrame = false; // Reseta o estado para o próximo frame
    }

    public void Shoot()
    {
        if (!IsOwner) return;

        bool isShooting = InputManager.GetKey(Settings.Instance._keybinds.TANK_shoot_key);

        if (isShooting && !is_pilot_gun_overheated)
        {
            wasFiringThisFrame = true;

            if (pilot_gun_next_time_to_fire <= 0f)
            {
                if (tankContext != null) tankContext.PlayWeaponSound(tankPilotGunProperties.shoot_shound);
                ApplyMachineGunRecoil();

                Bullet.BulletData data = new Bullet.BulletData
                {
                    position = pilotGunShootPos.position,
                    rotation = pilotGunShootPos.rotation,
                    direction = pilotGunShootPos.forward,
                    speed = tankPilotGunProperties.muzzle_velocity,
                    dropMultiplier = tankPilotGunProperties.bullet_drop,
                    infantaryDamage = tankPilotGunProperties.infantary_damage,
                    damageDropoff = tankPilotGunProperties.damage_dropoff,
                    damageDropoffTimer = tankPilotGunProperties.damage_dropoff_timer,
                    destructionForce = tankPilotGunProperties.destruction_force,
                    minimumDamage = tankPilotGunProperties.minimum_damage,
                    hsMultiplier = 2,
                    size = 1,
                    canDamageVehicles = false,
                    vehicleDamage = tankPilotGunProperties.vehicle_damage,
                    delaytoEnableForNonOwner = 0,
                };

                DummyBullet instantiatedDummyBullet = Instantiate(dummyBullet, data.position, data.rotation);
                instantiatedDummyBullet.CreateBullet(data, transform);

                CmdShootMachineGun(data);

                pilot_gun_next_time_to_fire = tankPilotGunProperties.interval;
            }

            pilot_gun_overheat_amount += Time.deltaTime;

            if (pilot_gun_overheat_amount >= tankPilotGunProperties.overheat_time)
                is_pilot_gun_overheated = true;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdShootMachineGun(Bullet.BulletData data)
    {
        Transform bulletObj = Instantiate(tankPilotGunProperties.bullefPref, data.position, data.rotation);
        Spawn(bulletObj.gameObject, Owner);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.CreateBullet(data);
        }

        RpcShootMachineGunEffects();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void RpcShootMachineGunEffects()
    {
        if (tankContext != null) tankContext.PlayWeaponSound(tankPilotGunProperties.shoot_shound);
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
        float recoilDuration = tankPilotGunProperties.interval / 2;
        float returnDuration = tankPilotGunProperties.interval / 2;

        Vector3 localRecoilDirection = -Vector3.forward;
        Vector3 recoilPosition = gunnerGunOriginalLocalPosition + (localRecoilDirection * recoilDistance);

        float recoilTimer = 0f;
        while (recoilTimer < recoilDuration)
        {
            recoilTimer += Time.deltaTime;
            float t = recoilTimer / recoilDuration;
            tankGunnerGun.localPosition = Vector3.Lerp(
                gunnerGunOriginalLocalPosition,
                recoilPosition,
                t
            );
            yield return null;
        }

        float returnTimer = 0f;
        while (returnTimer < returnDuration)
        {
            returnTimer += Time.deltaTime;
            float t = returnTimer / returnDuration;
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

    private void CoolDownGun()
    {
        float deltaTime = Time.deltaTime;
        float coolSpeed = is_pilot_gun_overheated ? (deltaTime / 3f) : deltaTime / 2;
        pilot_gun_overheat_amount = Mathf.MoveTowards(pilot_gun_overheat_amount, 0f, coolSpeed);

        if (pilot_gun_overheat_amount <= 0)
        {
            is_pilot_gun_overheated = false;
        }
    }

    #region IVehicleArmory Implementation
    public void ActivateArmory() { }

    public void DeactivateArmory() { }

    public Sprite GetArmoryIcon() => tankPilotGunProperties.hud_image;

    public string GetCurrentAmmo() => "";

    public float GetHeatingLevel() => pilot_gun_overheat_amount;

    public float GetMaxOverheat() => tankPilotGunProperties.overheat_time;
    #endregion
}