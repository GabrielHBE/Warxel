using UnityEngine;

public class AccountStatus : MonoBehaviour
{
    public ClassManager.Class most_used_class;
    public float kd_ratio;
    public int total_head_shot_kills;
    public int total_kills;
    public int total_assists;
    public int total_deaths;
    public WeaponProperties most_used_weapon;
    public int total_matches_played;
    public int total_matches_won;
    public int total_matches_lost;
    public float win_rate;

    private void Awake()
    {
        LoadAccountStatus();

        CalculateKdRatio();
        CalculateWinRate();
    }

    public void LoadAccountStatus()
    {
        // Carregar todos os valores do PlayerPrefs (usando 0 como valor padrão caso não exista)
        total_kills = PlayerPrefs.GetInt("AccountStatus_total_kills", 0);
        total_deaths = PlayerPrefs.GetInt("AccountStatus_total_deaths", 0);
        total_head_shot_kills = PlayerPrefs.GetInt("AccountStatus_total_head_shot_kills", 0);
        total_assists = PlayerPrefs.GetInt("AccountStatus_total_assists", 0);
        total_matches_played = PlayerPrefs.GetInt("AccountStatus_total_matches_played", 0);
        total_matches_won = PlayerPrefs.GetInt("AccountStatus_total_matches_won", 0);
        total_matches_lost = PlayerPrefs.GetInt("AccountStatus_total_matches_lost", 0);

        // Carregar a win_rate se você estiver salvando ela também
        win_rate = PlayerPrefs.GetFloat("AccountStatus_win_rate", 0f);

    }

    public void CalculateKdRatio()
    {
        // Evitar divisão por zero
        if (total_deaths > 0)
        {
            kd_ratio = total_kills / total_deaths;
        }
        else if (total_kills > 0)
        {
            kd_ratio = total_kills; // Se não morreu, K/D é igual ao número de kills
        }
        else
        {
            kd_ratio = 0f;
        }

        // Opcional: salvar o K/D ratio
        PlayerPrefs.SetFloat("AccountStatus_kd_ratio", kd_ratio);
        PlayerPrefs.Save();
    }

    public void AddKill()
    {
        total_kills += 1;
        PlayerPrefs.SetInt("AccountStatus_total_kills", total_kills);
        PlayerPrefs.Save();
        CalculateKdRatio();
    }

    public void AddDeath()
    {
        total_deaths += 1;
        PlayerPrefs.SetInt("AccountStatus_total_deaths", total_deaths);
        PlayerPrefs.Save();
        CalculateKdRatio();
    }

    public void AddHeadShotKill()
    {
        total_head_shot_kills += 1; // Corrigido: era =+1, agora é +=1
        PlayerPrefs.SetInt("AccountStatus_total_head_shot_kills", total_head_shot_kills);
        PlayerPrefs.Save();
    }

    public void AddKillAssist()
    {
        total_assists += 1;
        PlayerPrefs.SetInt("AccountStatus_total_assists", total_assists);
        PlayerPrefs.Save();
    }

    public void AddMatchesPlayed()
    {
        total_matches_played += 1;
        PlayerPrefs.SetInt("AccountStatus_total_matches_played", total_matches_played);
        PlayerPrefs.Save();
    }

    public void AddMatchWon()
    {
        total_matches_won += 1;
        PlayerPrefs.SetInt("AccountStatus_total_matches_won", total_matches_won);
        PlayerPrefs.Save();
        CalculateWinRate();
    }

    public void AddMatchesLost()
    {
        total_matches_lost += 1;
        PlayerPrefs.SetInt("AccountStatus_total_matches_lost", total_matches_lost);
        PlayerPrefs.Save();
        CalculateWinRate();
    }

    private void CalculateWinRate()
    {
        total_matches_played = total_matches_won + total_matches_lost;

        if (total_matches_played > 0)
        {
            win_rate = ((float)total_matches_won / (float)total_matches_played) * 100f;
        }
        else
        {
            win_rate = 0f;
        }

        // Salvar a win rate
        PlayerPrefs.SetFloat("AccountStatus_win_rate", win_rate);

        // Atualizar total_matches_played no PlayerPrefs
        PlayerPrefs.SetInt("AccountStatus_total_matches_played", total_matches_played);
        PlayerPrefs.Save();
    }

    // Método útil para resetar todos os status (para testes)
    public void ResetAllStats()
    {
        PlayerPrefs.DeleteKey("AccountStatus_total_kills");
        PlayerPrefs.DeleteKey("AccountStatus_total_deaths");
        PlayerPrefs.DeleteKey("AccountStatus_total_head_shot_kills");
        PlayerPrefs.DeleteKey("AccountStatus_total_assists");
        PlayerPrefs.DeleteKey("AccountStatus_total_matches_played");
        PlayerPrefs.DeleteKey("AccountStatus_total_matches_won");
        PlayerPrefs.DeleteKey("AccountStatus_total_matches_lost");
        PlayerPrefs.DeleteKey("AccountStatus_win_rate");
        PlayerPrefs.DeleteKey("AccountStatus_kd_ratio");
        PlayerPrefs.Save();

        LoadAccountStatus();
        CalculateKdRatio();
        CalculateWinRate();

        Debug.Log("Todos os status foram resetados!");
    }
}