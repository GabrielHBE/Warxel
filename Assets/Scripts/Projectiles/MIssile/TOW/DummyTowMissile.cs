using FishNet.Object;
using UnityEngine;

public class DummyTowMissile : DummyProjectile
{
    [Header("Homing Settings")]
    [SerializeField] private float turnSpeed = 120f; // Velocidade em que o míssil consegue fazer a curva (graus por segundo)

    [SerializeField] private NetworkObject fowardReference;

    public override void CreateProjectile(Projectile.ProjectileProperties prop, Projectile.ProjectileValues values)
    {
        SetProjectileProperties(prop);

        SetVisualsActive(true);
        Activate();

        StopAllCoroutines();
        //StartCoroutine(DespawnTimer());

        SetDirection(prop.direction, values.muzzleVelocity);

        isSetup = true;
    }

    protected override void SetProjectileProperties(Projectile.ProjectileProperties prop)
    {
        base.SetProjectileProperties(prop);
        fowardReference = prop.target;
    }

    public override void LocalFixedUpdate()
    {
        // Certifica-se de que o Rigidbody e a referência avançada existem antes de mover
        if (rb == null || !isSetup) return;

        ProcessRaycastHitValidation();

        // Se houver uma referência de direção, ajusta a rotação do míssil
        if (fowardReference != null)
        {
            // 1. Pega a rotação alvo baseada na direção frontal da referência (fowardReference)
            Quaternion targetRotation = Quaternion.LookRotation(fowardReference.transform.forward);

            // 2. Rotaciona suavemente da rotação atual do míssil em direção à rotação alvo, respeitando o turnSpeed
            Quaternion newRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

            // 3. Aplica a nova rotação ao Rigidbody de forma segura para a física
            rb.MoveRotation(newRotation);

            // 4. Força a velocidade linear a ir sempre para a nova direção frontal (forward atualizada) do míssil
            // Mantemos a magnitude da velocidade atual para não desacelerar nas curvas
            float currentVelocityMagnitude = rb.linearVelocity.magnitude;

            rb.linearVelocity = transform.forward * currentVelocityMagnitude;
        }
        else
        {
            // Fallback de segurança: se perder a referência, continua indo reto na última direção linear
            rb.linearVelocity = transform.forward * rb.linearVelocity.magnitude;
        }
    }
}
