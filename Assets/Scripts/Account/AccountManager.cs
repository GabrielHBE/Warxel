using TMPro;
using UnityEngine;

public class AccountManager : PersistentLocalSingleton<AccountManager>
{
    public string accountName;
    public string id;
    public int level;
    public int battleCoins;
    public FactionManager.Faction selectedFaction;
    public ClassManager.Class selectedClass;

    private int current_level_progression;
    private int pointsToLevelUp = 100;

    //Testing
    public UnityEngine.UI.Button switch_faction_button;
    public AccountStatus accountStatus = new AccountStatus();

    protected override void Awake()
    {
        LoadData();
        accountStatus.Initialize();

        base.Awake();
    }

    //Debug
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) AddBattleCoin(100);
    }

    public void SetClass(ClassManager.Class @class)
    {
        selectedClass = @class;
        SaveData();
    }

    public void SetFaction(FactionManager.Faction @selectedFaction)
    {
        this.selectedFaction = @selectedFaction;
        switch_faction_button.GetComponentInChildren<TextMeshProUGUI>().text = this.selectedFaction.ToString();
        SaveData();
    }

    public void AddBattleCoin(int qnt)
    {
        battleCoins += qnt;
        BattleCoinsUI.Instance.UpdateCurrentBattleCoins(battleCoins, qnt);

        SaveData();
    }

    public void RemoveBattleCoin(int qnt)
    {
        battleCoins -= qnt;
        if (battleCoins < 0) battleCoins = 0;
        SaveData();
    }

    public void SwitchFaction(FactionManager.Faction selectedFaction)
    {
        RemoveBattleCoin(100);
        this.selectedFaction = selectedFaction;
        SaveData();
    }

    public void SwitchName(string name)
    {
        accountName = name;
        SaveData();
    }

    public void AddPointsToLevelUp(int points)
    {
        current_level_progression += points;
        if (current_level_progression >= pointsToLevelUp)
        {
            LevelUp();
            current_level_progression = 0;
        }
        SaveData();
    }

    private void LevelUp()
    {
        current_level_progression = 0;
        level += 1;
        SaveData();
    }

    public void SaveData()
    {
        PlayerPrefs.SetString("AccountManager_accountName", accountName);
        PlayerPrefs.SetString("AccountManager_id", id);
        PlayerPrefs.SetString("AccountManager_selected_class", selectedClass.ToString());
        PlayerPrefs.SetInt("AccountManager_level", level);
        PlayerPrefs.SetInt("AccountManager_battle_coins", battleCoins);
        PlayerPrefs.SetInt("AccountManager_faction", (int)selectedFaction);
        PlayerPrefs.SetInt("AccountManager_current_level_progression", current_level_progression);
        PlayerPrefs.Save();
    }

    // Método para carregar todos os dados
    public void LoadData()
    {
        if (PlayerPrefs.HasKey("AccountManager_accountName"))
        {
            accountName = PlayerPrefs.GetString("AccountManager_accountName");
            id = PlayerPrefs.GetString("AccountManager_id");
            level = PlayerPrefs.GetInt("AccountManager_level", 1);
            battleCoins = PlayerPrefs.GetInt("AccountManager_battle_coins", 0);
            selectedFaction = (FactionManager.Faction)PlayerPrefs.GetInt("AccountManager_faction", 0);
            current_level_progression = PlayerPrefs.GetInt("AccountManager_current_level_progression", 0);

            string className = PlayerPrefs.GetString("AccountManager_selected_class", "None");

            if (System.Enum.TryParse(className, out ClassManager.Class loadedClass)) selectedClass = loadedClass;
            else selectedClass = ClassManager.Class.Assault;
            
        }
        else
        {
            accountName = "Player";
            id = System.Guid.NewGuid().ToString();
            level = 0;
            battleCoins = 0;
            selectedClass = ClassManager.Class.Assault;
            current_level_progression = 0;
        }
    }

    // Método opcional para resetar todos os dados
    public void ResetData()
    {
        PlayerPrefs.DeleteKey("AccountManager_accountName");
        PlayerPrefs.DeleteKey("AccountManager_id");
        PlayerPrefs.DeleteKey("AccountManager_level");
        PlayerPrefs.DeleteKey("AccountManager_battle_coins");
        PlayerPrefs.DeleteKey("AccountManager_faction");
        PlayerPrefs.DeleteKey("AccountManager_current_level_progression");
        PlayerPrefs.DeleteKey("AccountManager_selected_class");

        foreach (ClassManager.Class classEnum in System.Enum.GetValues(typeof(ClassManager.Class))) PlayerPrefs.DeleteKey($"AccountManager_Skin_{classEnum}");
        Debug.Log("AccountManager data was reset successfully!");
        LoadData();
    }
}
