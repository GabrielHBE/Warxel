using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(NetworkTransform))]
public abstract class Vehicle : NetworkBehaviour,
    //Interfaces
    ISspottable, ICurrentHpUIValues, ICountermeasuresStatusUIValues, IGunHeatLevelUIValues,
    ICurrentAmmoUIValues, IAltitudeLevelUIValues, IItemIconsUIValues, ICurrentSpeedUIValues,
    ICurrentThrottleUIValues, EntityFaction, UpgradeLevel,
    IDamageable
{
    [Header("--------------------------GENERAL VEHICLE SETTINGS--------------------------")]
    [Space(5)]

    [Header("General Settings")]
    public VehicleCategory vehicleCategory;
    public FactionManager.Faction vehicle_faction;
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
    [SerializeField] protected GameObject crashExplosion;
    public Countermeasures countermeasures;
    [SerializeField] protected Animator anim;

    [Header("Boost")]
    [SerializeField] protected bool canBoost;
    [FormerlySerializedAs("afterburnerMultiplier")]
    [SerializeField, Min(1f)] protected float boostMultiplier = 1.5f;
    [FormerlySerializedAs("afterburnerEffects")]
    [SerializeField] private ParticleSystem[] boostEffects;

    [Header("Third Person Camera")]
    [FormerlySerializedAs("cameraRotationLagStrength")]
    [SerializeField, HideInInspector] private float legacyCameraRotationLagStrength = 0.12f;
    private const float cameraRotationLagSmoothing = 0.2f;
    [FormerlySerializedAs("cameraRotationLagMaxAngle")]
    [SerializeField, HideInInspector] private float legacyCameraRotationLagMaxAngle = 15f;

    [Header("Crash Sound Properties")]
    [SerializeField] protected SoundManager.SoundComponents crashSound;

    [HideInInspector] public bool isInVehicle = false;
    [HideInInspector] public bool ignore_damage;
    [HideInInspector] public bool used_locking_countermeasure;
    private readonly SyncVar<float> throttle = new SyncVar<float>();
    [HideInInspector] public readonly SyncVar<bool> startEngine = new SyncVar<bool>();
    [HideInInspector] public readonly SyncVar<bool> vehicle_destroyed = new SyncVar<bool>();
    private bool did_explode = false;
    protected float exit_cooldown;

    [Header("Health & Damage")]
    protected float original_hp;
    public readonly SyncVar<float> hp = new SyncVar<float>();
    public readonly SyncVar<float> resistance = new SyncVar<float>();

    [Header("Physics & Collision")]
    [SerializeField] protected LayerMask collisionLayers;
    [HideInInspector] public float speed;
    protected float destructionRadius = 10;
    protected bool _isDestructionInitialized = false;
    protected float _destructionTimer = 0f;
    protected float _lastSentThrottle = -1f;
    protected float _throttleUpdateTimer = 0f;
    private float _controlledThrottle;
    private float _baseThrottle;
    private bool _isBoostToggled;
    protected float boostModifier;
    protected bool IsBoostActive { get; private set; }
    protected const float THROTTLE_THRESHOLD = 0.05f;
    protected const float THROTTLE_UPDATE_INTERVAL = 0.1f;

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        countermeasures?.SetVehicle(this);
        SetupRigidBody();
    }
    protected virtual void Update()
    {
        CountermeasuresUpdate();
        // Roda animação de destruição no servidor caso o dono tenha caído
        if (!Owner.IsValid && vehicle_destroyed.Value && IsServerInitialized) HandleDestructionSequence();

        speed = rb.linearVelocity.magnitude;
        UpdateBoostInput();

        if (isInVehicle)
        {
            currentSeat?.RemoveCameraOffset();
            // Validação de jogador
            if (currentSeat == null || currentSeat.playerGameObject == null || (currentSeat.playerProperties != null && currentSeat.playerProperties.isDead.Value))
            {
                ExitVehicle();
                return;
            }
            HandleCameraModifierState();
            SwitchCamera();
            HandleVehicleInput();
            SwitchWeapon();
            HandleShooting();
        }
    }
    protected virtual void LateUpdate()
    {
        if (isInVehicle && currentSeat != null)
            currentSeat.UpdateCameraOffset(
                transform.InverseTransformDirection(rb.angularVelocity) * Mathf.Rad2Deg,
                rb.linearVelocity, Time.deltaTime,
                legacyCameraRotationLagStrength, cameraRotationLagSmoothing, legacyCameraRotationLagMaxAngle);
    }
    protected virtual void FixedUpdate()
    {
        if (!IsController) return;

        UpdateBoost();

        if (vehicle_destroyed.Value)
        {
            HandleDestructionSequence();
        }
        else if (!Owner.IsValid)
        {
            HandleEmptyVehicle();
        }
        else if (!startEngine.Value)
        {
            HandleEngineOff();
        }
        else
        {
            HandleEngineOn();
        }

        SyncOwnerThrottle();
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

        if (InputManager.GetKeyDown(KeyCode.P)) TakeDamage(100);
    }
    protected virtual void HandleShooting()
    {
        if (vehicle_destroyed.Value) return;

        if (currentSeat.GetCurrentArmory() != null) currentSeat.GetCurrentArmory().Shoot();

    }
    protected virtual void HandleEmptyVehicle()
    {
        SetThrottle(0f);
        AddForceDown();
    }
    protected virtual void HandleEngineOff()
    {
        SetThrottle(0f);
        AddForceDown();
    }
    protected void AddForceDown(float multiplier = 1) => rb.AddForce(Vector3.down * rb.mass * multiplier, ForceMode.Force);
    protected abstract void HandleEngineOn();
    protected abstract void OnDestructionPhysicsTick(float timer);
    protected abstract void StartStopEngine();
    #endregion

    #region Boost
    private bool CanUseBoost() => canBoost && IsOwner && isInVehicle && currentSeat != null &&
        currentSeat.seatType == VehicleSeats.SeatType.Pilot && startEngine.Value && !vehicle_destroyed.Value;

    protected virtual KeyCode GetBoostKey() => Settings.Instance._keybinds.VEHICLE_boost_key;

    protected float MovementMultiplier => Mathf.Lerp(1f, Mathf.Max(1f, boostMultiplier), boostModifier / 150f);

    private void UpdateBoostInput()
    {
        if (!CanUseBoost())
        {
            ResetBoost();
            return;
        }

        if (Settings.Instance._controls.is_vehicle_boost_on_hold)
            _isBoostToggled = false;
        else if (InputManager.GetKeyDown(GetBoostKey()))
            _isBoostToggled = !_isBoostToggled;
    }

    private void UpdateBoost()
    {
        if (!CanUseBoost())
        {
            ResetBoost();
            return;
        }

        bool wantsBoost = Settings.Instance._controls.is_vehicle_boost_on_hold
            ? InputManager.GetKey(GetBoostKey())
            : _isBoostToggled;

        IsBoostActive = wantsBoost && CanApplyBoost();
        boostModifier = Mathf.MoveTowards(boostModifier, IsBoostActive ? 150f : 0f, Time.fixedDeltaTime * 80f);
        SetBoostEffects(IsBoostActive);
    }

    protected virtual bool CanApplyBoost() => Throttle > 0f;
    protected virtual ParticleSystem[] GetBoostEffects() => boostEffects;

    private void SetBoostEffects(bool enabled)
    {
        ParticleSystem[] effects = GetBoostEffects();
        if (effects == null) return;

        foreach (ParticleSystem effect in effects)
        {
            if (effect == null) continue;
            if (enabled && !effect.isPlaying) effect.Play();
            else if (!enabled && effect.isPlaying) effect.Stop();
        }
    }

    private void ResetBoost()
    {
        _isBoostToggled = false;
        boostModifier = 0f;
        IsBoostActive = false;
        SetBoostEffects(false);
    }
    #endregion

    #region Camera & FreeLook
    public bool ShouldBlockMouseRotationForFreeLook()
    {
        if (isInVehicle && currentSeat != null && currentSeat.IsArmoryCameraActive)
            return currentSeat.CanMainCameraFreeLook();

        return Settings.Instance._controls.block_vehicle_mouse_rotation_during_freelook &&
               isInVehicle && currentSeat != null &&
               currentSeat.GetCurrentCameraRotationPivot() != null &&
               currentSeat.CanMainCameraFreeLook() &&
               InputManager.GetKey(Settings.Instance._keybinds.VEHICLE_freeLookKey);
    }

    protected virtual void FreeLook()
    {
        if (currentSeat == null || currentSeat.GetCurrentCameraRotationPivot() == null) return;

        if (!currentSeat.CanMainCameraFreeLook()) return;

        if (currentSeat.IsArmoryCameraActive)
        {
            ApplyFreeLookRotation();
            return;
        }

        bool requiresFreeLookKey = currentSeat.seatType == VehicleSeats.SeatType.Pilot ||
                                   Settings.Instance._controls.block_vehicle_mouse_rotation_during_freelook;
        if (requiresFreeLookKey)
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

        Vector3 currentEuler = currentSeat.GetCurrentCameraRotationPivot().transform.localEulerAngles;
        float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
        float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

        if (currentSeat.IsCurrentCameraMain())
        {
            currentX = Mathf.Clamp(currentX + mouseY, -80f, 40f);
            currentY = Mathf.Clamp(currentY + mouseX, -89f, 89f);
        }
        else
        {
            currentX += mouseY;
            currentY += mouseX;
        }

        Quaternion newRotation = Quaternion.Euler(currentX, currentY, 0f);
        currentSeat.GetCurrentCameraRotationPivot().transform.localRotation = newRotation;
    }

    private void ReturnToCenter()
    {
        Quaternion targetRotation = Quaternion.Lerp(
            currentSeat.GetCurrentCameraRotationPivot().transform.localRotation,
            Quaternion.identity,
            Time.deltaTime * 3
        );

        currentSeat.GetCurrentCameraRotationPivot().transform.localRotation = targetRotation;
    }

    private void SwitchCamera()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_switch_camera_key)) currentSeat.SwitchCamera();
    }

    private void HandleCameraModifierState() => currentSeat.ActivateCameraEffect(InputManager.GetKey(GetCameraModifierKey()));

    protected virtual KeyCode GetCameraModifierKey() => Settings.Instance._keybinds.VEHICLE_zoom_key;


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
            currentSeat.playerController.TakeDamage(_destructionTimer);
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
    public virtual bool EnterVehicle(NetworkConnection conn, GameObject _player)
    {
        if (_player == null ||
            !_player.TryGetComponent(out PlayerProperties props) ||
            !_player.TryGetComponent(out PlayerController playerController) ||
            !_player.TryGetComponent(out NetworkObject playerNetObj))
        {
            return false;
        }

        // Activate protection before reserving the seat so collision damage sent
        // during the client/server round-trip is rejected by the server.
        playerController.SetVehicleCollisionProtectionServer(true);

        if (props.isDead.Value)
        {
            playerController.SetVehicleCollisionProtectionServer(false);
            return false;
        }

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
            occupantsNames.Add(props.playerName.Value);

            if (seat.vehicleArmory?.Length > 0) seat.SetAuthority(conn);
            if (seat.seatType == VehicleSeats.SeatType.Pilot) NetworkObject.GiveOwnership(conn);

            RpcUpdateSeatStatus(i, true, playerNetObj, conn);
            TargetVehicleEntered(conn, i, _player);

            foundSeat = true;
            break;
        }

        // Se percorreu todos os assentos e não encontrou um válido
        if (!foundSeat)
        {
            playerController.SetVehicleCollisionProtectionServer(false);
            TargetRpx(conn, "All seats are occupied", 2);
            return false;
        }

        TargetDisableEnterVehicleUI(conn);
        return true;
    }

    [TargetRpc]
    private void TargetRpx(NetworkConnection conn, string message, float duration)
    {
        AlertMessages.Instance.CreateMessage(message, duration);
    }

    [TargetRpc] private void TargetDisableEnterVehicleUI(NetworkConnection conn) => enterVehicle.SetLocalAvailability(false);

    [TargetRpc] private void TargetVehicleEntered(NetworkConnection conn, int seatIndex, GameObject _player) => OnVehicleEntered(seatIndex, _player);

    protected virtual void OnVehicleEntered(int seatIndex, GameObject _player)
    {
        playerSeatIndex = seatIndex;
        currentSeat = vehicleSeats[seatIndex];
        isInVehicle = true;
        exit_cooldown = 0f;

        currentSeat.EnterSeat(
            _player.GetComponent<PlayerProperties>(),
            _player.GetComponent<PlayerController>(),
            currentSeat.playerSeat,
            _player.GetComponent<Rigidbody>(),
            _player
        );

        VehicleStartEngineUI.ShowFor(this);
    }

    protected virtual void ExitVehicle()
    {
        if (!isInVehicle) return;

        int currentIndex = playerSeatIndex;
        VehicleSeats seat = vehicleSeats[currentIndex];
        PlayerController exitingPlayerController = seat?.playerController;
        isInVehicle = false;
        VehicleStartEngineUI.HideFor(this);

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
            exitingPlayerController?.EndVehicleCollisionProtection();
        }

        enterVehicle.SetLocalAvailability(true);
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
        foreach (VehicleArmory armoryObj in seat.vehicleArmory)
        {
            if (armoryObj == null) continue;
            armoryObj.GetComponent<VehicleArmory>()?.DeactivateArmory();
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
    protected float Throttle => IsController ? _controlledThrottle : throttle.Value;
    // Input uses the unboosted value so boost never feeds back into acceleration.
    protected float BaseThrottle => _baseThrottle;

    public override void OnOwnershipClient(NetworkConnection previousOwner)
    {
        base.OnOwnershipClient(previousOwner);

        if (IsOwner)
        {
            _controlledThrottle = throttle.Value;
            _baseThrottle = ClampThrottle(_controlledThrottle);
        }
    }

    public override void OnOwnershipServer(NetworkConnection previousOwner)
    {
        base.OnOwnershipServer(previousOwner);

        if (!Owner.IsValid)
        {
            _controlledThrottle = throttle.Value;
            _baseThrottle = ClampThrottle(_controlledThrottle);
        }
    }

    protected void SetThrottle(float value)
    {
        if (!IsController) return;

        _baseThrottle = ClampThrottle(value);
        _controlledThrottle = ClampBoostedThrottle(GetBoostedThrottle(_baseThrottle));

        // Em host ou em veiculo sem owner, o servidor publica o valor diretamente.
        if (IsServerInitialized)
            throttle.Value = _controlledThrottle;
    }

    protected virtual float ClampThrottle(float value) => value;
    protected virtual float GetBoostedThrottle(float value) => value > 0f ? value * MovementMultiplier : value;
    protected virtual float ClampBoostedThrottle(float value) => value;

    private void SyncOwnerThrottle()
    {
        // Um owner remoto controla localmente e o servidor apenas replica o valor recebido.
        if (!IsOwner || IsServerInitialized) return;

        _throttleUpdateTimer += Time.fixedDeltaTime;
        float throttleDiff = Mathf.Abs(_controlledThrottle - _lastSentThrottle);
        if (throttleDiff <= THROTTLE_THRESHOLD || _throttleUpdateTimer < THROTTLE_UPDATE_INTERVAL) return;

        CmdUpdateThrottle(_controlledThrottle);
        _lastSentThrottle = _controlledThrottle;
        _throttleUpdateTimer = 0f;
    }

    [ServerRpc]
    private void CmdUpdateThrottle(float value) => throttle.Value = ClampBoostedThrottle(value);

    [ServerRpc] private void RemoveArmoryOwnership(NetworkObject obj) => obj?.RemoveOwnership();
    [ServerRpc(RequireOwnership = true)] private void RemoveOwnershipFromPlayer() => NetworkObject.RemoveOwnership();

    [ServerRpc(RequireOwnership = false)]
    public void CmdUpdateSeatStatus(int seatIndex, bool occupiedStatus)
    {
        if (!occupiedStatus && seatIndex >= 0 && seatIndex < vehicleSeats.Length)
        {
            VehicleSeats seat = vehicleSeats[seatIndex];
            if (seat.playerGameObject != null && seat.playerGameObject.TryGetComponent(out PlayerProperties props))
                occupantsNames.Remove(props.playerName.Value);
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
            gameObject.GetComponent<PlayerController>()?.TakeVehicleCollisionDamage(rb.linearVelocity.magnitude * 10);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamage(float damage)
    {
        if (ignore_damage) return;
        hp.Value -= damage;
        if (hp.Value <= 0) vehicle_destroyed.Value = true;
    }
    protected void HandleCollision(Collision collision, float destruction_force)
    {
        if (destruction_force < 10) return;
        ContactPoint contact = collision.contacts[0];
        destructionRadius = Mathf.Clamp(destruction_force, 0, 30);
        Explosion.SphereExplosion(contact.point, 0, 0, destructionRadius, 0, null, gameObject);
        //voxCollider.SphereExplosion(contact.point, 0, 0);
        TakeDamage(destructionRadius / 2);
    }
    [ServerRpc(RequireOwnership = false)]
    protected void RequestToExplode(Vector3 contact_point)
    {
        foreach (VehicleSeats seat in vehicleSeats)
        {
            if (seat.isOccupied && seat.playerGameObject != null)
            {
                NetworkConnection conn = seat.playerGameObject.GetComponent<NetworkObject>().Owner;
                TargetForceExitAndDamage(conn);
            }
        }
        CmdExplode(contact_point);
    }
    [TargetRpc]
    private void TargetForceExitAndDamage(NetworkConnection conn)
    {
        if (isInVehicle && currentSeat?.playerController != null)
            currentSeat.playerController.TakeDamage(100);
        ExitVehicle();
    }
    [ObserversRpc]
    private void CmdExplode(Vector3 contact_point)
    {
        if (did_explode) return;
        did_explode = true;
        SoundManager.Play3dSoundLocal(crashSound.clip, crashSound.properties, contact_point);
        Instantiate(crashExplosion, contact_point, Quaternion.identity);
        RequestDespawn();
    }
    public virtual void Explode(Vector3 contact_point, Vector3 contact_normal, LayerMask layer, float explosionForce)
    {
        if (!IsOwner && !IsServerInitialized) return;
        RequestToExplode(contact_point);
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
    private void CountermeasuresUpdate()
    {
        if (countermeasures == null) return;
        countermeasures.LocalUpdate();
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_countermeasureKey) && isInVehicle && countermeasures.IsCooldownFinished() && currentSeat.seatType == VehicleSeats.SeatType.Pilot) countermeasures.UseCountermeasure();
    }
    protected void SetupRigidBody()
    {
        rb.mass = 2000;
        rb.linearDamping = 1;
        rb.angularDamping = 1;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
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
        int index = Array.FindIndex(currentSeat.vehicleArmory, item => item?.GetComponent<VehicleArmory>() == currentSeat.GetCurrentArmory());
        return index == -1 ? 0 : index;
    }
    private void ChangeArmory(int index)
    {
        if (currentSeat.vehicleArmory[index] == null) return;
        currentSeat.GetCurrentArmory()?.DeactivateArmory();
        VehicleArmory armory = currentSeat.vehicleArmory[index].GetComponent<VehicleArmory>();
        armory?.ActivateArmory();
        currentSeat.SetCurrentArmory(armory);
    }
    protected bool IsInLayerMask(int layer, LayerMask layerMask) => layerMask == (layerMask | (1 << layer));
    protected abstract void UpdateAnimator();
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
        if (countermeasures.reloading) return CountermeasuresStatusUI.CountermeasuresStatus.Reloading;
        return CountermeasuresStatusUI.CountermeasuresStatus.Ready;
    }
    public virtual string GetCountermeasuresStatusText()
    {
        if (countermeasures == null) return "Ready";
        if (countermeasures.is_active) return "In Use";
        if (countermeasures.reloading) return $"Reloading... [{countermeasures.reload_countermeasures_duration:F0}]";
        return "Ready";
    }
    public virtual float GetMaxHeat() => currentSeat?.GetCurrentArmory()?.GetMaxOverheat() ?? 0;
    public virtual float GetCurrentHeat() => currentSeat?.GetCurrentArmory()?.GetHeatingLevel() ?? 0;
    public virtual string GetCurrentAmmo() => currentSeat?.GetCurrentArmory()?.GetCurrentAmmo() ?? "";
    public virtual float GetCurrentAltitude() => transform.position.y;
    public virtual int GetCurrentActiveItem() => GetCurrentArmoryIndex();

    public virtual List<Sprite> GetItemIcon()
    {
        if (currentSeat?.vehicleArmory == null) return new List<Sprite>();
        return currentSeat.vehicleArmory
            .Where(obj => obj != null)
            .Select(obj => obj.GetComponent<VehicleArmory>()?.GetArmoryIcon())
            .Where(icon => icon != null)
            .ToList();
    }
    public virtual float GetCurrentSpeed() => rb.linearVelocity.magnitude;
    public virtual float GetMaxSpeed() => float.MaxValue;
    public virtual float GetCurrentThrottle() => Throttle;
    public virtual float GetMaxThrottle() => float.MaxValue;
    #endregion

    #region Enums
    public enum VehicleCategory { Plane, Boat, Helicopter, Tank }
    public enum VehicleType { Air, Land }
    public enum PropellerRotationAxis { X, Y, Z }
    #endregion

    #region Innter Classes
    [Serializable]
    public class PropellerData
    {
        public GameObject propeler;
        public PropellerRotationAxis rotationAxis;
    }
    #endregion
}
