using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;

public class ServerObjectPooling : MonoBehaviour
{
    public static ServerObjectPooling Instance { get; private set; }
    
    [System.Serializable]
    public struct ServerPoolSettings
    {
        public NetworkObject prefab;
        public int quantity;
    }

    [Header("Server Pooling")]
    [SerializeField] private List<ServerPoolSettings> serverPooledItems = new List<ServerPoolSettings>();
    
    [Header("Update Settings")]
    [SerializeField] private bool enableLocalUpdates = true;
    [SerializeField] private int maxUpdatesPerFrame = 50; // Limite para evitar picos de performance
    
    // Lista de objetos poolados ativos que precisam de update
    private List<NetworkPooledObject> activePooledObjects = new List<NetworkPooledObject>();
    private List<NetworkPooledObject> objectsToRemove = new List<NetworkPooledObject>();
    
    // Cache para evitar alocações
    private int updateIndex = 0;
    private bool isInitialized = false;

    void Awake()
    {
        Instance = this;
        if (!isInitialized && InstanceFinder.NetworkManager != null && InstanceFinder.NetworkManager.IsServerStarted)
        {
            InitializeServerPools();
        }
    }

    void Update()
    {
        if (!enableLocalUpdates || !isInitialized) return;
        
        // Processa updates em lotes para evitar picos de performance
        ProcessLocalUpdates();
        
        // Limpa objetos marcados para remoção
        CleanupRemovedObjects();
    }

    private void ProcessLocalUpdates()
    {
        if (activePooledObjects.Count == 0) return;
        
        int processed = 0;
        
        // Processa em lotes para distribuir a carga
        while (processed < maxUpdatesPerFrame && updateIndex < activePooledObjects.Count)
        {
            NetworkPooledObject obj = activePooledObjects[updateIndex];
            
            // Verifica se o objeto ainda está ativo antes de processar
            if (obj != null && obj.gameObject.activeInHierarchy)
            {
                obj.LocalUpdate();
                processed++;
            }
            
            updateIndex++;
        }
        
        // Se chegamos ao fim da lista, reiniciamos o índice
        if (updateIndex >= activePooledObjects.Count)
        {
            updateIndex = 0;
        }
    }

    private void CleanupRemovedObjects()
    {
        if (objectsToRemove.Count > 0)
        {
            foreach (var obj in objectsToRemove)
            {
                activePooledObjects.Remove(obj);
            }
            objectsToRemove.Clear();
        }
    }

    // Método chamado quando um objeto é ativado (Enable)
    public void RegisterPooledObject(NetworkPooledObject obj)
    {
        if (obj == null || !enableLocalUpdates) return;
        
        // Verifica se o objeto já está registrado
        if (!activePooledObjects.Contains(obj))
        {
            activePooledObjects.Add(obj);
        }
    }

    // Método chamado quando um objeto é desativado (Disable)
    public void UnregisterPooledObject(NetworkPooledObject obj)
    {
        if (obj == null) return;
        
        // Marca para remoção na próxima limpeza (evita modificar a lista durante iteração)
        if (!objectsToRemove.Contains(obj))
        {
            objectsToRemove.Add(obj);
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
        Debug.Log($"[ServerObjectPooling] Inicializando pools do servidor com {serverPooledItems.Count} itens...");

        foreach (ServerPoolSettings item in serverPooledItems)
        {
            if (item.prefab == null || item.quantity <= 0) continue;

            try
            {
                InstanceFinder.NetworkManager.CacheObjects(item.prefab, item.quantity, false);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ServerObjectPooling] Erro ao criar pool para {item.prefab.name}: {e.Message}");
            }
        }
    }

    // Método para limpar todos os objetos registrados (útil para reinicialização)
    public void ClearRegisteredObjects()
    {
        activePooledObjects.Clear();
        objectsToRemove.Clear();
        updateIndex = 0;
    }

    public bool IsInitialized()
    {
        return isInitialized;
    }

    public void Reinitialize()
    {
        ClearRegisteredObjects();
        isInitialized = false;
        InitializeServerPools();
    }

    // Método para obter estatísticas (debug)
    public string GetPoolStats()
    {
        return $"Active Objects: {activePooledObjects.Count}, Updates per frame: {maxUpdatesPerFrame}, Current Index: {updateIndex}";
    }
}