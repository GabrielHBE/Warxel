using FishNet.Demo.AdditiveScenes;
using UnityEngine;
using FishNet.Object;
using FishNet;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }
    
    // Cada cliente terá seu próprio controller, não compartilhado
    [System.NonSerialized]
    public PlayerSpawnController localPlayerSpawnController;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void SetPlayerSpawnController(PlayerSpawnController controller)
    {
        // Só define o controller local se for o owner
        if (controller.IsOwner)
        {
            localPlayerSpawnController = controller;
        }
    }
    
    public PlayerSpawnController GetPlayerSpawnController()
    {
        return localPlayerSpawnController;
    }
}