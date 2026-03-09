using System;
using UnityEngine;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxDataProviders;
using VoxelDestructionPro.VoxelObjects;

public class Vehicle : MonoBehaviour
{
    public string vehicle_name;

    [Header("Progression")]
    public int vehicle_level;
    public float points_to_up_level;
    public float vehicle_level_progression;
    public int level_to_unlock;


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
    protected float acceleration;
    public bool vehicle_destroyed = false;


    [Header("Player & Interaction")]
    [SerializeField] protected GameObject player;
    [SerializeField] protected PlayerProperties playerProperties;
    [SerializeField] protected Rigidbody player_rb;
    [SerializeField] protected PlayerController playerController;

    protected float minFov;

    [Header("Health & Damage")]
    public float original_hp;
    public float hp;
    protected float resistance;

    [Header("Voxel Systems")]
    VoxDataProvider[] voxelObj;


    [Header("Physics & Collision")]
    [SerializeField] protected LayerMask collisionLayers;
    public Vector3 forwardReference;




    #region Unity Lifecycle
    protected virtual void Update() { }
    protected virtual void FixedUpdate() { }
    public virtual void Spawn()
    {

        is_in_vehicle = false;

        if (vehicleHudManager != null) vehicleHudManager.gameObject.SetActive(false);
        voxelObj = GetComponentsInChildren<VoxDataProvider>();

        if (countermeasures != null)
        {
            countermeasures.SetVehicle(this);
            countermeasures.SetUseCountermeasureKey(Settings.Instance._keybinds.VEHICLE_countermeasureKey);
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
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
    public virtual void EnterVehicle(GameObject _player)
    {
        if (vehicleHudManager != null) vehicleHudManager.gameObject.SetActive(true);
        player = _player;

        playerProperties = _player.GetComponent<PlayerProperties>();
        playerController = _player.GetComponent<PlayerController>();
        vehicle_camera = player.GetComponent<PlayerController>().playerCamera;

        player_rb = playerController.rb;
        player_rb.isKinematic = true;
        player_rb.interpolation = RigidbodyInterpolation.None;

        playerProperties.is_in_vehicle = true;
        is_in_vehicle = true;
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
            if (hit_playerController != null) hit_playerController.Damage(rb.linearVelocity.magnitude);
        }
    }

    public float Damage(float damage)
    {
        if (ignore_damage)
        {
            Debug.Log($"Dano ignorado. Resistance: {resistance}%");
            return 0;
        }

        // Resistência de 0% = dano total, 100% = dano zero
        float effectiveDamage = damage * ((100f - resistance) / 100f);

        hp -= effectiveDamage;

        if (hp <= 0)
        {
            vehicle_destroyed = true;
        }

        return effectiveDamage;
    }

    protected virtual void DestroyAnimation() { }

    protected void HandleCollision(Collision collision, float destruction_force)
    {
        //Debug.Log(rb.linearVelocity.magnitude);
        if (destruction_force < 10) return;

        ContactPoint contact = collision.contacts[0];
        voxCollider.destructionRadius = Math.Clamp(destruction_force, 0, 30);


        voxCollider.SphereExplosion(contact.point, 1);
        ApplyFallUpperVoxels(collision, contact, voxCollider.destructionRadius);
        Damage(voxCollider.destructionRadius / 2);

    }

    protected void ApplyFallUpperVoxels(Collision collision, ContactPoint contact, float explosionForce)
    {
        Mod_DestroyAfterAll mod_DestroyAfterAll = collision.gameObject.GetComponentInParent<Mod_DestroyAfterAll>();
        mod_DestroyAfterAll?.StartCoroutine(mod_DestroyAfterAll.FallUpperVoxels(explosionForce, contact.point, true));
    }

    bool did_explode = false;

    protected virtual void Explode(Vector3 contact_point, Vector3 contact_normal, LayerMask layer, float explosionForce)
    {
        if (did_explode) return;

        did_explode = true;

        HandleSound(crash_sound);

        if (layer == LayerMask.NameToLayer("Ground"))
        {
            Instantiate(ground_explosion, contact_point, Quaternion.LookRotation(contact_normal));
        }
        else
        {
            Instantiate(crash_explosion, contact_point, Quaternion.LookRotation(contact_normal));
        }

        Destroy(gameObject);

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
        this.hp = hp;
        this.resistance = resistance;
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

        hp = original_hp;
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

    public void UpgradeVehicleLevel(float points)
    {
        vehicle_level_progression += points;

        if (vehicle_level_progression >= points_to_up_level)
        {
            vehicle_level += 1;
            vehicle_level_progression = 0;
        }

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
    #endregion
}