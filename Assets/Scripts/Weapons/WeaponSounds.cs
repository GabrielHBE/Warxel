using UnityEngine;

public class WeaponSounds : MonoBehaviour
{
    [Header("Shoot")]
    public AudioClip shootSound;
    public SoundManager.SoundProperties shootSoundProperties = SoundManager.SoundProperties.Default;

    [Header("Remove Mag")]
    [SerializeField] private AudioClip removeMagSound;
    [SerializeField] private SoundManager.SoundProperties removeMagSoundProperties = SoundManager.SoundProperties.Default;

    [Header("Put Mag")]
    [SerializeField] private AudioClip putMagSound;
    [SerializeField] private SoundManager.SoundProperties putMagSoundProperties = SoundManager.SoundProperties.Default;

    [Header("Pull Extractor")]
    [SerializeField] private AudioClip pullExtractorSound;
    [SerializeField] private SoundManager.SoundProperties pullExtractorSoundProperties = SoundManager.SoundProperties.Default;

    [Header("Push Extractor")]
    [SerializeField] private AudioClip pushExtractorSound;
    [SerializeField] private SoundManager.SoundProperties pushExtractorSoundProperties = SoundManager.SoundProperties.Default;

    private CameraShake cameraShake;
    private float originalPitch;
    void Awake()
    {   
        originalPitch = shootSoundProperties.pitch;
        cameraShake = GetComponentInParent<CameraShake>();
    }

    public void RemoveMag()
    {
        cameraShake.RequestShake(0.5f, 0.5f);
        SoundManager.Play2dSoundLocal(removeMagSound, removeMagSoundProperties);
    }

    public void PutMag()
    {
        cameraShake.RequestShake(0.5f, 0.5f);
        SoundManager.Play2dSoundLocal(putMagSound, putMagSoundProperties);
    }

    public void PushExtractor()
    {
        cameraShake.RequestShake(0.5f, 0.5f);
        SoundManager.Play2dSoundLocal(pushExtractorSound, pushExtractorSoundProperties);
    }

    public void PullExtractor()
    {
        cameraShake.RequestShake(0.5f, 0.5f);
        SoundManager.Play2dSoundLocal(pullExtractorSound, pullExtractorSoundProperties);
    }

    public void ShootSound()
    {   
        SoundManager.Instance.RequestPlay3dSound(shootSound.name, shootSoundProperties, transform.position, false);
        SoundManager.Play2dSoundLocal(shootSound, shootSoundProperties);
        //SoundManager.Play3dSoundLocal(shootSound, shootSoundProperties, transform.position);
    }
}
