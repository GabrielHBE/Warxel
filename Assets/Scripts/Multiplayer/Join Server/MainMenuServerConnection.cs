using UnityEngine;
using TMPro;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Transporting;

public class MainMenuConnection : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField ipInputField;
    
    [Header("Loading UI")]
    [SerializeField] private GameObject loadingPanel; // Arraste o painel da tela de loading aqui
    [SerializeField] private TextMeshProUGUI loadingText; // Arraste o texto do loading aqui

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "SampleScene";

    private const string DEFAULT_IP = "localhost";

    private void OnEnable()
    {
        // Se inscreve no evento para saber quando o servidor ligar
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;

        // Se inscreve no evento do cliente para monitorar erros e sucessos na conexão
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
    }

    private void OnDisable()
    {
        // Se desinscreve para evitar vazamento de memória
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;

        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
    }

    public void ConnectAsClient()
    {
        ShowLoading("Conectando ao servidor...");

        if (AccountManager.Instance != null) Destroy(AccountManager.Instance.gameObject);
        if (Settings.Instance != null) Destroy(Settings.Instance.gameObject);
        
        string ipAddress = string.IsNullOrWhiteSpace(ipInputField.text) ? DEFAULT_IP : ipInputField.text;
        InstanceFinder.ClientManager.StartConnection(ipAddress);
    }

    public void StartAsHost()
    {
        ShowLoading("Iniciando servidor...");

        if (AccountManager.Instance != null) Destroy(AccountManager.Instance.gameObject);
        if (Settings.Instance != null) Destroy(Settings.Instance.gameObject);

        // Apenas pede para iniciar. A cena será carregada pelo evento abaixo!
        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();
    }

    // Este evento é disparado automaticamente quando o status do Servidor muda
    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs args)
    {
        // Se o servidor acabou de ligar com sucesso...
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            if (loadingText != null) loadingText.text = "Carregando mundo...";

            SceneLoadData sld = new SceneLoadData(gameSceneName);
            sld.ReplaceScenes = ReplaceOption.All;
            InstanceFinder.SceneManager.LoadGlobalScenes(sld);
        }
    }

    // Este evento é disparado automaticamente quando o status do Cliente muda
    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // O cliente conectou com sucesso. O servidor vai mandar a instrução de trocar de cena.
            if (loadingText != null) loadingText.text = "Carregando mundo...";
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            // A conexão falhou (IP errado, servidor offline, etc.)
            HideLoading();
            
            // Opcional: Você pode chamar uma mensagem de erro aqui
            Debug.LogWarning("Falha ao conectar ou servidor desconectado.");
        }
    }

    private void ShowLoading(string message)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (loadingText != null) loadingText.text = message;
    }

    private void HideLoading()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
}