using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeadPlayerHud : MonoBehaviour
{
    public TextMeshProUGUI[] medics_players = new TextMeshProUGUI[6];
    public Image time_left_to_die;

    public void UpdateCloseMedics(List<PlayerController.PlayerInfo> players)
    {
        // Garante que não vamos acessar mais índices do que temos
        int playersCount = players.Count;
        int maxIndex = Mathf.Min(playersCount, 6); // Máximo de 6 elementos

        // Preenche com os nomes dos jogadores disponíveis
        for (int i = 0; i < maxIndex; i++)
        {
            if (i < playersCount)
            {
                medics_players[i].text = "[" + players[i].distance + "] " + players[i].player_name;
            }
            else
            {
                medics_players[i].text = "------";
            }
        }

        // Se tivermos menos de 6 jogadores, preenche o restante com "------"
        for (int i = maxIndex; i < 6; i++)
        {
            medics_players[i].text = "------";
        }
    }
}