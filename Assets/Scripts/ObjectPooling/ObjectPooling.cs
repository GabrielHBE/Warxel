using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;

public class ObjectPooling : MonoBehaviour
{
    public static ObjectPooling Instance { get; private set; }

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

    private Dictionary<GameObject, List<GameObject>> localPoolDictionary = new Dictionary<GameObject, List<GameObject>>();

    // NOVO: Dicionário para guardar as "pastas" (GameObjects vazios) organizadoras na Hierarchy
    private Dictionary<GameObject, Transform> poolFolders = new Dictionary<GameObject, Transform>();

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
            if (item.prefab == null || item.quantity <= 0) continue;

            if (!localPoolDictionary.ContainsKey(item.prefab))
            {
                localPoolDictionary[item.prefab] = new List<GameObject>();
            }

            // --- CRIAÇÃO DA PASTA ORGANIZADORA ---
            GameObject folder = new GameObject($"[Pool] {item.prefab.name}");
            folder.transform.SetParent(transform);
            poolFolders[item.prefab] = folder.transform;
            // -------------------------------------

            for (int i = 0; i < item.quantity; i++)
            {
                // Instancia o objeto já dentro da sua respectiva pasta
                GameObject obj = Instantiate(item.prefab, folder.transform);
                obj.SetActive(false);

                // Avisa ao objeto qual é a pasta dele para quando ele for desativado
                if (obj.TryGetComponent(out LocalPooledObject pooledObj))
                {
                    pooledObj.SetupPoolParent(folder.transform);
                }

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
                InstanceFinder.NetworkManager.CacheObjects(item.prefab, item.quantity, false);
            }
        }
        else
        {
            Debug.LogError("[ObjectPooling] NetworkManager não foi encontrado! Garanta que ele já existe na cena.");
        }
    }

    public GameObject GetLocalPooledItem(GameObject prefab)
    {
        if (prefab == null) return null;

        // Se o prefab não existir no dicionário, nós o criamos dinamicamente!
        if (!localPoolDictionary.TryGetValue(prefab, out List<GameObject> poolList))
        {
            Debug.Log($"[ObjectPooling] O prefab '{prefab.name}' não estava na lista do Inspector. Criando Pool dinamicamente...");

            poolList = new List<GameObject>();
            localPoolDictionary[prefab] = poolList;

            // Cria a pasta organizadora
            GameObject folder = new GameObject($"[Pool] {prefab.name}");
            folder.transform.SetParent(transform);
            poolFolders[prefab] = folder.transform;
        }

        // Procura um item desativado para usar
        foreach (GameObject obj in poolList)
        {
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
        }

        // Transbordo: Se todos estiverem em uso, cria um novo
        Transform folderTransform = poolFolders[prefab];

        GameObject newObj = Instantiate(prefab, folderTransform);
        newObj.SetActive(false);

        if (newObj.TryGetComponent(out LocalPooledObject pooledObj))
        {
            pooledObj.SetupPoolParent(folderTransform);
        }

        poolList.Add(newObj);
        return newObj;
    }
}