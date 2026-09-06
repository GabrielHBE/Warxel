using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

internal sealed class SettingsMenuController
{
    private readonly GameObject menu;
    private readonly GameObject resetKeybindButton;
    private readonly TextMeshProUGUI tabTitle;
    private readonly IReadOnlyList<GameObject> tabs;
    private readonly float scrollSpeed;
    private readonly Dictionary<GameObject, float> initialTabPositions = new Dictionary<GameObject, float>();

    public SettingsMenuController(
        GameObject menu,
        GameObject resetKeybindButton,
        TextMeshProUGUI tabTitle,
        IReadOnlyList<GameObject> tabs,
        float scrollSpeed)
    {
        this.menu = menu;
        this.resetKeybindButton = resetKeybindButton;
        this.tabTitle = tabTitle;
        this.tabs = tabs;
        this.scrollSpeed = scrollSpeed;

        for (int i = 0; i < tabs.Count; i++)
        {
            if (tabs[i] != null) initialTabPositions[tabs[i]] = tabs[i].transform.localPosition.y;
        }
    }

    public void SetOpen(bool isOpen)
    {
        if (menu != null) menu.SetActive(isOpen);
    }

    public void SelectTab(string title, GameObject selectedTab, bool showResetKeybindButton)
    {
        if (tabTitle != null)
        {
            tabTitle.text = title;
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            if (tabs[i] != null) tabs[i].SetActive(tabs[i] == selectedTab);
        }

        if (resetKeybindButton != null)  resetKeybindButton.SetActive(showResetKeybindButton);
        
    }

    public void HandleScroll()
    {
        if (Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Approximately(scroll, 0f))  return;

        for (int i = 0; i < tabs.Count; i++)
        {
            GameObject tab = tabs[i];
            if (tab == null || !tab.activeSelf)  continue;

            RectTransform content = tab.transform as RectTransform;
            RectTransform viewport = content != null ? content.parent as RectTransform : null;
            if (content == null || viewport == null) return;

            float initialY = initialTabPositions.TryGetValue(tab, out float value)
                ? value
                : content.localPosition.y;
            float scrollRange = Mathf.Max(0f, content.rect.height - viewport.rect.height + 24f);

            Vector3 position = content.localPosition;
            position.y = Mathf.Clamp(
                position.y + Mathf.Sign(scroll) * scrollSpeed * Time.unscaledDeltaTime,
                initialY,
                initialY + scrollRange);
            content.localPosition = position;
            return;
        }
    }
}
