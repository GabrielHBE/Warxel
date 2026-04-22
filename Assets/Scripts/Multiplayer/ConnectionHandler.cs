using FishNet.Managing;
using UnityEngine;

public enum ConnectionType
{
    Host,
    Client,
    Server
}

public class ConnectionHandler : MonoBehaviour
{
    [SerializeField] private ConnectionType connectionType;
    [SerializeField] private NetworkManager networkManager;

    void Start()
    {
        if (connectionType == ConnectionType.Client)
        {
            networkManager.ClientManager.StartConnection();
        }
        else if (connectionType == ConnectionType.Server)
        {
            networkManager.ServerManager.StartConnection();
        }
        else if (connectionType == ConnectionType.Host)
        {
            // Inicia como Host (Servidor + Cliente)
            networkManager.ServerManager.StartConnection(); // Inicia servidor
            networkManager.ClientManager.StartConnection(); // Inicia cliente
        }

    }

}
