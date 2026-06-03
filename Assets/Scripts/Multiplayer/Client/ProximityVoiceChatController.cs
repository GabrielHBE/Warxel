using FishNet.Object;
using UnityEngine;

public class ProximityVoiceChatController : NetworkBehaviour
{
    [SerializeField] private AudioSource voip_audio;

    private bool isTalking = false; 

    private void Update()
    {
        if (!IsOwner) return;

        bool isPressingKey = InputManager.GetKey(Settings.Instance._audio.in_world_voip_key);

        if (isPressingKey && !isTalking)
        {
            isTalking = true;
            voip_audio.volume = 1f;
            CmdVoipState(true);
        }

        else if (!isPressingKey && isTalking)
        {
            isTalking = false;
            voip_audio.volume = 0f;
            CmdVoipState(false);
        }
    }

    [ServerRpc]
    private void CmdVoipState(bool state)
    {
        ClientUpdateVoipState(state);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void ClientUpdateVoipState(bool state)
    {
        voip_audio.volume = state ? 1f : 0f;
    }
}