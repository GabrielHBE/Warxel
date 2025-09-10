using UnityEngine;

public class RicochetSounds : MonoBehaviour
{

    public AudioSource ricochet_sound1;
    public AudioSource ricochet_sound2;
    public AudioSource ricochet_sound3;
    public AudioSource ricochet_sound4;
    public AudioSource ricochet_sound5;
    public AudioSource ricochet_sound6;

    public void Play()
    {
        int[] options = {1, 2, 3, 4, 5, 6};
        int choosen = options[Random.Range(0, options.Length)];

        if (choosen == 1)
        {
            if (ricochet_sound1 != null)
            {

                ricochet_sound1.PlayOneShot(ricochet_sound1.clip);
            }
        }
        else if (choosen == 2)
        {
            if (ricochet_sound2 != null)
            {

                ricochet_sound2.PlayOneShot(ricochet_sound2.clip);
            }
        }
        else if (choosen == 3)
        {
            if (ricochet_sound3 != null)
            {

                ricochet_sound3.PlayOneShot(ricochet_sound3.clip);
            }
        }
        else if (choosen == 4)
        {
            if (ricochet_sound4 != null)
            {

                ricochet_sound4.PlayOneShot(ricochet_sound4.clip);
            }
        }
        else if (choosen == 5)
        {
            if (ricochet_sound5 != null)
            {

                ricochet_sound5.PlayOneShot(ricochet_sound5.clip);
            }
        }
        else if (choosen == 6) {
            if (ricochet_sound6 != null)
            {

                ricochet_sound6.PlayOneShot(ricochet_sound6.clip);
            }
        }
    }


}
