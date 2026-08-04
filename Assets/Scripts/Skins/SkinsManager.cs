using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SkinsManager : PersistentLocalSingleton<SkinsManager>
{
    private string rootFolder => "Assets/Prefabs/Skins/Models";

    [SerializeField]
    private List<Skin> internalSkinList = new List<Skin>();

    // Dicionário de cache estático para acesso rápido
    private readonly static Dictionary<SkinCacheKey, Skin> skinCache = new Dictionary<SkinCacheKey, Skin>();

    public struct SkinCacheKey
    {
        public string skinName;
        public ClassManager.Class skinClass;
    }

    protected override void Awake()
    {
        base.Awake();
        InitializeSkinCache();
    }

    private void InitializeSkinCache()
    {
        skinCache.Clear();
        foreach (var skin in internalSkinList)
        {
            if (skin == null) continue;

            SkinCacheKey key = new SkinCacheKey
            {
                skinName = !string.IsNullOrEmpty(skin.skingName) ? skin.skingName : skin.gameObject.name,
                skinClass = skin.skinClass
            };

            if (skinCache.TryAdd(key, skin)) continue;
            Debug.LogWarning($"[SkinsController] Skin duplicada detectada e ignorada: '{key}'");
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling) return;
        UpdateSkinsButton();
#endif
    }

    [ContextMenu("Update Skins Button")]
    public void UpdateSkinsButton()
    {
#if UNITY_EDITOR
        if (!AssetDatabase.IsValidFolder(rootFolder))
        {
            Debug.LogWarning($"[SkinsController] folder '{rootFolder}' not found!");
            return;
        }

        internalSkinList.Clear();

        // Procura por todos os prefabs na pasta raiz especificada
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { rootFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            // Verifica se o prefab contém o script Skin anexado
            if (prefab != null && prefab.TryGetComponent(out Skin skinComponent) && !internalSkinList.Contains(skinComponent)) internalSkinList.Add(skinComponent);

        }

        EditorUtility.SetDirty(this);
#endif
    }

    public static Skin GetSkin(string name, ClassManager.Class @class)
    {
        SkinCacheKey key = new SkinCacheKey { skinName = name, skinClass = @class };

        if (skinCache.TryGetValue(key, out Skin skin)) return skin;

        Debug.LogWarning($"[SkinsController] Skin '{key.skinName}' da classe {key.skinClass} não foi encontrada no cache!");
        return null;
    }

    // Adicione este método ao SkinsManager.cs
    public List<Skin> GetAllSkins() => internalSkinList;
}