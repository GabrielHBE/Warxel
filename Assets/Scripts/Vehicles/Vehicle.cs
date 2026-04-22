using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxDataProviders;
using VoxelDestructionPro.VoxelObjects;

public abstract class Vehicle : NetworkBehaviour, ISspottable
{
    public Transform spot_position;
    
    public string vehicle_name;
    public FactionManager.Faction vehicle_faction;

    [Header("Progression")]
    public int vehicle_kills;

    [Header("References & Components")]
    [SerializeField] protected VehicleHudManager vehicleHudManager;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected Countermeasures countermeasures;
    [SerializeField] protected GameObject fire_effects_parent;
    public Transform exit_vehicle_position;
    [SerializeField] protected AudioDistortionFilter distortion;
    [SerializeField] protected AudioSource crash_sound;
    [SerializeField] protected VoxCollider voxCollider;
    [SerializeField] protected GameObject crash_explosion;
    [SerializeField] protected GameObject ground_explosion;
    public Camera vehicle_camera;

    [Header("Vehicle State")]
    public bool can_spawn_in_vehicle;
    public bool is_in_vehicle;
    public bool ignore_damage;
    public bool used_locking_countermeasure;
    [HideInInspector] public float throttle;
    [HideInInspector] public bool start_engine = false;
    public readonly SyncVar<bool> vehicle_destroyed = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));


    [Header("Player & Interaction")]
    [SerializeField] protected GameObject player;
    [SerializeField] protected PlayerProperties playerProperties;
    [SerializeField] protected Rigidbody player_rb;
    [SerializeField] protected PlayerController playerController;

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

    [Server]
    public virtual void Initialize()
    {

        is_in_vehicle = false;

        if (vehicleHudManager != null) vehicleHudManager.gameObject.SetActive(false);
        voxelObj = GetComponentsInChildren<VoxDataProvider>();

        foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.enabled = true;
        }

        if (countermeasures != null)
        {
            countermeasures.SetVehicle(this);
        }
    }

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

    #region Vehicle Control 
    protected virtual void Start_Stop_Engine() { }
    protected virtual void Move() { }
    protected virtual void CameraController() { }
    protected virtual void Switch_weapon() { }
    #endregion

    #region Player Interaction
    
    public virtual void EnterVehicle(NetworkConnection conn, GameObject _player)
    {
        player = _player;

        if (vehicleHudManager != null) vehicleHudManager.gameObject.SetActive(true);

        playerProperties = _player.GetComponent<PlayerProperties>();
        playerController = _player.GetComponent<PlayerController>();
        vehicle_camera = player.GetComponent<PlayerController>().playerCamera;

        player_rb = playerController.rb;
        player_rb.isKinematic = true;
        player_rb.interpolation = RigidbodyInterpolation.None;

        playerProperties.is_in_vehicle = true;
        is_in_vehicle = true;

        if (countermeasures != null && Settings.Instance != null) countermeasures.SetUseCountermeasureKey(Settings.Instance._keybinds.VEHICLE_countermeasureKey);

    }

    protected virtual void ExitVehicle()
    {
        if (!is_in_vehicle) return;

        if (!player.activeSelf) player.SetActive(true);

        if (vehicleHudManager != null) vehicleHudManager.gameObject.SetActive(false);

        if (playerProperties != null) playerProperties.is_in_vehicle = false;

        if (playerController != null)
        {

            playerController.HideOwnerItems(true);
            playerController = null;
        }

        if (player_rb != null)
        {

            player_rb.isKinematic = false;
            player_rb.interpolation = RigidbodyInterpolation.Interpolate;
            player_rb = null;
        }

        playerProperties = null;
        is_in_vehicle = false;


        Quaternion spawnRotation = new Quaternion(0, exit_vehicle_position.transform.rotation.y, 0, exit_vehicle_position.transform.rotation.w);

        if (player != null)
        {
            player.transform.SetParent(null);

            if (exit_vehicle_position.position.y > 0)
            {
                player.transform.position = exit_vehicle_position.position;
            }
            else
            {
                player.transform.position = new Vector3(exit_vehicle_position.position.x, 0.1f, exit_vehicle_position.position.z);
            }


            player.transform.rotation = spawnRotation;

            player = null;
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

    protected virtual void DestroyAnimation() { }

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
        if (player != null)
        {
            if (playerController != null) playerController.RequestDamage(1000);
            ExitVehicle();
        }

        if (did_explode) return;

        did_explode = true;

        CmdRequestPlayExplosionSound();
        HandleSound(crash_sound);


        CmdRequestSpawnExplosionEffect(layer, contact_point);

        RequestDespawn();

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

    #region HUD 
    protected virtual void UpdateHUD() { }

    #endregion

    #region Utility 
    public GameObject GetGameObject() { return transform.gameObject; }
    public Transform GetTransform() { return transform; }
    public Vector3 GetLocalPosition() { return transform.localPosition; }
    public Vector3 GetPosition() { return transform.position; }
    public Quaternion GetLocalRotation() { return transform.localRotation; }
    public Quaternion GetRotation() { return transform.rotation; }

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

    public FactionManager.Faction GetFaction()
    {
        return vehicle_faction;
    }

    public Transform GetSpotPosition()
    {
        return spot_position;
    }
    #endregion
}