using FishNet.Object;
using UnityEngine;
using FishNet;
using FishNet.Component.Spawning;
using System.Collections;
using FishNet.Object.Synchronizing;
using FishNet.Connection;

public class ClientManager : ServerSingleton<ClientManager>
{
    [SerializeField] private GameObject playerSpawnController;
    [SerializeField] private GameObject loadoutCustomization;
    [SerializeField] private GameObject vehicleLoadoutCustomization;
    [SerializeField] private GameObject squadLoadoutSelecion;

    private GameObject instantiated_player_spawner;
    private GameObject instantiated_infantary_loadout_customization;
    private GameObject instantiated_vehicle_loadout_customization;
    private GameObject instantiated_squad_selection;

    private readonly SyncVar<SquadManager.SquadName> selectedSquad = new SyncVar<SquadManager.SquadName>();
    public readonly SyncVar<NetworkConnection> clientNetworkConnection = new SyncVar<NetworkConnection>();

    // Flag para controlar se já entrou no squad automaticamente
    private bool hasAutoJoinedSquad = false;
    private bool isEnteringSquad = false;

    protected override void Awake() { }
    
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            SetInstance();
            StartCoroutine(InitializeWhenReady());
            StartCoroutine(EnterSquad());
            RequestSetClientConnection();
        }
        else gameObject.SetActive(false);
    }

    [ServerRpc]
    private void RequestSetClientConnection() => clientNetworkConnection.Value = Owner;

    private IEnumerator InitializeWhenReady()
    {
        // Wait until the client is fully connected and ready
        while (!IsClientReady())
        {
            yield return new WaitForSeconds(0.1f);
        }
        // Now initialize
        SpawnClientObjects();
    }

    private IEnumerator EnterSquad()
    {
        // Aguarda o SquadManager estar pronto
        while (SquadManager.Instance == null)
        {
            yield return null;
        }

        while (!SquadManager.Instance.isSetup.Value)
        {
            yield return null;
        }

        // Aguarda o SquadSelecionUI estar inicializado
        while (SquadSelecionUI.Instance == null)
        {
            yield return null;
        }

        // Aguarda o UI estar inicializado
        while (!SquadSelecionUI.Instance.IsInitialized())
        {
            yield return null;
        }

        // Aguarda um frame para garantir que tudo está configurado
        yield return null;

        // Entra no squad automaticamente
        EnterSquadAutomatically();
    }

    private bool IsClientReady()
    {
        // Check if we're connected to the server
        if (!IsClientInitialized) return false;

        // Check if we have a valid connection
        if (Owner == null || !Owner.IsValid) return false;

        // Check if the connection is active
        if (!Owner.IsActive) return false;

        // Additional check: verify the client ID is valid (>= 0)
        if (Owner.ClientId < 0) return false;

        return true;
    }

    private void SpawnClientObjects()
    {
        // Spawna o PlayerSpawnController (que tem NetworkBehaviour)
        if (playerSpawnController != null)
        {
            SpawnPlayerSpawner();
        }

        if (loadoutCustomization != null)
            instantiated_infantary_loadout_customization = Instantiate(loadoutCustomization);

        if (vehicleLoadoutCustomization != null)
        {
            instantiated_vehicle_loadout_customization = Instantiate(vehicleLoadoutCustomization);
            StartCoroutine(DisableVehicleCustomization());
        }
        if (squadLoadoutSelecion != null)
        {
            instantiated_squad_selection = Instantiate(squadLoadoutSelecion);
        }
    }

    private IEnumerator DisableVehicleCustomization()
    {
        yield return null;
        VehicleLoadoutCustomization.Instance.gameObject.SetActive(false);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (instantiated_infantary_loadout_customization != null) Destroy(instantiated_infantary_loadout_customization);
        if (instantiated_vehicle_loadout_customization != null) Destroy(instantiated_vehicle_loadout_customization);
        if (instantiated_squad_selection != null) Destroy(instantiated_squad_selection);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerSpawner()
    {
        // Check if we're on the server
        if (!IsServerInitialized) return;

        // Only spawn if we haven't spawned this yet for this connection
        PlayerSpawner playerSpawner = InstanceFinder.NetworkManager.GetComponent<PlayerSpawner>();
        if (playerSpawner == null || playerSpawner.Spawns == null || playerSpawner.Spawns.Length == 0)
        {
            Debug.LogError("[ClientManager] PlayerSpawner not properly configured!");
            return;
        }

        instantiated_player_spawner = Instantiate(playerSpawnController);
        instantiated_player_spawner.transform.position = playerSpawner.Spawns[0].position;
        Spawn(instantiated_player_spawner, Owner);
    }

    private void EnterSquadAutomatically()
    {
        // Evita entrar múltiplas vezes
        if (hasAutoJoinedSquad || isEnteringSquad) return;
        
        if (AccountManager.Instance == null)
        {
            Debug.LogError("[ClientManager] AccountManager.Instance não encontrado!");
            return;
        }

        isEnteringSquad = true;
        
        FactionManager.Faction playerFaction = AccountManager.Instance.faction;
        string playerName = GetPlayerName();

        Debug.Log($"[ClientManager] Tentando entrar automaticamente no squad. Facção: {playerFaction}, Jogador: {playerName}");

        // Marca que já tentou entrar
        hasAutoJoinedSquad = true;

        // Chama o método no servidor
        RequestEnterSquad(playerFaction, playerName);

        // Aguarda a sincronização e força atualização da UI
        StartCoroutine(ForceUIRefreshAfterJoin());
    }

    private IEnumerator ForceUIRefreshAfterJoin()
    {
        // Aguarda alguns frames para garantir que a sincronização aconteceu
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.2f);

        isEnteringSquad = false;

        // Força a atualização da UI
        if (SquadSelecionUI.Instance != null && SquadSelecionUI.Instance.IsInitialized())
        {
            Debug.Log("[ClientManager] Forçando atualização da UI do squad após entrada automática.");
            SquadSelecionUI.Instance.ForceRefreshUI();
            
            // Verifica se o estado foi atualizado corretamente
            if (SquadSelecionUI.Instance.IsInSquad())
            {
                Debug.Log($"[ClientManager] UI atualizada. Jogador está no squad: {SquadSelecionUI.Instance.GetCurrentSquad()}");
            }
            else
            {
                Debug.LogWarning("[ClientManager] UI atualizada mas jogador não está marcado como em um squad. Tentando novamente...");
                // Tenta novamente após mais um delay
                yield return new WaitForSeconds(0.3f);
                SquadSelecionUI.Instance.ForceRefreshUI();
            }
        }
        else
        {
            Debug.LogWarning("[ClientManager] SquadSelecionUI não disponível para atualização.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEnterSquad(FactionManager.Faction faction, string playerName)
    {
        // Ensure we're on the server
        if (!IsServerInitialized) return;

        // Check if connection is valid
        if (Owner == null || !Owner.IsValid) return;

        Debug.Log($"[ClientManager-Server] Processando entrada automática para {playerName} na facção {faction}");

        // Verifica se o jogador já está em um squad
        if (IsPlayerInAnySquad(faction, Owner))
        {
            Debug.Log($"[ClientManager-Server] Jogador {playerName} já está em um squad.");
            return;
        }

        var availableSquad = SquadManager.Instance.FindAvailableSquad(faction);

        if (availableSquad.HasValue)
        {
            Debug.Log($"[ClientManager-Server] Squad disponível encontrado: {availableSquad.Value.squadName}");
            
            bool success = SquadManager.Instance.AddMemberToSquad(faction, availableSquad.Value.squadName, Owner, playerName);

            if (success)
            {
                selectedSquad.Value = availableSquad.Value.squadName;
                Debug.Log($"[ClientManager-Server] Jogador {playerName} entrou no squad {availableSquad.Value.squadName} na facção {faction}");
            }
            else
            {
                Debug.LogWarning($"[ClientManager-Server] Falha ao entrar no squad {availableSquad.Value.squadName} para {playerName}");
            }
        }
        else
        {
            Debug.LogWarning($"[ClientManager-Server] Não há squads disponíveis para facção {faction}");
        }
    }

    // Método auxiliar para verificar se o jogador já está em algum squad
    private bool IsPlayerInAnySquad(FactionManager.Faction faction, NetworkConnection connection)
    {
        if (!SquadManager.Instance.squads.ContainsKey(faction)) return false;

        foreach (var squad in SquadManager.Instance.squads[faction])
        {
            foreach (var member in squad.squadMembers)
            {
                if (member.connection == connection)
                    return true;
            }
        }
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestJoinSpecificSquad(FactionManager.Faction faction, SquadManager.SquadName squadName, string playerName)
    {
        if (!IsServerInitialized) return;
        if (Owner == null || !Owner.IsValid) return;

        bool success = SquadManager.Instance.AddMemberToSquad(faction, squadName, Owner, playerName);

        if (success)
        {
            selectedSquad.Value = squadName;
            Debug.Log($"[ClientManager] Jogador {playerName} entrou no squad {squadName} na facção {faction}");
        }
        else
        {
            Debug.LogWarning($"[ClientManager] Falha ao entrar no squad {squadName} para {playerName}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestLeaveSquad(FactionManager.Faction faction)
    {
        if (!IsServerInitialized) return;
        if (Owner == null || !Owner.IsValid) return;

        bool success = SquadManager.Instance.RemoveMemberFromSquad(faction, Owner);

        if (success)
        {
            selectedSquad.Value = default(SquadManager.SquadName);
            Debug.Log($"[ClientManager] Jogador {Owner.ClientId} saiu do squad");
        }
    }

    private string GetPlayerName()
    {
        if (AccountManager.Instance != null && !string.IsNullOrEmpty(AccountManager.Instance.account_name))
            return AccountManager.Instance.account_name;

        return $"Player_{Owner.ClientId}";
    }

    public bool IsInSquad() => selectedSquad.Value != default(SquadManager.SquadName);
    public string GetCurrentSquadName() => selectedSquad.Value.ToString();
}