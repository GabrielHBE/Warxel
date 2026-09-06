using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LoadoutCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image targetImage;
    private Color normalColor;
    private Color hoverColor;

    public void Configure(Image image, Color normal, Color hover)
    {
        targetImage = image;
        normalColor = normal;
        hoverColor = hover;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetImage != null) targetImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetImage != null) targetImage.color = normalColor;
    }
}
