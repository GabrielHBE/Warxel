using UnityEngine;

public class WeaponSounds : MonoBehaviour
{

    public AudioSource shoot_sound;
    private AudioSource pick_up_weapon_sound;
    [SerializeField] private AudioSource remove_mag_sound;
    [SerializeField] private AudioSource put_mag_sound;
    [SerializeField] private AudioSource pull_extractor_sound;
    [SerializeField] private AudioSource push_extractor_sound;

    private CameraShake cameraShake;
    void Start()
    {
        cameraShake = GetComponentInParent<CameraShake>();
    }


    public void RemoveMag()
    {
        if (remove_mag_sound != null)
        {
            cameraShake.RequestShake(CameraShake.ShakeType.Reload, 0.5f, 0.5f);
            remove_mag_sound.PlayOneShot(remove_mag_sound.clip);
        }
    }

    public void PutMag()
    {
        if (put_mag_sound != null)
        {
            cameraShake.RequestShake(CameraShake.ShakeType.Reload, 0.5f, 0.5f);
            put_mag_sound.PlayOneShot(put_mag_sound.clip);
        }
    }

    public void PushExtractor()
    {

        if (push_extractor_sound != null)
        {
            cameraShake.RequestShake(CameraShake.ShakeType.Reload, 0.5f, 0.5f);
            push_extractor_sound.PlayOneShot(push_extractor_sound.clip);
        }
    }

    public void PullExtractor()
    {

        if (pull_extractor_sound != null)
        {
            cameraShake.RequestShake(CameraShake.ShakeType.Reload, 0.5f, 0.5f);
            pull_extractor_sound.PlayOneShot(pull_extractor_sound.clip);
        }
    }

    public void ShootSound()
    {

        // Duplicar o GameObject que tem o audioDistanceController
        GameObject duplicatedObject = Instantiate(shoot_sound.gameObject, shoot_sound.transform.position, Quaternion.identity);

        // Obter o componente AudioDistanceController do objeto duplicado
        AudioDistanceController duplicatedController = duplicatedObject.GetComponent<AudioDistanceController>();

        if (duplicatedController != null)
        {
            // Chamar a função no objeto duplicado
            duplicatedController.StartGrowth();
        }
        else
        {
            // Fallback caso não encontre o componente
            shoot_sound.PlayOneShot(shoot_sound.clip);
        }

    }

}
