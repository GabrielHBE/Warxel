using FishNet.Object;
using UnityEngine;

public class ProximityVoiceChatController : NetworkBehaviour
{
    [SerializeField] private AudioSource voip_audio;

    // Usamos uma variável booleana de controle. É infinitamente mais leve e 
    // à prova de falhas do que checar o valor de um float todo frame.
    private bool isTalking = false; 

    private void Update()
    {
        if (!IsOwner) return;

        // Lemos o input uma vez por frame
        bool isPressingKey = Input.GetKey(Settings.Instance._audio.in_world_voip_key);

        // Se o botão está pressionado e eu ainda não estou falando
        if (isPressingKey && !isTalking)
        {
            isTalking = true;
            voip_audio.volume = 1f;
            CmdVoipState(true);
        }
        // Se soltei o botão e eu estava falando
        else if (!isPressingKey && isTalking)
        {
            isTalking = false;
            voip_audio.volume = 0f;
            CmdVoipState(false);
        }
    }

    // Apenas corrigindo o 'v' minúsculo de 'voip' para 'Voip' por convenção de nomenclatura
    [ServerRpc]
    private void CmdVoipState(bool state)
    {
        ClientUpdateVoipState(state);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void ClientUpdateVoipState(bool state)
    {
        // Operador ternário: se 'state' for true, o volume recebe 1f. Se for false, recebe 0f.
        voip_audio.volume = state ? 1f : 0f;
    }
}