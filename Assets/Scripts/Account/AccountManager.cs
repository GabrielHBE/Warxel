using UnityEngine;

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance { get; private set; }
    public AccountStatus status;
    public string account_name;
    public string id;
    public int level;
    public int battle_coins;
    public FactionManager.Faction faction;
    public ClassManager.Class selected_class;

    private int current_level_progression;
    private int points_to_level_up = 100;

    private void Awake()
    {
        Instance = this;
        LoadData(); // Carrega os dados ao iniciar
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            AddBattleCoin(100);
        }
    }

    public void SetClass(ClassManager.Class @class)
    {
        selected_class = @class;
        SaveData();
    }

    public void AddBattleCoin(int qnt)
    {
        battle_coins += qnt;
        SaveData();
    }

    public void RemoveBattleCoin(int qnt)
    {
        battle_coins -= qnt;
        if (battle_coins < 0)
        {
            battle_coins = 0;
        }
        SaveData();
    }

    public void SwitchFaction(FactionManager.Faction faction)
    {
        RemoveBattleCoin(100);
        this.faction = faction;
        SaveData();
    }

    public void AddPointsToLevelUp(int points)
    {
        current_level_progression += points;
        if (current_level_progression >= points_to_level_up)
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

    // Método para salvar todos os dados
    public void SaveData()
    {
        PlayerPrefs.SetString("AccountManager_account_name", account_name);
        PlayerPrefs.SetString("AccountManager_id", id);
        PlayerPrefs.SetString("AccountManager_selected_class", selected_class.ToString());
        PlayerPrefs.SetInt("AccountManager_level", level);
        PlayerPrefs.SetInt("AccountManager_battle_coins", battle_coins);
        PlayerPrefs.SetInt("AccountManager_faction", (int)faction);
        PlayerPrefs.SetInt("AccountManager_current_level_progression", current_level_progression);
        PlayerPrefs.Save();

        Debug.Log($"Dados salvos com sucesso! Classe: {selected_class}");
    }

    // Método para carregar todos os dados
    public void LoadData()
    {
        // Verificar se existe pelo menos uma chave (usando account_name como referência)
        if (PlayerPrefs.HasKey("AccountManager_account_name"))
        {
            account_name = PlayerPrefs.GetString("AccountManager_account_name");
            id = PlayerPrefs.GetString("AccountManager_id");
            level = PlayerPrefs.GetInt("AccountManager_level", 1); // Começa no nível 1 por padrão
            battle_coins = PlayerPrefs.GetInt("AccountManager_battle_coins", 0);
            faction = (FactionManager.Faction)PlayerPrefs.GetInt("AccountManager_faction", 0);
            current_level_progression = PlayerPrefs.GetInt("AccountManager_current_level_progression", 0);
            
            // Carregar a classe selecionada
            string className = PlayerPrefs.GetString("AccountManager_selected_class", "None");
            
            // Tentar converter a string de volta para enum ClassManager.Class
            if (System.Enum.TryParse(className, out ClassManager.Class loadedClass))
            {
                selected_class = loadedClass;
            }
            else
            {
                // Se falhar, definir um valor padrão (Assault, por exemplo)
                selected_class = ClassManager.Class.Assault; // Ajuste conforme seu enum
                Debug.LogWarning($"Classe '{className}' não encontrada. Usando padrão: {selected_class}");
            }

        }
        else
        {
            Debug.Log("Nenhum dado salvo encontrado. Usando valores padrão.");
            
            // Definir valores padrão para um novo jogador
            account_name = "Jogador";
            id = System.Guid.NewGuid().ToString();
            level = 0;
            battle_coins = 0;
            //faction = FactionManager.Faction.None; // Ajuste conforme seu enum
            selected_class = ClassManager.Class.Assault; // Ajuste conforme seu enum
            current_level_progression = 0;
        }
    }

    // Método opcional para resetar todos os dados
    public void ResetData()
    {
        PlayerPrefs.DeleteKey("AccountManager_account_name");
        PlayerPrefs.DeleteKey("AccountManager_id");
        PlayerPrefs.DeleteKey("AccountManager_level");
        PlayerPrefs.DeleteKey("AccountManager_battle_coins");
        PlayerPrefs.DeleteKey("AccountManager_faction");
        PlayerPrefs.DeleteKey("AccountManager_current_level_progression");
        PlayerPrefs.DeleteKey("AccountManager_selected_class");

        Debug.Log("Dados do AccountManager resetados com sucesso!");
        
        // Recarregar com valores padrão
        LoadData();
    }
}