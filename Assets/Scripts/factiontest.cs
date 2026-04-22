using TMPro;
using UnityEngine;

public class factiontest : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI a;

    void Update()
    {
        if(AccountManager.Instance != null)
        {
            a.text = AccountManager.Instance.faction.ToString();
        }
    }
    
}
