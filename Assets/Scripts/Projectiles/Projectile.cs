using System.Collections;
using FishNet.CodeGenerating;
using FishNet.Object;
using UnityEngine;

public class Projectile : LocalPooledObject
{
    [Header("References")]
    [SerializeField] protected ProjectileHitEffects hitEffects;
    [SerializeField] protected ProjectileSoundEffect soundEffects;

    [Header("Settings")]
    [SerializeField] protected Collider projectileCollider;
    [SerializeField] protected MeshRenderer meshRenderer;
    [SerializeField] protected TrailRenderer trail;
    [SerializeField] protected ParticleSystem particle;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected Light projectileLight;

    //Private variables
    protected float bulletDropMultiplier;
    protected float projectileDrag;
    protected float infantryDamage;
    protected float initialInfantryDamage;
    protected AnimationCurve infantryDamageByDistance;
    protected float traveledDistance;
    protected float explosionDamageFalloff;
    protected float hsMultiplier;
    protected bool canDamageArmoredVehicles;
    protected float vehicleDamage;
    protected bool didRicochet;
    protected float destructionRadius;
    protected float delaytoEnableForNonOwner;
    protected float delaytoEnableForOwner;
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

        public NetworkObject target = null;
    }

    [System.Serializable]
    public class ProjectileValues : ISerializationCallbackReceiver
    {
        [Header("Damage Model")]
        public float infantryDamage;
        public float headshotMultiplier;
        public float vehicleDamage;

        [Tooltip("Damage by distance: X = distance traveled in meters, Y = infantry damage. The point at distance 0 always uses Infantry Damage.")]
        [ExcludeSerialization]
        public AnimationCurve infantryDamageByDistance = new AnimationCurve();

        public float explosionDamageFalloff;

        [Header("Projectile Model")]
        public float muzzleVelocity;
        [Tooltip("Speed change in meters per second squared. Positive accelerates, negative decelerates, zero keeps the speed unchanged by drag.")]
        public float projectileDrag;
        public float dropMultiplier;
        public bool canDamageVehicles;

        [Header("Visuals")]
        public float delaytoEnableForNonOwner;
        public float delaytoEnableForOwner;

        [Header("Destruction")]
        [Tooltip("Voxel destruction radius. A value of 10 removes voxels up to 10 cells from the impact point.")]
        public float destructionRadius;

        public void OnBeforeSerialize() => EnsureDamageCurveStartsAtBaseDamage();
        public void OnAfterDeserialize() => EnsureDamageCurveStartsAtBaseDamage();

        public ProjectileValues WithMuzzleVelocity(float initialSpeed)
        {
            ProjectileValues shotValues = (ProjectileValues)MemberwiseClone();
            shotValues.muzzleVelocity = initialSpeed;
            return shotValues;
        }

        public AnimationCurve CreateRuntimeDamageCurve()
        {
            EnsureDamageCurveStartsAtBaseDamage();

            AnimationCurve runtimeCurve = new AnimationCurve(infantryDamageByDistance.keys)
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };

            return runtimeCurve;
        }

        private void EnsureDamageCurveStartsAtBaseDamage()
        {
            if (infantryDamageByDistance == null || infantryDamageByDistance.length == 0)
            {
                infantryDamageByDistance = AnimationCurve.Constant(0f, 100f, infantryDamage);
                return;
            }

            for (int i = 0; i < infantryDamageByDistance.length; i++)
            {
                Keyframe key = infantryDamageByDistance[i];

                if (!Mathf.Approximately(key.time, 0f)) continue;

                key.time = 0f;
                key.value = infantryDamage;
                infantryDamageByDistance.MoveKey(i, key);
                return;
            }

            infantryDamageByDistance.AddKey(new Keyframe(0f, infantryDamage));
        }
    }
    #endregion

    #region Bullet Creation
    public override void Activate()
    {
        didRicochet = false;
        isDespawning = false;
        visualsEnabled = false;
        isSetup = false;
        traveledDistance = 0f;

        base.Activate();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (projectileCollider != null) projectileCollider.enabled = true;
 
    }

    public virtual void CreateProjectile(ProjectileProperties prop, ProjectileValues values)
    {
        SetVisualsActive(false);

        SetProjectileValues(values);
        SetProjectileProperties(prop);

        Activate();

        SetDirection(prop.direction, values.muzzleVelocity);

        StopAllCoroutines();
        
        StartCoroutine(DespawnTimer());
        StartCoroutine(EnableVisualsRoutine(delaytoEnableForOwner));

        isSetup = true;
    }
    protected void SetProjectileValues(ProjectileValues values)
    {
        delaytoEnableForNonOwner = values.delaytoEnableForNonOwner == 0 ? 0.01f : values.delaytoEnableForNonOwner;
 
        delaytoEnableForOwner = values.delaytoEnableForOwner == 0 ? 0.01f : values.delaytoEnableForOwner;
        
        initialInfantryDamage = values.infantryDamage;
        infantryDamage = initialInfantryDamage;
        infantryDamageByDistance = values.CreateRuntimeDamageCurve();
        traveledDistance = 0f;
        destructionRadius = values.destructionRadius;
        hsMultiplier = values.headshotMultiplier;
        canDamageArmoredVehicles = values.canDamageVehicles;
        vehicleDamage = values.vehicleDamage;
        bulletDropMultiplier = values.dropMultiplier;
        projectileDrag = values.projectileDrag;
        explosionDamageFalloff = values.explosionDamageFalloff;
    }

    protected virtual void SetProjectileProperties(ProjectileProperties prop)
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
        if (rb != null) rb.linearVelocity = direction * muzzleVelocity;
        
    }
    #endregion

    #region Updates
    public override void LocalFixedUpdate()
    {
        if (!isSetup || isDespawning) return;

        ProcessRaycastHitValidation();
        if (isDespawning) return;

        ApplyProjectileDrag();
        AddForceDown();
    }

    public override void LocalUpdate()
    {
        if (!isSetup || isDespawning) return;
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

        bool hitVoxel = ProcessVoxelCollision(collider, hitPoint, hitNormal);

        if (!hitVoxel && hitObject.layer == LayerMask.NameToLayer("Ground")) ProcessGroundCollision(hitPoint);
        

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
        if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile") || collider.gameObject.layer == LayerMask.NameToLayer("Player")) return;
        if (ignoredTransform != null && collider.transform.IsChildOf(ignoredTransform)) return;

        AddTraveledDistance(Vector3.Distance(lastPosition, transform.position));
        lastPosition = transform.position;
        HandleBulletHit(collider.gameObject, transform.position, Vector3.zero, collider);
    }

    private bool ProcessVoxelCollision(Collider collider, Vector3 position, Vector3 hitNormal)
    {
        if (!ProcessHit.TryGetVoxel(collider.gameObject, out _)) return false;

        VoxelObj materialSource = collider.GetComponentInParent<VoxelObj>();
        VoxelObj.VoxelMaterialType material = materialSource != null
            ? materialSource.voxelMaterialType
            : VoxelObj.VoxelMaterialType.Concrete;

        if (hitEffects != null) hitEffects.VoxelHitEffect(position, material);
        if (soundEffects != null) soundEffects.RequestVoxelHitSound(position, material);

        if (destructionRadius > 2f)
        {
            Explosion.SphereExplosion(
                position,
                infantryDamage,
                vehicleDamage,
                destructionRadius,
                explosionDamageFalloff,
                null,
                shootRoot);
        }
        else
        {
            ProcessHit.VoxelHit(
                collider.gameObject,
                position,
                hitNormal.sqrMagnitude > 0f ? hitNormal : -transform.forward,
                destructionRadius);
        }

        return true;
    }

    private void ProcessGroundCollision(Vector3 pos)
    {
        Explosion.SphereExplosion(pos, infantryDamage, vehicleDamage, destructionRadius, explosionDamageFalloff, null, shootRoot);
        //voxCollider.SphereExplosion(pos, infantryDamage, vehicleDamage);
    }
    #endregion

    #region Visual Management
    public void SetVisualsActive(bool active)
    {
        if (meshRenderer != null) meshRenderer.enabled = active;

        if (projectileLight != null) projectileLight.enabled = active;

        if (trail != null)
        {
            trail.enabled = active;
            trail.Clear();
            trail.emitting = active;
        }

        if (particle != null)
        {
            if (active)
            {
                particle.Clear(true);
                particle.Play(true);
            }
            else particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        visualsEnabled = active;
    }
    
    // Coroutine para ativar os visuais com delay
    protected IEnumerator EnableVisualsRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Verifica se o projétil não foi desativado enquanto o tempo passava
        if (!isDespawning)
        {
            SetVisualsActive(true);
        }
    }
    #endregion

    #region Helpers
    protected void ApplyProjectileDrag()
    {
        if (rb == null || rb.isKinematic || projectileDrag == 0f) return;

        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        Vector3 direction = speed > 0f ? velocity / speed : transform.forward;
        float nextSpeed = Mathf.Max(0f, speed + projectileDrag * Time.fixedDeltaTime);

        rb.linearVelocity = direction * nextSpeed;
    }

    protected void AddForceDown()
    {
        if (rb != null) rb.AddForce(Vector3.down * bulletDropMultiplier, ForceMode.Acceleration);
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
                        AddTraveledDistance(closestHit.distance);
                        lastPosition = closestHit.point;
                        HandleBulletHit(closestHit.collider.gameObject, closestHit.point, closestHit.normal, closestHit.collider);
                        return;
                    }
                }

                AddTraveledDistance(distance);
            }

            lastPosition = currentPosition;
        }
    }

    protected void AddTraveledDistance(float distance)
    {
        if (distance <= 0f) return;

        traveledDistance += distance;
        infantryDamage = EvaluateInfantryDamage(traveledDistance);
    }

    protected float EvaluateInfantryDamage(float distance)
    {
        if (infantryDamageByDistance == null || infantryDamageByDistance.length == 0)
            return initialInfantryDamage;

        float maximumCurveDistance = infantryDamageByDistance[infantryDamageByDistance.length - 1].time;
        float evaluatedDistance = Mathf.Clamp(distance, 0f, maximumCurveDistance);

        return Mathf.Max(0f, infantryDamageByDistance.Evaluate(evaluatedDistance));
    }
    #endregion

    #region Despawning
    protected IEnumerator DespawnTimer(float timer = 10)
    {
        yield return new WaitForSeconds(timer);
        Deactivate();
    }

    public override void Deactivate()
    {
        if (isDespawning) return;

        // Para a Coroutine que possivelmente ainda estaria tentando ativar os visuais após ele colidir
        StopAllCoroutines(); 

        if (projectileCollider != null)
            projectileCollider.enabled = false;

        SetVisualsActive(false);
        visualsEnabled = false;
        isDespawning = true;

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }
        
        // Retorna o objeto base (LocalPooledObject) ao seu estado inativo do gameObject, se necessário.
        base.Deactivate(); 
    }
    #endregion
}
