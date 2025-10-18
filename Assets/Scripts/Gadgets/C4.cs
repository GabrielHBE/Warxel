using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class C4 : MonoBehaviour
{
    public bool is_active;
    [SerializeField] private KeyBinds keyBinds;
    [SerializeField] private C4Explosive c4Explosive;
    [SerializeField] private int c4_qtd;
    [SerializeField] private float throw_c4_force;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private Transform hand;
    [SerializeField] private float pick_up_c4_distance;
    [SerializeField] private GameObject right_hand;
    [SerializeField] private GameObject right_hand_pos;
    [SerializeField] private AudioSource beepSound;

    private List<C4Explosive> c4_list = new List<C4Explosive>(); // Mudança: lista de componentes em vez de GameObjects

    [Header("KeyCodes")]
    public KeyCode pick_up_c4_key;
    public KeyCode throw_c4_key;
    public KeyCode detonate_c4_key;

    [Header("Detonation timing (seconds)")]
    [SerializeField] private float initialDetonateDelay;
    [SerializeField] private float perC4Delay;

    private bool isDetonating = false;
    private float detonateTimer = 0f;
    private int detonateIndex = 0;

    void Update()
    {
        if (!is_active) return;

        hand.transform.position = transform.position;
        right_hand.transform.position = right_hand_pos.transform.position;

        if (c4_qtd > 0 && Input.GetKeyDown(keyBinds.throwC4Key))
        {
            Throw_C4();
        }

        if (Input.GetKeyDown(keyBinds.interactKey))
        {
            TryPickUpC4();
        }

        // Iniciar detonação se houver C4s ativos
        if (Input.GetKeyDown(keyBinds.detonateC4Key) && !isDetonating)
        {
            CleanupDestroyedC4s(); // Limpa C4s destruídos antes de começar
            
            if (c4_list.Count > 0)
            {
                beepSound?.Play();
                StartDetonationSequence();
            }
        }

        // Processar detonação sequencial
        if (isDetonating)
        {
            detonateTimer += Time.deltaTime;

            if (detonateIndex == 0) // Primeira detonação
            {
                if (detonateTimer >= initialDetonateDelay)
                {
                    DetonateNextC4();
                }
            }
            else // Demais detonações
            {
                if (detonateTimer >= perC4Delay)
                {
                    DetonateNextC4();
                }
            }
        }
    }

    private void StartDetonationSequence()
    {
        isDetonating = true;
        detonateTimer = 0f;
        detonateIndex = 0;
    }

    private void EndDetonationSequence()
    {
        isDetonating = false;
        detonateTimer = 0f;
        detonateIndex = 0;
        CleanupDestroyedC4s(); // Limpa a lista após terminar
    }

    private void DetonateNextC4()
    {
        // Verifica se ainda há C4s para detonar
        if (detonateIndex >= c4_list.Count || c4_list[detonateIndex] == null)
        {
            EndDetonationSequence();
            return;
        }
        
        // Detona o C4 atual
        C4Explosive c4ToDetonate = c4_list[detonateIndex];
        if (c4ToDetonate != null)
        {
            c4ToDetonate.Detonate();
        }

        detonateIndex++;
        detonateTimer = 0f;

        // Verifica se é o último C4
        if (detonateIndex >= c4_list.Count)
        {
            EndDetonationSequence();
        }
    }

    private void CleanupDestroyedC4s()
    {
        // Remove C4s que foram destruídos manualmente
        c4_list.RemoveAll(c4 => c4 == null || c4.gameObject == null);
    }

    private void TryPickUpC4()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, pick_up_c4_distance))
        {
            C4Explosive c4 = hit.collider.GetComponent<C4Explosive>();
            if (c4 != null)
            {
                PickUpC4();
                
                // Remove da lista se estiver nela
                if (c4_list.Contains(c4))
                {
                    c4_list.Remove(c4);
                }
                
                Destroy(hit.collider.gameObject);
            }
        }
    }

    private void PickUpC4()
    {
        c4_qtd += 1;
    }

    private void Throw_C4()
    {
        c4_qtd -= 1;

        // Instanciar o C4
        GameObject c4Instance = Instantiate(c4Explosive.gameObject, throwPoint.position, throwPoint.rotation);
        C4Explosive newC4 = c4Instance.GetComponent<C4Explosive>();
        c4_list.Add(newC4); // Adiciona o componente à lista

        // Garantir que está ativo
        c4Instance.SetActive(true);

        // Configurar Rigidbody
        Rigidbody rb = c4Instance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = c4Instance.AddComponent<Rigidbody>();
        }

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Aplicar força
        rb.AddForce(throwPoint.forward * throw_c4_force, ForceMode.Impulse);

    }

    private void OnDrawGizmos()
    {
        if (throwPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(throwPoint.position, 0.1f);
            Gizmos.DrawLine(throwPoint.position, throwPoint.position + throwPoint.forward * 2f);
        }
    }
}