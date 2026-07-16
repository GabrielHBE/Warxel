using FishNet.Object;
using UnityEngine;

public class JetMainCannon : NetworkBehaviour, IVehicleArmory
{
    public Transform shootPosition;
    public JetMainCannonProperties properties;

    // REMOVIDO: private int firingStateId;
    private float rotationValue;
    private bool isActive = true;
    private bool wasOverheatedLastFrame = false;

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

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_switchFireModeKey))
            SwitchFireMode();

        // Se estiver superaquecido, força o resfriamento no Update também
        if (Heating.isOverheated(properties.heatValues))
        {
            // Se acabou de superaquecer, para o som
            if (!wasOverheatedLastFrame)
            {
                wasOverheatedLastFrame = true;
            }
            StopFire(Time.deltaTime);
        }
        else
        {
            wasOverheatedLastFrame = false;
        }
    }

    public void Shoot()
    {
        float deltaTime = Time.deltaTime;

        // Obtém inputs
        bool isInputHeld = InputManager.GetKey(Settings.Instance._keybinds.JET_shootVehicleKey);
        bool isInputPressed = InputManager.GetKeyDown(Settings.Instance._keybinds.JET_shootVehicleKey);

        // Verifica se está superaquecido (usando o estado persistente)
        if (Heating.isOverheated(properties.heatValues))
        {
            // Força o resfriamento
            StopFire(deltaTime);

            // Rotação visual do canhão (para quando superaquecido)
            rotationValue = Mathf.Lerp(rotationValue, 0f, deltaTime * 3f);
            transform.Rotate(Vector3.left * rotationValue * deltaTime);
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
        bool isFiring = Firing.IsFiring();
        Firing.FireMode currentMode = Firing.GetCurrentFireMode();

        if (shootResult.shouldShoot)
        {
            SoundManager.Instance.RequestPlay3dSound(properties.shootSound.clip.name, properties.shootSound.properties, transform.position, false);
            SoundManager.Play2dSoundLocal(properties.shootSound.clip, properties.shootSound.properties);
            ExecuteFire();
        }

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
            StopFire(deltaTime);
        }

        if (isFiring)
        {
            rotationValue = properties.firing.rateOfFire;
        }

        transform.Rotate(Vector3.left * rotationValue * deltaTime);
    }

    private void ExecuteFire()
    {
        Quaternion finalRotation = Spread.CalculateSpreadRotation(shootPosition, properties.spreadValues.spreadState.currentSpread);

        properties.spreadValues.spreadState.currentSpread = Spread.AddSpread(
            properties.spreadValues.spreadState.currentSpread,
            properties.spreadValues.spreadIncreaser,
            properties.spreadValues.maxSpread
        );

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = shootPosition.position,
            rotation = finalRotation,
            ignoredObject = transform.root,
            root = gameObject
        };

        if (ProjectileSpawner.Instance != null) ProjectileSpawner.Instance.CreateProjectile(properties.bulletPref, properties.dummyBullet.gameObject, prop, properties.projectileValues);
    }

    private void StopFire(float deltaTime)
    {
        properties.spreadValues.spreadState.currentSpread = Spread.ResetSpread(
            properties.spreadValues.spreadState.currentSpread,
            properties.spreadValues.baseSpread,
            properties.spreadValues.spreadRecovery
        );

        rotationValue = Mathf.Lerp(rotationValue, 0f, deltaTime * 3f);

        // APENAS RESFRIAMENTO
        properties.heatValues.heatState.currentHeat = Heating.HandleCooling(properties.heatValues, deltaTime);
    }

    // Implementação IVehicleArmory
    public void ActivateArmory()
    {
        isActive = true;
        SetupFiringSystem();
    }
    public void DeactivateArmory() => isActive = false;
    public Sprite GetArmoryIcon() => properties.hudIcon;
    public string GetCurrentAmmo() => null;
    public float GetHeatingLevel() => properties.heatValues.heatState.currentHeat;
    public float GetMaxOverheat() => properties.heatValues.maxHeat;

    // Método para trocar modo de tiro (opcional)
    public void SwitchFireMode()
    {
        if (!IsOwner) return;

        if (!Firing.CanSwitchFireMode(properties.firing.fireModes)) return;

        // ATUALIZADO: sem stateId
        Firing.SwitchFireMode(properties.firing.fireModes);
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
}