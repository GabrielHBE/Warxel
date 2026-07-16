using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemIconsUI : MonoBehaviour
{
    [SerializeField] private List<ItemSlot> itemSlots;

    private IItemIconsUIValues currentActiveItemUIValues;

    private int currentItemIndex;

    void Awake()
    {
        currentActiveItemUIValues = GetComponentInParent<IItemIconsUIValues>();
        if (currentActiveItemUIValues == null)
        {
            Debug.LogError("ItemIconsUI: Não encontrou IItemIconsUIValues no pai.");
            return;
        }

        // Em vez de chamar direto, iniciamos uma rotina de espera
        StartCoroutine(WaitAndSetIcons());
    }

    private System.Collections.IEnumerator WaitAndSetIcons()
    {
        List<Sprite> icons = new List<Sprite>();
        int attempts = 0;

        // Tenta obter os ícones por até 10 frames ou até a lista não vir vazia
        while (attempts < 10)
        {
            icons = currentActiveItemUIValues.GetItemIcon();

            if (icons != null && icons.Count > 0)
                break;

            attempts++;
            yield return null; // Espera o próximo frame
        }

        // Se após 10 frames ainda estiver vazio, tenta uma última vez no final do frame atual
        if (icons.Count == 0)
        {
            yield return new WaitForEndOfFrame();
            icons = currentActiveItemUIValues.GetItemIcon();
        }

        SetItemIcons(icons);
        UpdateCurrentActiveItem(currentActiveItemUIValues.GetCurrentActiveItem());
    }

    void Update()
    {
        if (currentItemIndex != currentActiveItemUIValues.GetCurrentActiveItem())
        {
            currentItemIndex = currentActiveItemUIValues.GetCurrentActiveItem();
            UpdateCurrentActiveItem(currentItemIndex);
        }
    }

    private void SetItemIcons(List<Sprite> itemIcons)
    {
        if (itemIcons == null) return;

        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (i < itemIcons.Count)
            {
                itemSlots[i].image.sprite = itemIcons[i];
                itemSlots[i].image.enabled = true;
            }
            else
            {
                itemSlots[i].image.enabled = false;
            }
        }
    }

    private void UpdateCurrentActiveItem(int activeItemIndex)
    {
        if (activeItemIndex < 0 || activeItemIndex >= itemSlots.Count)
        {
            Debug.LogError("ItemIconsUI: Índice de item ativo fora do intervalo dos slots disponíveis.");
            return;
        }

        for (int i = 0; i < itemSlots.Count; i++)
        {
            itemSlots[i].outline.enabled = (i == activeItemIndex);
        }
    }

    [System.Serializable]
    private class ItemSlot
    {
        public Image image;
        public Outline outline;
    }

}


public interface IItemIconsUIValues
{
    public int GetCurrentActiveItem();
    public List<Sprite> GetItemIcon();
}
