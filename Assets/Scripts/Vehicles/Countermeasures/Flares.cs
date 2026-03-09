using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class Flares : Countermeasures
{
    [SerializeField] private GameObject flare_effect;
    private Coroutine flareCoroutine;
    [SerializeField] private float force_multiplier = 10f;

    protected override void Update()
    {
        base.Update();
        if (reload_countermeasures_duration <= 0)
        {
            is_reloading = false;
            return;
        }

        if (vehicle.used_locking_countermeasure == false)
        {
            is_reloading = true;
            reload_countermeasures_duration -= Time.deltaTime;
        }
        else
        {
            is_reloading = false;
            countermeasures_duration -= Time.deltaTime;
            if (countermeasures_duration <= 0)
            {
                StopCountermeasure();
            }
        }
    }

    protected override void StopCountermeasure()
    {
        is_active = false;
        vehicle.used_locking_countermeasure = false;
        countermeasures_duration = countermeasures_original_duration;

        // Parar a corrotina se estiver em execução
        if (flareCoroutine != null)
        {
            StopCoroutine(flareCoroutine);
            flareCoroutine = null;
        }
    }

    public override void UseCountermeasure()
    {
        is_active = true;
        vehicle.used_locking_countermeasure = true;
        reload_countermeasures_duration = reload_countermeasures_original_duration;

        // Iniciar a corrotina para instanciar flares
        if (flareCoroutine != null)
        {
            StopCoroutine(flareCoroutine);
        }
        flareCoroutine = StartCoroutine(InstantiateFlareParticles());
    }

    private IEnumerator InstantiateFlareParticles()
    {
        float spawnInterval = 0.2f; // Intervalo entre spawns
        float timer = 0f;

        // Continuar enquanto o countermeasure estiver ativo
        while (countermeasures_duration >= 0 && vehicle.used_locking_countermeasure)
        {
            timer += Time.deltaTime;

            // A cada 0.5 segundos, instanciar um novo flare
            if (timer >= spawnInterval)
            {
                InstantiateFlare();
                timer = 0f; // Resetar o timer
            }

            yield return null;
        }
    }

    private void InstantiateFlare()
    {
        if (flare_effect == null)
        {
            Debug.LogWarning("Flare effect prefab não atribuído!");
            return;
        }

        // Instanciar o primeiro flare (direita)
        GameObject flareInstance1 = Instantiate(
            flare_effect,
            transform.position,
            Quaternion.identity
        );

        // Instanciar o segundo flare (esquerda)
        GameObject flareInstance2 = Instantiate(
            flare_effect,
            transform.position,
            Quaternion.identity
        );

        // Adicionar Rigidbody e aplicar força ao primeiro flare (direita)
        AddRigidbodyAndForce(flareInstance1, transform.right);

        // Adicionar Rigidbody e aplicar força ao segundo flare (esquerda)
        AddRigidbodyAndForce(flareInstance2, -transform.right);

        // Destruir os flares após um tempo
        Destroy(flareInstance1, 5f);
        Destroy(flareInstance2, 5f);
    }

    private void AddRigidbodyAndForce(GameObject flare, Vector3 direction)
    {

        Rigidbody rb = flare.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = flare.AddComponent<Rigidbody>();
        }

        rb.useGravity = true;
        rb.linearDamping = 0.5f; 
        rb.angularDamping = 0.5f;

        rb.AddForce(direction * force_multiplier, ForceMode.Impulse);

        rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);

    }
}