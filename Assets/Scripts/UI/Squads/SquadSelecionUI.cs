using UnityEngine;
using TMPro;
using FishNet.Connection;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using FishNet.Object.Synchronizing;
using System;

public class SquadSelecionUI : InMatchClientSingleton<SquadSelecionUI>
{
    [Header("UI References")]
    [SerializeField] private Transform squadListContainer;
    [SerializeField] private GameObject squadItemPrefab;
    [SerializeField] private TMP_Text squadStatusText;
    [SerializeField] private Button refreshButton;

    [Header("Layout Settings")]
    [SerializeField] private float itemStartY = -50f;
    [SerializeField] private float itemSpacingY = 120f;
    [SerializeField] private float itemX = 0f;

    private SquadManager squadManager;
    private AccountManager accountManager;
    
    private Dictionary<SquadManager.SquadName, SquadUIItem> squadUIItems = new Dictionary<SquadManager.SquadName, SquadUIItem>();
    private SquadManager.SquadName? currentSquad;
    private bool isInSquad = false;
    private bool isInitialized = false;

    private readonly List<GameObject> _squadItemsList = new List<GameObject>();
    private Coroutine pollCoroutine;

    public event Action OnUIIsReady;

    private IEnumerator Start()
    {
        while (SquadManager.Instance == null || AccountManager.Instance == null)
        {
            yield return null;
        }

        while (!SquadManager.Instance.isSetup.Value)
        {
            yield return null;
        }
        
        squadManager = SquadManager.Instance;
        accountManager = AccountManager.Instance;

        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshSquadList);

        SubscribeToSquadEvents();

        isInitialized = true;
        OnUIIsReady?.Invoke();
        
        yield return null;
        RefreshSquadList();
    }

    // Obtém a conexão do jogador local dinamicamente
    private NetworkConnection GetPlayerConnection()
    {
        if (ClientManager.Instance != null && ClientManager.Instance.clientNetworkConnection.Value != null) return ClientManager.Instance.clientNetworkConnection.Value;
        
        return ClientManager.Instance != null ? ClientManager.Instance.LocalConnection : null;
    }

    private void SubscribeToSquadEvents()
    {
        if (squadManager == null) return;

        // Inscrição nativa do SyncDictionary do FishNet
        squadManager.squads.OnChange += OnSquadsChanged;
        StartPolling(); // Polling mantido como fallback de segurança
    }

    private void UnsubscribeFromSquadEvents()
    {
        if (squadManager == null) return;

        squadManager.squads.OnChange -= OnSquadsChanged;
        
        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
            pollCoroutine = null;
        }
    }

    private void OnSquadsChanged(SyncDictionaryOperation op, FactionManager.Faction key, List<SquadManager.SquadData> value, bool asServer)
    {
        if (accountManager != null && key == accountManager.selectedFaction) RefreshSquadList();
    }

    private void StartPolling()
    {
        if (pollCoroutine != null)  StopCoroutine(pollCoroutine);
        
        pollCoroutine = StartCoroutine(PollForUpdates());
    }

    private IEnumerator PollForUpdates()
    {
        while (isInitialized && gameObject != null && gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(1.5f);
            if (isInitialized && gameObject != null && gameObject.activeInHierarchy)
            {
                RefreshSquadList();
            }
        }
    }

    private void OnEnable()
    {
        if (isInitialized) 
        {
            RefreshSquadList();
            SubscribeToSquadEvents();
        }
    }

    private void OnDisable() => UnsubscribeFromSquadEvents();
    
    private void OnDestroy()
    {
        if (refreshButton != null) refreshButton.onClick.RemoveListener(RefreshSquadList);
        
        UnsubscribeFromSquadEvents();
    }

    public void RefreshSquadList()
    {
        if (!isInitialized || squadManager == null || accountManager == null) return;

        var faction = accountManager.selectedFaction;
        
        CheckPlayerSquadStatus(faction);
        ClearSquadList();

        if (!squadManager.squads.ContainsKey(faction) || squadManager.squads[faction].Count == 0)
        {
            UpdateUIStatus("No squads are available for your faction.", false);
            return;
        }

        var squadsList = squadManager.squads[faction];
        
        int itemIndex = 0;
        foreach (var squad in squadsList)
        {
            CreateSquadUIItem(squad, faction, itemIndex);
            itemIndex++;
        }

        UpdateUIStatus($"Showing {squadsList.Count} squads for faction {faction}", true);
    }

    private void ClearSquadList()
    {
        if (squadListContainer == null) return;

        foreach (Transform child in squadListContainer)
        {
            Destroy(child.gameObject);
        }
        
        squadUIItems.Clear();
        _squadItemsList.Clear();
    }

    private void CreateSquadUIItem(SquadManager.SquadData squad, FactionManager.Faction faction, int index)
    {
        if (squadItemPrefab == null || squadListContainer == null) return;

        GameObject squadItemGO = Instantiate(squadItemPrefab, squadListContainer);
        ApplyVerticalSpacing(squadItemGO, index);

        SquadUIItem squadUIItem = squadItemGO.GetComponent<SquadUIItem>();
        if (squadUIItem == null)
        {
            Destroy(squadItemGO);
            return;
        }

        bool isPlayerInThisSquad = IsPlayerInSquad(squad);
        bool isSquadFull = squad.squadMembers.Length >= SquadManager.MAX_MEMBERS_PER_SQUAD;
        
        squadUIItem.Initialize(
            squad.squadName,
            squad.squadMembers,
            isPlayerInThisSquad,
            isSquadFull,
            OnSquadActionButtonClicked
        );

        squadUIItems[squad.squadName] = squadUIItem;
        _squadItemsList.Add(squadItemGO);
    }

    private void ApplyVerticalSpacing(GameObject item, int index)
    {
        RectTransform rectTransform = item.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            float yPosition = itemStartY + (index * itemSpacingY);
            rectTransform.anchoredPosition = new Vector2(itemX, yPosition);
        }
    }

    private void CheckPlayerSquadStatus(FactionManager.Faction faction)
    {
        isInSquad = false;
        currentSquad = null;

        if (!squadManager.squads.ContainsKey(faction)) return;

        foreach (var squad in squadManager.squads[faction])
        {
            if (IsPlayerInSquad(squad))
            {
                isInSquad = true;
                currentSquad = squad.squadName;
                break;
            }
        }
    }

    private bool IsPlayerInSquad(SquadManager.SquadData squad)
    {
        var localConnection = GetPlayerConnection();
        if (localConnection == null || !localConnection.IsValid) return false;

        foreach (var member in squad.squadMembers)
        {
            // Comparação por ClientId para evitar falhas de referência de memória na rede
            if (member.connection != null && member.connection.ClientId == localConnection.ClientId)
                return true;
        }
        return false;
    }

    private void OnSquadActionButtonClicked(SquadManager.SquadName squadName, bool isCurrentlyInSquad)
    {
        if (isCurrentlyInSquad) ExitSquad();
        else JoinSpecificSquad(squadName);
        
    }

    public void JoinSpecificSquad(SquadManager.SquadName squadName)
    {
        if (!isInitialized || squadManager == null || accountManager == null) return;

        if (isInSquad)
        {
            UpdateUIStatus("You are already in a squad. Leave it before joining another one.", false);
            return;
        }

        var faction = accountManager.selectedFaction;
        var playerConn = GetPlayerConnection();

        bool success = squadManager.AddMemberToSquad(faction, squadName, playerConn, accountManager.accountName);
        if (success)
        {
            isInSquad = true;
            currentSquad = squadName;
            RefreshSquadList();
        }
    }

    public void ExitSquad()
    {
        if (!isInitialized || !isInSquad || accountManager == null || squadManager == null) return;

        var playerConn = GetPlayerConnection();
        bool success = squadManager.RemoveMemberFromSquad(accountManager.selectedFaction, playerConn);
        
        if (success)
        {
            isInSquad = false;
            currentSquad = null;
            RefreshSquadList();
        }
    }

    private void UpdateUIStatus(string message, bool success)
    {
        if (squadStatusText != null)
        {
            squadStatusText.text = message;
            squadStatusText.color = success ? Color.green : Color.red;
        }
    }

    public bool IsInSquad() => isInSquad;
    public SquadManager.SquadName? GetCurrentSquad() => currentSquad;
    public bool IsInitialized() => isInitialized;
    
    public void ForceRefreshUI()
    {
        if (isInitialized) RefreshSquadList();
    }
}
