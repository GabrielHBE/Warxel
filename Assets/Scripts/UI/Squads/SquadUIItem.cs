using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SquadUIItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI squadNameText;
    [SerializeField] private TextMeshProUGUI squadCountText;
    [SerializeField] private Button expandButton;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private Transform membersContainer;
    [SerializeField] private GameObject memberPrefab;
    [SerializeField] private GameObject membersPanel;

    private SquadManager.SquadName squadName;
    private SquadManager.SquadMemberData[] squadMembers;
    private bool isExpanded = false;
    private bool isPlayerInSquad = false;
    private bool isSquadFull = false;
    private System.Action<SquadManager.SquadName, bool> onActionClicked;

    public void Initialize(
        SquadManager.SquadName name,
        SquadManager.SquadMemberData[] members,
        bool playerInSquad,
        bool squadFull,
        System.Action<SquadManager.SquadName, bool> actionCallback)
    {
        squadName = name;
        squadMembers = members;
        isPlayerInSquad = playerInSquad;
        isSquadFull = squadFull;
        onActionClicked = actionCallback;

        // Configura o nome do squad
        if (squadNameText != null)
            squadNameText.text = name.ToString();

        // Configura o contador de membros
        if (squadCountText != null)
            squadCountText.text = $"{members.Length}/{SquadManager.MAX_MEMBERS_PER_SQUAD}";

        // Configura o botão de ação
        ConfigureActionButton();

        // Configura o botão de expandir
        if (expandButton != null)
            expandButton.onClick.AddListener(ToggleExpand);

        // Inicialmente o membersPanel fica escondido
        if (membersPanel != null)
            membersPanel.SetActive(false);

        // Popula os membros
        PopulateMembers();
    }

    private void ConfigureActionButton()
    {
        if (actionButton == null || actionButtonText == null) return;

        if (isPlayerInSquad)
        {
            actionButtonText.text = "Leave";
            actionButton.interactable = true;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => onActionClicked?.Invoke(squadName, true));
        }
        else if (isSquadFull)
        {
            actionButtonText.text = "Full";
            actionButton.interactable = false;
        }
        else
        {
            actionButtonText.text = "Enter";
            actionButton.interactable = true;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => onActionClicked?.Invoke(squadName, false));
        }
    }

    private void ToggleExpand()
    {
        isExpanded = !isExpanded;

        if (membersPanel != null)
            membersPanel.SetActive(isExpanded);
    }

    private void PopulateMembers()
    {
        if (membersContainer == null || memberPrefab == null) return;

        foreach (Transform child in membersContainer)
        {
            Destroy(child.gameObject);
        }

        if (squadMembers.Length == 0)
        {
            GameObject emptyGO = Instantiate(memberPrefab, membersContainer);
            TextMeshProUGUI emptyText = emptyGO.GetComponentInChildren<TextMeshProUGUI>();
            if (emptyText != null)
            {
                emptyText.text = "Nenhum membro";
                emptyText.color = Color.gray;
                emptyText.fontStyle = FontStyles.Italic;
            }
            return;
        }

        var localConn = ClientManager.Instance != null ? ClientManager.Instance.clientNetworkConnection.Value : null;

        foreach (var member in squadMembers)
        {
            GameObject memberGO = Instantiate(memberPrefab, membersContainer);
            TextMeshProUGUI memberText = memberGO.GetComponentInChildren<TextMeshProUGUI>();

            if (memberText != null)
            {
                // Comparação segura por ClientId
                bool isCurrentPlayer = (member.connection != null && localConn != null && member.connection.ClientId == localConn.ClientId);

                memberText.text = isCurrentPlayer ? $"{member.playerName} (Você)" : member.playerName;

                Image background = memberGO.GetComponent<Image>();
                if (background != null && isCurrentPlayer)
                {
                    background.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
                }
            }
        }
    }

    public void UpdateSquadStatus(SquadManager.SquadMemberData[] updatedMembers, bool playerInSquad)
    {
        squadMembers = updatedMembers;
        isPlayerInSquad = playerInSquad;

        // Atualiza o contador
        if (squadCountText != null)
            squadCountText.text = $"{updatedMembers.Length}/{SquadManager.MAX_MEMBERS_PER_SQUAD}";

        // Atualiza o botão de ação
        ConfigureActionButton();

        // Atualiza a lista de membros
        PopulateMembers();
    }

    private void OnDestroy()
    {
        if (expandButton != null)
            expandButton.onClick.RemoveListener(ToggleExpand);

        if (actionButton != null)
            actionButton.onClick.RemoveAllListeners();
    }
}