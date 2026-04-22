using UnityEngine;
using TMPro;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using Unity.VisualScripting; // Necessário para os eventos de conexão

public class MainMenuConnection : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField ipInputField;

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "SampleScene";

    private const string DEFAULT_IP = "localhost";

    private void OnEnable()
    {
        // Se inscreve no evento para saber quando o servidor ligar
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
    }

    private void OnDisable()
    {
        // Se desinscreve para evitar vazamento de memória
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
    }

    public void ConnectAsClient()
    {
        if (AccountManager.Instance != null) Destroy(AccountManager.Instance.gameObject);
        if (Settings.Instance != null) Destroy(Settings.Instance.gameObject);
        string ipAddress = string.IsNullOrWhiteSpace(ipInputField.text) ? DEFAULT_IP : ipInputField.text;
        InstanceFinder.ClientManager.StartConnection(ipAddress);
    }

    public void StartAsHost()
    {
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
            SceneLoadData sld = new SceneLoadData(gameSceneName);
            sld.ReplaceScenes = ReplaceOption.All;
            InstanceFinder.SceneManager.LoadGlobalScenes(sld);
        }
    }
}