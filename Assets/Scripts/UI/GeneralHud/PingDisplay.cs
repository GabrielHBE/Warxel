using UnityEngine;
using FishNet.Managing;
using FishNet;
using TMPro;

public class PingDisplay : PersistentLocalSingleton<PingDisplay>
{
    //public static PingDisplay Instance {get; private set;}

    [SerializeField] private TextMeshProUGUI pingText;
    private NetworkManager networkManager;

    protected override void Awake()
    {
        base.Awake();
        networkManager = InstanceFinder.NetworkManager;
    }

    private void Update()
    {
        if (networkManager != null && Settings.Instance._gameplay.show_network_status)
        {
            float ping = networkManager.TimeManager.RoundTripTime;
            pingText.text = $"Ping: {ping:F0} ms";

            // Código de cor baseado na latência
            if (ping < 50)
                pingText.color = Color.green;
            else if (ping < 100)
                pingText.color = Color.yellow;
            else
                pingText.color = Color.red;
        }
        else
        {
            pingText.text = "";
        }
    }

}
