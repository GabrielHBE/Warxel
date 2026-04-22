using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SoldierHudMagCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mag_ammo_hud;



    public void UpdateMagCount(int qtd = 0, List<int> mags = null)
    {

        if (mag_ammo_hud != null)
        {
            if (mags == null)
            {
                mag_ammo_hud.text = qtd.ToString("F0");
            }
            else
            {
                mag_ammo_hud.text = mags[^1].ToString("F0") + " / ";
                for (int i = 0; i < mags.Count - 1; i++)
                {
                    mag_ammo_hud.text += mags[i].ToString("F0") + " ";
                }
            }
        }
    }

}
