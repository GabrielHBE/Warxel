using UnityEngine;
using FishNet.Object;

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

        float applySpeed = Mathf.Max(properties.weapon_apply_recoil_speed, 0.01f);

        horizontalRecoilCurrent = Mathf.SmoothDamp(horizontalRecoilCurrent, horizontalRecoilTarget, ref horizontalRecoilVelocity, applySpeed);
        recoilVerticalCurrent = Mathf.SmoothDamp(recoilVerticalCurrent, recoilVerticalTarget, ref recoilVerticalVelocity, applySpeed);

        // Aplica o input + recuo de forma aditiva nas variáveis isoladas, idêntico ao PlayerController
        horizontalRotation += mouseX + horizontalRecoilCurrent;
        verticalRotation -= (mouseY + recoilVerticalCurrent);

        verticalRotation = Mathf.Clamp(verticalRotation, -5f, 80f);
        horizontalRotation = Mathf.Clamp(horizontalRotation, -90f, 90f);

        gunTransform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);

        // Zera os alvos logo após aplicá-los para não acumular forças irreais
        horizontalRecoilTarget = 0f;
        recoilVerticalTarget = 0f;
    }

    private void ApplyGunnerRecoil()
    {
        if (properties.recoilPattern == null || properties.recoilPattern.Length == 0) return;

        if (recoil_position_in_array >= properties.recoilPattern.Length)
        {
            recoil_position_in_array = 0;
        }

        float vr = properties.recoilPattern[recoil_position_in_array].verticalRecoil;
        float hr = properties.recoilPattern[recoil_position_in_array].horizontalRecoil;

        var recoil = Recoil.CalculateCameraRecoil(
            vr,
            hr,
            properties.first_shoot_increaser,
            is_first_shot,
            2
        );

        recoilVerticalTarget += recoil.vertical;
        horizontalRecoilTarget += recoil.horizontal;

        is_first_shot = true;
        recoil_position_in_array++;
    }

    private void Fire()
    {
        Quaternion finalRotation = Spread.CalculateSpreadRotation(shootPos, current_spread);

        current_spread = Spread.AddSpread(current_spread, properties.spread, properties.max_spread);

        ApplyGunnerRecoil();

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

        DummyBullet instantiatedDummyBullet = LocalObjectPooling.Instance.GetPooledItem(properties.dummyBullet.gameObject).GetComponent<DummyBullet>();
        if (instantiatedDummyBullet != null) instantiatedDummyBullet.CreateBullet(data, transform.root);

        CmdFireGunner(data);
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdFireGunner(Bullet.BulletData data)
    {
        Bullet bullet = NetworkManager.GetPooledInstantiated(properties.networkBullet, IsServerInitialized).GetComponent<Bullet>();
        Spawn(bullet, Owner);
        bullet.CreateBullet(data, transform.root, null);
    }

    private void StopFire()
    {
        recoil_position_in_array = 0;
        is_first_shot = false;

        current_spread = Spread.ResetSpread(current_spread);

        float coolSpeed = is_overheated ? (Time.deltaTime / 2f) : Time.deltaTime;
        current_overheat = Mathf.MoveTowards(current_overheat, 0f, coolSpeed);

        if (current_overheat <= 0) is_overheated = false;
    }

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