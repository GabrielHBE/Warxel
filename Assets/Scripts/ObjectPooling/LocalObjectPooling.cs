using System.Collections.Generic;
using UnityEngine;

public class LocalObjectPooling : MonoBehaviour
{
    public static LocalObjectPooling Instance { get; private set; }

    [System.Serializable]
    public struct LocalPoolSettings
    {
        public GameObject prefab;
        public int quantity;
    }

    [Header("Local Pooling")]
    [SerializeField] private List<LocalPoolSettings> localPooledItems = new List<LocalPoolSettings>();

    private Dictionary<GameObject, List<GameObject>> poolDictionary = new Dictionary<GameObject, List<GameObject>>();
    private Dictionary<GameObject, Transform> poolFolders = new Dictionary<GameObject, Transform>();

    private bool isInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializePools();
    }

    public void InitializePools()
    {
        if (isInitialized) return;
        isInitialized = true;

        foreach (LocalPoolSettings item in localPooledItems)
        {
            if (item.prefab == null || item.quantity <= 0) continue;

            if (!poolDictionary.ContainsKey(item.prefab))
            {
                poolDictionary[item.prefab] = new List<GameObject>();
            }

            GameObject folder = new GameObject($"[Local Pool] {item.prefab.name}");
            folder.transform.SetParent(transform);
            poolFolders[item.prefab] = folder.transform;

            for (int i = 0; i < item.quantity; i++)
            {
                GameObject obj = Instantiate(item.prefab, folder.transform);
                obj.SetActive(false);

                if (obj.TryGetComponent(out LocalPooledObject pooledObj))
                {
                    pooledObj.SetupPoolParent(folder.transform);
                }

                poolDictionary[item.prefab].Add(obj);
            }
        }
    }

    public GameObject GetPooledItem(GameObject prefab)
    {
        if (prefab == null) return null;

        // Se o prefab não existir no dicionário, cria o pool dinamicamente
        if (!poolDictionary.TryGetValue(prefab, out List<GameObject> poolList))
        {
            Debug.Log($"[LocalObjectPooling] Criando pool dinâmico para '{prefab.name}'...");

            poolList = new List<GameObject>();
            poolDictionary[prefab] = poolList;

            GameObject folder = new GameObject($"[Local Pool] {prefab.name}");
            folder.transform.SetParent(transform);
            poolFolders[prefab] = folder.transform;
        }

        // Procura um item desativado
        foreach (GameObject obj in poolList)
        {
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
        }

        // Transbordo: cria um novo
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

    // Método para limpar o pool (opcional)
    public void ClearPool(GameObject prefab)
    {
        if (poolDictionary.TryGetValue(prefab, out List<GameObject> poolList))
        {
            foreach (GameObject obj in poolList)
            {
                if (obj != null)
                    Destroy(obj);
            }
            poolList.Clear();
            poolDictionary.Remove(prefab);
        }
    }

    public void ClearAllPools()
    {
        foreach (var pool in poolDictionary.Values)
        {
            foreach (GameObject obj in pool)
            {
                if (obj != null)
                    Destroy(obj);
            }
        }
        poolDictionary.Clear();
        poolFolders.Clear();
    }
}