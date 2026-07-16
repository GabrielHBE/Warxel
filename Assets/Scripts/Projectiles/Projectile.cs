using System.Collections;
using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class Projectile : LocalPooledObject
{
    [Header("References")]
    [SerializeField] private ProjectileHitEffects hitEffects;
    [SerializeField] private ProjectileSoundEffect soundEffects;

    [Header("Settings")]
    [SerializeField] protected Collider projectileCollider;
    [SerializeField] protected MeshRenderer meshRenderer;
    [SerializeField] protected TrailRenderer trail;
    [SerializeField] protected ParticleSystem particle;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected VoxCollider voxCollider;
    [SerializeField] protected Light projectileLight;

    //Private variables
    protected float bulletDropMultiplier;
    protected float infantryDamage;
    protected float damageDropoff;
    protected float damageDropoffTimer;
    protected float minimumDamage;
    protected float hsMultiplier;
    protected bool canDamageArmoredVehicles;
    protected float vehicleDamage;
    protected bool didRicochet;
    protected float timer;
    protected float delaytoEnableForNonOwner;
    protected Vector3 lastPosition;
    protected Transform ignoredTransform;
    protected GameObject shootRoot;
    protected bool isDespawning;
    protected bool visualsEnabled = false;
    protected float timerToAnebleForNonOwners = 0;
    protected bool isSetup = false;
    private RaycastHit[] hitResults = new RaycastHit[128];

    #region Inner Classes
    public class ProjectileProperties
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 direction => rotation * Vector3.forward;
        public Transform ignoredObject = null;
        public GameObject root = null;
        public string customHitSound = null;
        public SoundManager.SoundProperties customHitSoundProperties = SoundManager.SoundProperties.Default;
        public GameObject customHitEffect;
    }

    [System.Serializable]
    public class ProjectileValues
    {
        [Header("Damage Model")]
        [SerializeField] public float infantryDamage;
        [SerializeField] public float headshotMultiplier;
        [SerializeField] public float vehicleDamage;
        [SerializeField] public float damageDropoff;
        [SerializeField] public float damageDropoffTimer;
        [SerializeField] public float minimumDamage;

        [Header("Projectile Model")]
        [SerializeField] public float muzzleVelocity;
        [SerializeField] public float dropMultiplier;
        [SerializeField] public bool canDamageVehicles;
        [SerializeField] public float delaytoEnableForNonOwner;

        [Header("Destruction")]
        [SerializeField] public float destructionForce;
    }
    #endregion

    #region Bullet Creation
    public override void Activate()
    {
        didRicochet = false;
        isDespawning = false;
        visualsEnabled = false;
        isSetup = false;
        timer = 0f;

        gameObject.SetActive(true);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (projectileCollider != null) projectileCollider.enabled = true;

        SetVisualsActive(true);
    }

    public virtual void CreateProjectile(ProjectileProperties prop, ProjectileValues values)
    {

        SetProjectileValues(values);
        SetProjectileProperties(prop);

        Activate();

        SetDirection(prop.direction, values.muzzleVelocity);

        StopAllCoroutines();
        StartCoroutine(DespawnTimer());

        isSetup = true;
    }
    protected void SetProjectileValues(ProjectileValues values)
    {
        delaytoEnableForNonOwner = values.delaytoEnableForNonOwner == 0 ? 0.01f : values.delaytoEnableForNonOwner;
        voxCollider.destructionRadius = values.destructionForce;
        infantryDamage = values.infantryDamage;
        damageDropoff = values.damageDropoff;
        damageDropoffTimer = values.damageDropoffTimer;
        minimumDamage = values.minimumDamage;
        hsMultiplier = values.headshotMultiplier;
        canDamageArmoredVehicles = values.canDamageVehicles;
        vehicleDamage = values.vehicleDamage;
        bulletDropMultiplier = values.dropMultiplier;
    }

    protected void SetProjectileProperties(ProjectileProperties prop)
    {
        ignoredTransform = prop.ignoredObject;
        shootRoot = prop.root;

        // Sincroniza a física instantaneamente
        if (rb != null)
        {
            rb.position = prop.position;
            rb.rotation = prop.rotation;
        }

        transform.position = prop.position;
        transform.rotation = prop.rotation;
        lastPosition = transform.position;

        if (prop.customHitSound != null) soundEffects.SetCustomHitSound(prop.customHitSound, prop.customHitSoundProperties);
        if (prop.customHitEffect != null) hitEffects.SetCustomHitEffect(prop.customHitEffect);
    }
    public void SetDirection(Vector3 direction, float muzzleVelocity)
    {
        if (rb != null)
        {
            rb.linearVelocity = direction * muzzleVelocity;
        }
    }
    #endregion

    #region Updates
    public override void LocalFixedUpdate()
    {
        if (!isSetup || isDespawning) return;

        ProcessRaycastHitValidation();
        AddForceDown();
    }

    public override void LocalUpdate()
    {
        if (!isSetup || isDespawning) return;

        ProcessDamageDropoff();
    }
    #endregion

    #region Collision Methods
    private void HandleBulletHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal, Collider collider)
    {
        if (didRicochet)
        {
            Deactivate();
            return;
        }

        hitEffects.CustomHitEffect(hitPoint);

        if (hitObject.layer == LayerMask.NameToLayer("Voxel"))
        {
            ProcessVoxelCollision(collider, hitPoint);
            VoxelObjBase voxelObj = hitObject.GetComponent<VoxelObjBase>();
            if (voxelObj != null)
            {
                VoxelMaterials.VoxelMaterialType material = voxelObj.material;
                hitEffects.VoxelHitEffect(hitPoint, material);
                soundEffects.RequestVoxelHitSound(hitPoint, material);
            }
        }

        if (hitObject.layer == LayerMask.NameToLayer("Ground"))
        {
            ProcessGroundCollision(hitPoint);
        }

        if (hitObject.layer == LayerMask.NameToLayer("Vehicle") && canDamageArmoredVehicles)
        {
            ProcessHit.VehicleHit(hitObject, infantryDamage, shootRoot);
            hitEffects.MetalHitEffect(hitPoint, Quaternion.LookRotation(hitNormal == Vector3.zero ? -transform.forward : hitNormal));
            soundEffects.PlayHitSound(ProjectileSoundEffect.HitSoundType.Metal, hitPoint);
        }

        if (hitObject.layer == LayerMask.NameToLayer("PlayerHitBox"))
        {
            ProcessHit.PlayerHit(hitObject, infantryDamage, hsMultiplier, shootRoot);
            hitEffects.BloodHitEffect(hitPoint, Quaternion.LookRotation(hitNormal == Vector3.zero ? -transform.forward : hitNormal));
        }

        Deactivate();
    }

    void OnTriggerEnter(Collider collider)
    {
        if (isDespawning || projectileCollider == null || !projectileCollider.isTrigger) return;

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
            voxCollider.SphereExplosion(position, infantryDamage, vehicleDamage);
        }
        else
        {
            voxCollider.Collide(collision);
        }
    }

    private void ProcessGroundCollision(Vector3 pos)
    {
        voxCollider.SphereExplosion(pos, infantryDamage, vehicleDamage);
    }
    #endregion

    #region Visual Management
    public void SetVisualsActive(bool active)
    {
        if (meshRenderer != null)
            meshRenderer.enabled = active;

        if (projectileLight != null)
            projectileLight.enabled = active;

        if (trail != null)
        {
            trail.enabled = active;
            trail.Clear();
        }

        if (particle != null)
        {
            if (active)
            {
                particle.Play();
            }
            else
            {
                particle.Stop();
            }
        }

        visualsEnabled = active;
    }
    #endregion

    #region Helpers
    protected void AddForceDown()
    {
        if (rb != null)
        {
            rb.AddForce(Vector3.down * bulletDropMultiplier, ForceMode.Acceleration);
        }
    }

    protected void ProcessRaycastHitValidation()
    {
        if (projectileCollider != null)
        {
            Vector3 currentPosition = transform.position;
            Vector3 direction = currentPosition - lastPosition;
            float distance = direction.magnitude;

            if (distance > 0)
            {
                // CORREÇÃO AQUI: Ignora "Projectile" e "Player" (cápsula de movimento), MAS NÃO "PlayerHitBox"
                int layerMask = ~(1 << LayerMask.NameToLayer("Projectile") | 1 << LayerMask.NameToLayer("Player"));

                int hits = Physics.RaycastNonAlloc(lastPosition, direction.normalized, hitResults, distance, layerMask);

                if (hits > 0)
                {
                    RaycastHit closestHit = default;
                    float minDistance = float.MaxValue;
                    bool foundValidHit = false;

                    for (int i = 0; i < hits; i++)
                    {
                        RaycastHit hit = hitResults[i];

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

    protected void ProcessDamageDropoff()
    {
        if (infantryDamage > minimumDamage && damageDropoff != 0 && damageDropoffTimer != 0)
        {
            timer += Time.deltaTime;
            if (timer >= damageDropoffTimer)
            {
                infantryDamage -= damageDropoff;
                timer = 0;
            }
        }
    }
    #endregion

    #region Despawning
    protected IEnumerator DespawnTimer()
    {
        yield return new WaitForSeconds(10f);
        Deactivate();
    }

    public override void Deactivate()
    {
        if (isDespawning) return;

        if (projectileCollider != null)
            projectileCollider.enabled = false;

        SetVisualsActive(false);
        visualsEnabled = false;
        isDespawning = true;

        if (rb != null)
        {
            // Só zera a velocidade se ele não for cinemático
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }
    }
    #endregion
}