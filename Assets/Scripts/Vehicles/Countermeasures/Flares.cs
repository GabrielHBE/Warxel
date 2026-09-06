using System.Collections;
using FishNet.Object;
using NUnit.Framework;
using UnityEngine;

public class Flares : Countermeasures
{
    [SerializeField] private GameObject flare_effect;
    private Coroutine flareCoroutine;
    private float force_multiplier = 15;

    public override void LocalUpdate()
    {
        if (reload_countermeasures_duration <= 0)
        {
            reloading = false;
            return;
        }

        if (vehicle.used_locking_countermeasure == false)
        {
            reloading = true;
            reload_countermeasures_duration -= Time.deltaTime;
        }
        else
        {
            reloading = false;
            countermeasures_duration -= Time.deltaTime;
            if (countermeasures_duration <= 0) StopCountermeasure();
            
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
        if (flareCoroutine != null) StopCoroutine(flareCoroutine);
        
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

    [ServerRpc(RequireOwnership = false)]
    private void InstantiateFlare()
    {
        if (flare_effect == null)
        {
            Debug.LogWarning("Flare effect prefab is not assigned!");
            return;
        }

        // Instanciar o primeiro flare (direita)
        GameObject flareInstance1 = Instantiate(
            flare_effect,
            transform.position,
            Quaternion.identity
        );
        Spawn(flareInstance1);

        // Instanciar o segundo flare (esquerda)
        GameObject flareInstance2 = Instantiate(
            flare_effect,
            transform.position,
            Quaternion.identity
        );

        Spawn(flareInstance2);

        AddRigidbodyAndForce(flareInstance1, transform.right);
        AddRigidbodyAndForce(flareInstance2, -transform.right);
    }

    [ObserversRpc]
    private void AddRigidbodyAndForce(GameObject flare, Vector3 direction)
    {
        Rigidbody rb = flare.GetComponent<Rigidbody>();
        if (rb == null)  rb = flare.AddComponent<Rigidbody>();

        rb.useGravity = true;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        rb.AddForce(direction * force_multiplier, ForceMode.Impulse);
    }
}
