using FishNet;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class ServerState : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI a;

    void Update()
    {
        if(IsServerInitialized)
        {
            a.text = "Server";
        }
        else
        {
            a.text = "Client";
        }
    }
    
}
