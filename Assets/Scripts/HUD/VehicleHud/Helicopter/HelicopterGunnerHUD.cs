using UnityEngine;
using UnityEngine.UI; // Adicione esta linha

public class HelicopterGunnerHUD : MonoBehaviour
{
    [SerializeField] private AttackHelicopter helicopter;
    [SerializeField] private HeliProperties heliProperties;
    [SerializeField] private RectTransform heat_bar;
    [SerializeField] private Image heat_bar_image; // Referência ao componente Image
    private float originalWidth;

    // Cores para interpolação
    [SerializeField] private Color coolColor = Color.white;
    [SerializeField] private Color hotColor = Color.red;

    void Start()
    {
        originalWidth = heat_bar.sizeDelta.x;
        
        // Garante que temos a referência para a Image
        if (heat_bar_image == null)
        {
            heat_bar_image = heat_bar.GetComponent<Image>();
        }
    }

    void Update()
    {
        float heatPercent = helicopter.overheat / heliProperties.overheat_time;
        heatPercent = Mathf.Clamp01(heatPercent);

        // Atualiza o tamanho da barra
        heat_bar.sizeDelta = new Vector2(
            originalWidth * heatPercent,
            heat_bar.sizeDelta.y
        );

        // Atualiza a cor da barra baseada no heatPercent
        if (heat_bar_image != null)
        {
            heat_bar_image.color = Color.Lerp(coolColor, hotColor, heatPercent);
        }
    }
}