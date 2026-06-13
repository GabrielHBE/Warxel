using System.Collections;
using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class Bullet : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BulletHitEffects hitEffects;
    [SerializeField] private BulletSoundEffect soundEffects;

    [Header("Settings")]
    [SerializeField] private Collider bullet_collider;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private VoxCollider voxCollider;

    //Private variables
    private float bulletDropMultiplier;
    private float infantary_damage;
    private float damage_dropoff;
    private float damage_dropoff_timer;
    private float minimum_damage;
    private float hs_multiplier;
    private bool can_damage_vehicles;
    private float vehicle_damage;
    private bool did_ricochet;
    private float timer;
    private float delaytoEnableForNonOwner;
    private Vector3 lastPosition;
    private Transform ignoredTransform;
    private GameObject shoot_root;

    // Flag de controle para evitar múltiplas execuções de desativação na mesma frame
    private bool _isDespawning;

    public struct BulletData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 direction;
        public float speed;
        public float dropMultiplier;
        public float infantaryDamage;
        public float damageDropoff;
        public float damageDropoffTimer;
        public float destructionForce;
        public float minimumDamage;
        public float hsMultiplier;
        public bool canDamageVehicles;
        public float vehicleDamage;
        public float delaytoEnableForNonOwner;
    }

    #region Bullet Creation
    [ObserversRpc]
    public void CreateBullet(BulletData data, Transform ignoredObject = null, GameObject root = null)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }

        if (trail != null)
        {
            trail.enabled = true;
            trail.Clear(); // Limpa o rastro antigo para não criar uma linha gigante cruzando o mapa
        }

        gameObject.SetActive(true);

        // Reseta o estado da flag de despawn para o Object Pooling funcionar nesta nova vida
        _isDespawning = false;
        timer = 0f; // Reseta o cronômetro do dropoff de dano

        delaytoEnableForNonOwner = data.delaytoEnableForNonOwner;
        voxCollider.destructionRadius = data.destructionForce;
        transform.position = data.position;
        transform.rotation = data.rotation;
        bullet_collider.enabled = true;

        // 3. Aplica a nova velocidade após limpar o estado antigo
        SetDirection(data.direction, data.speed);

        if (IsServerInitialized)
        {
            // Para segurança do Pool, interrompe qualquer contagem antiga antes de iniciar a nova
            StopAllCoroutines();
            StartCoroutine(DespawnTimer());
        }

        if (!IsOwner)
        {
            // Se não for o dono, pode ocultar temporariamente baseado no seu delay, se configurado
            if (delaytoEnableForNonOwner > 0f)
            {
                if (meshRenderer != null) meshRenderer.enabled = false;
                if (trail != null) trail.enabled = false;
                StartCoroutine(EnableViewForNonOwners());
            }
            return;
        }

        SetBulletProperties(data, ignoredObject, root);
    }

    private void SetBulletProperties(BulletData data, Transform ignoredObject = null, GameObject root = null)
    {
        ignoredTransform = ignoredObject;
        shoot_root = root;
        did_ricochet = false;
        infantary_damage = data.infantaryDamage;
        damage_dropoff = data.damageDropoff;
        damage_dropoff_timer = data.damageDropoffTimer;
        minimum_damage = data.minimumDamage;
        hs_multiplier = data.hsMultiplier;
        can_damage_vehicles = data.canDamageVehicles;
        vehicle_damage = data.vehicleDamage;
        bulletDropMultiplier = data.dropMultiplier;
        lastPosition = transform.position;
    }

    public void SetDirection(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction * speed;
    }

    private IEnumerator EnableViewForNonOwners()
    {
        yield return new WaitForSeconds(delaytoEnableForNonOwner);

        if (meshRenderer != null) meshRenderer.enabled = true;
        if (trail != null) trail.enabled = true;
    }
    #endregion

    #region Updates
    private RaycastHit[] hit_results = new RaycastHit[128];

    void FixedUpdate()
    {
        // Impede física e detecção se o projétil já iniciou o processo de destruição
        if (_isDespawning) return;

        rb.AddForce(Vector3.down * bulletDropMultiplier, ForceMode.Acceleration);

        if (!IsOwner) return;

        if (bullet_collider.isTrigger)
        {
            Vector3 currentPosition = transform.position;
            Vector3 direction = currentPosition - lastPosition;
            float distance = direction.magnitude;

            if (distance > 0)
            {
                int layerMask = ~(1 << LayerMask.NameToLayer("Projectile") | 1 << LayerMask.NameToLayer("Player"));

                int hits = Physics.RaycastNonAlloc(lastPosition, direction.normalized, hit_results, distance, layerMask);

                if (hits > 0)
                {
                    RaycastHit closestHit = default;
                    float minDistance = float.MaxValue;
                    bool foundValidHit = false;

                    for (int i = 0; i < hits; i++)
                    {
                        RaycastHit hit = hit_results[i];

                        if (ignoredTransform != null && hit.collider.transform.IsChildOf(ignoredTransform))
                        {
                            continue;
                        }

                        if (hit.distance < minDistance)
                        {
                            minDistance = hit.distance;
                            closestHit = hit;
                            foundValidHit = true;
                        }
                    }

                    if (foundValidHit)
                    {
                        HandleBulletHit(closestHit.collider.gameObject, closestHit.point, closestHit.normal, closestHit.collider);
                    }
                }
            }

            lastPosition = currentPosition;
        }
    }

    void Update()
    {
        if (_isDespawning || !IsOwner) return;

        if (infantary_damage > minimum_damage && damage_dropoff != 0)
        {
            timer += Time.deltaTime;
            if (timer >= damage_dropoff_timer)
            {
                infantary_damage -= damage_dropoff;
                timer = 0;
            }
        }
    }
    #endregion

    private void HandleBulletHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal, Collider collider)
    {
        if (did_ricochet)
        {
            RequestDisableBullet();
            return;
        }

        hitEffects.RequestCustomHitEffect(hitPoint);

        if (hitObject.layer == LayerMask.NameToLayer("Voxel"))
        {
            ProcessVoxelCollision(collider, hitPoint);
            VoxelObjBase voxelObj = hitObject.GetComponent<VoxelObjBase>();
            if (voxelObj != null)
            {
                VoxelMaterials.VoxelMaterialType material = voxelObj.material;
                hitEffects.RequestVoxelHitEffect(hitPoint, material);
                soundEffects.RequestVoxelHitSound(hitPoint, material);
            }
        }

        if (hitObject.layer == LayerMask.NameToLayer("Ground"))
        {
            ProcessGroundCollision(hitPoint);
        }

        if (hitObject.layer == LayerMask.NameToLayer("Vehicle") && can_damage_vehicles)
        {
            ProcessVehicleCollision(collider);
            hitEffects.RequestMetalHitEffect(hitPoint, Quaternion.LookRotation(hitNormal == Vector3.zero ? -transform.forward : hitNormal));
            soundEffects.PlayHitSound(BulletSoundEffect.HitSoundType.Metal, hitPoint);
        }

        if (hitObject.layer == LayerMask.NameToLayer("PlayerHitBox"))
        {
            ProcessPlayerCollision(hitObject);
            hitEffects.RequestBloodHitEffect(hitPoint, Quaternion.LookRotation(hitNormal == Vector3.zero ? -transform.forward : hitNormal));
        }

        RequestDisableBullet();
    }

    #region TriggerEnter
    void OnTriggerEnter(Collider collider)
    {
        if (!bullet_collider.isTrigger) return;
        if (!IsOwner || _isDespawning) return;

        if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile") ||
            collider.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            return;
        }

        if (ignoredTransform != null && collider.transform.IsChildOf(ignoredTransform))
        {
            return;
        }

        HandleBulletHit(collider.gameObject, transform.position, Vector3.zero, collider);
    }
    #endregion

    #region CollisionEnter
    void OnCollisionEnter(Collision collision)
    {
        if (bullet_collider.isTrigger) return;
        if (!IsOwner || _isDespawning) return;

        if (ignoredTransform != null && collision.gameObject.transform.IsChildOf(ignoredTransform))
        {
            return;
        }

        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            HandleBulletHit(contact.otherCollider.gameObject, contact.point, contact.normal, contact.otherCollider);
        }
    }
    #endregion

    #region Collision / Trigger Helper Methods
    private void ProcessVoxelCollision(Collider collision, Vector3 position)
    {
        if (voxCollider.destructionRadius > 2)
        {
            voxCollider.SphereExplosion(position, infantary_damage, vehicle_damage);
        }
        else
        {
            voxCollider.Collide(collision);
        }
    }

    private void ProcessGroundCollision(Vector3 pos)
    {
        voxCollider.SphereExplosion(pos, infantary_damage, vehicle_damage);
    }

    private void ProcessVehicleCollision(Collider collision)
    {
        Vehicle hit_vehicle = collision.gameObject.GetComponent<Vehicle>() ?? collision.gameObject.GetComponentInParent<Vehicle>();

        if (hit_vehicle != null)
        {
            string[] occupantNames = hit_vehicle.GetOccupantNames();

            if (!hit_vehicle.vehicle_destroyed.Value)
            {
                hit_vehicle.RequestDamage(vehicle_damage);

                float target_resistance = hit_vehicle.GetResistance();
                float final_actual_damage = vehicle_damage * ((100f - target_resistance) / 100f);

                DamageMarker.Instance.UpdateDamage(final_actual_damage);
            }
            else
            {
                ProcessKill.ProcessVehicleKill(shoot_root, occupantNames);
            }
        }
    }

    private void ProcessPlayerCollision(GameObject collision)
    {
        bool hs_hit = false;
        PlayerController player = collision.GetComponentInParent<PlayerController>();
        PlayerProperties playerProperties = player.GetComponent<PlayerProperties>();

        if (playerProperties.is_dead.Value) return;

        float start_hp = playerProperties.hp.Value;
        float base_damage;

        if (collision.gameObject.CompareTag("PlayerHead"))
        {
            base_damage = infantary_damage * hs_multiplier;
            hs_hit = true;
        }
        else if (collision.gameObject.CompareTag("Arms and Legs"))
        {
            base_damage = infantary_damage * 0.8f;
        }
        else if (collision.gameObject.CompareTag("Feet and Hands"))
        {
            base_damage = infantary_damage * 0.6f;
        }
        else
        {
            base_damage = infantary_damage;
        }

        player.RequestDamage(base_damage);

        float target_resistance = player.GetResistance();
        float dano_real_esperado = base_damage * ((100f - target_resistance) / 100f);
        float post_hp = start_hp - dano_real_esperado;
        bool is_lethal_shot = post_hp <= 0;

        if (is_lethal_shot)
        {
            ProcessKill.ProcessInfantryKill(shoot_root, hs_hit, playerProperties.player_name.Value);
        }

        DamageMarker.Instance.UpdateDamage(dano_real_esperado);
    }
    #endregion

    #region Extras
    private IEnumerator DespawnTimer()
    {
        yield return new WaitForSeconds(10f);
        RequestDisableBullet();
    }
    #endregion

    #region Despawning
    private void RequestDisableBullet()
    {
        if (_isDespawning) return;
        _isDespawning = true;

        // Desativa imediatamente a física local para evitar registros fantasmas
        if (bullet_collider != null) bullet_collider.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (IsServerInitialized)
        {
            ExecuteServerDespawn();
        }
        else
        {
            CmdRequestDespawn();
        }
    }

    [ServerRpc]
    private void CmdRequestDespawn() => ExecuteServerDespawn();

    private void ExecuteServerDespawn() => ServerManager.Despawn(GetComponent<NetworkObject>());
    #endregion
}