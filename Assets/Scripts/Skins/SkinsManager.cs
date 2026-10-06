using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;
using System;
using System.Threading.Tasks;

public class SkinsManager : PersistentLocalSingleton<SkinsManager>
{
    [SerializeField] private AssetLabelReference skinsAssetLabelReference;

    private readonly static Dictionary<SkinCacheKey, Skin> skinCache = new Dictionary<SkinCacheKey, Skin>();
    [SerializeField] private Skin[] internalSkinList = Array.Empty<Skin>();
    private Task<bool> initializationTask;

    public struct SkinCacheKey
    {
        public string skinName;
        public ClassManager.Class skinClass;
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this) initializationTask = InitializeSkinCache();
    }

    public Task<bool> WaitUntilReadyAsync() => initializationTask ??= InitializeSkinCache();

    private async Task<bool> InitializeSkinCache()
    {
        try
        {
            var skinsHandle = Addressables.LoadAssetsAsync<GameObject>(skinsAssetLabelReference, null);
            GameObject[] gameObjects = (await skinsHandle.Task).ToArray();
            skinCache.Clear();

            foreach (var go in gameObjects)
            {
                Skin skin = go.GetComponent<Skin>();
                if (skin == null) continue;
                SkinCacheKey key = new SkinCacheKey
                {
                    skinName = GetSkinName(skin),
                    skinClass = skin.skinClass
                };

                if (skinCache.TryAdd(key, skin)) continue;
                Debug.LogWarning($"[SkinsController] Duplicate skin detected and ignored: '{key.skinName}' for {key.skinClass}");
            }

            internalSkinList = skinCache.Values.ToArray();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SkinsController] Could not load skins: {exception}", this);
            return false;
        }
    }

    public static string GetSkinName(Skin skin) => !string.IsNullOrEmpty(skin.skingName)
        ? skin.skingName : skin.gameObject.name;

    public static bool TryGetSkin(string name, ClassManager.Class skinClass, out Skin skin)
    {
        skin = null;
        return !string.IsNullOrEmpty(name) && skinCache.TryGetValue(
            new SkinCacheKey { skinName = name, skinClass = skinClass }, out skin) && skin != null;
    }

    public static Skin GetSkin(string name, ClassManager.Class @class)
    {
        SkinCacheKey key = new SkinCacheKey { skinName = name, skinClass = @class };

        if (skinCache.TryGetValue(key, out Skin skin)) return skin;

        Debug.LogWarning($"[SkinsController] Skin '{key.skinName}' for class {key.skinClass} was not found in the cache!");
        return null;
    }

    // Adicione este método ao SkinsManager.cs
    public Skin[] GetAllSkins() => internalSkinList;
}
