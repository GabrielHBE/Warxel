using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxDataProviders;
using VoxelDestructionPro.VoxelObjects;

public abstract class Vehicle : NetworkBehaviour,
                /*Spot Interface*/ ISspottable,
                /* UI Interfaces*/ ICurrentHpUIValues, ICountermeasuresStatusUIValues, IGunHeatLevelUIValues, ICurrentAmmoUIValues, IAltitudeLevelUIValues, IItemIconsUIValues, ICurrentSpeedUIValues, ICurrentThrottleUIValues
{
    public Transform spot_position;
    public VehicleCategory vehicleCategory;
    public FactionManager.Faction vehicle_faction;
    public VehicleCustomizableParts[] customizableParts;

    [Header("Progression")]
    public int vehicle_kills;

    [Header("Seats Configuration")]
    public VehicleSeats[] vehicleSeats;
    protected int playerSeatIndex = -1;
    [HideInInspector] public VehicleSeats currentSeat;

    [Header("References & Components")]
    [SerializeField] protected Rigidbody rb;
    public Countermeasures countermeasures;
    [SerializeField] protected GameObject fire_effects_parent;
    public Transform exit_vehicle_position;
    [SerializeField] protected AudioDistortionFilter distortion;
    [SerializeField] protected AudioSource crash_sound;
    [SerializeField] protected VoxCollider voxCollider;
    [SerializeField] protected GameObject crash_explosion;
    [SerializeField] protected GameObject ground_explosion;


    [Header("Vehicle State")]
    public bool can_spawn_in_vehicle;
    public bool is_in_vehicle = false;
    public bool ignore_damage;
    public bool used_locking_countermeasure;
    [HideInInspector] public float throttle;
    [HideInInspector] public bool start_engine = false;
    public readonly SyncVar<bool> vehicle_destroyed = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));

    protected float minFov;

    [Header("Health & Damage")]
    public float original_hp;
    public readonly SyncVar<float> hp = new SyncVar<float>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    public readonly SyncVar<float> resistance = new SyncVar<float>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));

    [Header("Voxel Systems")]
    VoxDataProvider[] voxelObj;

    [Header("Physics & Collision")]
    [SerializeField] protected LayerMask collisionLayers;
    [HideInInspector] public float speed;

    #region Unity Lifecycle
    protected virtual void Update() { }
    protected virtual void FixedUpdate() { }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
            pc.RequestDamage(speed * 10);
        }

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
    #endregion

    #region Player Interaction
    public virtual void EnterVehicle(NetworkConnection conn, GameObject _player)
    {
        if (!IsServerInitialized) return;

        for (int i = 0; i < vehicleSeats.Length; i++)
        {
            VehicleSeats seat = vehicleSeats[i];
            if (!seat.isOccupied)
            {
                seat.isOccupied = true;

                // Dá autoridade das armas do assento para o jogador que entrou
                print("seat.vehicleArmory: " + seat.vehicleArmory);
                print("seat.vehicleArmory.Length : " + seat.vehicleArmory.Length);
                if (seat.vehicleArmory != null && seat.vehicleArmory.Length > 0)
                {
                    seat.SetAuthority(conn);
                }

                if (seat.seatType == VehicleSeats.SeatType.Pilot)
                {
                    GiveOwnershipToPlayer(conn);
                }

                // Avisa TODOS os clientes que a vaga "i" foi ocupada
                NetworkObject playerNetObj = _player.GetComponent<NetworkObject>();
                RpcUpdateSeatStatus(i, true, playerNetObj, conn);

                TargetVehicleEntered(conn, i, _player);

                break;
            }
        }
    }

    [ObserversRpc]
    public void RpcUpdateSeatStatus(int seatIndex, bool occupiedStatus, NetworkObject playerNetObj, NetworkConnection authorizedConn = null)
    {
        if (seatIndex >= 0 && seatIndex < vehicleSeats.Length)
        {
            VehicleSeats seat = vehicleSeats[seatIndex];
            seat.isOccupied = occupiedStatus;

            if (occupiedStatus && playerNetObj != null)
            {
                seat.playerGameObject = playerNetObj.gameObject;
                seat.authorizedConnection = authorizedConn;
            }
            else
            {
                seat.playerGameObject = null;
                seat.authorizedConnection = null;
            }
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
            countermeasures.SetUseCountermeasureKey(Settings.Instance._keybinds.VEHICLE_countermeasureKey);
    }

    [ServerRpc]
    private void RemoveArmoryOwnership(NetworkObject obj)
    {
        obj?.RemoveOwnership();
    }

    [ServerRpc(RequireOwnership = false)]
    private void GiveOwnershipToPlayer(NetworkConnection conn)
    {
        NetworkObject.GiveOwnership(conn);
    }

    [ServerRpc(RequireOwnership = true)]
    private void RemoveOwnershipFromPlayer()
    {
        NetworkObject.RemoveOwnership();
    }

    // Adicione isto no final do seu Vehicle.cs
    [ServerRpc(RequireOwnership = false)]
    public void CmdUpdateSeatStatus(int seatIndex, bool occupiedStatus)
    {
        RpcUpdateSeatStatus(seatIndex, occupiedStatus, null);
    }

    [ObserversRpc]
    public void RpcUpdateSeatStatus(int seatIndex, bool occupiedStatus, NetworkObject playerNetObj)
    {
        if (seatIndex >= 0 && seatIndex < vehicleSeats.Length)
        {
            VehicleSeats seat = vehicleSeats[seatIndex];
            seat.isOccupied = occupiedStatus;

            // Mantém a referência do player nos clientes (útil para ver quem está dentro do veículo)
            if (occupiedStatus && playerNetObj != null)
            {
                seat.playerGameObject = playerNetObj.gameObject;
            }
            else
            {
                seat.playerGameObject = null;
            }
        }
    }


    protected virtual void ExitVehicle()
    {
        if (!is_in_vehicle) return;

        int currentIndex = playerSeatIndex;
        VehicleSeats seat = vehicleSeats[currentIndex];

        is_in_vehicle = false;

        Quaternion spawnRotation = new Quaternion(0, exit_vehicle_position.transform.rotation.y, 0, exit_vehicle_position.transform.rotation.w);

        if (seat != null)
        {
            // Remove autoridade das armas ANTES de sair
            //seat.SetAuthority(null);

            if (seat.vehicleArmory != null)
            {
                foreach (GameObject armoryObj in seat.vehicleArmory)
                {
                    if (armoryObj != null)
                    {
                        IVehicleArmory armory = armoryObj.GetComponent<IVehicleArmory>();
                        armory?.DeactivateArmory();

                        NetworkObject nObj = armoryObj.GetComponent<NetworkObject>();
                        //nObj?.RemoveOwnership();
                        RemoveArmoryOwnership(nObj);
                    }
                }
            }
            RemoveOwnershipFromPlayer();

            if (exit_vehicle_position.position.y > 0)
            {
                seat.playerGameObject.transform.position = exit_vehicle_position.position;
            }
            else
            {
                seat.playerGameObject.transform.position = new Vector3(exit_vehicle_position.position.x, 0.1f, exit_vehicle_position.position.z);
            }

            seat.playerGameObject.transform.rotation = spawnRotation;

            if (currentIndex >= 0)
            {
                if (IsServerInitialized)
                {
                    RpcUpdateSeatStatus(currentIndex, false, null, null);
                }
                else
                {
                    CmdUpdateSeatStatus(currentIndex, false);
                }
            }

            playerSeatIndex = -1;
            seat.ExitSeat();
        }
    }
    #endregion

    #region Damage & Destruction 
    protected void PlayerDamage(GameObject gameObject)
    {
        if (gameObject.layer == LayerMask.NameToLayer("Player") && rb.linearVelocity.magnitude != 0)
        {
            PlayerController hit_playerController = gameObject.GetComponent<PlayerController>();
            if (hit_playerController != null) hit_playerController.RequestDamage(rb.linearVelocity.magnitude);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDamage(float damage)
    {
        if (ignore_damage) return;

        // Resistência de 0% = dano total, 100% = dano zero
        float effectiveDamage = damage * ((100f - resistance.Value) / 100f);

        hp.Value -= effectiveDamage;

        if (hp.Value <= 0)
        {
            vehicle_destroyed.Value = true;
        }

    }

    protected abstract void DestroyAnimation();

    protected void HandleCollision(Collision collision, float destruction_force)
    {
        //Debug.Log(rb.linearVelocity.magnitude);
        if (destruction_force < 10) return;

        ContactPoint contact = collision.contacts[0];
        voxCollider.destructionRadius = Math.Clamp(destruction_force, 0, 30);


        voxCollider.SphereExplosion(contact.point, 0, 0);
        ApplyFallUpperVoxels(collision, contact, voxCollider.destructionRadius);
        RequestDamage(voxCollider.destructionRadius / 2);

    }

    protected void ApplyFallUpperVoxels(Collision collision, ContactPoint contact, float explosionForce)
    {
        Mod_DestroyAfterAll mod_DestroyAfterAll = collision.gameObject.GetComponentInParent<Mod_DestroyAfterAll>();
        mod_DestroyAfterAll?.StartCoroutine(mod_DestroyAfterAll.FallUpperVoxels(explosionForce, contact.point, true));
    }

    bool did_explode = false;

    protected virtual void Explode(Vector3 contact_point, Vector3 contact_normal, LayerMask layer, float explosionForce)
    {
        // We only want the Server executing the destruction logic
        if (!IsServerInitialized) return;

        if (did_explode) return;
        did_explode = true;

        if (currentSeat.playerGameObject != null)
        {
            // Send a TargetRpc to the client who owns the player to forcefully exit them
            // BEFORE we despawn the vehicle they are sitting in.
            TargetForceExitVehicle(currentSeat.playerGameObject.GetComponent<NetworkObject>().Owner);

            if (currentSeat.playerController != null) currentSeat.playerController.RequestDamage(1000); // Server applies damage

        }

        PlayExplosionSound(); // Play locally on Server
        CmdRequestPlayExplosionSound(); // Tell clients to play it

        // Instantiate explosion effect directly on server and spawn it
        GameObject explosion_effect;
        if (layer == LayerMask.NameToLayer("Ground"))
        {
            explosion_effect = Instantiate(ground_explosion, contact_point, Quaternion.identity);
        }
        else
        {
            explosion_effect = Instantiate(crash_explosion, contact_point, Quaternion.identity);
        }
        Spawn(explosion_effect);

        RequestDespawn();
    }

    // New TargetRpc to force the client to exit gracefully
    [TargetRpc]
    private void TargetForceExitVehicle(NetworkConnection conn)
    {
        ExitVehicle();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestPlayExplosionSound()
    {
        PlayExplosionSound();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void PlayExplosionSound()
    {
        HandleSound(crash_sound);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestSpawnExplosionEffect(LayerMask layer, Vector3 pos)
    {
        GameObject explosion_effect;
        if (layer == LayerMask.NameToLayer("Ground"))
        {
            explosion_effect = Instantiate(ground_explosion, pos, Quaternion.identity);

        }
        else
        {
            explosion_effect = Instantiate(crash_explosion, pos, Quaternion.identity);
        }

        Spawn(explosion_effect);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDespawn()
    {
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            Despawn(gameObject);
        }
    }
    #endregion

    #region Countermeasure 
    protected virtual void UseCountermeasure()
    {
        if (countermeasures != null)
        {
            countermeasures.UseCountermeasure();
        }
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
        foreach (VoxDataProvider vox in voxelObj)
        {
            if (vox != null)
            {
                vox.Load(false);
                vox.GetComponent<DynamicVoxelObj>().damage_taken = 0;
            }
        }

        hp.Value = original_hp;
        //vehicleHudManager.vehicleHpHudManager.UpdateDamage();
    }
    #endregion

    #region Utility 
    public GameObject GetGameObject() => transform.gameObject;
    public Transform GetTransform() => transform; 
    public Vector3 GetLocalPosition() => transform.localPosition; 
    public Vector3 GetPosition() => transform.position; 
    public Quaternion GetLocalRotation() => transform.localRotation;
    public Quaternion GetRotation()  => transform.rotation; 
    public Vector3 GetLinearVelocity() => rb.linearVelocity;

    protected virtual void SwitchWeapon()
    {
        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();

        if (scrollDelta.y != 0)
        {
            // Localiza o índice atual comparando as referências da interface
            int currentIndex = Array.FindIndex(currentSeat.vehicleArmory,
                item => item.GetComponent<IVehicleArmory>() == currentSeat.currentArmory);

            // Se por algum motivo não encontrar, assume 0 para não quebrar a lógica
            if (currentIndex == -1) currentIndex = 0;

            int direction = scrollDelta.y < 0 ? -1 : 1;
            int nextArmoryIndex = (currentIndex + direction + currentSeat.vehicleArmory.Length) % currentSeat.vehicleArmory.Length;

            if (nextArmoryIndex != currentIndex)
            {
                currentSeat.currentArmory.DeactivateArmory();

                // Pega o componente do novo GameObject selecionado
                currentSeat.currentArmory = currentSeat.vehicleArmory[nextArmoryIndex].GetComponent<IVehicleArmory>();

                currentSeat.currentArmory.ActivateArmory();
                Debug.Log($"Selecionado: {nextArmoryIndex}");
            }
        }

        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon1))
        {
            if (currentSeat.vehicleArmory.Length > 0 && currentSeat.vehicleArmory[0] != null)
            {
                currentSeat.currentArmory.DeactivateArmory();
                currentSeat.currentArmory = currentSeat.vehicleArmory[0].GetComponent<IVehicleArmory>();
                currentSeat.currentArmory.ActivateArmory();
            }

        }
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon2))
        {
            if (currentSeat.vehicleArmory.Length > 1 && currentSeat.vehicleArmory[1] != null)
            {
                currentSeat.currentArmory.DeactivateArmory();
                currentSeat.currentArmory = currentSeat.vehicleArmory[1].GetComponent<IVehicleArmory>();
                currentSeat.currentArmory.ActivateArmory();
            }

        }
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon3))
        {
            if (currentSeat.vehicleArmory.Length > 2 && currentSeat.vehicleArmory[2] != null)
            {
                currentSeat.currentArmory.DeactivateArmory();
                currentSeat.currentArmory = currentSeat.vehicleArmory[2].GetComponent<IVehicleArmory>();
                currentSeat.currentArmory.ActivateArmory();
            }

        }
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon4))
        {
            if (currentSeat.vehicleArmory.Length > 3 && currentSeat.vehicleArmory[3] != null)
            {
                currentSeat.currentArmory.DeactivateArmory();
                currentSeat.currentArmory = currentSeat.vehicleArmory[3].GetComponent<IVehicleArmory>();
                currentSeat.currentArmory.ActivateArmory();
            }

        }
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon5))
        {
            if (currentSeat.vehicleArmory.Length > 4 && currentSeat.vehicleArmory[4] != null)
            {
                currentSeat.currentArmory.DeactivateArmory();
                currentSeat.currentArmory = currentSeat.vehicleArmory[4].GetComponent<IVehicleArmory>();
                currentSeat.currentArmory.ActivateArmory();
            }

        }
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon6))
        {
            if (currentSeat.vehicleArmory.Length > 5 && currentSeat.vehicleArmory[5] != null)
            {
                currentSeat.currentArmory.DeactivateArmory();
                currentSeat.currentArmory = currentSeat.vehicleArmory[5].GetComponent<IVehicleArmory>();
                currentSeat.currentArmory.ActivateArmory();
            }

        }
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon7))
        {
            if (currentSeat.vehicleArmory.Length > 6 && currentSeat.vehicleArmory[6] != null)
            {
                currentSeat.currentArmory.DeactivateArmory();
                currentSeat.currentArmory = currentSeat.vehicleArmory[6].GetComponent<IVehicleArmory>();
                currentSeat.currentArmory.ActivateArmory();
            }
        }
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon8))
        {
            if (currentSeat.vehicleArmory.Length > 7 && currentSeat.vehicleArmory[7] != null)
            {
                currentSeat.currentArmory.DeactivateArmory();
                currentSeat.currentArmory = currentSeat.vehicleArmory[7].GetComponent<IVehicleArmory>();
                currentSeat.currentArmory.ActivateArmory();
            }

        }
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon9))
        {
            if (currentSeat.vehicleArmory.Length > 8 && currentSeat.vehicleArmory[8] != null)
            {
                currentSeat.currentArmory.DeactivateArmory();
                currentSeat.currentArmory = currentSeat.vehicleArmory[8].GetComponent<IVehicleArmory>();
                currentSeat.currentArmory.ActivateArmory();
            }

        }
    }

    public void AddKill()
    {
        vehicle_kills += 1;
    }

    protected void HandleSound(AudioSource sound)
    {
        // Duplicar o GameObject que tem o audioDistanceController
        GameObject duplicatedObject = Instantiate(sound.gameObject, sound.transform.position, Quaternion.identity);

        // Obter o componente AudioDistanceController do objeto duplicado
        AudioDistanceController duplicatedController = duplicatedObject.GetComponent<AudioDistanceController>();

        if (duplicatedController != null)
        {
            // Chamar a função no objeto duplicado
            duplicatedController.StartGrowth();
        }
        else
        {
            // Fallback caso não encontre o componente
            sound.PlayOneShot(sound.clip);
        }

    }

    protected bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }

    public float GetHp()
    {
        return hp.Value;
    }

    public float GetResistance()
    {
        return resistance.Value;
    }
    #endregion

    #region Interfaces Implementation
    //ISspottable
    public FactionManager.Faction GetFaction()
    {
        return vehicle_faction;
    }

    public Transform GetSpotPosition()
    {
        return spot_position;
    }

    //ICurrentHpUIValues
    public float GetCurrentHp()
    {
        return hp.Value;
    }

    public float GetMaxHp()
    {
        return original_hp;
    }

    //ICountermeasuresStatusUIValues
    public virtual CountermeasuresStatusUI.CountermeasuresStatus GetCountermeasuresStatus()
    {
        if (countermeasures == null) return CountermeasuresStatusUI.CountermeasuresStatus.Ready;

        if (countermeasures.is_active)
            return CountermeasuresStatusUI.CountermeasuresStatus.InUse;
        else if (countermeasures.is_reloading)
            return CountermeasuresStatusUI.CountermeasuresStatus.Reloading;
        else
            return CountermeasuresStatusUI.CountermeasuresStatus.Ready;
    }

    public virtual string GetCountermeasuresStatusText()
    {
        string status = null;

        if (countermeasures != null)
        {
            if (countermeasures.is_active)
                status = "In Use";
            else if (countermeasures.is_reloading)
                status = $"Reloading... [{countermeasures.reload_countermeasures_duration:F0}]";
            else
                status = "Ready";
        }
        return status;
    }

    //IGunHeatLevelUIValues
    public virtual float GetMaxHeat()
    {
        if (currentSeat.currentArmory == null) return 0;

        return currentSeat.currentArmory.GetMaxOverheat();
    }

    public virtual float GetCurrentHeat()
    {
        if (currentSeat.currentArmory == null) return 0;

        return currentSeat.currentArmory.GetHeatingLevel();
    }

    //ICurrentAmmoUIValues
    public virtual string GetCurrentAmmo()
    {
        if (currentSeat.currentArmory == null) return "";

        return currentSeat.currentArmory.GetCurrentAmmo();
    }

    //IAltitudeLevelUIValues
    public virtual float GetCurrentAltitude()
    {
        return transform.position.y;
    }

    //IItemIconsUIValues
    public virtual int GetCurrentActiveItem()
    {
        if (currentSeat == null || currentSeat.vehicleArmory == null || currentSeat.currentArmory == null)
            return 0;

        for (int i = 0; i < currentSeat.vehicleArmory.Length; i++)
        {
            // Compara se o componente IVehicleArmory do GameObject no array 
            // é a mesma instância que está selecionada atualmente
            if (currentSeat.vehicleArmory[i] != null &&
                currentSeat.vehicleArmory[i].GetComponent<IVehicleArmory>() == currentSeat.currentArmory)
            {
                return i;
            }
        }

        return 0;
    }

    public virtual List<Sprite> GetItemIcon()
    {
        List<Sprite> sprites = new List<Sprite>();

        if (currentSeat == null)
        {
            return sprites;
        }

        if (currentSeat.vehicleArmory == null || currentSeat.vehicleArmory.Length == 0)
        {
            return sprites;
        }

        for (int i = 0; i < currentSeat.vehicleArmory.Length; i++)
        {
            GameObject obj = currentSeat.vehicleArmory[i];
            if (obj == null)
            {
                continue;
            }

            IVehicleArmory armory = obj.GetComponent<IVehicleArmory>();
            if (armory != null)
            {
                Sprite icon = armory.GetArmoryIcon();
                if (icon != null)
                {
                    sprites.Add(icon);
                }

            }

        }
        return sprites;
    }

    //ICurrentSpeedUIValues
    public virtual float GetCurrentSpeed() => rb.linearVelocity.magnitude;
    public virtual float GetMaxSpeed() => float.MaxValue;

    //ICurrentThrottleUIValues
    public virtual float GetCurrentThrottle() => throttle;
    public virtual float GetMaxThrottle() => float.MaxValue;
    #endregion

    #region Enums
    public enum VehicleCategory
    {
        MBT,
        IFV,
        ScoutHelicopter,
        AttackHelicopter,
        TransportHelicopter,
        AttackJet,
        StealthJet,
        Gunship
    }
    #endregion
}