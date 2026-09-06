using UnityEngine;
using UnityEngine.UI;

public class GunHeatLevelUI : MonoBehaviour
{

    [SerializeField] private RectTransform heat_bar;
    private Image heat_bar_image;
    private float originalWidth;
    // Cores para interpolação
    [SerializeField] private Color coolColor = Color.white;
    [SerializeField] private Color hotColor = Color.red;

    private float maxHeat;
    private IGunHeatLevelUIValues gunHeatLevelUIValues;
    void Start()
    {
        gunHeatLevelUIValues = GetComponentInParent<IGunHeatLevelUIValues>();
        if (gunHeatLevelUIValues == null)
        {
            Debug.LogError("GunHeatLevelUI: Could not find a component implementing IGunHeatLevelUIValues on the parent object.");
            return;
        }
        StartCoroutine(WaitAndSetIcons());
        originalWidth = heat_bar.sizeDelta.x;

        if (heat_bar_image == null)  heat_bar_image = heat_bar.GetComponent<Image>();
        
    }

    private System.Collections.IEnumerator WaitAndSetIcons()
    {
        int attempts = 0;

        // Tenta obter os ícones por até 10 frames ou até a lista não vir vazia
        while (attempts < 10)
        {
            attempts++;
            yield return null; // Espera o próximo frame
        }
        
        maxHeat = gunHeatLevelUIValues.GetMaxHeat();

    }


    void Update() => UpdateHeat(gunHeatLevelUIValues.GetCurrentHeat());
    
    public void UpdateHeat(float currentHeat)
    {
        float heatPercent = currentHeat / maxHeat;
        heatPercent = Mathf.Clamp01(heatPercent);

        // Atualiza o tamanho da barra
        heat_bar.sizeDelta = new Vector2(
            originalWidth * heatPercent,
            heat_bar.sizeDelta.y
        );

        if (heat_bar_image != null) heat_bar_image.color = Color.Lerp(coolColor, hotColor, heatPercent);
    }
}


public interface IGunHeatLevelUIValues
{
    public float GetMaxHeat();
    public float GetCurrentHeat();

}
