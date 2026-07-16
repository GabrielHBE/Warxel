using System.Collections;
using TMPro;
using UnityEngine;

public class AccountMainMenu : MainMenuTabs
{
    [Header("Button")]
    [SerializeField] private UnityEngine.UI.Button switch_faction_button;

    [Header("Accounts references")]
    [SerializeField] private TMP_InputField account_name;
    [SerializeField] private TextMeshProUGUI current_battle_coins;
    [SerializeField] private TextMeshProUGUI account_level;
    [SerializeField] private TextMeshProUGUI account_faction;
    [SerializeField] private TextMeshProUGUI selected_class;

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
        account_name.text = AccountManager.Instance.account_name;
        switch_faction_button.onClick.AddListener(SwitchFaction);
    }

    private void SwitchFaction()
    {
        FactionManager.Faction current_faction = AccountManager.Instance.faction;
        if (current_faction == FactionManager.Faction.FactionA)
        {
            AccountManager.Instance.SwitchFaction(FactionManager.Faction.FactionB);
        }
        else
        {
            AccountManager.Instance.SwitchFaction(FactionManager.Faction.FactionA);
        }
    }

    void Update()
    {
        // Só atualiza se o AccountManager já estiver pronto
        if (AccountManager.Instance == null) return;

        // Não sobrescreva se o campo estiver vazio no início
        if (!string.IsNullOrEmpty(account_name.text))
        {
            if (account_name.text != AccountManager.Instance.account_name) AccountManager.Instance.SwitchName(account_name.text);
        }

        current_battle_coins.text = "Current Battle Coins: " + AccountManager.Instance.battle_coins.ToString();
        account_level.text = "Accont Level: " + AccountManager.Instance.level.ToString();
        account_faction.text = "Current Faction: " + AccountManager.Instance.faction.ToString();
        selected_class.text = "Selected Class: " + AccountManager.Instance.selected_class.ToString().Replace("_", " ");

        //Account Status
        most_used_class.text = "Most Used Class: " + AccountManager.Instance.status.most_used_class.ToString().Replace("_", " ");
        kd_ratio.text = "K/D Ratio: " + AccountManager.Instance.status.kd_ratio.ToString();
        total_head_shot_kills.text = "Head Shot Kills: " + AccountManager.Instance.status.total_head_shot_kills.ToString();
        total_kills.text = "Total Kills: " + AccountManager.Instance.status.total_kills.ToString();
        total_assists.text = "Total Assists: " + AccountManager.Instance.status.total_assists.ToString();
        total_deaths.text = "Total Deaths: " + AccountManager.Instance.status.total_deaths.ToString();
        if (AccountManager.Instance.status.most_used_weapon != null)
        {
            most_used_weapon.text = "Most Used Weapon: " + AccountManager.Instance.status.most_used_weapon.ToString();
        }
        else
        {
            most_used_weapon.text = "Most Used Weapon: None";
        }
        total_matches_played.text = "Total Matches Played: " + AccountManager.Instance.status.total_matches_played.ToString();
        total_matches_won.text = "Total Matches Won: " + AccountManager.Instance.status.total_matches_won.ToString();
        total_matches_lost.text = "Total Matches Lost: " + AccountManager.Instance.status.total_matches_lost.ToString();
        win_rate.text = "Win Rate: " + AccountManager.Instance.status.win_rate.ToString();
    }

}
