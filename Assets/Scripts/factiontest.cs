using FishNet.Object;
using TMPro;
using UnityEngine;

public class ServerState : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI a;
    [SerializeField] private AnimationClip anim;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))  FirstPersonArms.Instance.StartAnimation(anim);
        
        
        if(IsServerInitialized) a.text = "Server";
        else a.text = "Client";
        
        if (AccountManager.Instance != null)  a.text += " / " + AccountManager.Instance.selectedFaction;
        
    }
    
}
