using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator anim;
    private PlayerController movement;

    void Start()
    {
        anim = GetComponent<Animator>();
        movement = GetComponent<PlayerController>();

        /*
        pv = GetComponent<PhotonView>();

        if (!pv.IsMine)
        {
            enabled = false; // Desativa o script nos jogadores remotos
        }
        */

    }

    void Update()
    {
        
        anim.SetFloat("Horizontal", movement.moveHorizontal);
        anim.SetFloat("Vertical", movement.moveForward);

        //Debug.Log(movement.moveHorizontal);
        //Debug.Log(movement.moveForward);
        
        //anim.Play("Idle");
    }
}
