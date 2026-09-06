using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoadoutUITheme : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.067f, 0.075f, 0.082f, 0.975f);
    private static readonly Color PanelBorderColor = new Color(0.333f, 0.357f, 0.388f, 0.9f);
    private static readonly Color CardColor = new Color(0.125f, 0.137f, 0.157f, 1f);
    private static readonly Color AccentColor = new Color(0.886f, 0.898f, 0.914f, 1f);
    private static readonly Color SelectedColor = new Color(0.208f, 0.224f, 0.251f, 1f);
    private static readonly Color MutedTextColor = new Color(0.584f, 0.604f, 0.631f, 1f);
    private static readonly Color DangerColor = new Color(0.361f, 0.184f, 0.196f, 1f);
    private static readonly Color PrimaryTextColor = new Color(0.031f, 0.035f, 0.039f, 1f);

    private InfantryLoadoutCustomization loadout;
    private TextMeshProUGUI statusText;
    private TMP_FontAsset font;
    private RectTransform themeRoot;
    private RectTransform leftPanel;
    private RectTransform rightPanel;
    private RectTransform previewPanel;
    private RectTransform bottomPanel;
    private RectTransform topBar;
    private TextMeshProUGUI leftPanelTitle;
    private RectTransform attachmentPointsBadge;
    private Image attachmentPointsFill;
    private TextMeshProUGUI attachmentPointsText;
    private GameObject bottomPanelObject;
    private bool initialized;
    private int lastDynamicSignature = int.MinValue;
    private InfantryLoadoutCustomization.SelectionStage lastStage = (InfantryLoadoutCustomization.SelectionStage)(-1);

    public RectTransform PreviewPanel => previewPanel;

    public void Initialize(InfantryLoadoutCustomization parent, TextMeshProUGUI referenceText)
    {
        if (initialized || parent == null) return;

        loadout = parent;
        statusText = referenceText;
        font = referenceText != null ? referenceText.font : TMP_Settings.defaultFontAsset;

        // Newer versions of the prefab already contain the complete layout. The
        // fallback keeps older scene/prefab instances compatible without creating
        // a duplicate hierarchy.
        if (!TryBindExistingStructure())
        {
            CreateStructure();
            MoveExistingContentIntoLayout();
        }

        RemoveTopBar();
        RemoveWeaponDataTitle();
        ConfigureTopBarlessLayout();
        EnsureAttachmentPointsCounter();
        ConfigureStaticContent();
        ConfigureThemeValues();
        StyleStaticButtons();
        Refresh(true);

        initialized = true;
    }

    private bool TryBindExistingStructure()
    {
        themeRoot = transform.Find("BattleBit Inspired Layout") as RectTransform;
        if (themeRoot == null) return false;

        leftPanel = themeRoot.Find("Navigation Panel") as RectTransform;
        rightPanel = themeRoot.Find("Weapon Data Panel") as RectTransform;
        previewPanel = themeRoot.Find("Weapon Preview") as RectTransform;
        bottomPanel = themeRoot.Find("Attachment Bar") as RectTransform;
        topBar = themeRoot.Find("Top Bar") as RectTransform;

        if (leftPanel == null || rightPanel == null || previewPanel == null ||
            bottomPanel == null)
            return false;

        bottomPanelObject = bottomPanel.gameObject;
        Transform navigationTitle = leftPanel.Find("Navigation Title");
        leftPanelTitle = navigationTitle != null
            ? navigationTitle.GetComponent<TextMeshProUGUI>()
            : null;

        return leftPanelTitle != null;
    }

    public void UpdateAttachmentPoints(float currentPoints, float maxPoints)
    {
        EnsureAttachmentPointsCounter();
        if (attachmentPointsText == null) return;

        float normalizedPoints = maxPoints > 0f ? Mathf.Clamp01(currentPoints / maxPoints) : 0f;
        bool limitReached = currentPoints >= maxPoints;
        bool nearLimit = normalizedPoints >= 0.8f;
        Color stateColor = limitReached
            ? new Color(0.82f, 0.16f, 0.13f, 0.95f)
            : nearLimit
                ? new Color(0.88f, 0.55f, 0.12f, 0.9f)
                : new Color(0.18f, 0.48f, 0.68f, 0.85f);

        attachmentPointsText.text = $"CURRENT POINTS: {currentPoints:0.##} / {maxPoints:0}";
        attachmentPointsText.color = Color.white;

        if (attachmentPointsFill != null)
        {
            RectTransform fillRect = attachmentPointsFill.rectTransform;
            fillRect.anchorMax = new Vector2(normalizedPoints, 1f);
            attachmentPointsFill.color = stateColor;
            attachmentPointsFill.gameObject.SetActive(normalizedPoints > 0f);
        }

        if (attachmentPointsBadge != null)
        {
            Outline badgeOutline = attachmentPointsBadge.GetComponent<Outline>();
            if (badgeOutline != null) badgeOutline.effectColor = stateColor;
        }
    }

    public void Refresh(bool force = false)
    {
        if (loadout == null) return;

        InfantryLoadoutCustomization.SelectionStage stage = loadout.GetCurrentStage();
        int signature = CalculateDynamicSignature();

        if (force || signature != lastDynamicSignature)
        {
            StyleClassButtons();
            StyleLoadoutOptionButtons();
            StyleItemCards();
            lastDynamicSignature = signature;
        }

        if (force || stage != lastStage)
        {
            UpdateStagePresentation(stage);
            lastStage = stage;
        }
    }

    private void CreateStructure()
    {
        themeRoot = CreateRect("BattleBit Inspired Layout", transform);
        StretchToParent(themeRoot);
        themeRoot.SetAsFirstSibling();

        // The preview camera renders behind this Canvas. Keep the center panel almost
        // transparent so its Image does not cover the weapon rendered by that camera.
        previewPanel = CreatePanel("Weapon Preview", themeRoot, new Color(0f, 0f, 0f, 0.025f), true);
        SetOffsets(previewPanel, new Vector2(400f, 175f), new Vector2(-490f, -94f));

        leftPanel = CreatePanel("Navigation Panel", themeRoot, PanelColor, true);
        leftPanel.anchorMin = new Vector2(0f, 0f);
        leftPanel.anchorMax = new Vector2(0f, 1f);
        leftPanel.pivot = new Vector2(0f, 0.5f);
        leftPanel.anchoredPosition = new Vector2(24f, -33f);
        leftPanel.sizeDelta = new Vector2(360f, -114f);
        leftPanel.gameObject.AddComponent<RectMask2D>();

        rightPanel = CreatePanel("Weapon Data Panel", themeRoot, PanelColor, true);
        rightPanel.anchorMin = new Vector2(1f, 0f);
        rightPanel.anchorMax = new Vector2(1f, 1f);
        rightPanel.pivot = new Vector2(1f, 0.5f);
        rightPanel.anchoredPosition = new Vector2(-24f, -33f);
        rightPanel.sizeDelta = new Vector2(450f, -114f);

        bottomPanel = CreatePanel("Attachment Bar", themeRoot, PanelColor, true);
        bottomPanel.anchorMin = new Vector2(0f, 0f);
        bottomPanel.anchorMax = new Vector2(1f, 0f);
        bottomPanel.pivot = new Vector2(0.5f, 0f);
        bottomPanel.offsetMin = new Vector2(400f, 24f);
        bottomPanel.offsetMax = new Vector2(-490f, 159f);
        bottomPanelObject = bottomPanel.gameObject;

        CreateFixedLabels();
    }

    private void CreateFixedLabels()
    {
        leftPanelTitle = CreateText("Navigation Title", leftPanel, "LOADOUT", 16f, FontStyles.Bold);
        leftPanelTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        leftPanelTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        leftPanelTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
        leftPanelTitle.rectTransform.anchoredPosition = new Vector2(0f, -15f);
        leftPanelTitle.rectTransform.sizeDelta = new Vector2(-28f, 32f);
        leftPanelTitle.alignment = TextAlignmentOptions.MidlineLeft;
        leftPanelTitle.color = AccentColor;

        TextMeshProUGUI previewTitle = CreateText("Preview Title", previewPanel, "EQUIPMENT PREVIEW", 13f, FontStyles.Bold);
        previewTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        previewTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        previewTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
        previewTitle.rectTransform.anchoredPosition = new Vector2(0f, -12f);
        previewTitle.rectTransform.sizeDelta = new Vector2(-24f, 24f);
        previewTitle.alignment = TextAlignmentOptions.MidlineLeft;
        previewTitle.color = MutedTextColor;

        TextMeshProUGUI attachmentTitle = CreateText("Attachment Title", bottomPanel, "ATTACHMENTS", 13f, FontStyles.Bold);
        attachmentTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        attachmentTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        attachmentTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
        attachmentTitle.rectTransform.anchoredPosition = new Vector2(0f, -8f);
        attachmentTitle.rectTransform.sizeDelta = new Vector2(-24f, 22f);
        attachmentTitle.alignment = TextAlignmentOptions.MidlineLeft;
        attachmentTitle.color = AccentColor;
    }

    private void EnsureAttachmentPointsCounter()
    {
        if (bottomPanel == null) return;

        if (attachmentPointsBadge == null)
            attachmentPointsBadge = bottomPanel.Find("Attachment Points Badge") as RectTransform;

        if (attachmentPointsText == null)
        {
            Transform existingCounter = attachmentPointsBadge != null
                ? attachmentPointsBadge.Find("Attachment Points Counter")
                : bottomPanel.Find("Attachment Points Counter");
            attachmentPointsText = existingCounter != null
                ? existingCounter.GetComponent<TextMeshProUGUI>()
                : null;
        }

        if (attachmentPointsBadge == null)
        {
            attachmentPointsBadge = CreatePanel(
                "Attachment Points Badge",
                bottomPanel,
                new Color(0.035f, 0.047f, 0.059f, 1f),
                true);
        }

        attachmentPointsBadge.anchorMin = Vector2.one;
        attachmentPointsBadge.anchorMax = Vector2.one;
        attachmentPointsBadge.pivot = Vector2.one;
        attachmentPointsBadge.anchoredPosition = new Vector2(-10f, -5f);
        attachmentPointsBadge.sizeDelta = new Vector2(300f, 40f);
        attachmentPointsBadge.localScale = Vector3.one;

        if (attachmentPointsFill == null)
        {
            Transform existingFill = attachmentPointsBadge.Find("Points Fill");
            attachmentPointsFill = existingFill != null
                ? existingFill.GetComponent<Image>()
                : null;
        }

        if (attachmentPointsFill == null)
        {
            RectTransform fillRect = CreatePanel(
                "Points Fill",
                attachmentPointsBadge,
                new Color(0.18f, 0.48f, 0.68f, 0.85f),
                false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            attachmentPointsFill = fillRect.GetComponent<Image>();
        }

        if (attachmentPointsText == null)
        {
            attachmentPointsText = CreateText(
                "Attachment Points Counter",
                attachmentPointsBadge,
                $"POINTS USED: 0 / {AttatchmentManager.MaxAttachmentPoints:0}",
                18f,
                FontStyles.Bold);
        }
        else if (attachmentPointsText.transform.parent != attachmentPointsBadge)
        {
            attachmentPointsText.transform.SetParent(attachmentPointsBadge, false);
        }

        RectTransform counterRect = attachmentPointsText.rectTransform;
        counterRect.anchorMin = Vector2.zero;
        counterRect.anchorMax = Vector2.one;
        counterRect.pivot = new Vector2(0.5f, 0.5f);
        counterRect.offsetMin = new Vector2(12f, 0f);
        counterRect.offsetMax = new Vector2(-12f, 0f);
        counterRect.localScale = Vector3.one;

        attachmentPointsText.font = font;
        attachmentPointsText.fontSize = 18f;
        attachmentPointsText.fontStyle = FontStyles.Bold;
        attachmentPointsText.alignment = TextAlignmentOptions.Center;
        attachmentPointsText.color = Color.white;
        attachmentPointsText.textWrappingMode = TextWrappingModes.NoWrap;
        attachmentPointsText.raycastTarget = false;

        attachmentPointsFill.transform.SetAsFirstSibling();
        attachmentPointsText.transform.SetAsLastSibling();
    }

    private void MoveExistingContentIntoLayout()
    {
        MoveListToNavigationPanel(loadout.classesParent);
        MoveListToNavigationPanel(loadout.loadoutOptionsParent);
        MoveListToNavigationPanel(loadout.weaponsGadgetsParent);

        if (loadout.class_selection_image != null)
            loadout.class_selection_image.gameObject.SetActive(false);

        if (loadout.weaponsGadgetsSlider != null)
        {
            RectTransform sliderRect = loadout.weaponsGadgetsSlider.GetComponent<RectTransform>();
            sliderRect.SetParent(leftPanel, false);
            sliderRect.anchorMin = new Vector2(1f, 0f);
            sliderRect.anchorMax = new Vector2(1f, 1f);
            sliderRect.pivot = new Vector2(1f, 0.5f);
            sliderRect.offsetMin = new Vector2(-13f, 78f);
            sliderRect.offsetMax = new Vector2(-5f, -65f);
            sliderRect.localScale = Vector3.one;

            loadout.weaponsGadgetsSlider.direction = Slider.Direction.BottomToTop;
            if (loadout.weaponsGadgetsSlider.fillRect != null)
            {
                Image fillImage = loadout.weaponsGadgetsSlider.fillRect.GetComponent<Image>();
                if (fillImage != null) fillImage.color = AccentColor;
            }
            if (loadout.weaponsGadgetsSlider.handleRect != null)
            {
                Image handleImage = loadout.weaponsGadgetsSlider.handleRect.GetComponent<Image>();
                if (handleImage != null) handleImage.color = Color.white;
            }
        }

        if (loadout.weaponStatusParent != null)
        {
            RectTransform statusRect = loadout.weaponStatusParent.GetComponent<RectTransform>();
            statusRect.SetParent(rightPanel, false);
            StretchToParent(statusRect);
            statusRect.localScale = Vector3.one;

            Image statusBackground = loadout.weaponStatusParent.GetComponent<Image>();
            if (statusBackground != null)
            {
                statusBackground.color = Color.clear;
                statusBackground.raycastTarget = false;
            }
        }

        if (loadout.customization_buttons_parent != null)
        {
            RectTransform customizationRect = loadout.customization_buttons_parent.GetComponent<RectTransform>();
            customizationRect.SetParent(bottomPanel, false);
            StretchToParent(customizationRect);
            customizationRect.offsetMin = new Vector2(12f, 8f);
            customizationRect.offsetMax = new Vector2(-12f, -28f);
            customizationRect.localScale = Vector3.one;
        }

        MoveUpdateButtonToNavigationPanel();
    }

    private void RemoveTopBar()
    {
        MoveBackButtonToPreviewPanel();
        if (topBar == null) return;

        if (loadout.currentSelectionText != null &&
            loadout.currentSelectionText.transform.IsChildOf(topBar))
            loadout.currentSelectionText = null;

        topBar.gameObject.SetActive(false);
        Destroy(topBar.gameObject);
        topBar = null;
    }

    private void RemoveWeaponDataTitle()
    {
        if (rightPanel == null) return;

        Transform weaponDataTitle = rightPanel.Find("Weapon Data Title");
        if (weaponDataTitle == null) return;

        weaponDataTitle.gameObject.SetActive(false);
        Destroy(weaponDataTitle.gameObject);
    }

    private void ConfigureTopBarlessLayout()
    {
        ExpandPanelToTop(leftPanel);
        ExpandPanelToTop(rightPanel);

        if (previewPanel != null)
            previewPanel.offsetMax = new Vector2(previewPanel.offsetMax.x, -24f);
    }

    private static void ExpandPanelToTop(RectTransform panel)
    {
        if (panel == null) return;

        panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, 0f);
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, -48f);
    }

    private void MoveListToNavigationPanel(Transform listTransform)
    {
        if (listTransform == null) return;

        RectTransform rect = listTransform as RectTransform;
        if (rect == null) return;

        rect.SetParent(leftPanel, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(14f, 76f);
        rect.offsetMax = new Vector2(-20f, -62f);
        rect.localScale = Vector3.one;
    }

    private void MoveBackButtonToPreviewPanel()
    {
        if (loadout.backButton == null || previewPanel == null) return;

        RectTransform rect = loadout.backButton.GetComponent<RectTransform>();
        if (rect == null) return;

        rect.SetParent(previewPanel, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(14f, -10f);
        rect.sizeDelta = new Vector2(48f, 48f);
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();
        StyleButton(loadout.backButton, CardColor, 15f, TextAlignmentOptions.Center);
    }

    private void MoveUpdateButtonToNavigationPanel()
    {
        if (loadout.updateLoadoutButton == null) return;

        RectTransform rect = loadout.updateLoadoutButton.GetComponent<RectTransform>();
        if (rect == null) return;

        rect.SetParent(leftPanel, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 14f);
        rect.sizeDelta = new Vector2(-28f, 48f);
        rect.localScale = Vector3.one;
        StyleButton(loadout.updateLoadoutButton, AccentColor, 16f, TextAlignmentOptions.Center, PrimaryTextColor);
        SetButtonText(loadout.updateLoadoutButton, "CONFIRM LOADOUT");
    }

    private void ConfigureStaticContent()
    {
        if (previewPanel != null)
        {
            Transform previewTitleTransform = previewPanel.Find("Preview Title");
            RectTransform previewTitleRect = previewTitleTransform as RectTransform;
            if (previewTitleRect != null)
            {
                previewTitleRect.anchoredPosition = new Vector2(32f, -12f);
                previewTitleRect.sizeDelta = new Vector2(-88f, 24f);
            }
        }

        if (statusText != null)
        {
            statusText.fontSize = 18f;
            statusText.fontStyle = FontStyles.Normal;
            statusText.color = new Color(0.902f, 0.914f, 0.882f, 1f);
            statusText.lineSpacing = 7f;
            statusText.alignment = TextAlignmentOptions.TopLeft;
            statusText.raycastTarget = false;
        }

        if (loadout.customizeWeaponButton != null && previewPanel != null)
        {
            RectTransform rect = loadout.customizeWeaponButton.GetComponent<RectTransform>();
            rect.SetParent(previewPanel, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-14f, -10f);
            rect.sizeDelta = new Vector2(168f, 40f);
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();
            StyleButton(loadout.customizeWeaponButton, AccentColor, 14f, TextAlignmentOptions.Center, PrimaryTextColor);
            SetButtonText(loadout.customizeWeaponButton, "CUSTOMIZE");
        }

        ArrangeAttachmentCategoryButtons();

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void ConfigureThemeValues()
    {
        loadout.selectedOutlineColor = AccentColor;
        loadout.outlineWidth = 2f;
        loadout.classButtonSpacingX = 0f;
        loadout.classButtonStartX = 0f;
        loadout.classButtonY = 0f;
        loadout.itemButtonSpacingY = -114f;
        loadout.itemButtonStartY = 0f;
        loadout.itemButtonX = 0f;
        loadout.loadoutOptionSpacingY = -66f;
        loadout.loadoutOptionStartY = 0f;
        loadout.maxScrollYIncreaser = 114f;

        if (loadout.classSelectionManager != null)
            loadout.classSelectionManager.ConfigureThemeColors(CardColor, SelectedColor);

        if (loadout.itemSelectionManager != null)
            loadout.itemSelectionManager.RefreshLayoutOrigin();
    }

    private void StyleStaticButtons()
    {
        ArrangeAttachmentCategoryButtons();
    }

    private void ArrangeAttachmentCategoryButtons()
    {
        GameObject[] buttons =
        {
            loadout.customizeWeaponButtonSight,
            loadout.customizeWeaponButtonCantedSight,
            loadout.customizeWeaponButtonNozzle,
            loadout.customizeWeaponButtonBarrel,
            loadout.customizeWeaponButtonMag,
            loadout.customizeWeaponButtonGrip,
            loadout.customizeWeaponButtonSideGrip,
            loadout.customizeWeaponButtonErgonomics,
            loadout.resetWeaponAttachmentsButton
        };

        string[] labels =
        {
            "SIGHT",
            "CANTED SIGHT",
            "MUZZLE",
            "BARREL",
            "MAGAZINE",
            "GRIP",
            "SIDE GRIP",
            "ERGONOMICS",
            "RESTORE"
        };

        int validButtonCount = 0;
        foreach (GameObject button in buttons)
            if (button != null) validButtonCount++;

        if (validButtonCount == 0) return;

        const float buttonWidth = 104f;
        const float spacing = 8f;
        float totalWidth = validButtonCount * buttonWidth + (validButtonCount - 1) * spacing;
        float x = -totalWidth * 0.5f + buttonWidth * 0.5f;
        int visibleIndex = 0;

        foreach (GameObject button in buttons)
        {
            if (button == null) continue;

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x + visibleIndex * (buttonWidth + spacing), -6f);
            rect.sizeDelta = new Vector2(buttonWidth, 46f);
            rect.localScale = Vector3.one;

            bool isResetButton = button == loadout.resetWeaponAttachmentsButton;
            StyleButton(button, isResetButton ? DangerColor : CardColor, 12f, TextAlignmentOptions.Center);
            SetButtonText(button, labels[System.Array.IndexOf(buttons, button)]);
            visibleIndex++;
        }
    }

    private void StyleClassButtons()
    {
        if (loadout.classesParent == null) return;

        int index = 0;
        foreach (Transform child in loadout.classesParent)
        {
            if (child.GetComponent<Button>() == null) continue;

            StyleListButton(child.gameObject, index++, 52f, 60f);
        }

        if (loadout.classSelectionManager != null)
            loadout.classSelectionManager.RefreshButtonColors();
    }

    private void StyleLoadoutOptionButtons()
    {
        if (loadout.loadoutOptionsParent == null) return;

        int index = 0;
        foreach (Transform child in loadout.loadoutOptionsParent)
        {
            if (child.GetComponent<Button>() == null) continue;

            StyleListButton(child.gameObject, index++, 56f, 66f);
        }
    }

    private void StyleListButton(GameObject buttonObject, int index, float height, float spacing)
    {
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * spacing);
        rect.sizeDelta = new Vector2(0f, height);
        rect.localScale = Vector3.one;

        StyleButton(buttonObject, CardColor, 17f, TextAlignmentOptions.MidlineLeft);

        TextMeshProUGUI text = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null) text.margin = new Vector4(18f, 0f, 12f, 0f);
    }

    private void StyleItemCards()
    {
        if (loadout.weaponsGadgetsParent == null) return;

        int index = 0;
        foreach (Transform child in loadout.weaponsGadgetsParent)
        {
            RectTransform rect = child as RectTransform;
            if (rect == null) continue;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -index * 114f);
            rect.sizeDelta = new Vector2(0f, 104f);
            rect.localScale = Vector3.one;

            Image background = child.GetComponent<Image>();
            if (background != null)
            {
                background.color = CardColor;
                LoadoutCardHover hover = child.GetComponent<LoadoutCardHover>();
                if (hover == null) hover = child.gameObject.AddComponent<LoadoutCardHover>();
                hover.Configure(background, CardColor, new Color(0.208f, 0.224f, 0.251f, 1f));
            }

            TextMeshProUGUI nameText = child.GetComponentInChildren<TextMeshProUGUI>(true);
            if (nameText != null)
            {
                nameText.font = font;
                nameText.fontSize = 15f;
                nameText.fontStyle = FontStyles.Bold;
                nameText.color = Color.white;
                nameText.alignment = TextAlignmentOptions.MidlineLeft;
                nameText.textWrappingMode = TextWrappingModes.NoWrap;

                RectTransform textRect = nameText.rectTransform;
                textRect.localScale = Vector3.one;
                textRect.anchorMin = new Vector2(0f, 1f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.pivot = new Vector2(0.5f, 1f);
                textRect.anchoredPosition = new Vector2(0f, -5f);
                textRect.sizeDelta = new Vector2(-20f, 27f);
                textRect.SetAsLastSibling();
            }

            Image[] images = child.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image.transform == child || image.sprite == null) continue;

                RectTransform imageRect = image.rectTransform;
                imageRect.anchorMin = Vector2.zero;
                imageRect.anchorMax = Vector2.one;
                imageRect.offsetMin = new Vector2(12f, 8f);
                imageRect.offsetMax = new Vector2(-12f, -33f);
                imageRect.localScale = Vector3.one;
                image.preserveAspect = true;
            }

            foreach (Button nestedButton in child.GetComponentsInChildren<Button>(true))
            {
                if (nestedButton.transform == child) continue;

                RectTransform nestedRect = nestedButton.GetComponent<RectTransform>();
                nestedRect.anchorMin = new Vector2(1f, 0f);
                nestedRect.anchorMax = new Vector2(1f, 0f);
                nestedRect.pivot = new Vector2(1f, 0f);
                nestedRect.anchoredPosition = new Vector2(-8f, 8f);
                nestedRect.sizeDelta = new Vector2(112f, 30f);
                nestedRect.localScale = Vector3.one;
            }

            index++;
        }
    }

    private void UpdateStagePresentation(InfantryLoadoutCustomization.SelectionStage stage)
    {
        leftPanelTitle.text = stage switch
        {
            InfantryLoadoutCustomization.SelectionStage.ClassSelection => "CLASSES",
            InfantryLoadoutCustomization.SelectionStage.LoadoutOptionSelection => "LOADOUT",
            InfantryLoadoutCustomization.SelectionStage.ItemSelection => "EQUIPMENT",
            InfantryLoadoutCustomization.SelectionStage.WeaponCustomization => "AVAILABLE ATTACHMENTS",
            InfantryLoadoutCustomization.SelectionStage.SkinSelection => "SKINS",
            _ => "LOADOUT"
        };

        bool isCustomizingWeapon = stage == InfantryLoadoutCustomization.SelectionStage.WeaponCustomization;
        bool showEquipmentDetails = stage != InfantryLoadoutCustomization.SelectionStage.ClassSelection;

        previewPanel.gameObject.SetActive(showEquipmentDetails);
        rightPanel.gameObject.SetActive(showEquipmentDetails);
        bottomPanelObject.SetActive(isCustomizingWeapon);

    }

    private int CalculateDynamicSignature()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + GetChildrenSignature(loadout.classesParent);
            hash = hash * 31 + GetChildrenSignature(loadout.loadoutOptionsParent);
            hash = hash * 31 + GetChildrenSignature(loadout.weaponsGadgetsParent);
            return hash;
        }
    }

    private static int GetChildrenSignature(Transform parent)
    {
        if (parent == null) return 0;

        unchecked
        {
            int hash = parent.childCount;
            foreach (Transform child in parent)
                hash = hash * 31 + child.GetInstanceID();
            return hash;
        }
    }

    private void StyleButton(
        GameObject buttonObject,
        Color backgroundColor,
        float fontSize,
        TextAlignmentOptions alignment,
        Color? textColor = null)
    {
        if (buttonObject == null) return;

        Image image = buttonObject.GetComponent<Image>();
        if (image != null) image.color = backgroundColor;

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.925f, 0.933f, 0.945f, 1f);
            colors.pressedColor = new Color(0.722f, 0.741f, 0.769f, 1f);
            colors.selectedColor = new Color(0.847f, 0.863f, 0.882f, 1f);
            colors.disabledColor = new Color(0.412f, 0.431f, 0.459f, 0.48f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        TextMeshProUGUI text = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null) return;

        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = textColor ?? Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
    }

    private static void SetButtonText(GameObject buttonObject, string text)
    {
        if (buttonObject == null) return;

        TextMeshProUGUI label = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.text = text;
    }

    private RectTransform CreatePanel(string objectName, Transform parent, Color color, bool addOutline)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.layer = gameObject.layer;
        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        if (addOutline)
        {
            Outline outline = panelObject.AddComponent<Outline>();
            outline.effectColor = PanelBorderColor;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;
        }

        return panelObject.GetComponent<RectTransform>();
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        return label;
    }

    private void CreateAccentLine(Transform parent)
    {
        RectTransform line = CreatePanel("Accent Line", parent, AccentColor, false);
        line.anchorMin = new Vector2(0f, 0f);
        line.anchorMax = new Vector2(1f, 0f);
        line.pivot = new Vector2(0.5f, 0f);
        line.anchoredPosition = Vector2.zero;
        line.sizeDelta = new Vector2(0f, 3f);
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.layer = parent.gameObject.layer;
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetOffsets(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
