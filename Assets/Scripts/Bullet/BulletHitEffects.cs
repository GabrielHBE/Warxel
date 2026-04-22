using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class BulletHitEffects : NetworkBehaviour
{
    [Header("HitEffects")]
    [SerializeField] private GameObject blood_hit_effect;
    [SerializeField] private GameObject glass_hit_effect;
    [SerializeField] private GameObject metal_hit_effect;
    [SerializeField] private GameObject wood_hit_effect;
    [SerializeField] private GameObject concrete_hit_effect;
    [SerializeField] private GameObject sand_hit_effect;
    [SerializeField] private GameObject dirt_hit_effect;
    [SerializeField] private GameObject softbody_hit_effect;
    private readonly SyncVar<GameObject> custom_hit_effect = new SyncVar<GameObject>();

    public void SetCustomHitEffect(GameObject effect)
    {
        custom_hit_effect.Value = effect;
    }

    public void RequestVoxelHitEffect(Vector3 position, VoxelMaterials.VoxelMaterialType material)
    {
        switch (material)
        {
            case VoxelMaterials.VoxelMaterialType.Glass:
                RequestGlassHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.Metal:
                RequestMetalHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.Wood:
                RequestWoodHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.Concrete:
                RequestConcreteHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.Sand:
                RequestSandHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.Dirt:
                RequestDirtHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.SoftBody:
                RequestSoftBodyHitEffect(position, Quaternion.identity);
                break;
        }
    }

    [ServerRpc]
    public void RequestMetalHitEffect(Vector3 pos, Quaternion rot)
    {   
        if(metal_hit_effect == null) return;

        GameObject metal = Instantiate(metal_hit_effect, pos, rot);
        Spawn(metal);
    }

    [ServerRpc]
    public void RequestGlassHitEffect(Vector3 pos, Quaternion rot)
    {
        if(glass_hit_effect == null) return;

        GameObject glass = Instantiate(glass_hit_effect, pos, rot);
        Spawn(glass, null);
    }

    [ServerRpc]
    public void RequestWoodHitEffect(Vector3 pos, Quaternion rot)
    {
        if(wood_hit_effect == null) return;

        GameObject wood = Instantiate(wood_hit_effect, pos, rot);
        Spawn(wood, null);
    }

    [ServerRpc]
    public void RequestConcreteHitEffect(Vector3 pos, Quaternion rot)
    {
        if(concrete_hit_effect == null) return;

        GameObject concrete = Instantiate(concrete_hit_effect, pos, rot);
        Spawn(concrete, null);
    }

    [ServerRpc]
    public void RequestSandHitEffect(Vector3 pos, Quaternion rot)
    {
        if(sand_hit_effect == null) return;

        GameObject sand = Instantiate(sand_hit_effect, pos, rot);
        Spawn(sand, null);
    }

    [ServerRpc]
    public void RequestDirtHitEffect(Vector3 pos, Quaternion rot)
    {
        if(dirt_hit_effect == null) return;

        GameObject dirt = Instantiate(dirt_hit_effect, pos, rot);
        Spawn(dirt, null);
    }

    [ServerRpc]
    public void RequestSoftBodyHitEffect(Vector3 pos, Quaternion rot)
    {
        if(softbody_hit_effect == null) return;

        GameObject softbody = Instantiate(softbody_hit_effect, pos, rot);
        Spawn(softbody, null);
    }

    [ServerRpc]
    public void RequestBloodHitEffect(Vector3 pos, Quaternion rot)
    {
        if(blood_hit_effect == null) return;
        
        GameObject blood = Instantiate(blood_hit_effect, pos, rot);
        Spawn(blood, null);
    }

    [ServerRpc]
    public void RequestCustomHitEffect(Vector3 position)
    {
        if (custom_hit_effect.Value == null) return;

        GameObject effectToSpawn = Instantiate(custom_hit_effect.Value, position, Quaternion.identity);
        Spawn(effectToSpawn, null);

    }

}
