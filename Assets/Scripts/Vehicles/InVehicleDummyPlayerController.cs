using FishNet.Object;
using UnityEngine;

public class InVehicleDummyPlayerController : NetworkBehaviour
{
    [SerializeField] private GameObject dummyPlayer;
    
    // ==========================================
    // INSTANTIATE DUMMY PLAYER
    // ==========================================
    [ServerRpc(RequireOwnership = false)]
    public void RequestInstantiateDummyPlayer(Transform parent) => CmdInstantiateDummyPlayer(parent);

    [ObserversRpc]
    public void CmdInstantiateDummyPlayer(Transform parent) => InstantiateDummyPlayer(parent);

    public void InstantiateDummyPlayer(Transform parent)
    {
        if (parent == null) return;
        Instantiate(dummyPlayer, parent);
    }
    
    // ==========================================
    // DESTROY DUMMY PLAYER
    // ==========================================
    [ServerRpc(RequireOwnership = false)]
    public void RequestDestroyDummyPlayer(Transform parent) => CmdDestroyDummyPlayer(parent);

    [ObserversRpc]
    public void CmdDestroyDummyPlayer(Transform parent) => DestroyDummyPlayer(parent);

    public void DestroyDummyPlayer(Transform parent)
    {
        if (parent == null) return;

        // Percorre todos os objetos filhos do parent informado
        foreach (Transform child in parent)
        {
            // O Unity adiciona "(Clone)" ao nome dos objetos instanciados.
            // Verificamos se o nome do filho contém o nome do prefab original.
            if (child.gameObject.name.Contains(dummyPlayer.name))
            {
                Destroy(child.gameObject);
                break; // Encerra o loop assim que encontrar e destruir o objeto
            }
        }
    }
}