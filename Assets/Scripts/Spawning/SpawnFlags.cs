using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public class SpawnFlags : NetworkBehaviour
{
    private System.Nullable<FactionManager.Faction> faction_in_control;
    private Color ally_on_controll_color = Color.blue;
    private Color enemy_on_controll_color = Color.red;
    private Color neutral_color = Color.white;

    [SerializeField] private Transform[] spawn_points;
    [SerializeField] private Image flag_image;

    private Dictionary<FactionManager.Faction, float> faction_capture_progress = new Dictionary<FactionManager.Faction, float>()
    {
        { FactionManager.Faction.factionA, 0 },
        { FactionManager.Faction.factionB, 0 }
    };

    private void OnMouseDown()
    {
        // Verifica se tem um controller local antes de tentar spawnar
        PlayerSpawnController localController = PlayerSpawnManager.Instance?.GetPlayerSpawnController();
        if (localController == null)
        {
            Debug.LogWarning("No local spawn controller found!");
            return;
        }

        // Só permite spawn se o jogador tiver um controller válido
        Transform selected_spawn_point = spawn_points[Random.Range(0, spawn_points.Length)];
        localController.InitializeSpawnPlayer(selected_spawn_point);
    }

    private void OnTriggerStay(Collider other)
    {
        // Capture logic should run on server
        if (!IsServerInitialized) return;
        
        //Capture the flag
        if (other.gameObject.layer == LayerMask.NameToLayer("Player") || other.gameObject.layer == LayerMask.NameToLayer("Vehicle"))
        {
            // Get the player's faction - you need to implement this properly
            FactionManager.Faction playerFaction = GetPlayerFaction(other.gameObject);
            
            if (playerFaction == FactionManager.Faction.factionA)
            {
                faction_capture_progress[FactionManager.Faction.factionA] += Time.deltaTime;
                faction_capture_progress[FactionManager.Faction.factionB] -= Time.deltaTime;
            }
            else if (playerFaction == FactionManager.Faction.factionB)
            {
                faction_capture_progress[FactionManager.Faction.factionB] += Time.deltaTime;
                faction_capture_progress[FactionManager.Faction.factionA] -= Time.deltaTime;
            }

            faction_capture_progress[FactionManager.Faction.factionA] = Mathf.Clamp(faction_capture_progress[FactionManager.Faction.factionA], 0, 1);
            faction_capture_progress[FactionManager.Faction.factionB] = Mathf.Clamp(faction_capture_progress[FactionManager.Faction.factionB], 0, 1);

            if (faction_capture_progress[FactionManager.Faction.factionA] == 1)
            {
                faction_in_control = FactionManager.Faction.factionA;
                UpdateFlagColorClientRpc(FactionManager.Faction.factionA);
            }
            else if (faction_capture_progress[FactionManager.Faction.factionB] == 1)
            {
                faction_in_control = FactionManager.Faction.factionB;
                UpdateFlagColorClientRpc(FactionManager.Faction.factionB);
            }
            else if (faction_capture_progress[FactionManager.Faction.factionA] == 0 && faction_capture_progress[FactionManager.Faction.factionB] == 0)
            {
                faction_in_control = null;
                UpdateFlagColorClientRpc(null);
            }
        }
    }

    [ObserversRpc]
    private void UpdateFlagColorClientRpc(System.Nullable<FactionManager.Faction> controllingFaction)
    {
        Color targetColor;
        
        if (controllingFaction == null)
        {
            targetColor = neutral_color;
        }
        else if (AccountManager.Instance.faction == controllingFaction)
        {
            targetColor = ally_on_controll_color;
        }
        else
        {
            targetColor = enemy_on_controll_color;
        }
        
        UpdateFlagColor(targetColor);
    }

    private void UpdateFlagColor(Color color)
    {
        flag_image.color = color;
    }
    
    private FactionManager.Faction GetPlayerFaction(GameObject player)
    {
        // Implement proper faction retrieval from player object
        // This should come from your player data
        return AccountManager.Instance.faction;
    }
}