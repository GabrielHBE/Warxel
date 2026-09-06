using UnityEngine;

public class EquippableItemAudio : MonoBehaviour
{
    [Header("Shoot")]
    public AudioClip[] shootSounds;
    public SoundManager.SoundProperties shootSoundProperties = SoundManager.SoundProperties.Default;

    [Header("Remove Mag")]
    [SerializeField] private SoundManager.SoundComponents removeMagSound;

    [Header("Put Mag")]
    [SerializeField] private SoundManager.SoundComponents putMagSound;

    [Header("Pull Extractor")]
    [SerializeField] private SoundManager.SoundComponents pullExtractorSound;

    [Header("Push Extractor")]
    [SerializeField] private SoundManager.SoundComponents pushExtractorSound;

    [Header("Draw Weapon")]
    [SerializeField] private SoundManager.SoundComponents drawSound;

    [Header("Store Weapon")]
    [SerializeField] private SoundManager.SoundComponents storeSound;

    [Header("ADS Sound")]
    [SerializeField] private SoundManager.SoundComponents adsSound;

    [Header("Custom Sunds")]
    public AudioClip[] customSounds;
    public SoundManager.SoundProperties customSoundsProperties = SoundManager.SoundProperties.Default;


    public void RemoveMag()
    {
        if (removeMagSound.clip == null) return;

        SoundManager.Play2dSoundLocal(removeMagSound.clip, removeMagSound.properties);
    }

    public void PutMag()
    {
        if (putMagSound.clip == null) return;

        SoundManager.Play2dSoundLocal(putMagSound.clip, putMagSound.properties);
    }

    public void PushExtractor()
    {
        if (pushExtractorSound.clip == null) return;

        SoundManager.Play2dSoundLocal(pushExtractorSound.clip, pushExtractorSound.properties);
    }

    public void PullExtractor()
    {
        if (pullExtractorSound.clip == null) return;

        SoundManager.Play2dSoundLocal(pullExtractorSound.clip, pullExtractorSound.properties);
    }

    public void CustomSound(int index)
    {
        if (customSounds == null || customSounds.Length == 0) return;

        SoundManager.Play2dSoundLocal(customSounds[index], customSoundsProperties);
    }

    public void AdsSound()
    {
        if (adsSound.clip == null) return;

        SoundManager.Play2dSoundLocal(adsSound.clip, adsSound.properties);
    }

    public void DrawSound()
    {
        if (drawSound.clip == null) return;

        SoundManager.Play2dSoundLocal(drawSound.clip, drawSound.properties);
    }

    public void StoreSound()
    {
        if (storeSound.clip == null) return;

        SoundManager.Play2dSoundLocal(storeSound.clip, storeSound.properties);
    }

    public void ShootSound()
    {
        if (shootSounds == null || shootSounds.Length == 0) return;

        AudioClip shootSound = SoundManager.GetRandomAudioClip(shootSounds);

        SoundManager.Instance.RequestPlay3dSound(shootSound.name, shootSoundProperties, transform.position, false);
        SoundManager.Play2dSoundLocal(shootSound, shootSoundProperties);
    }
}
