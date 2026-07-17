using UnityEngine;

public class DummyLockInMissile : DummyProjectile
{

    [Header("Homing Settings")]
    [SerializeField] private float turnSpeed = 120f; // Velocidade em que o míssil consegue fazer a curva (graus por segundo)
    [SerializeField] private float timeToExplodeAfterFlare = 2f; // Tempo após ser enganado pelo flare para explodir

    private Vehicle vehicleTarget;
    private Flares flare;
    private float currentSpeed;
    private bool isFooledByFlare = false; // Indica se o míssil foi enganado pelo flare
    private float flareFoolTimer = 0f; // Timer para controlar a explosão após ser enganado

    public override void CreateProjectile(Projectile.ProjectileProperties prop, Projectile.ProjectileValues values)
    {
        SetProjectileProperties(prop);

        SetVisualsActive(true);
        Activate();

        StopAllCoroutines();
        //StartCoroutine(DespawnTimer());

        // Salva a velocidade inicial para manter o míssil acelerado na direção certa
        currentSpeed = values.muzzleVelocity;

        // Reseta o estado do flare
        isFooledByFlare = false;
        flareFoolTimer = 0f;

        isSetup = true;

    }

    protected override void SetProjectileProperties(Projectile.ProjectileProperties prop)
    {
        base.SetProjectileProperties(prop);
        Debug.LogError(prop.target);
        vehicleTarget = prop.target.GetComponent<Vehicle>();
        if (vehicleTarget != null && vehicleTarget.countermeasures != null)
        {
            flare = vehicleTarget.countermeasures.GetComponent<Flares>();
        }
    }

    public override void LocalFixedUpdate()
    {
        if (vehicleTarget == null || rb == null || !isSetup) return;

        ProcessRaycastHitValidation();

        // Verifica se o flare está ativo e se o míssil ainda não foi enganado
        if (!isFooledByFlare && flare != null && flare.is_active)
        {
            // Marca o míssil como enganado
            isFooledByFlare = true;
            flareFoolTimer = 0f;
            Debug.Log("Missile fooled by flare!");
        }

        // Se foi enganado pelo flare, vai em linha reta e depois explode
        if (isFooledByFlare)
        {
            // Mantém a direção atual (em linha reta)
            rb.linearVelocity = transform.forward * currentSpeed;

            // Incrementa o timer
            flareFoolTimer += Time.fixedDeltaTime;

            // Se passou o tempo limite, explode
            if (flareFoolTimer >= timeToExplodeAfterFlare)
            {
                ExplodeMissile();
            }
        }
        else
        {
            // Comportamento normal de perseguição
            Vector3 targetDirection = (vehicleTarget.transform.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

            rb.linearVelocity = transform.forward * currentSpeed;
        }
    }

    private void ExplodeMissile()
    {
        // Toca efeitos de explosão
        if (hitEffects != null)
        {
            hitEffects.CustomHitEffect(transform.position);
        }

        // Desativa o míssil
        Deactivate();
    }

    // Método para resetar manualmente o estado (útil para reutilização do objeto)
    public override void Deactivate()
    {
        isFooledByFlare = false;
        flareFoolTimer = 0f;
        base.Deactivate();
    }
}