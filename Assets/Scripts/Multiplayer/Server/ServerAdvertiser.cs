using FishNet;
using FishNet.Discovery;
using FishNet.Transporting;
using UnityEngine;

// Este script deve ficar NO MESMO GAMEOBJECT que o NetworkManager e o NetworkDiscovery na cena do Menu!
[RequireComponent(typeof(NetworkDiscovery))]
public class ServerAdvertiser : MonoBehaviour
{
    private NetworkDiscovery _networkDiscovery;

    private void Start()
    {
        _networkDiscovery = GetComponent<NetworkDiscovery>();

        // Inscreve-se no evento global de conexão do servidor
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
        }
    }

    private void OnDestroy()
    {
        // Limpa o evento para evitar memory leaks
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        }
    }

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Assim que o servidor iniciar (seja na cena do Menu ou após carregar o Jogo), ele começa a anunciar
            _networkDiscovery.AdvertiseServer();
            Debug.Log("Servidor está online e anunciando na LAN.");
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            // Quando o servidor desligar, para de anunciar
            _networkDiscovery.StopSearchingOrAdvertising();
            Debug.Log("Servidor parou de anunciar.");
        }
    }
}