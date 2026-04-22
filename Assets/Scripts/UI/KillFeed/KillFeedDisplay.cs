using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using System.Text;
using TMPro;

public class KillFeedDisplay : NetworkBehaviour
{
    public static KillFeedDisplay Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI kill_feed_container_text;
    private List<string> killfeed_list = new List<string>();

    private float timer_to_delete_itens = 1;
    private float timer;
    private StringBuilder sb = new StringBuilder();

    private int max_kills = 12;
    void Awake()
    {
        Instance = this;
    }

    [Server]
    void Update()
    {
        if (!IsServerInitialized) return;

        if (killfeed_list.Count > 0) timer += Time.deltaTime;

        if (timer >= timer_to_delete_itens && killfeed_list.Count > 0)
        {
            RemoveKill();
            timer = 0;
        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveKill()
    {
        killfeed_list.RemoveAt(0);


        if (killfeed_list.Count == 0)
        {
            UpdateClientKillFeed("");
            return;
        }

        foreach (string kill in killfeed_list)
        {
            sb.AppendLine(kill);
        }

        UpdateClientKillFeed(sb.ToString());
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddKill(string killer_name, string victim_name, string item_used_to_kill)
    {
        if (killfeed_list.Count == max_kills)
        {
            RemoveKill();
        }

        killfeed_list.Add($"<color=#FF5733>{killer_name}</color> [{item_used_to_kill}] <color=#3399FF>{victim_name}</color>");

        foreach (string kill in killfeed_list)
        {
            sb.AppendLine(kill);
        }

        UpdateClientKillFeed(sb.ToString());

    }

    [ObserversRpc]
    private void UpdateClientKillFeed(string text)
    {
        kill_feed_container_text.text = text;
    }

}
