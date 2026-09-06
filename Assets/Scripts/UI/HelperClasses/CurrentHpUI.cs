using UnityEngine;

public class CurrentHpUI : MonoBehaviour
{
    [SerializeField] private RectTransform hp_bar;
    private ICurrentHpUIValues currentHpUIValues;
    private float originalWidth;
    private float maxHp;
    void Start()
    {
        currentHpUIValues = GetComponentInParent<ICurrentHpUIValues>();
        if (currentHpUIValues == null)
        {
            Debug.LogError("CurrentHpUI: Could not find a component implementing ICurrentHpUIValues on the parent object.");
            return;
        }
        maxHp = currentHpUIValues.GetMaxHp();
        originalWidth = hp_bar.sizeDelta.x;
    }

    void Update()
    {
        UpdateHp(currentHpUIValues.GetCurrentHp());
    }

    public void UpdateHp(float currentHp)
    {
        float hpPercent = currentHp / maxHp;
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
