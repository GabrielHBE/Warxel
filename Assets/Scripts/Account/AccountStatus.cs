using System.Collections.Generic;
using UnityEngine;
using System;

public class AccountStatus
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

    private Dictionary<ClassManager.Class, int> classSelecion = new Dictionary<ClassManager.Class, int>();

    // Constante para o prefixo das keys no PlayerPrefs
    private const string CLASS_SELECTION_PREFIX = "AccountStatus_class_selection_";

    public void Initialize()
    {
        LoadAccountStatus();
        LoadClassSelections();
        CalculateKdRatio();
        CalculateWinRate();
        UpdateMostUsedClass();
    }

    public void LoadAccountStatus()
    {
        total_kills = PlayerPrefs.GetInt("AccountStatus_total_kills", 0);
        total_deaths = PlayerPrefs.GetInt("AccountStatus_total_deaths", 0);
        total_head_shot_kills = PlayerPrefs.GetInt("AccountStatus_total_head_shot_kills", 0);
        total_assists = PlayerPrefs.GetInt("AccountStatus_total_assists", 0);
        total_matches_played = PlayerPrefs.GetInt("AccountStatus_total_matches_played", 0);
        total_matches_won = PlayerPrefs.GetInt("AccountStatus_total_matches_won", 0);
        total_matches_lost = PlayerPrefs.GetInt("AccountStatus_total_matches_lost", 0);
        win_rate = PlayerPrefs.GetFloat("AccountStatus_win_rate", 0f);
    }

    public void LoadClassSelections()
    {
        classSelecion.Clear();
        
        // Pega todos os valores do enum ClassManager.Class
        foreach (ClassManager.Class classType in Enum.GetValues(typeof(ClassManager.Class)))
        {
            string key = GetClassSelectionKey(classType);
            int value = PlayerPrefs.GetInt(key, 0);
            classSelecion[classType] = value;
        }
    }

    public void SaveClassSelections()
    {
        foreach (var kvp in classSelecion)
        {
            string key = GetClassSelectionKey(kvp.Key);
            PlayerPrefs.SetInt(key, kvp.Value);
        }
        PlayerPrefs.Save();
    }

    public void IncreaseClassSelecion()
    {
        if (classSelecion.ContainsKey(AccountManager.Instance.selected_class)) classSelecion[AccountManager.Instance.selected_class] += 1;
        else return;
        
        // Salva imediatamente a mudança
        SaveClassSelections();
        
        // Atualiza a classe mais usada
        UpdateMostUsedClass();
    }

    public int GetClassSelectionCount(ClassManager.Class _class)
    {
        if (classSelecion.ContainsKey(_class)) return classSelecion[_class];
        
        return 0;
    }

    private void UpdateMostUsedClass()
    {
        if (classSelecion.Count == 0) return;

        ClassManager.Class mostUsed = ClassManager.Class.Assault;
        int maxCount = 0;

        foreach (var kvp in classSelecion)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                mostUsed = kvp.Key;
            }
        }

        most_used_class = mostUsed;
        
        // Salva a classe mais usada no PlayerPrefs
        PlayerPrefs.SetString("AccountStatus_most_used_class", most_used_class.ToString());
        PlayerPrefs.Save();
    }

    private string GetClassSelectionKey(ClassManager.Class classType) => $"{CLASS_SELECTION_PREFIX}{classType}";
    public Dictionary<ClassManager.Class, int> GetClassSelections() => new Dictionary<ClassManager.Class, int>(classSelecion);

    public void CalculateKdRatio()
    {
        if (total_deaths > 0) kd_ratio = (float)total_kills / total_deaths;
        else if (total_kills > 0) kd_ratio = total_kills;
        else kd_ratio = 0f;
        
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
        total_head_shot_kills += 1;
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

        if (total_matches_played > 0) win_rate = ((float)total_matches_won / (float)total_matches_played) * 100f;
        else win_rate = 0f;
        
        PlayerPrefs.SetFloat("AccountStatus_win_rate", win_rate);
        PlayerPrefs.SetInt("AccountStatus_total_matches_played", total_matches_played);
        PlayerPrefs.Save();
    }

    public void ResetAllStats()
    {
        // Remove todas as keys de status
        PlayerPrefs.DeleteKey("AccountStatus_total_kills");
        PlayerPrefs.DeleteKey("AccountStatus_total_deaths");
        PlayerPrefs.DeleteKey("AccountStatus_total_head_shot_kills");
        PlayerPrefs.DeleteKey("AccountStatus_total_assists");
        PlayerPrefs.DeleteKey("AccountStatus_total_matches_played");
        PlayerPrefs.DeleteKey("AccountStatus_total_matches_won");
        PlayerPrefs.DeleteKey("AccountStatus_total_matches_lost");
        PlayerPrefs.DeleteKey("AccountStatus_win_rate");
        PlayerPrefs.DeleteKey("AccountStatus_kd_ratio");
        PlayerPrefs.DeleteKey("AccountStatus_most_used_class");

        // Remove todas as keys de seleção de classe
        foreach (ClassManager.Class classType in System.Enum.GetValues(typeof(ClassManager.Class)))
        {
            PlayerPrefs.DeleteKey(GetClassSelectionKey(classType));
        }

        PlayerPrefs.Save();

        // Recarrega os dados
        LoadAccountStatus();
        LoadClassSelections();
        CalculateKdRatio();
        CalculateWinRate();
        UpdateMostUsedClass();

        Debug.Log("Todos os status foram resetados!");
    }
}