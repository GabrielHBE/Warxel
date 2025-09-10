using System.Collections.Generic;
using Photon.Pun;

using Unity.VisualScripting;
using UnityEngine;

public class PlayerSetup : MonoBehaviourPun
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject fpCamera;
    public PlayerController player_controller;

    public List<SkinnedMeshRenderer> owner_disable_itens = new List<SkinnedMeshRenderer>();
    public List<GameObject> not_owner_disable_itens = new List<GameObject>();

    //public GameObject playerHead;

    void Start()
    {
        if (photonView.IsMine)
        {
            fpCamera.SetActive(true);
            player_controller.enabled = true;
            //playerHead.SetActive(false);
            for (int i = 0; i < owner_disable_itens.Count; i++)
            {
                owner_disable_itens[i].enabled = false;

            }
        }
        else
        {
            fpCamera.SetActive(false);
            player_controller.enabled = false;
            //playerHead.SetActive(false);
            for (int i = 0; i < not_owner_disable_itens.Count; i++)
            {
                not_owner_disable_itens[i].SetActive(false);
    
            }
        }

    }
}
