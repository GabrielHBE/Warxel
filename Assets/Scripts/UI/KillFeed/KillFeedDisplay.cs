using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using System.Text;
using TMPro;
using UnityEngine.InputSystem.Utilities;
using FishNet.Object.Synchronizing;

public class KillFeedDisplay : ServerSingleton<KillFeedDisplay>
{
    [SerializeField] private TextMeshProUGUI kill_feed_container_text;
    private readonly SyncList<string> killfeedList = new SyncList<string>();
    private float timer;
    private StringBuilder sb = new StringBuilder();
    private const int MAX_KILLS = 12;
    private const float TIME_TO_DELETE_ITENS = 2;

    protected override void Awake()
    {
        base.Awake();
        killfeedList.OnChange += OnChangeKillfeedList;
    }

    void OnDestroy() => killfeedList.OnChange -= OnChangeKillfeedList;
    
    void Update()
    {
        if (!IsServerInitialized) return;

        if (killfeedList.Count > 0) timer += Time.deltaTime;

        if (timer >= TIME_TO_DELETE_ITENS && killfeedList.Count > 0)
        {
            RemoveKill();
            timer = 0;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveKill()
    {
        if (killfeedList.Count > 0)
        {
            killfeedList.RemoveAt(0);
            UpdateKillFeedText();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestAddKill(string killer_name, string victim_name, string item_used_to_kill)
    {
        if (killfeedList.Count == MAX_KILLS && killfeedList.Count > 0) killfeedList.RemoveAt(0);

        killfeedList.Add($"<color=#FF5733>{killer_name}</color> [{item_used_to_kill}] <color=#3399FF>{victim_name}</color>");
        
        UpdateKillFeedText();
    }

    private void UpdateKillFeedText()
    {
        if (kill_feed_container_text == null) return;

        sb.Clear(); // Limpa o StringBuilder para reutilização
        
        // Constrói a string com todas as mortes, separadas por \n
        foreach (string kill in killfeedList)
        {
            sb.AppendLine(kill);
        }

        // Remove a última quebra de linha extra
        if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
        {
            sb.Length--;
        }

        kill_feed_container_text.text = sb.ToString();
    }

    // Evento chamado quando a lista é alterada (servidor para cliente)
    private void OnChangeKillfeedList(SyncListOperation op, int index, string oldItem, string newItem, bool asServer)
    {
        if (!asServer) UpdateKillFeedText();
    }
}