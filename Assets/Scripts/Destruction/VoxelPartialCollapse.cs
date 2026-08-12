using System.Linq;
using FishNet.Component.Transforming;
using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform))]
public class VoxelPartialCollapse : VoxelDestruction
{
    [SerializeField] protected VoxelPartialCollapse[] chainCollapse;
    [SerializeField] protected DamageModelSwap[] damageModelSwap;

    private MeshCollider physicsMeshCllider;

    private void OnDestroy()
    {
        damageTaken.OnChange -= OnDamageTakenChanged;
    }

    protected override void Start()
    {
        base.Start();
        physicsMeshCllider = GetComponents<MeshCollider>().FirstOrDefault(collider => collider.convex);
        if (physicsMeshCllider == null)
        {
            physicsMeshCllider = gameObject.AddComponent<MeshCollider>();
            physicsMeshCllider.convex = true;
        }
        physicsMeshCllider.enabled = false;
        damageTaken.OnChange += OnDamageTakenChanged;
    }

    // Callback chamado automaticamente em todos os clientes e servidor quando damageTaken muda
    private void OnDamageTakenChanged(float prev, float next, bool asServer)
    {
        UpdateModel(next);
    }

    private void UpdateModel(float currentDamage)
    {
        if (damageModelSwap == null || damageModelSwap.Length == 0) return;

        foreach (var swap in damageModelSwap)
        {
            if (swap.damageRange == null || swap.damageRange.Length == 0) continue;

            // Pega o valor mínimo. Se houver um segundo valor no array, usa como máximo.
            float minDamage = swap.damageRange[0];
            float maxDamage = swap.damageRange.Length > 1 ? swap.damageRange[1] : float.MaxValue;

            // Se o dano atual estiver dentro do range configurado
            if (currentDamage >= minDamage && currentDamage <= maxDamage)
            {
                // Atualiza o Mesh Visual
                if (swap.mesh != null)
                {
                    meshFilter.sharedMesh = swap.mesh;
                    meshCollider.sharedMesh = swap.mesh;
                }

                // Atualiza o Material Visual
                if (swap.material != null)  meshRenderer.sharedMaterial = swap.material;
                

                break;
            }
        }
    }

    [Server]
    public override void Destroy()
    {
        damageTaken.Value = damageToDestroy;
        isDestroyed.Value = true;

        // Verifica se existe um objeto encadeado e se ele não é nulo
        if (chainCollapse != null && chainCollapse.Length > 0)
        {
            foreach (VoxelPartialCollapse vox in chainCollapse)
            {
                vox.Damage(vox.damageToDestroy);
            }
        }

        meshCollider.enabled = false;
        physicsMeshCllider.enabled = true;

        rb.isKinematic = false;
        // Adiciona torque aleatório para rotação
        ApplyRandomTorque();
    }


    void OnCollisionEnter(Collision collision)
    {
        if (!IsServerInitialized || !isDestroyed.Value) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Vehicle")) ProcessHit.VehicleHit(collision.gameObject, rb.linearVelocity.magnitude, gameObject);
        if (collision.gameObject.layer == LayerMask.NameToLayer("PlayerHitBox")) ProcessHit.PlayerHit(collision.gameObject, rb.linearVelocity.magnitude, 1, gameObject);
        
    }

    [Server]
    public void ResetCollapse()
    {
        isDestroyed.Value = false;
        damageTaken.Value = 0; // Isso vai disparar o OnChange novamente e restaurar o Mesh inicial se configurado no range 0

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    [System.Serializable]
    public struct DamageModelSwap
    {
        public Mesh mesh;
        public Material material;
        public float[] damageRange;
    }
}