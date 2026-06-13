using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;

public class ObjectPooling : MonoBehaviour
{
    public static ObjectPooling Instance {get; private set;}

    // Estruturas criadas para exibir o Prefab + Quantidade perfeitamente no Inspector
    [System.Serializable]
    public struct LocalPoolSettings
    {
        public GameObject prefab;
        public int quantity;
    }

    [System.Serializable]
    public struct ServerPoolSettings
    {
        public NetworkObject prefab;
        public int quantity;
    }

    [Header("Local Pooling")]
    [SerializeField] private List<LocalPoolSettings> localPooledItens = new List<LocalPoolSettings>();

    [Header("Server Pooling")]
    [SerializeField] private List<ServerPoolSettings> serverPooledItens = new List<ServerPoolSettings>();

    // Dicionário para organizar os objetos instanciados locais pelo seu Prefab de origem
    private Dictionary<GameObject, List<GameObject>> localPoolDictionary = new Dictionary<GameObject, List<GameObject>>();

    void Start()
    {
        Instance = this;
        StartServerPooling();
        StartLocalPooling();
    }

    private void StartLocalPooling()
    {
        foreach (LocalPoolSettings item in localPooledItens)
        {
            // Proteção básica para não tentar instanciar itens nulos ou vazios
            if (item.prefab == null || item.quantity <= 0) continue;

            // Se o Prefab ainda não tem uma lista no dicionário, cria uma nova
            if (!localPoolDictionary.ContainsKey(item.prefab))
            {
                localPoolDictionary[item.prefab] = new List<GameObject>();
            }

            // Instancia a quantidade exata definida no Inspector
            for (int i = 0; i < item.quantity; i++)
            {
                GameObject obj = Instantiate(item.prefab, transform);
                obj.SetActive(false); // Mantém desativado no pool
                localPoolDictionary[item.prefab].Add(obj);
            }
        }
    }

    private void StartServerPooling()
    {
        if (InstanceFinder.NetworkManager != null)
        {
            foreach (ServerPoolSettings item in serverPooledItens)
            {
                if (item.prefab == null || item.quantity <= 0) continue;

                // Passamos a quantidade dinâmica vinda do Inspector para o FishNet
                InstanceFinder.NetworkManager.CacheObjects(item.prefab, item.quantity, false);
            }
        }
        else
        {
            Debug.LogError("[ObjectPooling] NetworkManager não foi encontrado! Garanta que ele já existe na cena.");
        }
    }

    /// <summary>
    /// Função pública para você pegar um objeto do Pool Local quando precisar.
    /// </summary>
    public GameObject GetLocalPooledItem(GameObject prefab)
    {
        if (localPoolDictionary.TryGetValue(prefab, out List<GameObject> poolList))
        {
            // Procura um objeto que não esteja ativo na cena
            foreach (GameObject obj in poolList)
            {
                if (!obj.activeInHierarchy)
                {
                    return obj;
                }
            }

            // Opcional (Transbordo): Se todos os objetos estiverem ativos e você precisar de mais um,
            // ele cria um extra dinamicamente para o jogo não quebrar.
            GameObject newObj = Instantiate(prefab, transform);
            newObj.SetActive(false);
            poolList.Add(newObj);
            return newObj;
        }

        Debug.LogWarning($"[ObjectPooling] O prefab {prefab.name} não foi pré-aquecido no Local Pooling!");
        return null;
    }
}