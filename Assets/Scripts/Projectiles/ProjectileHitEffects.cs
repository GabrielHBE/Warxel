using FishNet.Object;
using UnityEngine;

public class ProjectileHitEffects : MonoBehaviour
{
    public enum HitEffectType : byte
    {
        Blood, Glass, Metal, Wood, Concrete, Sand, Dirt, SoftBody, Custom
    }

    [Header("HitEffects")]
    [SerializeField] private GameObject blood_hit_effect;
    [SerializeField] private GameObject glass_hit_effect;
    [SerializeField] private GameObject metal_hit_effect;
    [SerializeField] private GameObject wood_hit_effect;
    [SerializeField] private GameObject concrete_hit_effect;
    [SerializeField] private GameObject sand_hit_effect;
    [SerializeField] private GameObject dirt_hit_effect;
    [SerializeField] private GameObject softbody_hit_effect;
    [SerializeField] private GameObject custom_hit_effect;

    public void SetCustomHitEffect(GameObject effect)
    {
        custom_hit_effect = effect;
    }

    #region Playe Effect Methods
    public void VoxelHitEffect(Vector3 position, VoxelMaterials.VoxelMaterialType material)
    {
        switch (material)
        {
            case VoxelMaterials.VoxelMaterialType.Glass:
                GlassHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.Metal:
                MetalHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.Wood:
                WoodHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.Concrete:
                ConcreteHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.Sand:
                SandHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.Dirt:
                DirtHitEffect(position, Quaternion.identity);
                break;
            case VoxelMaterials.VoxelMaterialType.SoftBody:
                SoftBodyHitEffect(position, Quaternion.identity);
                break;
        }
    }
    public void MetalHitEffect(Vector3 pos, Quaternion rot) => InstantiateHitEffect(HitEffectType.Metal, pos, rot);
    public void GlassHitEffect(Vector3 pos, Quaternion rot) => InstantiateHitEffect(HitEffectType.Glass, pos, rot);
    public void WoodHitEffect(Vector3 pos, Quaternion rot) => InstantiateHitEffect(HitEffectType.Wood, pos, rot);
    public void ConcreteHitEffect(Vector3 pos, Quaternion rot) => InstantiateHitEffect(HitEffectType.Concrete, pos, rot);
    public void SandHitEffect(Vector3 pos, Quaternion rot) => InstantiateHitEffect(HitEffectType.Sand, pos, rot);
    public void DirtHitEffect(Vector3 pos, Quaternion rot) => InstantiateHitEffect(HitEffectType.Dirt, pos, rot);
    public void SoftBodyHitEffect(Vector3 pos, Quaternion rot) => InstantiateHitEffect(HitEffectType.SoftBody, pos, rot);
    public void BloodHitEffect(Vector3 pos, Quaternion rot) => InstantiateHitEffect(HitEffectType.Blood, pos, rot);
    public void CustomHitEffect(Vector3 pos) => InstantiateHitEffect(HitEffectType.Custom, pos, Quaternion.identity);
    #endregion

    #region Helper Methods
    private void InstantiateHitEffect(HitEffectType effectType, Vector3 pos, Quaternion rot)
    {
        GameObject effectToSpawn = GetEffectPrefab(effectType);
        if (effectToSpawn != null)
        {
            Instantiate(effectToSpawn, pos, rot);
        }
    }

    private GameObject GetEffectPrefab(HitEffectType type)
    {
        switch (type)
        {
            case HitEffectType.Blood: return blood_hit_effect;
            case HitEffectType.Glass: return glass_hit_effect;
            case HitEffectType.Metal: return metal_hit_effect;
            case HitEffectType.Wood: return wood_hit_effect;
            case HitEffectType.Concrete: return concrete_hit_effect;
            case HitEffectType.Sand: return sand_hit_effect;
            case HitEffectType.Dirt: return dirt_hit_effect;
            case HitEffectType.SoftBody: return softbody_hit_effect;
            case HitEffectType.Custom: return custom_hit_effect;
            default: return null;
        }
    }
    #endregion
}