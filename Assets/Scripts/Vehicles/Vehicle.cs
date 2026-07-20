using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;
using VoxelDestructionPro.Tools;

public abstract class Vehicle : NetworkBehaviour,
    //Interfaces
    ISspottable, ICurrentHpUIValues, ICountermeasuresStatusUIValues, IGunHeatLevelUIValues,
    ICurrentAmmoUIValues, IAltitudeLevelUIValues, IItemIconsUIValues, ICurrentSpeedUIValues,
    ICurrentThrottleUIValues, EntityFaction, UpgradeLevel
{
    [Header("--------------------------GENERAL VEHICLE SETTINGS--------------------------")]
    [Space(5)]

    [Header("General Settings")]
    public VehicleCategory vehicleCategory;
    public FactionManager.Faction vehicle_faction;
    public VehicleCustomizableParts[] customizableParts;
    public VehicleType vehicleType;
    public Transform spot_position;
    public int vehicle_kills;

    [Header("Seats Configuration")]
    public VehicleSeats[] vehicleSeats;
    [HideInInspector] public VehicleSeats currentSeat;
    protected int playerSeatIndex = -1;
    private readonly SyncList<string> occupantsNames = new SyncList<string>();

    [Header("References & Components")]
    public Rigidbody rb;
    public EnterVehicle enterVehicle;
    [SerializeField] protected GameObject fire_effects_parent;
    [SerializeField] protected VoxCollider voxCollider;
    [SerializeField] protected GameObject crash_explosion;
    [SerializeField] protected GameObject ground_explosion;
    public Countermeasures countermeasures;

    [Header("Crash Sound Properties")]
    [SerializeField] protected SoundManager.SoundComponents crashSound;

    [Header("Vehicle State")]
    [HideInInspector] public bool is_in_vehicle = false;
    [HideInInspector] public bool ignore_damage;
    [HideInInspector] public bool used_locking_countermeasure;
    [HideInInspector] public readonly SyncVar<float> throttle = new SyncVar<float>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    [HideInInspector] public readonly SyncVar<bool> startEngine = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    [HideInInspector] public readonly SyncVar<bool> vehicle_destroyed = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    private bool did_explode = false;
    protected float exit_cooldown;

    [Header("Health & Damage")]
    protected float original_hp;
    public readonly SyncVar<float> hp = new SyncVar<float>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    public readonly SyncVar<float> resistance = new SyncVar<float>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));

    [Header("Physics & Collision")]
    [SerializeField] protected LayerMask collisionLayers;
    [HideInInspector] public float speed;

    protected bool _isDestructionInitialized = false;
    protected float _destructionTimer = 0f;


    protected float _lastSentThrottle = -1f;
    protected float _throttleUpdateTimer = 0f;
    protected const float THROTTLE_THRESHOLD = 0.05f;
    protected const float THROTTLE_UPDATE_INTERVAL = 0.1f;

    #region Unity Lifecycle
    protected virtual void Update()
    {
        // Roda animação de destruição no servidor caso o dono tenha caído
        if (!Owner.IsValid && vehicle_destroyed.Value && IsServerInitialized)
        {
            HandleDestructionSequence();
        }

        speed = rb.linearVelocity.magnitude;

        if (is_in_vehicle)
        {

            // Validação de jogador
            if (currentSeat == null || currentSeat.playerGameObject == null || (currentSeat.playerProperties != null && currentSeat.playerProperties.is_dead.Value))
            {
                ExitVehicle();
                return;
            }

            SyncPlayerPosition();
            HandleVehicleInput();
            SwitchWeapon();
            HandleShooting();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!Owner.IsValid && IsServerInitialized)
        {
            if (vehicle_destroyed.Value) HandleDestructionSequence();
            else HandleEmptyVehicle();
            return;
        }

        if (!IsOwner) return;

        if (vehicle_destroyed.Value)
        {
            HandleDestructionSequence();
            return;
        }

        if (!is_in_vehicle)
            HandleEmptyVehicle();
        else if (!startEngine.Value)
            HandleEngineOff();
        else
            HandleEngineOn();
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        PlayerDamage(collision.gameObject);

        if (vehicle_destroyed.Value)
        {
            if (IsInLayerMask(collision.gameObject.layer, collisionLayers))
            {
                HandleCollision(collision, 50);
                Explode(collision.contacts[0].point, collision.contacts[0].normal, collision.gameObject.layer, 12);
            }
            return;
        }

        if (IsInLayerMask(collision.gameObject.layer, collisionLayers))
        {
            HandleCollision(collision, rb.linearVelocity.magnitude);
        }
    }

    protected void OnCollisionStay(Collision collision)
    {
        if (vehicle_destroyed.Value && IsInLayerMask(collision.gameObject.layer, collisionLayers))
        {
            Explode(collision.contacts[0].point, collision.contacts[0].normal, collision.gameObject.layer, 12);
        }
    }
    #endregion

    #region State Machine Methods
    protected virtual void HandleVehicleInput()
    {
        exit_cooldown += Time.deltaTime;

        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot && !vehicle_destroyed.Value)
            StartStopEngine();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_switchSeatKey))
            SwitchSeats();

        FreeLook();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey) && exit_cooldown > 0.1f)
        {
            if (currentSeat.playerController != null)
                currentSeat.playerController.playerCamera.enabled = true;
            ExitVehicle();
        }

        if (InputManager.GetKeyDown(KeyCode.P)) RequestDamage(100);
    }

    protected virtual void HandleShooting()
    {
        if (vehicle_destroyed.Value) return;

        if (currentSeat.currentArmory != null)
        {
            currentSeat.currentArmory.Shoot();
        }
    }

    protected virtual void HandleEmptyVehicle()
    {
        throttle.Value = 0;
        AddForceDown();
    }

    protected virtual void HandleEngineOff()
    {
        throttle.Value = 0;
        AddForceDown();
    }

    protected void AddForceDown(float multiplier = 1)
    {
        rb.AddForce(Vector3.down * rb.mass * multiplier, ForceMode.Force);
    }

    protected abstract void HandleEngineOn();
    protected abstract void OnDestructionPhysicsTick(float timer);
    protected abstract void StartStopEngine();
    #endregion

    #region Camera & FreeLook
    protected virtual void FreeLook()
    {
        if (currentSeat == null || currentSeat.activeCamera == null) return;

        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot)
        {
            if (InputManager.GetKey(Settings.Instance._keybinds.VEHICLE_freeLookKey))
                ApplyFreeLookRotation();
            else
                ReturnToCenter();
        }
        else
        {
            ApplyFreeLookRotation();
        }
    }

    private void ApplyFreeLookRotation()
    {
        float sensitivity = GetCameraSensitivity();
        float mouseY = InputManager.GetAxis("Mouse Y") * -sensitivity;
        float mouseX = InputManager.GetAxis("Mouse X") * sensitivity;

        Vector3 currentEuler = currentSeat.activeCamera.transform.localEulerAngles;
        float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
        float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

        currentX = Mathf.Clamp(currentX + mouseY, -80f, 40f);
        currentY = Mathf.Clamp(currentY + mouseX, -90f, 90f);

        Quaternion newRotation = Quaternion.Euler(currentX, currentY, 0f);
        currentSeat.activeCamera.transform.localRotation = newRotation;
    }

    private void ReturnToCenter()
    {
        Quaternion targetRotation = Quaternion.Lerp(
            currentSeat.activeCamera.transform.localRotation,
            Quaternion.identity,
            Time.deltaTime * 3
        );

        currentSeat.activeCamera.transform.localRotation = targetRotation;
    }

    protected virtual float GetCameraSensitivity() => Settings.Instance._controls.helicopter_sensibility;

    #endregion

    #region Destruction Sequence
    protected virtual void HandleDestructionSequence()
    {
        if (!_isDestructionInitialized)
        {
            CmdRequestEnableFireEffects();
            if (fire_effects_parent != null) fire_effects_parent.SetActive(true);
            _isDestructionInitialized = true;
        }

        _destructionTimer += Time.fixedDeltaTime;

        if (currentSeat != null && currentSeat.playerController != null)
        {
            currentSeat.playerController.RequestDamage(_destructionTimer);
        }

        OnDestructionPhysicsTick(_destructionTimer);

        if (_destructionTimer >= 5f)
        {
            Explode(transform.position, transform.up, LayerMask.NameToLayer("Voxel"), 1);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestEnableFireEffects() => RequestEnableFireEffects();

    [ObserversRpc(ExcludeOwner = true)]
    private void RequestEnableFireEffects()
    {
        if (fire_effects_parent != null) fire_effects_parent.SetActive(true);
    }
    #endregion

    #region Player Entry/Exit
    protected void SyncPlayerPosition()
    {
        if (currentSeat == null || currentSeat.playerGameObject == null) return;

        currentSeat.playerGameObject.transform.position = currentSeat.playerSeat.position;
        currentSeat.playerGameObject.transform.rotation = currentSeat.playerSeat.rotation;
    }

    public virtual void EnterVehicle(NetworkConnection conn, GameObject _player)
    {
        // Verifica se o jogador possui os componentes necessários
        if (!_player.TryGetComponent(out PlayerProperties props)) return;
        bool foundSeat = false;

        for (int i = 0; i < vehicleSeats.Length; i++)
        {
            VehicleSeats seat = vehicleSeats[i];

            // Verifica se o assento já está ocupado
            if (seat.isOccupied) continue;

            // Regra de restrição: Se não for Piloto, só pode ocupar assentos do tipo Passenger
            if (props.selectedClass.Value != ClassManager.Class.Pilot)
            {
                if (seat.seatType != VehicleSeats.SeatType.Passenger)
                    continue;
            }

            // Se passou pelas verificações, ocupa o assento
            seat.isOccupied = true;
            occupantsNames.Add(props.player_name.Value);

            if (seat.vehicleArmory?.Length > 0) seat.SetAuthority(conn);
            if (seat.seatType == VehicleSeats.SeatType.Pilot) NetworkObject.GiveOwnership(conn);

            NetworkObject playerNetObj = _player.GetComponent<NetworkObject>();
            RpcUpdateSeatStatus(i, true, playerNetObj, conn);
            TargetVehicleEntered(conn, i, _player);

            foundSeat = true;
            break;
        }

        // Se percorreu todos os assentos e não encontrou um válido
        if (!foundSeat)
        {
            TargetRpx(conn, "All seats are occupied", 2);
            return;
        }

        TargetDisableEnterVehicleUI(conn);
    }

    [TargetRpc]
    private void TargetRpx(NetworkConnection conn, string message, float duration)
    {
        GeneralHudAlertMessages.Instance.CreateMessage(message, duration);
    }

    [TargetRpc] private void TargetDisableEnterVehicleUI(NetworkConnection conn) => enterVehicle.gameObject.SetActive(false);

    [TargetRpc] private void TargetVehicleEntered(NetworkConnection conn, int seatIndex, GameObject _player) => OnVehicleEntered(seatIndex, _player);

    protected virtual void OnVehicleEntered(int seatIndex, GameObject _player)
    {
        playerSeatIndex = seatIndex;
        currentSeat = vehicleSeats[seatIndex];
        is_in_vehicle = true;
        exit_cooldown = 0f;

        currentSeat.EnterSeat(
            _player.GetComponent<PlayerProperties>(),
            _player.GetComponent<PlayerController>(),
            currentSeat.playerSeat,
            _player.GetComponent<Rigidbody>(),
            _player
        );

        if (countermeasures != null && Settings.Instance != null)
            countermeasures.SetUseCountermeasureKey(Settings.Instance._keybinds.VEHICLE_countermeasureKey);
    }

    protected virtual void ExitVehicle()
    {
        if (!is_in_vehicle) return;

        int currentIndex = playerSeatIndex;
        VehicleSeats seat = vehicleSeats[currentIndex];
        is_in_vehicle = false;

        if (seat != null)
        {
            ClearSeatArmory(seat);
            RemoveOwnershipFromPlayer();
            RepositionPlayerOnExit(seat.playerGameObject);

            if (currentIndex >= 0)
            {
                if (IsServerInitialized) RpcUpdateSeatStatus(currentIndex, false, null, null);
                else CmdUpdateSeatStatus(currentIndex, false);
            }

            playerSeatIndex = -1;
            seat.ExitSeat();
        }

        enterVehicle.gameObject.SetActive(true);
    }

    private void RepositionPlayerOnExit(GameObject player)
    {
        if (player == null) return;
        Quaternion spawnRotation = Quaternion.Euler(0, currentSeat.exitPosition.rotation.eulerAngles.y, 0);
        float yPos = currentSeat.exitPosition.position.y > 0 ? currentSeat.exitPosition.position.y : 0.1f;
        player.transform.position = new Vector3(currentSeat.exitPosition.position.x, yPos, currentSeat.exitPosition.position.z);
        player.transform.rotation = spawnRotation;
    }

    private void ClearSeatArmory(VehicleSeats seat)
    {
        if (seat.vehicleArmory == null) return;
        foreach (GameObject armoryObj in seat.vehicleArmory)
        {
            if (armoryObj == null) continue;
            armoryObj.GetComponent<IVehicleArmory>()?.DeactivateArmory();
            RemoveArmoryOwnership(armoryObj.GetComponent<NetworkObject>());
        }
    }
    #endregion

    #region Switch Seats
    protected void SwitchSeats()
    {
        if (vehicleSeats.Length <= 1) return;

        // Obtém as propriedades do jogador atual
        PlayerProperties props = currentSeat.playerProperties;
        if (props == null) return;

        int searchIndex = (playerSeatIndex == vehicleSeats.Length - 1) ? 0 : playerSeatIndex + 1;

        for (int i = 0; i < vehicleSeats.Length; i++)
        {
            // Garante que o loop verifique todos os assentos a partir do próximo
            int index = (searchIndex + i) % vehicleSeats.Length;
            VehicleSeats seat = vehicleSeats[index];

            // Pula assentos ocupados
            if (seat.isOccupied) continue;

            // Regra de restrição: Se não for Piloto, não pode ocupar Pilot ou Gunner[cite: 1, 2]
            if (props.selectedClass.Value != ClassManager.Class.Pilot)
            {
                if (seat.seatType == VehicleSeats.SeatType.Pilot || seat.seatType == VehicleSeats.SeatType.Gunner)
                    continue;
            }

            // Executa a troca se o assento for válido[cite: 1]
            int oldIndex = playerSeatIndex;
            int newIndex = index;

            PlayerController controller = currentSeat.playerController;
            Rigidbody rb = currentSeat.playerRigidbody;
            GameObject pGo = currentSeat.playerGameObject;
            NetworkConnection conn = pGo.GetComponent<NetworkObject>().Owner;

            currentSeat.ClearReferences();
            currentSeat = seat;
            playerSeatIndex = newIndex;
            currentSeat.EnterSeat(props, controller, seat.playerSeat, rb, pGo);

            UpdateServerSwitchSeatsStatus(oldIndex, newIndex, pGo, conn);
            break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateServerSwitchSeatsStatus(int oldSeatIndex, int newSeatIndex, GameObject playerGameObject, NetworkConnection conn)
    {
        if (playerGameObject == null) return;
        NetworkObject playerNetObj = playerGameObject.GetComponent<NetworkObject>();
        if (playerNetObj == null) return;

        if (vehicleSeats[oldSeatIndex].vehicleArmory != null) vehicleSeats[oldSeatIndex].SetAuthority(null);
        if (vehicleSeats[newSeatIndex].vehicleArmory != null) vehicleSeats[newSeatIndex].SetAuthority(conn);

        RpcUpdateSeatStatus(oldSeatIndex, false, null, null);
        RpcUpdateSeatStatus(newSeatIndex, true, playerNetObj, conn);

        if (vehicleSeats[newSeatIndex].seatType == VehicleSeats.SeatType.Pilot)
            this.NetworkObject.GiveOwnership(playerNetObj.Owner);
    }
    #endregion

    #region Network Status & Ownership
    [ServerRpc] private void RemoveArmoryOwnership(NetworkObject obj) => obj?.RemoveOwnership();
    [ServerRpc(RequireOwnership = true)] private void RemoveOwnershipFromPlayer() => NetworkObject.RemoveOwnership();

    [ServerRpc(RequireOwnership = false)]
    public void CmdUpdateSeatStatus(int seatIndex, bool occupiedStatus)
    {
        if (!occupiedStatus && seatIndex >= 0 && seatIndex < vehicleSeats.Length)
        {
            VehicleSeats seat = vehicleSeats[seatIndex];
            if (seat.playerGameObject != null && seat.playerGameObject.TryGetComponent(out PlayerProperties props))
                occupantsNames.Remove(props.player_name.Value);
        }
        RpcUpdateSeatStatus(seatIndex, occupiedStatus, null);
    }

    [ObserversRpc]
    public void RpcUpdateSeatStatus(int seatIndex, bool occupiedStatus, NetworkObject playerNetObj, NetworkConnection authorizedConn = null)
    {
        if (seatIndex < 0 || seatIndex >= vehicleSeats.Length) return;
        VehicleSeats seat = vehicleSeats[seatIndex];
        seat.isOccupied = occupiedStatus;
        seat.playerGameObject = occupiedStatus && playerNetObj != null ? playerNetObj.gameObject : null;
        if (authorizedConn != null) seat.authorizedConnection = authorizedConn;
    }
    #endregion

    #region Damage & Destruction 
    protected void PlayerDamage(GameObject gameObject)
    {
        if (gameObject.layer == LayerMask.NameToLayer("Player") && rb.linearVelocity.magnitude > 0)
        {
            gameObject.GetComponent<PlayerController>()?.RequestDamage(rb.linearVelocity.magnitude * 10);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDamage(float damage)
    {
        if (ignore_damage) return;
        float effectiveDamage = damage * ((100f - resistance.Value) / 100f);
        hp.Value -= effectiveDamage;
        if (hp.Value <= 0) vehicle_destroyed.Value = true;
    }

    protected void HandleCollision(Collision collision, float destruction_force)
    {
        if (destruction_force < 10) return;
        ContactPoint contact = collision.contacts[0];
        voxCollider.destructionRadius = Mathf.Clamp(destruction_force, 0, 30);
        voxCollider.SphereExplosion(contact.point, 0, 0);
        RequestDamage(voxCollider.destructionRadius / 2);
    }

    [ServerRpc(RequireOwnership = false)]
    protected void RequestToExplode(Vector3 contact_point, Vector3 contact_normal, LayerMask layer, float explosionForce)
    {
        foreach (VehicleSeats seat in vehicleSeats)
        {
            if (seat.isOccupied && seat.playerGameObject != null)
            {
                NetworkConnection conn = seat.playerGameObject.GetComponent<NetworkObject>().Owner;
                TargetForceExitAndDamage(conn);
            }
        }
        CmdExplode(contact_point, contact_normal, layer, explosionForce);
    }

    [TargetRpc]
    private void TargetForceExitAndDamage(NetworkConnection conn)
    {
        if (is_in_vehicle && currentSeat?.playerController != null)
            currentSeat.playerController.RequestDamage(100);
        ExitVehicle();
    }

    [ObserversRpc]
    private void CmdExplode(Vector3 contact_point, Vector3 contact_normal, LayerMask layer, float explosionForce)
    {
        if (did_explode) return;
        did_explode = true;
        SoundManager.Play3dSoundLocal(crashSound.clip, crashSound.properties, contact_point);
        GameObject prefabToSpawn = layer == LayerMask.NameToLayer("Ground") ? ground_explosion : crash_explosion;
        Instantiate(prefabToSpawn, contact_point, Quaternion.identity);
        RequestDespawn();
    }

    public virtual void Explode(Vector3 contact_point, Vector3 contact_normal, LayerMask layer, float explosionForce)
    {
        if (!IsOwner && !IsServerInitialized) return;
        RequestToExplode(contact_point, contact_normal, layer, explosionForce);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDespawn()
    {
        if (gameObject != null && gameObject.activeInHierarchy) Despawn(gameObject);
    }

    protected void SetHpProperties(float hp, float resistance)
    {
        original_hp = hp;
        this.hp.Value = hp;
        this.resistance.Value = resistance;
    }
    #endregion

    #region Utilities & Weapons
    protected virtual void UseCountermeasure() => countermeasures?.UseCountermeasure();
    public Vector3 GetLinearVelocity() => rb.linearVelocity;
    public string[] GetOccupantNames() => occupantsNames.ToArray();
    public void AddKill() => vehicle_kills++;
    public abstract float GetMinFov();

    protected virtual void SwitchWeapon()
    {
        if (currentSeat.vehicleArmory == null || currentSeat.vehicleArmory.Length == 0) return;

        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
        if (scrollDelta.y != 0)
        {
            int currentIndex = GetCurrentArmoryIndex();
            int direction = scrollDelta.y < 0 ? -1 : 1;
            int nextIndex = (currentIndex + direction + currentSeat.vehicleArmory.Length) % currentSeat.vehicleArmory.Length;
            ChangeArmory(nextIndex);
            return;
        }

        KeyCode[] weaponKeys = {
            Settings.Instance._keybinds.VEHICLE_weapon1, Settings.Instance._keybinds.VEHICLE_weapon2,
            Settings.Instance._keybinds.VEHICLE_weapon3, Settings.Instance._keybinds.VEHICLE_weapon4,
            Settings.Instance._keybinds.VEHICLE_weapon5, Settings.Instance._keybinds.VEHICLE_weapon6,
            Settings.Instance._keybinds.VEHICLE_weapon7, Settings.Instance._keybinds.VEHICLE_weapon8,
            Settings.Instance._keybinds.VEHICLE_weapon9
        };

        for (int i = 0; i < weaponKeys.Length; i++)
        {
            if (InputManager.GetKeyDown(weaponKeys[i]) && i < currentSeat.vehicleArmory.Length)
            {
                ChangeArmory(i);
                break;
            }
        }
    }

    private int GetCurrentArmoryIndex()
    {
        int index = Array.FindIndex(currentSeat.vehicleArmory, item => item?.GetComponent<IVehicleArmory>() == currentSeat.currentArmory);
        return index == -1 ? 0 : index;
    }

    private void ChangeArmory(int index)
    {
        if (currentSeat.vehicleArmory[index] == null) return;
        currentSeat.currentArmory?.DeactivateArmory();
        currentSeat.currentArmory = currentSeat.vehicleArmory[index].GetComponent<IVehicleArmory>();
        currentSeat.currentArmory?.ActivateArmory();
    }

    protected bool IsInLayerMask(int layer, LayerMask layerMask) => layerMask == (layerMask | (1 << layer));
    public float GetHp() => hp.Value;
    public float GetResistance() => resistance.Value;
    #endregion

    #region Interfaces Implementation
    public FactionManager.Faction GetFaction() => vehicle_faction;
    public Transform GetSpotPosition() => spot_position;
    public float GetCurrentHp() => hp.Value;
    public float GetMaxHp() => original_hp;

    public virtual CountermeasuresStatusUI.CountermeasuresStatus GetCountermeasuresStatus()
    {
        if (countermeasures == null) return CountermeasuresStatusUI.CountermeasuresStatus.Ready;
        if (countermeasures.is_active) return CountermeasuresStatusUI.CountermeasuresStatus.InUse;
        if (countermeasures.is_reloading) return CountermeasuresStatusUI.CountermeasuresStatus.Reloading;
        return CountermeasuresStatusUI.CountermeasuresStatus.Ready;
    }

    public virtual string GetCountermeasuresStatusText()
    {
        if (countermeasures == null) return "Ready";
        if (countermeasures.is_active) return "In Use";
        if (countermeasures.is_reloading) return $"Reloading... [{countermeasures.reload_countermeasures_duration:F0}]";
        return "Ready";
    }

    public virtual float GetMaxHeat() => currentSeat?.currentArmory?.GetMaxOverheat() ?? 0;
    public virtual float GetCurrentHeat() => currentSeat?.currentArmory?.GetHeatingLevel() ?? 0;
    public virtual string GetCurrentAmmo() => currentSeat?.currentArmory?.GetCurrentAmmo() ?? "";
    public virtual float GetCurrentAltitude() => transform.position.y;
    public virtual int GetCurrentActiveItem() => GetCurrentArmoryIndex();

    public virtual List<Sprite> GetItemIcon()
    {
        if (currentSeat?.vehicleArmory == null) return new List<Sprite>();
        return currentSeat.vehicleArmory
            .Where(obj => obj != null)
            .Select(obj => obj.GetComponent<IVehicleArmory>()?.GetArmoryIcon())
            .Where(icon => icon != null)
            .ToList();
    }

    public virtual float GetCurrentSpeed() => rb.linearVelocity.magnitude;
    public virtual float GetMaxSpeed() => float.MaxValue;
    public virtual float GetCurrentThrottle() => throttle.Value;
    public virtual float GetMaxThrottle() => float.MaxValue;
    public int GetUpgradeLevel() => 1;
    #endregion

    public enum VehicleCategory { MBT, IFV, ScoutHelicopter, AttackHelicopter, TransportHelicopter, AttackJet, StealthJet, Gunship }
    public enum VehicleType { Air, Land }
}