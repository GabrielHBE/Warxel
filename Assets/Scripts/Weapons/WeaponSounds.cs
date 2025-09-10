using System.Linq;
using UnityEngine;

public class WeaponSounds : MonoBehaviour
{

    public AudioSource shoot_sound;
    private AudioSource pick_up_weapon_sound;
    public AudioSource remove_mag_sound;
    public AudioSource put_mag_sound;
    public AudioSource pull_extractor_sound;
    public AudioSource push_extractor_sound;

    private CameraShake cameraShake;

    void Start()
    {
        cameraShake = GetComponentInParent<CameraShake>();
    }


    public void RemoveMag()
    {
        if (remove_mag_sound != null)
        {
            StartCoroutine(cameraShake.ReloadShake());
            remove_mag_sound.PlayOneShot(remove_mag_sound.clip);
        }
    }

    public void PutMag()
    {
        if (put_mag_sound != null)
        {
            StartCoroutine(cameraShake.ReloadShake());
            put_mag_sound.PlayOneShot(put_mag_sound.clip);
        }
    }

    public void PushExtractor()
    {

        if (push_extractor_sound != null)
        {
            StartCoroutine(cameraShake.ReloadShake());
            push_extractor_sound.PlayOneShot(push_extractor_sound.clip);
        }
    }
    
    public void PullExtractor()
    {

        if (pull_extractor_sound != null)
        {
            StartCoroutine(cameraShake.ReloadShake());
            pull_extractor_sound.PlayOneShot(pull_extractor_sound.clip);
        }
    }


}
