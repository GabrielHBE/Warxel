using FishNet.Object;
using UnityEngine;

public class ParticlesBehaviour : NetworkBehaviour
{
    [SerializeField] protected float destroyTimer = 5f;

    // O FishNet chama esse método automaticamente APENAS no Servidor 
    // assim que o objeto é criado (spawnado) na rede.
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // Inicia a contagem de tempo de forma super leve (sem precisar de Update)
        Invoke(nameof(DestroyAfterTime), destroyTimer);
    }

    protected void DestroyAfterTime()
    {
        if (IsSpawned)
        {
            // O comando Despawn no servidor avisa todos os Clients para 
            // destruírem este GameObject em suas próprias telas de forma limpa.
            Despawn(gameObject); 
        }
        else
        {
            // Fallback caso ele não tenha sido spawnado na rede por algum motivo
            Destroy(gameObject);
        }
    }
}