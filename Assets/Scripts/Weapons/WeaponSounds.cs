using UnityEngine;

public class WeaponSounds : MonoBehaviour
{
    [Header("Shoot")]
    public SoundManager.SoundComponents shootSound;

    [Header("Remove Mag")]
    [SerializeField] private SoundManager.SoundComponents removeMagSound;

    [Header("Put Mag")]
    [SerializeField] private SoundManager.SoundComponents putMagSound;

    [Header("Pull Extractor")]
    [SerializeField] private SoundManager.SoundComponents pullExtractorSound;

    [Header("Push Extractor")]
    [SerializeField] private SoundManager.SoundComponents pushExtractorSound;

    private CameraShake cameraShake;
    void Awake()
    {   
        cameraShake = GetComponentInParent<CameraShake>();
    }

    public void RemoveMag()
    {
        cameraShake.RequestShake(0.5f, 0.5f);
        SoundManager.Play2dSoundLocal(removeMagSound.clip, removeMagSound.properties);
    }

    public void PutMag()
    {
        cameraShake.RequestShake(0.5f, 0.5f);
        SoundManager.Play2dSoundLocal(putMagSound.clip, putMagSound.properties);
    }

    public void PushExtractor()
    {
        cameraShake.RequestShake(0.5f, 0.5f);
        SoundManager.Play2dSoundLocal(pushExtractorSound.clip, pushExtractorSound.properties);
    }

    public void PullExtractor()
    {
        cameraShake.RequestShake(0.5f, 0.5f);
        SoundManager.Play2dSoundLocal(pullExtractorSound.clip, pullExtractorSound.properties);
    }

    public void ShootSound()
    {   
        SoundManager.Instance.RequestPlay3dSound(shootSound.clip.name, shootSound.properties, transform.position, false);
        SoundManager.Play2dSoundLocal(shootSound.clip, shootSound.properties);
        //SoundManager.Play3dSoundLocal(shootSound, shootproperties, transform.position);
    }
}
