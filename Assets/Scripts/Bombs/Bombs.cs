using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.Tools;

public abstract class Bombs : NetworkBehaviour
{
    [Header("Damage")]
    [SerializeField] protected float infantary_damage;
    [SerializeField] protected float vehicle_damage;

    [Header("Physics")]
    [SerializeField] protected float time_to_explode;
    [SerializeField] protected Collider bomb_collider;
    [SerializeField] protected VoxCollider voxCollider;
    [SerializeField] protected float travel_speed;
    [SerializeField] protected Rigidbody rb;

    [Header("Effects")]
    [SerializeField] protected AudioSource explosion_sound;
    [SerializeField] protected GameObject explosion_effect;
    [SerializeField] protected TrailRenderer trail;
    [SerializeField] protected AudioSource shoot_sound;

    [Header("Visual")]
    public MeshRenderer mesh;

    protected float speed;
    public GameObject parent_gameobject;
    protected bool didShoot;
    protected bool hasExploded; // Adicionado para consistência com Missiles

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        SetParentVehicle();
        if (!didShoot && rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    protected virtual void Update()
    {
        if (!IsSpawned || hasExploded || !IsOwner) return;

        if (parent_gameobject == null || !parent_gameobject.activeSelf)
            Explode(transform.position);

        if (didShoot) DestroyTimer();
    }

    protected virtual void LateUpdate()
    {
        if (!didShoot && transform.parent != null)
        {
            transform.localPosition = Vector3.zero;
        }
    }

    protected void DestroyTimer()
    {
        if (didShoot)
        {
            time_to_explode -= Time.deltaTime;
            if (time_to_explode <= 0) Explode(transform.position);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!IsSpawned || hasExploded || !IsOwner) return;

        if (rb != null) speed = rb.linearVelocity.magnitude;
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (!IsSpawned || hasExploded) return;

        GameObject ignoreGoInExplosion = null;

        if (trail != null)
        {
            trail.transform.SetParent(null, true);
            Destroy(trail);
        }

        if (collision.gameObject == parent_gameobject) return;

        hasExploded = true;

        // Lógica de dano
        if (collision.gameObject.layer == LayerMask.NameToLayer("Vehicle"))
        {
            Vehicle vehicle = collision.gameObject.GetComponent<Vehicle>() ?? collision.gameObject.GetComponentInParent<Vehicle>();

            if (vehicle != null && !vehicle.vehicle_destroyed.Value)
            {
                ignoreGoInExplosion = vehicle.gameObject;

                string[] occupantNames = vehicle.GetOccupantNames();

                float target_resistance = vehicle.GetResistance();
                float final_actual_damage = vehicle_damage * ((100f - target_resistance) / 100f);
                vehicle.RequestDamage(vehicle_damage);

                if (vehicle.vehicle_destroyed.Value)
                    ProcessKill.ProcessVehicleKill(gameObject, occupantNames);
                else
                    DamageMarker.Instance?.UpdateDamage(final_actual_damage);
            }
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>() ?? collision.gameObject.GetComponentInParent<PlayerController>();
            PlayerProperties player_properties = player?.GetComponent<PlayerProperties>();

            ignoreGoInExplosion = player.gameObject;

            if (player_properties != null)
            {
                player.RequestDamage(infantary_damage);
                float target_resistance = player.GetResistance();
                float final_actual_damage = infantary_damage * ((100f - target_resistance) / 100f);

                if (player_properties.is_dead.Value)
                {
                    ProcessKill.ProcessInfantryKill(gameObject, false, player_properties.player_name.Value);
                }
                else
                {
                    DamageMarker.Instance?.UpdateDamage(final_actual_damage);
                }
            }
        }

        if (voxCollider != null)  voxCollider.SphereExplosion(collision.contacts[0].point, infantary_damage, vehicle_damage, ignoreGoInExplosion);

        bomb_collider.enabled = false;

        Explode(collision.contacts[0].point);
    }
    #endregion

    #region Collision / Explosion
    [ServerRpc]
    protected void Explode(Vector3 contact_point)
    {
        if (!IsSpawned) return;

        CmdPlayExplosionSound();

        if (explosion_effect != null)
        {
            GameObject explosion = Instantiate(explosion_effect, contact_point, Quaternion.identity);
            Spawn(explosion);
            explosion.transform.localScale *= 2;
        }

        RequestDespawn();
    }

    [ObserversRpc]
    private void CmdPlayExplosionSound()
    {
        if (explosion_sound != null)
        {
            AudioDistanceController adc = explosion_sound.GetComponent<AudioDistanceController>();
            if (adc != null) adc.StartGrowth();
        }
    }

    protected virtual void CreateSound(AudioSource sound)
    {
        if (sound == null) return;

        AudioDistanceController audioDistanceController = sound.GetComponent<AudioDistanceController>();
        if (audioDistanceController != null)
        {
            audioDistanceController.StartGrowth();
            return;
        }

        sound.gameObject.AddComponent<CreateSoundDestroyManager>().Initialize(sound, sound.clip.length);
    }

    private class CreateSoundDestroyManager : MonoBehaviour
    {
        public void Initialize(AudioSource audio, float destroyTimer)
        {
            transform.SetParent(null);
            audio.Play();
            Destroy(gameObject, destroyTimer);
        }
    }
    #endregion

    #region Utility
    // Modificado para aceitar direção (como nos mísseis)
    public virtual void ShootBomb() { }

    protected void SetParentVehicle()
    {
        Vehicle vehicle = GetComponentInParent<Vehicle>();
        if (vehicle != null && voxCollider != null)
        {
            voxCollider.parent_vehicle = vehicle;
        }
    }

    private void RequestDespawn()
    {
        if (IsSpawned)
            Despawn(gameObject);
        else
            Destroy(gameObject);
    }
    #endregion
}