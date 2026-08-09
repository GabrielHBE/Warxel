using System;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

public class SquadManager : ServerSingleton<SquadManager>
{
    public const int MAX_MEMBERS_PER_SQUAD = 8;
    public const int MAX_SQUAD_PER_FACTION = 8;

    public readonly SyncDictionary<FactionManager.Faction, List<SquadData>> squads = new SyncDictionary<FactionManager.Faction, List<SquadData>>();
    public readonly SyncVar<bool> isSetup = new SyncVar<bool>(false);

    public event Action<FactionManager.Faction, SquadName, NetworkConnection> OnPlayerJoinedSquad;
    public event Action<FactionManager.Faction, NetworkConnection> OnPlayerLeftSquad;

    public struct SquadData
    {
        public string id;
        public SquadName squadName;
        public SquadMemberData[] squadMembers;
    }

    public struct SquadMemberData
    {
        public NetworkConnection connection;
        public string playerName;
    }

    public enum SquadName
    {
        Alpha,
        Bravo,
        Charlie,
        Delta,
        Echo,
        Foxtrot,
        Golf,
        Hotel
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeAllSquads();
        isSetup.Value = true;
    }

    private void InitializeAllSquads()
    {
        // Inicializa as listas para cada facção
        squads[FactionManager.Faction.FactionA] = new List<SquadData>(MAX_SQUAD_PER_FACTION);
        squads[FactionManager.Faction.FactionB] = new List<SquadData>(MAX_SQUAD_PER_FACTION);

        // Cria squads para ambas as facções
        CreateSquadsForFaction(FactionManager.Faction.FactionA);
        CreateSquadsForFaction(FactionManager.Faction.FactionB);

        Debug.Log($"[SquadManager] Criados {MAX_SQUAD_PER_FACTION} squads para cada facção. Total: {MAX_SQUAD_PER_FACTION * 2}");
    }

    private void CreateSquadsForFaction(FactionManager.Faction faction)
    {
        // Pega todos os nomes de squad disponíveis
        SquadName[] squadNames = (SquadName[])Enum.GetValues(typeof(SquadName));

        // Garante que temos nomes suficientes
        int namesCount = squadNames.Length;
        int squadsToCreate = Math.Min(MAX_SQUAD_PER_FACTION, namesCount);

        for (int i = 0; i < squadsToCreate; i++)
        {
            SquadName squadName = squadNames[i];

            // Cria um ID único combinando facção e nome
            string squadId = $"{faction}_{squadName}_{Guid.NewGuid().ToString().Substring(0, MAX_SQUAD_PER_FACTION)}";

            var newSquad = new SquadData
            {
                id = squadId,
                squadName = squadName,
                squadMembers = new SquadMemberData[0]
            };

            squads[faction].Add(newSquad);
        }
    }

    // Método para criar um squad individual (mantido para uso futuro)
    public SquadData? CreateSquad(FactionManager.Faction faction, SquadName squadName)
    {
        if (!IsServerInitialized) return null;

        if (!squads.ContainsKey(faction)) squads[faction] = new List<SquadData>(MAX_SQUAD_PER_FACTION);
        
        if (squads[faction].Count >= MAX_SQUAD_PER_FACTION) return null;
        

        // Verifica se já existe um squad com este nome para esta facção
        foreach (var squad in squads[faction])
        {
            if (squad.squadName == squadName) return null;
            
        }

        var newSquad = new SquadData
        {
            id = $"{faction}_{squadName}_{Guid.NewGuid().ToString().Substring(0, 8)}",
            squadName = squadName,
            squadMembers = new SquadMemberData[0]
        };

        squads[faction].Add(newSquad);
        return newSquad;
    }

    // Método para adicionar um membro a um squad
    public bool AddMemberToSquad(FactionManager.Faction faction, SquadName squadName, NetworkConnection connection, string playerName)
    {
        if (!IsServerInitialized) return false;

        if (!squads.ContainsKey(faction)) return false;
        

        for (int i = 0; i < squads[faction].Count; i++)
        {
            var squad = squads[faction][i];

            if (squad.squadName == squadName)
            {
                if (squad.squadMembers.Length >= MAX_MEMBERS_PER_SQUAD)
                {
                    Debug.LogWarning($"Squad {squadName} está cheio!");
                    return false;
                }

                foreach (var member in squad.squadMembers)
                {
                    if (member.connection == connection) return false;
                    
                }

                var newMembersList = new List<SquadMemberData>(squad.squadMembers)
            {
                new SquadMemberData
                {
                    connection = connection,
                    playerName = playerName
                }
            };

                var updatedSquad = new SquadData
                {
                    id = squad.id,
                    squadName = squad.squadName,
                    squadMembers = newMembersList.ToArray()
                };

                squads[faction][i] = updatedSquad;

                // Dispara o evento
                OnPlayerJoinedSquad?.Invoke(faction, squadName, connection);

                Debug.Log($"Jogador {playerName} entrou no squad {squadName} (Facção {faction})");
                return true;
            }
        }

        return false;
    }

    public bool RemoveMemberFromSquad(FactionManager.Faction faction, NetworkConnection connection)
    {
        if (!IsServerInitialized) return false;

        if (!squads.ContainsKey(faction)) return false;

        for (int i = 0; i < squads[faction].Count; i++)
        {
            var squad = squads[faction][i];
            var membersList = new List<SquadMemberData>(squad.squadMembers);

            for (int j = 0; j < membersList.Count; j++)
            {
                if (membersList[j].connection == connection)
                {
                    membersList.RemoveAt(j);

                    var updatedSquad = new SquadData
                    {
                        id = squad.id,
                        squadName = squad.squadName,
                        squadMembers = membersList.ToArray()
                    };

                    squads[faction][i] = updatedSquad;

                    // Dispara o evento
                    OnPlayerLeftSquad?.Invoke(faction, connection);

                    return true;
                }
            }
        }

        return false;
    }

    public SquadData? FindAvailableSquad(FactionManager.Faction faction)
    {
        if (!squads.ContainsKey(faction)) return null;

        foreach (var squad in squads[faction])
        {
            if (squad.squadMembers.Length < MAX_MEMBERS_PER_SQUAD) return squad;
        }

        return null;
    }

}