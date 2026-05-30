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
    ISspottable, ICurrentHpUIValues, ICountermeasuresStatusUIValues, IGunHeatLevelUIValues,
    ICurrentAmmoUIValues, IAltitudeLevelUIValues, IItemIconsUIValues, ICurrentSpeedUIValues,
    ICurrentThrottleUIValues, EntityFaction, UpgradeLevel
{
    [Header("General Settings")]
    public VehicleCategory vehicleCategory;
    public FactionManager.Faction vehicle_faction;
    public VehicleCustomizableParts[] customizableParts;
    public Transform spot_position;
    public int vehicle_kills;

    [Header("Seats Configuration")]
    public VehicleSeats[] vehicleSeats;
    [HideInInspector] public VehicleSeats currentSeat;
    protected int playerSeatIndex = -1;
    private readonly SyncList<string> occupantsNames = new SyncList<string>();

    [Header("References & Components")]
    public Rigidbody rb;
    [SerializeField] protected GameObject fire_effects_parent;
    [SerializeField] protected AudioDistortionFilter distortion;
    [SerializeField] protected AudioSource crash_sound;
    [SerializeField] protected VoxCollider voxCollider;
    [SerializeField] protected GameObject crash_explosion;
    [SerializeField] protected GameObject ground_explosion;
    public Countermeasures countermeasures;

    [Header("Vehicle State")]
    public bool can_spawn_in_vehicle;
    public bool is_in_vehicle = false;
    public bool ignore_damage;
    public bool used_locking_countermeasure;
    [HideInInspector] public float throttle;
    [HideInInspector] public bool start_engine = false;
    public readonly SyncVar<bool> vehicle_destroyed = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    protected float minFov;
    private bool did_explode = false;

    [Header("Health & Damage")]
    public float original_hp;
    public readonly SyncVar<float> hp = new SyncVar<float>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    public readonly SyncVar<float> resistance = new SyncVar<float>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));

    [Header("Physics & Collision")]
    [SerializeField] protected LayerMask collisionLayers;
    [HideInInspector] public float speed;
    #region Unity Lifecycle
    protected virtual void Update() { }
    protected virtual void FixedUpdate() { }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        PlayerDamage(collision.gameObject);

        if (IsInLayerMask(collision.gameObject.layer, collisionLayers))
        {
            HandleCollision(collision, rb.linearVelocity.magnitude);
        }
    }
    #endregion

    #region Abstract Methods
    protected abstract void StartStopEngine();
    protected abstract void Move();
    protected abstract void CameraController();
    protected abstract void DestroyAnimation();
    #endregion

    #region Player Interaction
    protected void SyncPlayerPosition()
    {
        if (currentSeat == null) return;

        currentSeat.playerGameObject.transform.position = currentSeat.playerSeat.position;
        currentSeat.playerGameObject.transform.rotation = currentSeat.playerSeat.rotation;
    }

    public virtual void EnterVehicle(NetworkConnection conn, GameObject _player)
    {
        if (!IsServerInitialized) return;

        for (int i = 0; i < vehicleSeats.Length; i++)
        {
            VehicleSeats seat = vehicleSeats[i];
            if (seat.isOccupied) continue;

            seat.isOccupied = true;

            if (_player.TryGetComponent(out PlayerProperties props))
            {
                occupantsNames.Add(props.player_name.Value);
            }

            if (seat.vehicleArmory?.Length > 0)
            {
                seat.SetAuthority(conn);
            }

            if (seat.seatType == VehicleSeats.SeatType.Pilot)
            {
                GiveOwnershipToPlayer(conn);
            }

            NetworkObject playerNetObj = _player.GetComponent<NetworkObject>();
            RpcUpdateSeatStatus(i, true, playerNetObj, conn);
            TargetVehicleEntered(conn, i, _player);

            break;
        }
    }

    [TargetRpc]
    private void TargetVehicleEntered(NetworkConnection conn, int seatIndex, GameObject _player)
    {
        OnVehicleEntered(seatIndex, _player);
    }

    protected virtual void OnVehicleEntered(int seatIndex, GameObject _player)
    {
        playerSeatIndex = seatIndex;
        currentSeat = vehicleSeats[seatIndex];
        is_in_vehicle = true;

        currentSeat.EnterSeat(
            _player.GetComponent<PlayerProperties>(),
            _player.GetComponent<PlayerController>(),
            currentSeat.playerSeat,
            _player.GetComponent<Rigidbody>(),
            _player
        );

        if (countermeasures != null && Settings.Instance != null)
        {
            countermeasures.SetUseCountermeasureKey(Settings.Instance._keybinds.VEHICLE_countermeasureKey);
        }
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

        int searchIndex;

        if (playerSeatIndex == vehicleSeats.Length - 1)
            searchIndex = 0;
        else
            searchIndex = playerSeatIndex + 1;

        for (int i = searchIndex; i < vehicleSeats.Length; i++)
        {
            VehicleSeats seat = vehicleSeats[i];
            if (seat.isOccupied) continue;

            int oldIndex = playerSeatIndex;
            int newIndex = i;

            // Cache das referências locais
            PlayerProperties props = currentSeat.playerProperties;
            PlayerController controller = currentSeat.playerController;
            Rigidbody rb = currentSeat.playerRigidbody;
            GameObject pGo = currentSeat.playerGameObject;
            NetworkConnection conn = pGo.GetComponent<NetworkObject>().Owner;

            // Limpeza local
            currentSeat.ClearReferences();

            // Setup local do novo assento
            currentSeat = seat;
            playerSeatIndex = newIndex;
            currentSeat.EnterSeat(props, controller, seat.playerSeat, rb, pGo);

            // SOLICITA AO SERVIDOR A TROCA
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

        // Remove autoridade das armas do assento antigo
        if (vehicleSeats[oldSeatIndex].vehicleArmory != null)
        {
            vehicleSeats[oldSeatIndex].SetAuthority(null);
        }

        // Dá autoridade das armas do novo assento
        if (vehicleSeats[newSeatIndex].vehicleArmory != null)
        {
            vehicleSeats[newSeatIndex].SetAuthority(conn);
        }

        // Esvazia a cadeira antiga e preenche a nova na rede
        RpcUpdateSeatStatus(oldSeatIndex, false, null, null);
        RpcUpdateSeatStatus(newSeatIndex, true, playerNetObj, conn);

        // Se mudou para piloto, dá o Ownership do helicóptero
        if (vehicleSeats[newSeatIndex].seatType == VehicleSeats.SeatType.Pilot)
        {
            this.NetworkObject.GiveOwnership(playerNetObj.Owner);
        }
    }

    #endregion

    #region Network Status & Ownership
    [ServerRpc]
    private void RemoveArmoryOwnership(NetworkObject obj) => obj?.RemoveOwnership();

    [ServerRpc(RequireOwnership = false)]
    private void GiveOwnershipToPlayer(NetworkConnection conn) => NetworkObject.GiveOwnership(conn);

    [ServerRpc(RequireOwnership = true)]
    private void RemoveOwnershipFromPlayer() => NetworkObject.RemoveOwnership();

    [ServerRpc(RequireOwnership = false)]
    public void CmdUpdateSeatStatus(int seatIndex, bool occupiedStatus)
    {
        if (!occupiedStatus && seatIndex >= 0 && seatIndex < vehicleSeats.Length)
        {
            VehicleSeats seat = vehicleSeats[seatIndex];
            if (seat.playerGameObject != null && seat.playerGameObject.TryGetComponent(out PlayerProperties props))
            {
                occupantsNames.Remove(props.player_name.Value);
            }
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
        ApplyFallUpperVoxels(collision, contact, voxCollider.destructionRadius);
        RequestDamage(voxCollider.destructionRadius / 2);
    }

    protected void ApplyFallUpperVoxels(Collision collision, ContactPoint contact, float explosionForce)
    {
        Mod_DestroyAfterAll mod = collision.gameObject.GetComponentInParent<Mod_DestroyAfterAll>();
        if (mod != null) mod.StartCoroutine(mod.FallUpperVoxels(explosionForce, contact.point, true));
    }

    protected virtual void Explode(Vector3 contact_point, Vector3 contact_normal, LayerMask layer, float explosionForce)
    {
        if (!IsServerInitialized || did_explode) return;
        did_explode = true;

        if (currentSeat?.playerGameObject != null)
        {
            TargetForceExitVehicle(currentSeat.playerGameObject.GetComponent<NetworkObject>().Owner);
            currentSeat.playerController?.RequestDamage(1000);
        }

        PlayExplosionSound();
        CmdRequestPlayExplosionSound();

        GameObject prefabToSpawn = layer == LayerMask.NameToLayer("Ground") ? ground_explosion : crash_explosion;
        GameObject explosion_effect = Instantiate(prefabToSpawn, contact_point, Quaternion.identity);
        Spawn(explosion_effect);

        RequestDespawn();
    }

    [TargetRpc]
    private void TargetForceExitVehicle(NetworkConnection conn) => ExitVehicle();

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestPlayExplosionSound() => PlayExplosionSound();

    [ObserversRpc(ExcludeOwner = true)]
    private void PlayExplosionSound() => HandleSound(crash_sound);

    [ServerRpc(RequireOwnership = false)]
    private void RequestDespawn()
    {
        if (gameObject != null && gameObject.activeInHierarchy) Despawn(gameObject);
    }
    #endregion

    #region Health & Repair 
    protected void SetHpProperties(float hp, float resistance)
    {
        original_hp = hp;
        this.hp.Value = hp;
        this.resistance.Value = resistance;
    }

    protected virtual void RestoreHp()
    {
        hp.Value = original_hp;
    }
    #endregion

    #region Utilities & Weapons
    protected virtual void UseCountermeasure() => countermeasures?.UseCountermeasure();
    public GameObject GetGameObject() => gameObject;
    public Transform GetTransform() => transform;
    public Vector3 GetLocalPosition() => transform.localPosition;
    public Vector3 GetPosition() => transform.position;
    public Quaternion GetLocalRotation() => transform.localRotation;
    public Quaternion GetRotation() => transform.rotation;
    public Vector3 GetLinearVelocity() => rb.linearVelocity;
    public string[] GetOccupantNames() => occupantsNames.ToArray();
    public void AddKill() => vehicle_kills++;

    protected virtual void SwitchWeapon()
    {
        if (currentSeat.vehicleArmory == null || currentSeat.vehicleArmory.Length == 0) return;

        // Mouse Scroll Logic
        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
        if (scrollDelta.y != 0)
        {
            int currentIndex = GetCurrentArmoryIndex();
            int direction = scrollDelta.y < 0 ? -1 : 1;
            int nextIndex = (currentIndex + direction + currentSeat.vehicleArmory.Length) % currentSeat.vehicleArmory.Length;

            ChangeArmory(nextIndex);
            return;
        }

        // Number Keys Logic (Refactored to avoid repetition)
        KeyCode[] weaponKeys = {
            Settings.Instance._keybinds.VEHICLE_weapon1, Settings.Instance._keybinds.VEHICLE_weapon2,
            Settings.Instance._keybinds.VEHICLE_weapon3, Settings.Instance._keybinds.VEHICLE_weapon4,
            Settings.Instance._keybinds.VEHICLE_weapon5, Settings.Instance._keybinds.VEHICLE_weapon6,
            Settings.Instance._keybinds.VEHICLE_weapon7, Settings.Instance._keybinds.VEHICLE_weapon8,
            Settings.Instance._keybinds.VEHICLE_weapon9
        };

        for (int i = 0; i < weaponKeys.Length; i++)
        {
            if (Input.GetKeyDown(weaponKeys[i]) && i < currentSeat.vehicleArmory.Length)
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

    protected void HandleSound(AudioSource sound)
    {
        GameObject duplicatedObject = Instantiate(sound.gameObject, sound.transform.position, Quaternion.identity);
        if (duplicatedObject.TryGetComponent(out AudioDistanceController controller))
        {
            controller.StartGrowth();
        }
        else
        {
            sound.PlayOneShot(sound.clip);
        }
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

    public virtual float GetMaxHeat() => currentSeat.currentArmory?.GetMaxOverheat() ?? 0;
    public virtual float GetCurrentHeat() => currentSeat.currentArmory?.GetHeatingLevel() ?? 0;
    public virtual string GetCurrentAmmo() => currentSeat.currentArmory?.GetCurrentAmmo() ?? "";
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
    public virtual float GetCurrentThrottle() => throttle;
    public virtual float GetMaxThrottle() => float.MaxValue;
    #endregion

    public enum VehicleCategory
    {
        MBT, IFV, ScoutHelicopter, AttackHelicopter, TransportHelicopter, AttackJet, StealthJet, Gunship
    }
}