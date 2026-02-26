using UnityEngine;

public class SoldierHudHpManager : MonoBehaviour
{
    [SerializeField] private RectTransform hp_bar;
    [SerializeField] private PlayerProperties playerProperties;

    private float originalWidth;

    void Start()
    {
        originalWidth = hp_bar.sizeDelta.x;
    }


    public void UpdateHp()
    {
        float hpPercent = playerProperties.hp / playerProperties.max_hp;
        hpPercent = Mathf.Clamp01(hpPercent);

        hp_bar.sizeDelta = new Vector2(
            originalWidth * hpPercent,
            hp_bar.sizeDelta.y
        );

    }


}
