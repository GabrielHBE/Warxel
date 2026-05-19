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
    void Awake()
    {
        cameraShake = GetComponentInParent<CameraShake>();
    }


    public void RemoveMag()
    {
        if (remove_mag_sound != null)
        {
            cameraShake.RequestShake(0.5f, 0.5f);
            remove_mag_sound.PlayOneShot(remove_mag_sound.clip);
        }
    }

    public void PutMag()
    {
        if (put_mag_sound != null)
        {
            cameraShake.RequestShake(0.5f, 0.5f);
            put_mag_sound.PlayOneShot(put_mag_sound.clip);
        }
    }

    public void PushExtractor()
    {

        if (push_extractor_sound != null)
        {
            cameraShake.RequestShake(0.5f, 0.5f);
            push_extractor_sound.PlayOneShot(push_extractor_sound.clip);
        }
    }

    public void PullExtractor()
    {

        if (pull_extractor_sound != null)
        {
            cameraShake.RequestShake(0.5f, 0.5f);
            pull_extractor_sound.PlayOneShot(pull_extractor_sound.clip);
        }
    }

    public void ShootSound()
    {
        // 1. Toca instantaneamente para o atirador (Client-Side Prediction)
        PlayShootSoundLocally(shoot_sound.transform.position);

        // 2. Procura o Spawner e a Arma para mandar o aviso pela rede
        PlayerNetworkObjectSpawner spawner = GetComponentInParent<PlayerNetworkObjectSpawner>();
        WeaponProperties wp = GetComponent<WeaponProperties>();

        if (spawner != null && wp != null)
        {
            // Manda a string com o nome da arma e a posição exata de onde o som saiu
            spawner.CmdPlayWeaponSound(wp.gameObject.name, shoot_sound.transform.position);
        }
    }

    // Separei a lógica de instanciar numa função própria para ficar mais limpo
    private void PlayShootSoundLocally(Vector3 position)
    {
        shoot_sound.PlayOneShot(shoot_sound.clip);
        /*
        GameObject duplicatedObject = Instantiate(shoot_sound.gameObject, position, Quaternion.identity);
        AudioDistanceController duplicatedController = duplicatedObject.GetComponent<AudioDistanceController>();

        if (duplicatedController != null)
        {
            duplicatedController.StartGrowth();
        }
        else
        {
            shoot_sound.PlayOneShot(shoot_sound.clip);
        }
        */
    }

}
