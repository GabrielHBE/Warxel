using UnityEngine;
using Photon.Pun;
using System;

public class RoomManager : MonoBehaviourPunCallbacks
{

    public string roomCode = "";
    public GameObject player;
    public Transform spawnPoint;
    [Space]
    public GameObject roomCamera;

    private string currentName;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        

    }

    public void ChangeName(string name)
    {
        currentName = name;
    }

    public void  ConnectedToServer()
    {
        Debug.Log("Connecting...");
        PhotonNetwork.ConnectUsingSettings();
    }

    // Update is called once per frame
    public override void OnConnectedToMaster()
    {
        Debug.Log("Joining lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joining or creating room...");
        PhotonNetwork.JoinOrCreateRoom(roomCode, null, null);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room");

        PhotonNetwork.Instantiate(player.name, spawnPoint.position, Quaternion.identity);

        roomCamera.SetActive(false);

        PhotonNetwork.LocalPlayer.NickName = currentName;
    
    }
}
