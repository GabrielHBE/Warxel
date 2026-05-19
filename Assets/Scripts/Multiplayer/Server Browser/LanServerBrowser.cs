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
        if (networkDiscovery != null)
        {
            // Inscreve-se no evento disparado quando um servidor é encontrado
            networkDiscovery.ServerFoundCallback += OnServerFound;
        }

        // Inicia a busca automaticamente assim que o painel for ativado na tela!
        StartSearching();
    }

    private void OnDisable()
    {
        if (networkDiscovery != null)
        {
            networkDiscovery.ServerFoundCallback -= OnServerFound;
        }

        // Para a busca ao fechar o painel para não gastar processamento à toa
        StopSearching();
    }

    // Chame este método em um botão de "Atualizar / Search" caso o jogador queira recarregar a lista
    public void StartSearching()
    {
        ClearServerList();

        if (networkDiscovery != null)
        {
            networkDiscovery.SearchForServers();
            Debug.Log("Procurando servidores na LAN...");
        }
    }

    // Chame este método se quiser parar a busca manualmente
    public void StopSearching()
    {
        if (networkDiscovery != null)
        {
            // TRAVA DE SEGURANÇA:
            // Se nós somos o Host (o servidor está ligado), não podemos usar o comando que desliga tudo,
            // senão nós vamos matar o nosso próprio anúncio!
            if (InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started)
            {
                // Se a sua versão do FishNet Discovery tiver a função "StopSearching()", use ela:
                // networkDiscovery.StopSearching();

                Debug.Log("Busca interrompida. (Anúncio do servidor mantido ligado).");
                return; // Sai da função antes de desligar o anúncio
            }

            // Se não somos o Host, é seguro desligar tudo
            networkDiscovery.StopSearchingOrAdvertising();
            Debug.Log("Busca de servidores interrompida.");
        }
    }
    private void OnServerFound(IPEndPoint endpoint)
    {
        // Instancia o botão na lista
        GameObject newButton = Instantiate(serverButtonPrefab, serverListParent);

        // Pega o texto e coloca o IP do servidor
        TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = $"Servidor: {endpoint.Address}";
        }

        // Adiciona a lógica de clique no botão gerado
        UnityEngine.UI.Button btn = newButton.GetComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(() => ConnectToServer(endpoint.Address.ToString()));
    }

    private void ConnectToServer(string ipAddress)
    {
        if (mainMenuConnection != null)
            mainMenuConnection.StartMapImage("Conectando ao servidor...");

        // Para a busca antes de conectar para evitar problemas
        StopSearching();

        // Inicia a conexão usando o IP encontrado
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