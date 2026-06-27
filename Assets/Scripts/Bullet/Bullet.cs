using System.Collections;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class Bullet : NetworkPooledObject
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
    [SerializeField] private Light bulletLight;

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

    private bool _isDespawning;
    private bool _visualsEnabled = false;

    private float timerToAnebleForNonOwners = 0;

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

    #region Unity Lifecycle
    private void OnEnable()
    {
        _isDespawning = false;
        _visualsEnabled = false;

        if (bullet_collider != null)
            bullet_collider.enabled = false;

        if (!IsOwner)
        {
            SetVisualsActive(false);
        }
    }

    private void OnDisable()
    {
        if (trail != null)
        {
            trail.Clear();
            trail.enabled = false;
        }

        _isDespawning = true;
        _visualsEnabled = false;
    }
    #endregion

    #region Bullet Creation
    [ObserversRpc]
    public void CreateBullet(BulletData data, Transform ignoredObject = null, GameObject root = null)
    {
        // Reseta estado
        timerToAnebleForNonOwners = 0;
        _isDespawning = false;
        _visualsEnabled = false;
        timer = 0f;
        SetVisualsActive(false);

        // Ativa o GameObject
        gameObject.SetActive(true);

        // Configura o rigidbody
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Ativa o collider
        if (bullet_collider != null)
            bullet_collider.enabled = true;

        SetBulletProperties(data, ignoredObject, root);

        SetDirection(data.direction, data.speed);

        if (IsServerInitialized)
        {
            StopAllCoroutines();
            StartCoroutine(DespawnTimer());
        }
    }


    private void SetBulletProperties(BulletData data, Transform ignoredObject = null, GameObject root = null)
    {
        delaytoEnableForNonOwner = data.delaytoEnableForNonOwner;
        voxCollider.destructionRadius = data.destructionForce;
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
        transform.position = data.position;
        transform.rotation = data.rotation;
    }

    public void SetDirection(Vector3 direction, float speed)
    {
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }
    #endregion

    #region Updates
    private RaycastHit[] hit_results = new RaycastHit[128];

    void FixedUpdate()
    {
        if (_isDespawning) return;

        if (rb != null)
        {
            rb.AddForce(Vector3.down * bulletDropMultiplier, ForceMode.Acceleration);
        }

        if (!IsOwner) return;

        if (bullet_collider != null && bullet_collider.isTrigger)
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
        if (!IsOwner)
        {
            timerToAnebleForNonOwners += Time.deltaTime;
            if (timerToAnebleForNonOwners >= delaytoEnableForNonOwner && !_visualsEnabled)
            {
                _visualsEnabled = true;
                SetVisualsActive(true);
            }
            return;
        }

        if (_isDespawning) return;

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

    #region Collision Methods
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
            ProcessHit.VehicleHit(hitObject, infantary_damage, shoot_root);
            hitEffects.RequestMetalHitEffect(hitPoint, Quaternion.LookRotation(hitNormal == Vector3.zero ? -transform.forward : hitNormal));
            soundEffects.PlayHitSound(BulletSoundEffect.HitSoundType.Metal, hitPoint);
        }

        if (hitObject.layer == LayerMask.NameToLayer("PlayerHitBox"))
        {
            ProcessHit.PlayerHit(hitObject, infantary_damage, hs_multiplier, shoot_root);
            hitEffects.RequestBloodHitEffect(hitPoint, Quaternion.LookRotation(hitNormal == Vector3.zero ? -transform.forward : hitNormal));
        }

        RequestDisableBullet();
    }

    void OnTriggerEnter(Collider collider)
    {
        if (!IsOwner || _isDespawning || bullet_collider == null || !bullet_collider.isTrigger) return;

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
    #endregion

    #region Visual Management
    private void SetVisualsActive(bool active)
    {
        if (meshRenderer != null)
            meshRenderer.enabled = active;

        if (bulletLight != null)
            bulletLight.enabled = active;

        if (trail != null)
        {
            trail.enabled = active;
            trail.Clear();
        }

        _visualsEnabled = active;
    }
    #endregion

    #region Despawning
    private IEnumerator DespawnTimer()
    {
        yield return new WaitForSeconds(10f);
        RequestDisableBullet();
    }

    private void RequestDisableBullet()
    {
        if (_isDespawning) return;
        _isDespawning = true;

        if (IsServerInitialized)
        {
            Despawn(gameObject);
        }
        else
        {
            CmdRequestDespawn();
        }
    }

    [ServerRpc]
    private void CmdRequestDespawn()
    {
        Despawn(gameObject);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        DisableBulletCompletely();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (!IsServerStarted)
        {
            DisableBulletCompletely();
        }
    }

    private void DisableBulletCompletely()
    {

        if (bullet_collider != null)
            bullet_collider.enabled = false;

        SetVisualsActive(false);
        _visualsEnabled = false;
        _isDespawning = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
    #endregion
}