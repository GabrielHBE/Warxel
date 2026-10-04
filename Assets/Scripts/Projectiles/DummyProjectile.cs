using System.Collections;
using UnityEngine;

public class DummyProjectile : LocalPooledObject
{
    [Header("References")]
    [SerializeField] protected ProjectileHitEffects hitEffects; // Adicionado para efeitos visuais
    [SerializeField] protected ProjectileSoundEffect soundEffects; // Adicionado para efeitos sonoros

    [Header("Settings")]
    [SerializeField] private MeshRenderer mesh;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected TrailRenderer trailRenderer;
    [SerializeField] protected ParticleSystem particle;
    [SerializeField] protected Light projectileLight;

    private Transform ignoredTransform;
    private Vector3 lastPosition;
    private float bulletDropMultiplier;
    private float projectileDrag;
    private int layerMask;

    protected bool isSetup;

    private RaycastHit[] hit_results = new RaycastHit[128];

    protected void Awake()
    {
        layerMask = ~(1 << LayerMask.NameToLayer("Projectile") | 1 << LayerMask.NameToLayer("Player"));
    }

    public virtual void CreateProjectile(Projectile.ProjectileProperties prop, Projectile.ProjectileValues values)
    {
        SetProjectileProperties(prop);
        SetProjectileValues(values);

        lastPosition = transform.position;

        Activate();

        StartCoroutine(DelayToEnable(values.delaytoEnableForNonOwner));

        SetDirection(prop.direction, values.muzzleVelocity);
        isSetup = true;
    }

    protected virtual void SetProjectileProperties(Projectile.ProjectileProperties prop)
    {
        rb.position = prop.position;
        rb.rotation = prop.rotation;
        transform.position = prop.position;
        transform.rotation = prop.rotation;
        lastPosition = prop.position;
        ignoredTransform = prop.ignoredObject;
        // Sincroniza sons e efeitos customizados caso existam
        if (prop.customHitSound != null && soundEffects != null) soundEffects.SetCustomHitSound(prop.customHitSound, prop.customHitSoundProperties);
        if (prop.customHitEffect != null && hitEffects != null) hitEffects.SetCustomHitEffect(prop.customHitEffect);
    }

    protected virtual void SetProjectileValues(Projectile.ProjectileValues values)
    {
        bulletDropMultiplier = values.dropMultiplier;
        projectileDrag = values.projectileDrag;
    }

    protected IEnumerator DelayToEnable(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetVisualsActive(true);
    }

    public void SetDirection(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction * speed;
    }

    private void HandleBulletHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (hitEffects != null) hitEffects.CustomHitEffect(hitPoint);

        if (ProcessHit.TryGetVoxel(hitObject, out _))
        {
            VoxelObj materialSource = hitObject.GetComponentInParent<VoxelObj>();
            VoxelObj.VoxelMaterialType material = materialSource != null
                ? materialSource.voxelMaterialType
                : VoxelObj.VoxelMaterialType.Concrete;

            if (hitEffects != null) hitEffects.VoxelHitEffect(hitPoint, material);
            if (soundEffects != null) soundEffects.RequestVoxelHitSound(hitPoint, material);

        }

        if (hitObject.layer == LayerMask.NameToLayer("Vehicle"))
        {
            if (hitEffects != null) hitEffects.MetalHitEffect(hitPoint, Quaternion.LookRotation(hitNormal == Vector3.zero ? -transform.forward : hitNormal));
            if (soundEffects != null) soundEffects.PlayHitSound(ProjectileSoundEffect.HitSoundType.Metal, hitPoint);
        }

        // Apenas efeitos visuais de sangue no Player (sem ProcessHit)
        if (hitObject.layer == LayerMask.NameToLayer("PlayerHitBox"))
        {
            if (hitEffects != null) hitEffects.BloodHitEffect(hitPoint, Quaternion.LookRotation(hitNormal == Vector3.zero ? -transform.forward : hitNormal));
        }

        Deactivate();
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile") ||
            collider.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            return;
        }

        if (ignoredTransform != null && collider.transform.IsChildOf(ignoredTransform))
        {
            return;
        }

        // Substituímos o simples Deactivate pela nova lógica de colisão
        HandleBulletHit(collider.gameObject, transform.position, Vector3.zero);
    }



    public override void LocalFixedUpdate()
    {
        if (!isSetup || rb == null) return;

        ProcessRaycastHitValidation();
        if (!isSetup) return;

        ApplyProjectileDrag();
        rb.AddForce(Vector3.down * bulletDropMultiplier, ForceMode.Acceleration);
    }

    protected void ApplyProjectileDrag()
    {
        if (rb == null || rb.isKinematic || projectileDrag == 0f) return;

        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        Vector3 direction = speed > 0f ? velocity / speed : transform.forward;
        float nextSpeed = Mathf.Max(0f, speed + projectileDrag * Time.fixedDeltaTime);

        rb.linearVelocity = direction * nextSpeed;
    }

    protected void ProcessRaycastHitValidation()
    {
        Vector3 currentPosition = transform.position;
        Vector3 direction = currentPosition - lastPosition;
        float distance = direction.magnitude;

        if (distance > 0)
        {
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
                    // Chama a função de hit com os dados do Raycast para gerar as partículas na direção correta
                    HandleBulletHit(closestHit.collider.gameObject, closestHit.point, closestHit.normal);
                }
            }
        }

        lastPosition = currentPosition;
    }

    public override void LocalUpdate()
    {
        return;
    }

    public override void Deactivate()
    {
        isSetup = false;

        transform.localPosition = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        SetVisualsActive(false);

        base.Deactivate();
    }

    public override void Activate()
    {
        base.Activate();
        rb.isKinematic = false;
    }

    public void SetVisualsActive(bool active)
    {
        if (projectileLight != null) projectileLight.enabled = active;

        if (mesh != null) mesh.enabled = active;

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.enabled = active;
            trailRenderer.emitting = active;
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
    }
}
