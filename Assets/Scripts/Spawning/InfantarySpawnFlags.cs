using UnityEngine;

public class InfantarySpawnFlags : MonoBehaviour
{
    [SerializeField] private FlagCapture flagCapture;

    [SerializeField] private Transform[] spawn_points;

    [Header("Click Settings")]
    private float doubleClickThreshold = 0.2f; // Tempo máximo entre os cliques para ser considerado duplo
    private float lastClickTime = 0f;

    private void OnMouseDown()
    {
        // Calcula o tempo desde o último clique
        float timeSinceLastClick = Time.time - lastClickTime;

        // Atualiza o tempo do último clique para o tempo atual
        lastClickTime = Time.time;

        // Se o tempo entre os cliques for menor que o limite, é um clique duplo!
        if (timeSinceLastClick <= doubleClickThreshold)
        {
            ExecuteSpawn();
        }
    }

    private void ExecuteSpawn()
    {

        // Só permite spawn se o jogador tiver um controller válido
        Transform selected_spawn_point = spawn_points[Random.Range(0, spawn_points.Length)];
        PlayerSpawnController.Instance.InitializeSpawnPlayer(selected_spawn_point);
    }

}