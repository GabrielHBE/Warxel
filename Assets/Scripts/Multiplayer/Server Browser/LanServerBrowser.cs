using FishNet;
using FishNet.Discovery;
using System.Net;
using UnityEngine;
using TMPro;

public class LANServerBrowser : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MainMenuConnection mainMenuConnection;

    [Header("FishNet Setup")]
    [SerializeField] private NetworkDiscovery networkDiscovery;

    [Header("UI References")]
    [SerializeField] private Transform serverListParent; // Onde os botões vão ser gerados (ex: Content de um ScrollView)
    [SerializeField] private GameObject serverButtonPrefab; // Um prefab de botão com um TextMeshProUGUI

    private void OnEnable()
    {
        if (networkDiscovery != null)  networkDiscovery.ServerFoundCallback += OnServerFound;

        StartSearching();
    }

    private void OnDisable()
    {
        if (networkDiscovery != null) networkDiscovery.ServerFoundCallback -= OnServerFound;
        
        StopSearching();
    }

    public void StartSearching()
    {
        ClearServerList();

        if (networkDiscovery != null)
        {
            networkDiscovery.SearchForServers();
            Debug.Log("Searching for servers on the LAN...");
        }
    }

    // Chame este método se quiser parar a busca manualmente
    public void StopSearching()
    {
        if (networkDiscovery != null)
        {

            if (InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started) return;

            networkDiscovery.StopSearchingOrAdvertising();

        }
    }
    private void OnServerFound(IPEndPoint endpoint)
    {
        GameObject newButton = Instantiate(serverButtonPrefab, serverListParent);

        TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)  buttonText.text = $"Server: {endpoint.Address}";
        
        UnityEngine.UI.Button btn = newButton.GetComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(() => ConnectToServer(endpoint.Address.ToString()));
    }

    private void ConnectToServer(string ipAddress)
    {
        if (mainMenuConnection != null) mainMenuConnection.StartMapImage("Connecting to server...");


        StopSearching();

        InstanceFinder.ClientManager.StartConnection(ipAddress);
    }

    private void ClearServerList()
    {
        foreach (Transform child in serverListParent)
        {
            Destroy(child.gameObject);
        }
    }
}
