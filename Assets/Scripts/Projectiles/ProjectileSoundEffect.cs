using FishNet.Object;
using UnityEngine;

public class ProjectileSoundEffect : MonoBehaviour
{
    [Header("HitSounds")]
    [SerializeField] private SoundManager.SoundComponents[] ricochetSounds;
    [SerializeField] private SoundManager.SoundComponents woshSound;
    [SerializeField] private SoundManager.SoundComponents bloodHitSound;
    [SerializeField] private SoundManager.SoundComponents glassHitSound;
    [SerializeField] private SoundManager.SoundComponents metalHitSound;
    [SerializeField] private SoundManager.SoundComponents woodHitSound;
    [SerializeField] private SoundManager.SoundComponents concreteHitSound;
    [SerializeField] private SoundManager.SoundComponents sandHitSound;
    [SerializeField] private SoundManager.SoundComponents dirtHitSound;
    [SerializeField] private SoundManager.SoundComponents softbodyHitSound;
    [SerializeField] private SoundManager.SoundComponents customHitSound;


    public void SetCustomHitSound(string customHitSoundName, SoundManager.SoundProperties customHitSoundProperties)
    {
        customHitSound.clip.name = customHitSoundName;
        customHitSound.properties = customHitSoundProperties;
    }

    #region Sound Playing
    public void PlayHitSound(HitSoundType soundType, Vector3 pos)
    {
        if (customHitSound.clip != null)
        {
            SoundManager.Instance.RequestPlay3dSound(customHitSound.clip.name, customHitSound.properties, pos, false);
            SoundManager.Play3dSoundLocal(SoundManager.GetClip(customHitSound.clip.name), customHitSound.properties, pos);
        }

        SoundManager.SoundComponents clipToPlay = null;

        switch (soundType)
        {
            case HitSoundType.Ricochet:
                clipToPlay = ricochetSounds[Random.Range(0, ricochetSounds.Length)];
                break;
            case HitSoundType.Blood:
                clipToPlay = bloodHitSound;
                break;
            case HitSoundType.Metal:
                clipToPlay = metalHitSound;
                break;
            case HitSoundType.Glass:
                clipToPlay = glassHitSound;
                break;
            case HitSoundType.Wood:
                clipToPlay = woodHitSound;
                break;
            case HitSoundType.Concrete:
                clipToPlay = concreteHitSound;
                break;
            case HitSoundType.Sand:
                clipToPlay = sandHitSound;
                break;
            case HitSoundType.Dirt:
                clipToPlay = dirtHitSound;
                break;
            case HitSoundType.SoftBody:
                clipToPlay = softbodyHitSound;
                break;
        }

        if (clipToPlay != null && clipToPlay.clip != null)
        {
            SoundManager.Instance.RequestPlay3dSound(clipToPlay.clip.name, clipToPlay.properties, pos, false);
            SoundManager.Play3dSoundLocal(clipToPlay.clip, clipToPlay.properties, pos);
        }
    }

    public void RequestVoxelHitSound(Vector3 pos, VoxelMaterials.VoxelMaterialType material)
    {
        HitSoundType type = HitSoundType.Concrete;

        switch (material)
        {
            case VoxelMaterials.VoxelMaterialType.Glass: type = HitSoundType.Glass; break;
            case VoxelMaterials.VoxelMaterialType.Metal: type = HitSoundType.Metal; break;
            case VoxelMaterials.VoxelMaterialType.Wood: type = HitSoundType.Wood; break;
            case VoxelMaterials.VoxelMaterialType.Concrete: type = HitSoundType.Concrete; break;
            case VoxelMaterials.VoxelMaterialType.Sand: type = HitSoundType.Sand; break;
            case VoxelMaterials.VoxelMaterialType.Dirt: type = HitSoundType.Dirt; break;
            case VoxelMaterials.VoxelMaterialType.SoftBody: type = HitSoundType.SoftBody; break;
        }

        PlayHitSound(type, pos);

    }
    #endregion

    #region Enums
    public enum HitSoundType
    {
        Ricochet,
        Blood,
        Metal,
        Glass,
        Wood,
        Concrete,
        Sand,
        Dirt,
        SoftBody
    }
    #endregion
}