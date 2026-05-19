using UnityEngine;

public class CurrentHpUI : MonoBehaviour
{
    [SerializeField] private RectTransform hp_bar;
    private ICurrentHpUIValues currentHpUIValues;
    private float originalWidth;
    private float max_hp;
    void Start()
    {
        currentHpUIValues = GetComponentInParent<ICurrentHpUIValues>();
        if (currentHpUIValues == null)
        {
            Debug.LogError("CurrentHpUI: Não foi possível encontrar um componente que implemente ICurrentHpUIValues no objeto pai.");
            return;
        }
        max_hp = currentHpUIValues.GetMaxHp();
        originalWidth = hp_bar.sizeDelta.x;
    }

    void Update()
    {
        UpdateHp(currentHpUIValues.GetCurrentHp());
    }

    public void UpdateHp(float currentHp)
    {
        float hpPercent = currentHp / max_hp;
        hpPercent = Mathf.Clamp01(hpPercent);

        hp_bar.sizeDelta = new Vector2(
            originalWidth * hpPercent,
            hp_bar.sizeDelta.y
        );

    }  

}

public interface ICurrentHpUIValues
{
    public float GetCurrentHp();
    public float GetMaxHp();
}
