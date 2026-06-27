using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;

public class ServerObjectPooling : NetworkBehaviour
{
    public static ServerObjectPooling Instance {get; private set;}
    [System.Serializable]
    public struct ServerPoolSettings
    {
        public NetworkObject prefab;
        public int quantity;
    }

    [Header("Server Pooling")]
    [SerializeField] private List<ServerPoolSettings> serverPooledItems = new List<ServerPoolSettings>();

    private bool isInitialized = false;


    public override void OnStartClient()
    {
        base.OnStartClient();
        Instance = this;
        // Aguarda o NetworkManager estar pronto
        if (InstanceFinder.NetworkManager != null)
        {
            if (InstanceFinder.NetworkManager.IsServerStarted)
            {
                InitializeServerPools();
            }
            else
            {
                // Se não for servidor, desativa este GameObject
                Debug.Log("[ServerObjectPooling] Não é servidor, desativando...");
                gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("[ServerObjectPooling] NetworkManager não encontrado!");
            gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        // Tenta inicializar novamente se for reativado
        if (!isInitialized && InstanceFinder.NetworkManager != null && InstanceFinder.NetworkManager.IsServerStarted)
        {
            InitializeServerPools();
        }
    }

    public void InitializeServerPools()
    {
        if (isInitialized) return;
        
        if (InstanceFinder.NetworkManager == null)
        {
            Debug.LogError("[ServerObjectPooling] NetworkManager é nulo!");
            return;
        }

        if (!InstanceFinder.NetworkManager.IsServerStarted)
        {
            Debug.LogWarning("[ServerObjectPooling] Não é servidor, não é possível inicializar pools!");
            return;
        }

        isInitialized = true;

        Debug.Log("[ServerObjectPooling] Inicializando pools do servidor...");

        foreach (ServerPoolSettings item in serverPooledItems)
        {
            if (item.prefab == null || item.quantity <= 0) continue;

            try
            {
                InstanceFinder.NetworkManager.CacheObjects(item.prefab, item.quantity, false);
                Debug.Log($"[ServerObjectPooling] Pool criado para: {item.prefab.name} (x{item.quantity})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ServerObjectPooling] Erro ao criar pool para {item.prefab.name}: {e.Message}");
            }
        }
    }

    // Método para verificar se o pool está inicializado
    public bool IsInitialized()
    {
        return isInitialized;
    }

    // Método para reinicializar (se necessário)
    public void Reinitialize()
    {
        isInitialized = false;
        InitializeServerPools();
    }


    void OnDestroy()
    {
        // Limpa os pools do servidor quando destruído
        if (InstanceFinder.NetworkManager != null && InstanceFinder.NetworkManager.IsServerStarted)
        {
            foreach (ServerPoolSettings item in serverPooledItems)
            {
                if (item.prefab == null) continue;
                try
                {
                    // Nota: FishNet não tem um método ClearCache, mas os objetos serão limpos naturalmente
                    Debug.Log($"[ServerObjectPooling] Limpando pool para: {item.prefab.name}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[ServerObjectPooling] Erro ao limpar pool: {e.Message}");
                }
            }
        }
    }
}