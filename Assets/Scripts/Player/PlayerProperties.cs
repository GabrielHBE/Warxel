using Unity.VisualScripting;
using UnityEngine;

public class PlayerProperties : MonoBehaviour
{
    public bool crouched;
    public bool sprinting;
    public bool freeLook;
    public bool is_aiming;
    public bool is_reloading;
    public bool is_firing;
    public bool isGrounded;

    public float hp;
    public float resistance;

    public void Damage(float dmg)
    {
        hp -= dmg * (1 - resistance);

        if (hp < 0)
        {
            hp = 0;
        }
            
    }
    
    void Update()
    {
        Debug.Log(hp);
    }

}
