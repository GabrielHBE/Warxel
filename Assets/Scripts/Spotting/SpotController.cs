using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class SpotController : NetworkBehaviour
{
    [SerializeField] LayerMask spot_layer;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float spot_distance = 500;


    public override void OnStartClient()
    {
        base.OnStartClient();

        if (playerProperties.selected_class != ClassManager.Class.Recoon || !IsOwner)
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.PLAYER_spotKey))
        {
            print("Apertou o botao");
            TrySpotTarget();
        }

    }


    private void TrySpotTarget()
    {
        Vector3 origin = playerController.playerCamera.transform.position;
        Vector3 direction = playerController.playerCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, spot_distance, spot_layer))
        {
            // Tenta pegar um NetworkObject (precisamos dele para mandar pro servidor)
            NetworkObject targetNetObj = hit.collider.GetComponentInParent<NetworkObject>();

            // Tenta pegar a interface no objeto hitado
            ISspottable spottableTarget = hit.collider.GetComponentInParent<ISspottable>();

            if (targetNetObj != null && spottableTarget != null)
            {
        
                // Verifica a facção LOCALMENTE primeiro para evitar spam na rede
                if (spottableTarget.GetFaction() != playerProperties.faction.Value)
                {
                    CmdSpotTarget(targetNetObj);
                }
                
            }
        }
    }

    // --- REQUISIÇÃO PARA O SERVIDOR ---
    [ServerRpc(RequireOwnership = true)]
    private void CmdSpotTarget(NetworkObject targetNetObj)
    {
        if (targetNetObj == null) return;

        ISspottable spottableTarget = targetNetObj.GetComponent<ISspottable>();
        if (spottableTarget == null) return;

        // Validação no servidor (Anti-cheat/Garantia)
        FactionManager.Faction targetFaction = spottableTarget.GetFaction();
        FactionManager.Faction myFaction = playerProperties.faction.Value;

        if (myFaction != targetFaction)
        {
            // O spot é válido! Envia o sinal para TODOS os clientes
            RpcNotifyTeamSpot(targetNetObj, myFaction);
        }
    }

    // --- BROADCAST PARA OS CLIENTES ---
    [ObserversRpc]
    private void RpcNotifyTeamSpot(NetworkObject targetNetObj, FactionManager.Faction teamFaction)
    {
        // Se eu não for o jogador local principal da máquina, ignoro (pra evitar rodar UI em clones)
        if (!IsOwner && PlayerController.Instance != this) return;

        // Verifica se a MINHA facção (no PC local) é a mesma do cara que fez o Spot
        if (PlayerController.Instance.playerProperties.faction.Value == teamFaction)
        {
            ISspottable spottable = targetNetObj.GetComponent<ISspottable>();
            if (spottable != null)
            {
                // Manda a UI rastrear o transform específico (spot_position)
                SpotUIManager.Instance.ShowSpot(spottable.GetSpotPosition());
            }
        }
    }

}
