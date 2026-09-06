using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountermeasuresStatusUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countermeasures_status;
    [SerializeField] private Outline countermeasures_outline;

    private ICountermeasuresStatusUIValues countermeasuresStatusUIValues;

    void Start()
    {
        countermeasuresStatusUIValues = GetComponentInParent<ICountermeasuresStatusUIValues>();
        if (countermeasuresStatusUIValues == null)
        {
            Debug.LogError("CountermeasuresStatusUI: Could not find a component implementing ICountermeasuresStatusUIValues on the parent object.");
            return;
        }
    }

    void Update()
    {
        UpdateCountermeasuresStatus(countermeasuresStatusUIValues.GetCountermeasuresStatus(), countermeasuresStatusUIValues.GetCountermeasuresStatusText());
    }  

    public void UpdateCountermeasuresStatus(CountermeasuresStatus status, string text = null)
    {
        if (countermeasures_status == null) return;

        if (status == CountermeasuresStatus.Ready)
        {
            countermeasures_outline.enabled = true;
        }
        else
        {
            countermeasures_outline.enabled = false;
        }

        countermeasures_status.text = "Countermeasures: " + text;

    }

    public enum CountermeasuresStatus
    {
        Ready,
        Reloading,
        InUse
    }
}

public interface ICountermeasuresStatusUIValues
{
    public CountermeasuresStatusUI.CountermeasuresStatus GetCountermeasuresStatus();
    public string GetCountermeasuresStatusText();
}
