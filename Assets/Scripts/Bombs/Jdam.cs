
using UnityEngine;

public class Jdam : Bombs
{
    [SerializeField] private TrailRenderer trailRenderer;
    
    [Header("Sounds")]
    [SerializeField] private AudioSource drop_bomb_sound;
    
    private bool isShot = false;
    private Vector3 predictedImpactPoint;
    private bool hasPredictedImpact = false;


    protected override void Update()
    {
        DestroyTimer();
        CalculateImpactPoint();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (isShot)
        {
            rb.AddForce(Vector3.down * travel_speed);
        }
    }

    private void CalculateImpactPoint()
    {
        // Usar raycasting para prever onde a bomba vai atingir
        Vector3 velocity = rb.linearVelocity;
        Vector3 position = transform.position;

        // Se estiver caindo quase verticalmente, usar cálculo simplificado
        if (Mathf.Abs(velocity.y) > Mathf.Abs(velocity.x) && Mathf.Abs(velocity.y) > Mathf.Abs(velocity.z))
        {
            // Calcular tempo até atingir o solo (y=0 ou outro limite)
            float groundHeight = 0f; // Ajuste conforme necessário
            float heightAboveGround = position.y - groundHeight;

            if (heightAboveGround > 0 && velocity.y < 0)
            {
                // Usar física básica para prever impacto
                float gravity = Physics.gravity.y;
                float timeToImpact = (-velocity.y - Mathf.Sqrt(velocity.y * velocity.y - 2 * gravity * heightAboveGround)) / gravity;

                if (timeToImpact > 0)
                {
                    // Calcular posição horizontal no momento do impacto
                    Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
                    predictedImpactPoint = position + horizontalVelocity * timeToImpact;
                    predictedImpactPoint.y = groundHeight;
                    hasPredictedImpact = true;
                }
            }
        }

        // Alternativa: usar raycast para detectar o chão na trajetória
        RaycastHit hit;
        float raycastDistance = 500f;

        // Lançar um raio na direção da velocidade
        if (Physics.Raycast(position, velocity.normalized, out hit, raycastDistance))
        {
            predictedImpactPoint = hit.point;
            hasPredictedImpact = true;
        }
        else
        {
            // Se não acertar nada, estimar baseado na altura
            float timeToHitGround = -position.y / velocity.y;
            if (timeToHitGround > 0 && velocity.y < 0)
            {
                predictedImpactPoint = position + velocity * timeToHitGround;
                predictedImpactPoint.y = 0;
                hasPredictedImpact = true;
            }
        }
    }

    public Vector3 GetPredictedImpactPoint()
    {
        if (hasPredictedImpact)
            return predictedImpactPoint;
        else
            return transform.position + Vector3.down * 50f; // Fallback
    }

    public bool HasPredictedImpact()
    {
        return hasPredictedImpact;
    }


    public override void Shoot()
    {
        if (isShot) return;

        trailRenderer.enabled = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        transform.SetParent(null);

        // PEGAR O MOMENTUM DO JET
        Rigidbody jetRb = voxCollider.parent_vehicle.GetComponent<Rigidbody>();
        if (jetRb != null)
        {
            rb.linearVelocity = jetRb.linearVelocity;
        }

        isShot = true;
        hasPredictedImpact = false; // Resetar previsão
    }
}