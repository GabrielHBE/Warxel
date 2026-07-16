using FishNet.Object;
using UnityEngine;

public class ProximityVoiceChatController : NetworkBehaviour
{
    [SerializeField] private AudioSource voipAudioSource;

    private bool isTalking = false;

    private void Update()
    {
        // 1. LÓGICA DE AUDIÇÃO (Executa para TODOS os jogadores na tela local)
        if (Settings.Instance != null)
        {
            if (Settings.Instance._audio.selected_in_world_voip_mode == Audio.VoipModes.Off)
            {
                // Se o seu VOIP local está desligado, força o silêncio absoluto neste AudioSource
                voipAudioSource.volume = 0f;
            }
            else if (!IsOwner)
            {
                // Se o seu VOIP está ligado e este é o boneco de OUTRO jogador,
                // o volume vai depender estritamente se o dono dele está transmitindo voz
                voipAudioSource.volume = isTalking ? 1f : 0f;
            }
        }

        // 2. LÓGICA DE TRANSMISSÃO (Daqui para baixo, apenas o DONO do personagem executa)
        if (!IsOwner) return;
        if (Settings.Instance == null) return;

        switch (Settings.Instance._audio.selected_in_world_voip_mode)
        {
            case Audio.VoipModes.Off:
                // Só envia o comando se o estado mudou (evita floodar a rede a cada frame)
                if (isTalking)
                {
                    isTalking = false;
                    voipAudioSource.volume = 0f;
                    CmdVoipState(false);
                }
                break;

            case Audio.VoipModes.Push:
                bool isPressingKey = InputManager.GetKey(Settings.Instance._audio.in_world_voip_key);

                if (isPressingKey && !isTalking)
                {
                    isTalking = true;
                    voipAudioSource.volume = 1f;
                    CmdVoipState(true);
                }
                else if (!isPressingKey && isTalking)
                {
                    isTalking = false;
                    voipAudioSource.volume = 0f;
                    CmdVoipState(false);
                }
                break;

            case Audio.VoipModes.Enabled:
                // Só envia se acabou de ativar
                if (!isTalking)
                {
                    isTalking = true;
                    voipAudioSource.volume = 1f;
                    CmdVoipState(true);
                }
                break;
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
        // Agora o RPC apenas avisa aos outros clientes se este jogador específico está falando ou não
        isTalking = state;
    }
}