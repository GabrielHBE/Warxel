using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;

public class SkinsManager : PersistentLocalSingleton<SkinsManager>
{
    [SerializeField] private AssetLabelReference skinsAssetLabelReference;

    private readonly static Dictionary<SkinCacheKey, Skin> skinCache = new Dictionary<SkinCacheKey, Skin>();
    private Skin[] internalSkinList;

    public struct SkinCacheKey
    {
        public string skinName;
        public ClassManager.Class skinClass;
    }

    protected override void Awake()
    {
        base.Awake();
        WaitForLoadAllAddressables();
    }

    private async void WaitForLoadAllAddressables() => await InitializeSkinCache();

    private async System.Threading.Tasks.Task InitializeSkinCache()
    {
        var skinsHandle = Addressables.LoadAssetsAsync<GameObject>(skinsAssetLabelReference, null);

        GameObject[] gameObjects = (await skinsHandle.Task).ToArray();

        foreach (var go in gameObjects)
        {
            Skin skin = go.GetComponent<Skin>();
            SkinCacheKey key = new SkinCacheKey
            {
                skinName = !string.IsNullOrEmpty(skin.skingName) ? skin.skingName : skin.gameObject.name,
                skinClass = skin.skinClass
            };

            if (skinCache.TryAdd(key, skin)) continue;
            Debug.LogWarning($"[SkinsController] Duplicate skin detected and ignored: '{key}'");
        }

        // Popula o array internalSkinList com as skins validadas no cache
        internalSkinList = skinCache.Values.ToArray();

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
