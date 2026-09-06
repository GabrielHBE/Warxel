using TMPro;
using UnityEngine;

public class AccountMainMenu : MainMenuTabs
{
    [Header("Button")]
    [SerializeField] private UnityEngine.UI.Button switch_faction_button;

    [Header("Accounts references")]
    [SerializeField] private TMP_InputField accountName;
    [SerializeField] private TextMeshProUGUI current_battle_coins;
    [SerializeField] private TextMeshProUGUI account_level;
    [SerializeField] private TextMeshProUGUI account_faction;
    [SerializeField] private TextMeshProUGUI selectedClass;

    [Header("Accounts Status references")]
    [SerializeField] private TextMeshProUGUI most_used_class;
    [SerializeField] private TextMeshProUGUI kd_ratio;
    [SerializeField] private TextMeshProUGUI total_head_shot_kills;
    [SerializeField] private TextMeshProUGUI total_kills;
    [SerializeField] private TextMeshProUGUI total_assists;
    [SerializeField] private TextMeshProUGUI total_deaths;
    [SerializeField] private TextMeshProUGUI most_used_weapon;
    [SerializeField] private TextMeshProUGUI total_matches_played;
    [SerializeField] private TextMeshProUGUI total_matches_won;
    [SerializeField] private TextMeshProUGUI total_matches_lost;
    [SerializeField] private TextMeshProUGUI win_rate;


    public override void Activate()
    {
        accountName.text = AccountManager.Instance.accountName;
        switch_faction_button.onClick.AddListener(SwitchFaction);
    }

    private void SwitchFaction()
    {
        FactionManager.Faction current_faction = AccountManager.Instance.selectedFaction;
        if (current_faction == FactionManager.Faction.FactionA) AccountManager.Instance.SwitchFaction(FactionManager.Faction.FactionB);
        else AccountManager.Instance.SwitchFaction(FactionManager.Faction.FactionA);
    }

    void Update()
    {
        if (AccountManager.Instance == null) return;

        if (!string.IsNullOrEmpty(accountName.text) && accountName.text != AccountManager.Instance.accountName) AccountManager.Instance.SwitchName(accountName.text);

        current_battle_coins.text = "Current Battle Coins: " + AccountManager.Instance.battleCoins.ToString();
        account_level.text = "Accont Level: " + AccountManager.Instance.level.ToString();
        account_faction.text = "Current Faction: " + AccountManager.Instance.selectedFaction.ToString();
        selectedClass.text = "Selected Class: " + AccountManager.Instance.selectedClass.ToString().Replace("_", " ");

        //Account Status
        most_used_class.text = "Most Used Class: " + AccountManager.Instance.accountStatus.most_used_class.ToString().Replace("_", " ");
        kd_ratio.text = "K/D Ratio: " + AccountManager.Instance.accountStatus.kd_ratio.ToString();
        total_head_shot_kills.text = "Head Shot Kills: " + AccountManager.Instance.accountStatus.total_head_shot_kills.ToString();
        total_kills.text = "Total Kills: " + AccountManager.Instance.accountStatus.total_kills.ToString();
        total_assists.text = "Total Assists: " + AccountManager.Instance.accountStatus.total_assists.ToString();
        total_deaths.text = "Total Deaths: " + AccountManager.Instance.accountStatus.total_deaths.ToString();
        if (AccountManager.Instance.accountStatus.most_used_weapon != null) most_used_weapon.text = "Most Used Weapon: " + AccountManager.Instance.accountStatus.most_used_weapon.ToString();
        else most_used_weapon.text = "Most Used Weapon: None"; 
        total_matches_played.text = "Total Matches Played: " + AccountManager.Instance.accountStatus.total_matches_played.ToString();
        total_matches_won.text = "Total Matches Won: " + AccountManager.Instance.accountStatus.total_matches_won.ToString();
        total_matches_lost.text = "Total Matches Lost: " + AccountManager.Instance.accountStatus.total_matches_lost.ToString();
        win_rate.text = "Win Rate: " + AccountManager.Instance.accountStatus.win_rate.ToString();
    }

}
