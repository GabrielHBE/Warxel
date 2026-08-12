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
        if (playersInMatch.TryGetValue(faction, out List<string> players)) players.Add(playerName);
        else playersInMatch[faction] = new List<string> { playerName };
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRemovePlayer(FactionManager.Faction faction, string playerName)
    {
        if (playersInMatch.TryGetValue(faction, out List<string> players)) players.Remove(playerName);
        else playersInMatch[faction] = new List<string> { playerName };
    }
}
