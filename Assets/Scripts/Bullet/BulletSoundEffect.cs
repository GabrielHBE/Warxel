using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class BulletSoundEffect : NetworkBehaviour
{
    [SerializeField] private AudioSource audioSource;

    [Header("HitEffects")]
    [SerializeField] private AudioClip[] ricochet_sound;
    [SerializeField] private AudioClip wosh_sound;
    [SerializeField] private AudioClip blood_hit_sound;
    [SerializeField] private AudioClip glass_hit_sound;
    [SerializeField] private AudioClip metal_hit_sound;
    [SerializeField] private AudioClip wood_hit_sound;
    [SerializeField] private AudioClip concrete_hit_sound;
    [SerializeField] private AudioClip sand_hit_sound;
    [SerializeField] private AudioClip dirt_hit_sound;
    [SerializeField] private AudioClip softbody_hit_sound;

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

    #region Unified Network Sound Playing

    // 1. The Client calls this to ask the Server to play a sound
    [ServerRpc(RequireOwnership = false)]
    public void CmdPlayHitSound(HitSoundType soundType, Vector3 pos)
    {
        RpcPlayHitSound(soundType, pos);
    }

    // 2. The Server broadcasts the instruction to all clients
    [ObserversRpc]
    private void RpcPlayHitSound(HitSoundType soundType, Vector3 pos)
    {
        AudioClip clipToPlay = null;

        switch (soundType)
        {
            case HitSoundType.Ricochet:
                clipToPlay = ricochet_sound[Random.Range(0, ricochet_sound.Length)];
                break;
            case HitSoundType.Blood:
                clipToPlay = blood_hit_sound;
                break;
            case HitSoundType.Metal:
                clipToPlay = metal_hit_sound;
                break;
            case HitSoundType.Glass:
                clipToPlay = glass_hit_sound;
                break;
            case HitSoundType.Wood:
                clipToPlay = wood_hit_sound;
                break;
            case HitSoundType.Concrete:
                clipToPlay = concrete_hit_sound;
                break;
            case HitSoundType.Sand:
                clipToPlay = sand_hit_sound;
                break;
            case HitSoundType.Dirt:
                clipToPlay = dirt_hit_sound;
                break;
            case HitSoundType.SoftBody:
                clipToPlay = softbody_hit_sound;
                break;
        }

        if (clipToPlay != null)
        {
            AudioSource tempSource = Instantiate(audioSource, pos, Quaternion.identity);
            tempSource.clip = clipToPlay;
            tempSource.Play();
            tempSource.gameObject.AddComponent<SoundDestroyAfterTime>().SetTime(clipToPlay.length);
        }
    }

    // For Voxel Materials, we map the VoxelMaterialType to our HitSoundType enum
    public void RequestVoxelHitSound(Vector3 pos, VoxelMaterials.VoxelMaterialType material)
    {
        HitSoundType type = HitSoundType.Concrete; // Default fallback

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

        CmdPlayHitSound(type, pos);
    }

    #endregion

    [TargetRpc]
    private void TargetPlayWhooshSound(NetworkConnection conn)
    {
        if (wosh_sound == null) return;
        AudioSource tempSource = Instantiate(audioSource, transform.position, Quaternion.identity);
        tempSource.clip = wosh_sound;
        tempSource.Play();
        tempSource.gameObject.AddComponent<SoundDestroyAfterTime>().SetTime(wosh_sound.length);
    }

    private class SoundDestroyAfterTime : MonoBehaviour
    {
        public void SetTime(float time)
        {
            Destroy(gameObject, time);
        }
    }
}