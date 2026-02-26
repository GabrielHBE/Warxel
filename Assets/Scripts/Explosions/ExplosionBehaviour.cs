using System.Collections;
using UnityEngine;

public class ExplosionBehaviour : MonoBehaviour
{
    [SerializeField] private float destroyTimer = 15;
    [SerializeField] private Rigidbody[] sparkles;
    [SerializeField] private float minForce = 1f;
    [SerializeField] private float maxForce = 10f;
    [SerializeField] private LayerMask groundLayer; // Adicionar camada pelo Inspector
    [SerializeField] private Light explosion_light;

    void Awake()
    {
        explosion_light.enabled = true;

        // Se não foi atribuída uma camada, usa a padrão "Ground"
        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }

        Destroy(gameObject, destroyTimer);
        StartCoroutine(ReduceLightIntensity());


        if (sparkles != null)
        {
            foreach (Rigidbody spark in sparkles)
            {
                if (spark != null)
                {
                    SetupSpark(spark);
                }
            }

        }

    }

    private IEnumerator ReduceLightIntensity()
    {
        while (explosion_light.intensity > 0)
        {
            explosion_light.intensity -= Time.deltaTime * 3500;
            yield return null;
        }
        Destroy(explosion_light.gameObject);
    }

    private void SetupSpark(Rigidbody spark)
    {
        spark.transform.SetParent(null, true);
        spark.transform.localScale = Vector3.one;
        spark.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Adicionar componente Sparkle
        Sparkle sparkleComponent = spark.gameObject.AddComponent<Sparkle>();
        sparkleComponent.Initialize(groundLayer, destroyTimer);

        // Aplicar força
        Vector3 randomDirection = Random.insideUnitSphere.normalized;
        float randomForce = Random.Range(minForce, maxForce);
        spark.AddForce(randomDirection * randomForce, ForceMode.Impulse);
        spark.AddTorque(Random.insideUnitSphere * randomForce, ForceMode.Impulse);
    }

    // Classe Sparkle modificada
    private class Sparkle : MonoBehaviour
    {
        private LayerMask _groundLayer;
        private bool _isGrounded = false;

        public void Initialize(LayerMask groundLayer, float lifeTime)
        {
            Destroy(gameObject, lifeTime);
            _groundLayer = groundLayer;
        }

        void OnTriggerEnter(Collider collision)
        {
            // Verificar se colidiu com o chão usando LayerMask
            if (((1 << collision.gameObject.layer) & _groundLayer) != 0 && !_isGrounded)
            {
                _isGrounded = true;

                // Opcional: reduzir física ao tocar o chão
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearDamping = 2f; // Aumentar arrasto
                    rb.angularDamping = 2f;

                    // Destruir após um tempo no chão (opcional)
                    Invoke(nameof(DestroySparkle), 2f);
                }
            }
        }

        private void DestroySparkle()
        {
            // Opcional: adicionar efeito visual antes de destruir
            if (TryGetComponent(out Renderer renderer))
            {
                renderer.enabled = false;
            }
            Destroy(gameObject, 0.1f);
        }

    }

}