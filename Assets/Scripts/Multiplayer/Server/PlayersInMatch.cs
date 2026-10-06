using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayersInMatch : ServerSingleton<PlayersInMatch>
{
    private readonly SyncDictionary<FactionManager.Faction, List<string>> playersInMatch = new SyncDictionary<FactionManager.Faction, List<string>>()
    {
        { FactionManager.Faction.FactionA, new List<string>() },
        { FactionManager.Faction.FactionB, new List<string>() },
    };

    [ServerRpc(RequireOwnership = false)]
    public void RequestAddPlayer(FactionManager.Faction faction, string playerName)
    {
        AddPlayerServer(faction, playerName);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRemovePlayer(FactionManager.Faction faction, string playerName)
    {
        RemovePlayerServer(faction, playerName);
    }

    public void AddPlayerServer(FactionManager.Faction faction, string playerName)
    {
        if (!IsServerInitialized || !IsServerStarted) return;

        if (playersInMatch.TryGetValue(faction, out List<string> players))
        {
            players.Add(playerName);
            playersInMatch.Dirty(faction);
        }
        else playersInMatch[faction] = new List<string> { playerName };
    }

    public void RemovePlayerServer(FactionManager.Faction faction, string playerName)
    {
        if (!IsServerInitialized || !IsServerStarted) return;

        if (playersInMatch.TryGetValue(faction, out List<string> players) && players.Remove(playerName))
            playersInMatch.Dirty(faction);
    }
}
