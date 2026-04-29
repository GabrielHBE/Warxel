using UnityEngine;

public class SpawnFlags : MonoBehaviour
{

    [SerializeField] private Transform[] spawn_points;

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

}