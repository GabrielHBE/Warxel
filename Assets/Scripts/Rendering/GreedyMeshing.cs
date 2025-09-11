using UnityEngine;
using System.Collections.Generic;

public class GreedyMeshing : MonoBehaviour
{
    [Header("Greedy Meshing Settings")]
    public bool combineOnStart = true;
    public bool destroyOriginalObjects = true;
    public bool use32BitIndexFormat = true;
    public int maxVerticesPerMesh = 65000;
    public bool preserveWorldPosition = true; // Nova opção importante

    [Header("Backup Settings")]
    public bool enableBackup = true;
    public bool autoRebuild = true;

    private List<MeshBackup> meshBackups = new List<MeshBackup>();

    [System.Serializable]
    private struct MeshBackup
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Material material;
        public Mesh mesh;
    }

    void Start()
    {
        if (combineOnStart)
        {
            ApplyGreedyMeshing();
        }
    }

    // Método para reconstruir a partir do backup
    public void RebuildFromBackup()
    {
        if (meshBackups.Count == 0)
        {
            Debug.LogWarning("Nenhum backup disponível para reconstrução");
            return;
        }

        // Limpar objetos existentes
        ClearCombinedMeshes();

        // Recriar objetos individuais
        foreach (MeshBackup backup in meshBackups)
        {
            GameObject newObj = new GameObject("Rebuilt_Object");
            newObj.transform.SetParent(transform);
            newObj.transform.localPosition = backup.position;
            newObj.transform.localRotation = backup.rotation;
            newObj.transform.localScale = backup.scale;

            MeshFilter meshFilter = newObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = backup.mesh;

            MeshRenderer renderer = newObj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = backup.material;
        }

        // Reaplicar greedy meshing
        if (autoRebuild)
        {
            ApplyGreedyMeshing();
        }

        Debug.Log($"Reconstruídos {meshBackups.Count} objetos");
    }

    // Método melhorado para limpar meshes combinadas
    public void ClearCombinedMeshes()
    {
        List<GameObject> toDestroy = new List<GameObject>();

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("CombinedMesh_"))
            {
                toDestroy.Add(child.gameObject);
            }
        }

        foreach (GameObject obj in toDestroy)
        {
            if (obj != null)
            {
                // Destruir mesh para evitar memory leak
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    DestroyImmediate(meshFilter.sharedMesh);
                }

                DestroyImmediate(obj);
            }
        }

        Resources.UnloadUnusedAssets();
    }

    public void ApplyGreedyMeshing()
    {
        Dictionary<Material, List<GameObject>> objectsByMaterial = GroupChildrenByMaterial();

        foreach (var materialGroup in objectsByMaterial)
        {
            if (materialGroup.Value.Count > 1)
            {
                CombineObjectsWithSameMaterial(materialGroup.Value, materialGroup.Key);
            }
        }

        Debug.Log("Greedy meshing aplicado com sucesso!");
    }

    private Dictionary<Material, List<GameObject>> GroupChildrenByMaterial()
    {
        var materialGroups = new Dictionary<Material, List<GameObject>>();

        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                MeshFilter meshFilter = child.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();

                if (renderer != null && meshFilter != null && meshFilter.sharedMesh != null && meshRenderer.enabled)
                {
                    Material material = renderer.sharedMaterial;

                    if (material != null)
                    {
                        if (!materialGroups.ContainsKey(material))
                        {
                            materialGroups[material] = new List<GameObject>();
                        }
                        materialGroups[material].Add(child.gameObject);
                    }
                }
            }
        }

        return materialGroups;
    }

    private void CombineObjectsWithSameMaterial(List<GameObject> objects, Material material)
    {
        List<MeshInfo> meshInfos = new List<MeshInfo>();
        int totalVertices = 0;

        foreach (GameObject obj in objects)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Mesh mesh = meshFilter.sharedMesh;
                int vertexCount = mesh.vertexCount;

                // Calcular a matriz de transformação CORRETAMENTE
                Matrix4x4 transformMatrix;
                if (preserveWorldPosition)
                {
                    // Preserva a posição mundial relativa ao pai
                    transformMatrix = obj.transform.localToWorldMatrix;
                    transformMatrix = transform.worldToLocalMatrix * transformMatrix;
                }
                else
                {
                    // Usa a transformação local original
                    transformMatrix = meshFilter.transform.localToWorldMatrix;
                }

                meshInfos.Add(new MeshInfo
                {
                    mesh = mesh,
                    transform = transformMatrix,
                    vertexCount = vertexCount,
                    originalObject = obj
                });

                totalVertices += vertexCount;
            }
        }

        if (meshInfos.Count == 0) return;

        if (totalVertices > maxVerticesPerMesh)
        {
            CombineInBatches(meshInfos, objects, material);
        }
        else
        {
            CombineSingleMesh(meshInfos, objects, material);
        }
    }

    private void CombineSingleMesh(List<MeshInfo> meshInfos, List<GameObject> originalObjects, Material material)
    {
        List<CombineInstance> combineInstances = new List<CombineInstance>();

        foreach (MeshInfo meshInfo in meshInfos)
        {
            CombineInstance combineInstance = new CombineInstance();
            combineInstance.mesh = meshInfo.mesh;
            combineInstance.transform = meshInfo.transform;
            combineInstances.Add(combineInstance);
        }

        Mesh combinedMesh = new Mesh();

        if (use32BitIndexFormat)
        {
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        combinedMesh.CombineMeshes(combineInstances.ToArray(), true);
        combinedMesh.RecalculateBounds();
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateTangents();

        CreateCombinedObject(combinedMesh, material, "CombinedMesh_" + material.name);
        HandleOriginalObjects(originalObjects);
    }

    private void CombineInBatches(List<MeshInfo> meshInfos, List<GameObject> originalObjects, Material material)
    {
        List<List<MeshInfo>> batches = new List<List<MeshInfo>>();
        List<MeshInfo> currentBatch = new List<MeshInfo>();
        int currentVertexCount = 0;

        foreach (MeshInfo meshInfo in meshInfos)
        {
            if (currentVertexCount + meshInfo.vertexCount > maxVerticesPerMesh && currentBatch.Count > 0)
            {
                batches.Add(currentBatch);
                currentBatch = new List<MeshInfo>();
                currentVertexCount = 0;
            }

            currentBatch.Add(meshInfo);
            currentVertexCount += meshInfo.vertexCount;
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        for (int i = 0; i < batches.Count; i++)
        {
            List<CombineInstance> combineInstances = new List<CombineInstance>();

            foreach (MeshInfo meshInfo in batches[i])
            {
                CombineInstance combineInstance = new CombineInstance();
                combineInstance.mesh = meshInfo.mesh;
                combineInstance.transform = meshInfo.transform;
                combineInstances.Add(combineInstance);
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combineInstances.ToArray(), true);
            combinedMesh.RecalculateBounds();
            combinedMesh.RecalculateNormals();
            combinedMesh.RecalculateTangents();

            CreateCombinedObject(combinedMesh, material, $"CombinedMesh_{material.name}_Batch_{i + 1}");
        }

        HandleOriginalObjects(originalObjects);
    }

    private void CreateCombinedObject(Mesh mesh, Material material, string name)
    {
        GameObject combinedObject = new GameObject(name);
        combinedObject.transform.SetParent(transform);

        // Manter a posição, rotação e escala do pai
        combinedObject.transform.localPosition = Vector3.zero;
        combinedObject.transform.localRotation = Quaternion.identity;
        combinedObject.transform.localScale = Vector3.one;

        MeshFilter newMeshFilter = combinedObject.AddComponent<MeshFilter>();
        newMeshFilter.sharedMesh = mesh;

        MeshRenderer newRenderer = combinedObject.AddComponent<MeshRenderer>();
        newRenderer.sharedMaterial = material;

        // Adicionar MeshCollider se necessário
        if (HasAnyColliderInOriginalObjects())
        {
            MeshCollider collider = combinedObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }
    }

    private bool HasAnyColliderInOriginalObjects()
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Collider>() != null)
            {
                return true;
            }
        }
        return false;
    }

    private void HandleOriginalObjects(List<GameObject> originalObjects)
    {
        if (destroyOriginalObjects)
        {
            foreach (GameObject obj in originalObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
        }
        else
        {
            foreach (GameObject obj in originalObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }

    private struct MeshInfo
    {
        public Mesh mesh;
        public Matrix4x4 transform;
        public int vertexCount;
        public GameObject originalObject;
    }

    // Método alternativo: Combinar mantendo as transformações individuais
    public void CombineWithIndividualTransforms()
    {
        Dictionary<Material, List<GameObject>> objectsByMaterial = GroupChildrenByMaterial();

        foreach (var materialGroup in objectsByMaterial)
        {
            if (materialGroup.Value.Count > 1)
            {
                // Para cada objeto, criar uma mesh combinada individual
                foreach (GameObject obj in materialGroup.Value)
                {
                    CombineSingleObject(obj, materialGroup.Key);
                }
            }
        }
    }

    private void CombineSingleObject(GameObject obj, Material material)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        CombineInstance combineInstance = new CombineInstance();
        combineInstance.mesh = meshFilter.sharedMesh;
        combineInstance.transform = obj.transform.localToWorldMatrix;

        combinedMesh.CombineMeshes(new CombineInstance[] { combineInstance }, true);

        GameObject combinedObject = new GameObject("Combined_" + obj.name);
        combinedObject.transform.SetParent(transform);
        combinedObject.transform.position = obj.transform.position;
        combinedObject.transform.rotation = obj.transform.rotation;
        combinedObject.transform.localScale = obj.transform.localScale;

        MeshFilter newMeshFilter = combinedObject.AddComponent<MeshFilter>();
        newMeshFilter.sharedMesh = combinedMesh;

        MeshRenderer newRenderer = combinedObject.AddComponent<MeshRenderer>();
        newRenderer.sharedMaterial = material;

        obj.SetActive(false);
    }

    public void DebugTransforms()
    {
        foreach (Transform child in transform)
        {
            Debug.Log($"Object: {child.name}, Position: {child.position}, Local Position: {child.localPosition}");
        }
    }
}