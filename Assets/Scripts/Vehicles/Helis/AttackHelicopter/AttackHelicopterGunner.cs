using UnityEngine;
using FishNet.Object;
using System.Collections;

public class AttackHelicopterGunner : NetworkBehaviour, IVehicleArmory
{
    [SerializeField] private AttackHelicopter helicopter;
    [SerializeField] private Camera gunnerGunCamera;
    public AttackHelicopterGunnerProperties properties;
    public Transform shootPos;

    // Estado Interno
    private float next_time_to_fire = 0;
    private float current_overheat;
    private bool is_overheated;
    private float current_spread;
    private Vector3 shakeOffset;
    private float current_camera;
    private bool isActive = true;

    void Update()
    {
        if (!IsOwner) return;

        if (!isActive)
        {
            StopFire();
            return;
        }

        HandleSwitchCamera();

        if (gunnerGunCamera.enabled)
        {
            RotateGun(transform);
            Vector3 finalRotation = transform.localEulerAngles + shakeOffset;
            transform.localEulerAngles = finalRotation;
        }
        else
        {
            ApplyFreeLookRotation();
        }

    }

    public void UpdateCooldown(float dt)
    {
        float coolSpeed = is_overheated ? (dt / 2f) : dt;
        current_overheat = Mathf.MoveTowards(current_overheat, 0f, coolSpeed);
        if (current_overheat <= 0) is_overheated = false;
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
        float mouseX = InputManager.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;
        float mouseY = InputManager.GetAxis("Mouse Y") * Settings.Instance._controls.helicopter_sensibility;

        Vector3 currentEuler = gunTransform.localEulerAngles;
        float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
        float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

        currentY += mouseX;
        currentX -= mouseY;

        currentX = Mathf.Clamp(currentX, -5f, 80f);
        currentY = Mathf.Clamp(currentY, -90f, 90f);

        gunTransform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    private void Fire()
    {
        Quaternion finalRotation = Spread.CalculateSpreadRotation(shootPos, current_spread);

        current_spread = Spread.AddSpread(current_spread, properties.spread, properties.max_spread);

        Bullet.BulletData data = new Bullet.BulletData
        {
            position = shootPos.position,
            rotation = shootPos.rotation,
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

        DummyBullet instantiatedDummyBullet = ObjectPooling.Instance.GetLocalPooledItem(properties.dummyBullet.gameObject).GetComponent<DummyBullet>();
        if (instantiatedDummyBullet != null) instantiatedDummyBullet.CreateBullet(data, transform.root);

        // Chama a lógica de rede
        CmdFireGunner(data);
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdFireGunner(Bullet.BulletData data)
    {
        Bullet bullet = NetworkManager.GetPooledInstantiated(properties.networkBullet, IsServerInitialized).GetComponent<Bullet>();
        Spawn(bullet, Owner);
        bullet.CreateBullet(data, transform.root, null);
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        float intensity = 0.1f;
        float duration = 0.2f;

        shakeOffset = Vector3.zero;
        while (elapsed < duration)
        {
            float time = Time.time * 20f;
            shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(time * 1.2f, 0) * 2f - 1f) * intensity * 2f,
                    ((Mathf.PerlinNoise(0, time * 1.5f) * 2f - 1f) * 0.4f +
                     (Mathf.Sin(time * 3f) * 0.6f)) * intensity * 0.8f,
                    (Mathf.PerlinNoise(time * 0.8f, time * 0.8f) * 2f - 1f) * intensity * 0.7f
                );

            elapsed += Time.deltaTime;
            yield return null;
        }

        float returnTime = Mathf.Min(0.1f, duration * 0.5f);
        elapsed = 0f;
        Vector3 startingShake = shakeOffset;

        while (elapsed < returnTime)
        {
            float t = elapsed / returnTime;
            shakeOffset = Vector3.Lerp(startingShake, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }

    private void StopFire()
    {
        current_spread = Spread.ResetSpread(current_spread);

        float coolSpeed = is_overheated ? (Time.deltaTime / 2f) : Time.deltaTime;
        current_overheat = Mathf.MoveTowards(current_overheat, 0f, coolSpeed);

        if (current_overheat <= 0) is_overheated = false;
    }

    // Implementações da Interface
    public void Shoot()
    {
        float dt = Time.deltaTime;
        next_time_to_fire -= dt;

        if (InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_shoot_key) && !is_overheated)
        {

            if (next_time_to_fire <= 0f)
            {
                SoundManager.Instance.RequestPlay3dSound(properties.shoot_sound.name, properties.shootSoundProperties, transform.position, false);
                SoundManager.Play2dSoundLocal(properties.shoot_sound, properties.shootSoundProperties);
                Fire();
                next_time_to_fire = properties.interval;
                current_overheat += dt;
            }

            if (current_overheat >= properties.overheat_time) is_overheated = true;

        }
        else
        {
            StopFire();
        }
    }
    public float GetHeatingLevel() => current_overheat;
    public float GetMaxOverheat() => properties.overheat_time;
    public Sprite GetArmoryIcon() => properties.image_hud;
    public void ActivateArmory() => isActive = true;
    public void DeactivateArmory()
    {
        current_camera = 2;
        gunnerGunCamera.enabled = false;
        isActive = false;
        helicopter.currentSeat.playerController.playerCamera.enabled = true;
    }
    public string GetCurrentAmmo() => "";
}