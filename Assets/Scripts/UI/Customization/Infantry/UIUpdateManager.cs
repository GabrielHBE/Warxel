using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUpdateManager : MonoBehaviour
{
    private InfantryLoadoutCustomization infantryLoadoutCustomization;

    [Header("Camera")]
    public Camera switchLoadoutCamera;

    [Header("Attachment Preview Focus")]
    [SerializeField, Min(0.1f)] private float attachmentFocusDistance = 1.6f;
    [SerializeField, Range(1f, 179f)] private float attachmentFocusFov = 40f;
    [SerializeField, Min(0f)] private float attachmentFocusTransitionSpeed = 10f;

    [Header("Preview Rotation")]
    [SerializeField, Min(0f)] private float previewRotationSensitivity = 0.25f;
    [SerializeField, Min(0f)] private float previewRotationReturnSpeed = 10f;

    [Header(" Stats Display")]
    [SerializeField] public TextMeshProUGUI itemStatusText;
    [SerializeField] private DamageCurveGraph damageCurveGraph;

    [Header("Damage Graph Layout")]
    [SerializeField, Range(0.2f, 0.5f)] private float damageGraphHeightRatio = 0.35f;

    [Header("Stat Slider Animation")]
    [SerializeField, Min(0.1f)] private float statSliderAnimationSpeed = 2.8f;

    [Header("Stat Text Layout")]
    [SerializeField, Range(12f, 24f)] private float statRowFontSize = 18f;
    [SerializeField, Range(12f, 24f)] private float statFooterFontSize = 18f;
    [SerializeField, Range(32f, 52f)] private float statRowHeight = 42f;

    private LoadoutUITheme loadoutUITheme;
    private Transform previewRoot;
    private Vector3 defaultPreviewLocalPosition;
    private Vector3 targetPreviewLocalPosition;
    private float defaultPreviewFov;
    private bool attachmentFocusActive;
    private bool attachmentFocusDefaultsCached;
    private RectTransform previewInteractionArea;
    private GameObject trackedPreviewItem;
    private Quaternion defaultPreviewItemLocalRotation;
    private Vector3 lastPreviewPointerPosition;
    private bool rotatingPreviewItem;
    private RectTransform weaponStatsContainer;
    private TextMeshProUGUI weaponStatsFooterLabel;
    private readonly List<WeaponStatRow> weaponStatRows = new List<WeaponStatRow>();
    private WeaponProperties displayedWeaponProperties;

    private static readonly Color StatTextColor = new Color(0.9f, 0.92f, 0.94f, 1f);
    private static readonly Color StatTrackColor = new Color(0.09f, 0.105f, 0.12f, 1f);
    private static readonly Color StatFillColor = new Color(255, 255, 255, 1f);
    private static readonly Color PositiveStatColor = new Color(0.3f, 0.88f, 0.46f, 1f);
    private static readonly Color NegativeStatColor = new Color(1f, 0.32f, 0.29f, 1f);
    private const float StatComparisonEpsilon = 0.0001f;

    public void Initialize(InfantryLoadoutCustomization parent)
    {
        if (switchLoadoutCamera != null) switchLoadoutCamera.enabled = false;

        infantryLoadoutCustomization = parent;
        CacheAttachmentFocusDefaults();
        EnsureDamageCurveGraph();
        loadoutUITheme = GetComponent<LoadoutUITheme>();
        if (loadoutUITheme == null) loadoutUITheme = gameObject.AddComponent<LoadoutUITheme>();
        loadoutUITheme.Initialize(parent, itemStatusText);
        previewInteractionArea = loadoutUITheme.PreviewPanel;
        EnsureWeaponStatsPanel();
    }

    public void UpdateUI()
    {

        UpdateWeaponStatusDisplay();
        UpdateCameraAndButtonVisibility();
        UpdateAttachmentFocus();
        UpdatePreviewRotation();
        UpdateStatSliderAnimations();
        UpdateClassParentVisibility();
        if (loadoutUITheme != null) loadoutUITheme.Refresh();
    }



    private void UpdateWeaponStatusDisplay()
    {
        GameObject currentItem = infantryLoadoutCustomization._currentItemSelected;
        bool isWeaponSelected = currentItem != null && currentItem.GetComponent<WeaponProperties>() != null;

        if (isWeaponSelected)
        {
            infantryLoadoutCustomization.weaponStatusParent.SetActive(true);
            bool showCustomizeButton = infantryLoadoutCustomization.GetCurrentStage() == InfantryLoadoutCustomization.SelectionStage.ItemSelection &&
                                      IsPrimaryOrSecondaryWeapon();
            infantryLoadoutCustomization.customizeWeaponButton.SetActive(showCustomizeButton);
        }
        else
        {
            infantryLoadoutCustomization.weaponStatusParent.SetActive(false);
            infantryLoadoutCustomization.customizeWeaponButton.SetActive(false);
            infantryLoadoutCustomization.customization_buttons_parent.SetActive(false);
            displayedWeaponProperties = null;
        }
    }

    private bool IsPrimaryOrSecondaryWeapon()
    {
        var currentOption = infantryLoadoutCustomization.loadoutOptionManager.GetCurrentOption();

        return currentOption == LoadoutOptionManager.LoadoutOption.PrimaryWeapon || currentOption == LoadoutOptionManager.LoadoutOption.SecondaryWeapon;
    }

    private void UpdateCameraAndButtonVisibility()
    {
        if (infantryLoadoutCustomization.GetCurrentStage() == InfantryLoadoutCustomization.SelectionStage.ClassSelection)
        {
            if (switchLoadoutCamera != null) switchLoadoutCamera.enabled = false;
            infantryLoadoutCustomization.updateLoadoutButton.SetActive(true);
        }
        else
        {
            if (switchLoadoutCamera != null) switchLoadoutCamera.enabled = true;
            infantryLoadoutCustomization.updateLoadoutButton.SetActive(false);
        }
    }

    private void UpdateClassParentVisibility() => infantryLoadoutCustomization.classesParent.gameObject.SetActive(infantryLoadoutCustomization.GetCurrentStage() == InfantryLoadoutCustomization.SelectionStage.ClassSelection);
    public void UpdateItemStatusText(string text)
    {
        if (itemStatusText != null) itemStatusText.text = text;
    }

    public void UpdateWeaponStatSliders(WeaponProperties weaponProperties)
    {
        if (weaponProperties == null) return;

        displayedWeaponProperties = weaponProperties;
        WeaponStatsSnapshot currentStats = WeaponStatsSnapshot.Capture(weaponProperties);
        RenderWeaponStats(currentStats, null, null);
    }

    public void PreviewAttachmentStats(WeaponProperties weaponProperties, Attatchment attachment)
    {
        if (weaponProperties == null || attachment == null) return;

        displayedWeaponProperties = weaponProperties;
        WeaponStatsSnapshot currentStats = WeaponStatsSnapshot.Capture(weaponProperties);
        WeaponStatsSnapshot projectedStats = CreateProjectedStats(weaponProperties, attachment, currentStats);
        string attachmentName = string.IsNullOrWhiteSpace(attachment.attachmentName)
            ? attachment.gameObject.name
            : attachment.attachmentName;

        RenderWeaponStats(currentStats, projectedStats, attachmentName);
    }

    public void PreviewWeaponStats(WeaponProperties equippedWeapon, WeaponProperties previewWeapon)
    {
        if (equippedWeapon == null || previewWeapon == null) return;

        displayedWeaponProperties = previewWeapon;
        WeaponStatsSnapshot currentStats = WeaponStatsSnapshot.Capture(equippedWeapon);
        WeaponStatsSnapshot projectedStats = WeaponStatsSnapshot.Capture(previewWeapon);
        string weaponName = string.IsNullOrWhiteSpace(previewWeapon.weaponName)
            ? previewWeapon.gameObject.name
            : previewWeapon.weaponName;

        RenderWeaponStats(currentStats, projectedStats, weaponName);
    }

    public void ClearAttachmentStatsPreview()
    {
        if (displayedWeaponProperties != null)
            UpdateWeaponStatSliders(displayedWeaponProperties);
    }

    public void UpdateAttachmentPoints(float currentPoints)
    {
        if (loadoutUITheme == null) return;
        loadoutUITheme.UpdateAttachmentPoints(currentPoints, AttatchmentManager.MaxAttachmentPoints);
    }

    public void UpdateDamageCurveGraph(AnimationCurve damageCurve)
    {
        EnsureDamageCurveGraph();
        damageCurveGraph.SetCurve(damageCurve);
    }

    public bool FocusAttachmentHolder(Transform attachmentHolder)
    {
        if (attachmentHolder == null || switchLoadoutCamera == null) return false;
        if (!attachmentFocusDefaultsCached) CacheAttachmentFocusDefaults();
        if (previewRoot == null) return false;

        Transform cameraTransform = switchLoadoutCamera.transform;
        Vector3 holderOffsetFromPreviewRoot = attachmentHolder.position - previewRoot.position;
        Vector3 desiredHolderPosition = cameraTransform.TransformPoint(Vector3.forward * attachmentFocusDistance);
        Vector3 desiredPreviewWorldPosition = desiredHolderPosition - holderOffsetFromPreviewRoot;

        targetPreviewLocalPosition = previewRoot.parent != null
            ? previewRoot.parent.InverseTransformPoint(desiredPreviewWorldPosition)
            : desiredPreviewWorldPosition;
        attachmentFocusActive = true;
        return true;
    }

    public void ClearAttachmentFocus()
    {
        attachmentFocusActive = false;
    }

    private void CacheAttachmentFocusDefaults()
    {
        if (infantryLoadoutCustomization == null || switchLoadoutCamera == null) return;

        previewRoot = infantryLoadoutCustomization.currentItemParent;
        if (previewRoot == null) return;

        defaultPreviewLocalPosition = previewRoot.localPosition;
        targetPreviewLocalPosition = defaultPreviewLocalPosition;
        defaultPreviewFov = switchLoadoutCamera.fieldOfView;
        attachmentFocusDefaultsCached = true;
    }

    private void UpdateAttachmentFocus()
    {
        if (!attachmentFocusDefaultsCached || previewRoot == null || switchLoadoutCamera == null) return;

        float interpolation = attachmentFocusTransitionSpeed <= 0f
            ? 1f
            : 1f - Mathf.Exp(-attachmentFocusTransitionSpeed * Time.unscaledDeltaTime);
        Vector3 desiredPosition = attachmentFocusActive
            ? targetPreviewLocalPosition
            : defaultPreviewLocalPosition;
        float desiredFov = attachmentFocusActive ? attachmentFocusFov : defaultPreviewFov;

        previewRoot.localPosition = Vector3.Lerp(previewRoot.localPosition, desiredPosition, interpolation);
        switchLoadoutCamera.fieldOfView = Mathf.Lerp(switchLoadoutCamera.fieldOfView, desiredFov, interpolation);
    }

    private void UpdatePreviewRotation()
    {
        GameObject currentPreviewItem = infantryLoadoutCustomization._currentItemSelected;
        if (currentPreviewItem == null)
        {
            trackedPreviewItem = null;
            rotatingPreviewItem = false;
            return;
        }

        if (currentPreviewItem != trackedPreviewItem)
        {
            trackedPreviewItem = currentPreviewItem;
            defaultPreviewItemLocalRotation = currentPreviewItem.transform.localRotation;
            rotatingPreviewItem = false;
        }

        if (Input.GetMouseButtonDown(0) && IsPointerInsidePreviewArea())
        {
            rotatingPreviewItem = true;
            lastPreviewPointerPosition = Input.mousePosition;
        }

        if (rotatingPreviewItem && Input.GetMouseButton(0))
        {
            Vector3 pointerPosition = Input.mousePosition;
            Vector2 pointerDelta = pointerPosition - lastPreviewPointerPosition;
            lastPreviewPointerPosition = pointerPosition;

            Transform itemTransform = currentPreviewItem.transform;
            Transform cameraTransform = switchLoadoutCamera != null ? switchLoadoutCamera.transform : transform;
            itemTransform.Rotate(cameraTransform.up, -pointerDelta.x * previewRotationSensitivity, Space.World);
            itemTransform.Rotate(cameraTransform.right, pointerDelta.y * previewRotationSensitivity, Space.World);
        }

        if (rotatingPreviewItem && !Input.GetMouseButton(0)) rotatingPreviewItem = false;
        if (rotatingPreviewItem) return;

        float interpolation = previewRotationReturnSpeed <= 0f
            ? 1f
            : 1f - Mathf.Exp(-previewRotationReturnSpeed * Time.unscaledDeltaTime);
        Transform previewItemTransform = currentPreviewItem.transform;
        previewItemTransform.localRotation = Quaternion.Slerp(
            previewItemTransform.localRotation,
            defaultPreviewItemLocalRotation,
            interpolation);

        if (Quaternion.Angle(previewItemTransform.localRotation, defaultPreviewItemLocalRotation) < 0.05f)
            previewItemTransform.localRotation = defaultPreviewItemLocalRotation;
    }

    private bool IsPointerInsidePreviewArea()
    {
        if (previewInteractionArea == null || !previewInteractionArea.gameObject.activeInHierarchy) return false;

        Canvas canvas = previewInteractionArea.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(
            previewInteractionArea,
            Input.mousePosition,
            eventCamera);
    }

    public void HideDamageCurveGraph()
    {
        if (damageCurveGraph != null) damageCurveGraph.Hide();
    }

    private void EnsureDamageCurveGraph()
    {
        if (itemStatusText == null) return;

        Transform graphParent = itemStatusText.transform.parent;
        if (damageCurveGraph == null)
        {
            damageCurveGraph = graphParent.GetComponentInChildren<DamageCurveGraph>(true);

            if (damageCurveGraph == null)
            {
                GameObject graphObject = new GameObject(
                    "Damage Curve Graph",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(DamageCurveGraph));

                graphObject.layer = itemStatusText.gameObject.layer;
                graphObject.transform.SetParent(graphParent, false);
                damageCurveGraph = graphObject.GetComponent<DamageCurveGraph>();
            }
        }

        RectTransform textRect = itemStatusText.rectTransform;
        textRect.anchorMin = new Vector2(0f, damageGraphHeightRatio);
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 4f);
        textRect.offsetMax = new Vector2(-12f, -12f);
        textRect.pivot = new Vector2(0.5f, 1f);
        itemStatusText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform graphRect = damageCurveGraph.rectTransform;
        graphRect.anchorMin = Vector2.zero;
        graphRect.anchorMax = new Vector2(1f, damageGraphHeightRatio);
        graphRect.offsetMin = new Vector2(8f, 8f);
        graphRect.offsetMax = new Vector2(-8f, -4f);
        graphRect.pivot = new Vector2(0.5f, 0.5f);

        damageCurveGraph.SetFont(itemStatusText.font);
        if (!damageCurveGraph.HasCurve) damageCurveGraph.Hide();
    }

    private void EnsureWeaponStatsPanel()
    {
        if (itemStatusText == null) return;

        Transform statsParent = itemStatusText.transform.parent;
        if (weaponStatsContainer == null)
        {
            Transform existingPanel = statsParent.Find("Weapon Stat Sliders");
            if (existingPanel != null) Destroy(existingPanel.gameObject);

            GameObject panelObject = new GameObject("Weapon Stat Sliders", typeof(RectTransform));
            panelObject.layer = itemStatusText.gameObject.layer;
            panelObject.transform.SetParent(statsParent, false);
            weaponStatsContainer = panelObject.GetComponent<RectTransform>();

            weaponStatsFooterLabel = CreateStatsText(
                "Additional Stats",
                weaponStatsContainer,
                statFooterFontSize,
                FontStyles.Normal);
            weaponStatsFooterLabel.alignment = TextAlignmentOptions.TopLeft;
            weaponStatsFooterLabel.textWrappingMode = TextWrappingModes.Normal;
            weaponStatsFooterLabel.overflowMode = TextOverflowModes.Ellipsis;
            weaponStatsFooterLabel.lineSpacing = 2f;
        }

        weaponStatsContainer.anchorMin = new Vector2(0f, damageGraphHeightRatio);
        weaponStatsContainer.anchorMax = Vector2.one;
        weaponStatsContainer.offsetMin = new Vector2(12f, 4f);
        weaponStatsContainer.offsetMax = new Vector2(-12f, -12f);
        weaponStatsContainer.pivot = new Vector2(0.5f, 1f);
        weaponStatsContainer.localScale = Vector3.one;

        itemStatusText.gameObject.SetActive(false);
    }

    private void RenderWeaponStats(
        WeaponStatsSnapshot currentStats,
        WeaponStatsSnapshot projectedStats,
        string previewAttachmentName)
    {
        EnsureWeaponStatsPanel();
        if (weaponStatsContainer == null) return;

        weaponStatsContainer.gameObject.SetActive(true);

        List<WeaponStatView> stats = CreateStatViews(currentStats, projectedStats);
        EnsureWeaponStatRows(stats.Count);

        for (int i = 0; i < weaponStatRows.Count; i++)
        {
            bool shouldShow = i < stats.Count;
            WeaponStatRow row = weaponStatRows[i];
            row.Root.gameObject.SetActive(shouldShow);
            if (!shouldShow) continue;

            WeaponStatView stat = stats[i];
            bool hasComparison = stat.ProjectedValue.HasValue &&
                                 !Mathf.Approximately(stat.CurrentValue, stat.ProjectedValue.Value);
            bool isPositive = hasComparison &&
                              (stat.HigherIsBetter
                                  ? stat.ProjectedValue.Value > stat.CurrentValue + StatComparisonEpsilon
                                  : stat.ProjectedValue.Value < stat.CurrentValue - StatComparisonEpsilon);
            Color differenceColor = isPositive ? PositiveStatColor : NegativeStatColor;

            float displayedValue = stat.ProjectedValue ?? stat.CurrentValue;
            float currentNormalized = Mathf.InverseLerp(
                stat.Minimum,
                stat.Maximum,
                Mathf.Clamp(stat.CurrentValue, stat.Minimum, stat.Maximum));
            float displayedNormalized = Mathf.InverseLerp(
                stat.Minimum,
                stat.Maximum,
                Mathf.Clamp(displayedValue, stat.Minimum, stat.Maximum));

            row.Label.text = stat.Label;
            row.Label.color = StatTextColor;
            row.Value.text = hasComparison
                ? $"{stat.Format(stat.CurrentValue)}  →  {stat.Format(displayedValue)}"
                : stat.Format(stat.CurrentValue);
            row.Value.color = StatTextColor;
            row.Slider.minValue = stat.Minimum;
            row.Slider.maxValue = stat.Maximum;
            row.Fill.color = StatFillColor;

            if (!row.IsInitialized)
            {
                row.AnimatedBaseNormalized = currentNormalized;
                row.AnimatedDifferenceNormalized = currentNormalized;
                row.Slider.SetValueWithoutNotify(Mathf.Lerp(
                    row.Slider.minValue,
                    row.Slider.maxValue,
                    currentNormalized));
                row.IsInitialized = true;
            }

            row.TargetBaseNormalized = currentNormalized;
            row.DifferenceStartNormalized = currentNormalized;

            if (hasComparison)
            {
                if (!row.DifferenceVisible)
                    row.AnimatedDifferenceNormalized = currentNormalized;

                row.TargetDifferenceNormalized = displayedNormalized;
                row.ComparisonActive = true;
                row.DifferenceVisible = true;
                row.DifferenceFill.color = differenceColor;
                row.DifferenceFill.gameObject.SetActive(true);
            }
            else
            {
                row.TargetDifferenceNormalized = currentNormalized;
                row.ComparisonActive = false;
            }
        }

        UpdateAdditionalStats(currentStats, projectedStats, stats.Count);
    }

    private void UpdateAdditionalStats(
        WeaponStatsSnapshot currentStats,
        WeaponStatsSnapshot projectedStats,
        int sliderCount)
    {
        if (weaponStatsFooterLabel == null) return;

        RectTransform footerRect = weaponStatsFooterLabel.rectTransform;
        footerRect.anchorMin = new Vector2(0f, 1f);
        footerRect.anchorMax = new Vector2(1f, 1f);
        footerRect.pivot = new Vector2(0.5f, 1f);
        footerRect.anchoredPosition = new Vector2(0f, -(sliderCount * statRowHeight));
        footerRect.sizeDelta = new Vector2(0f, 88f);

        string fireModes = FormatComparedText(
            FormatFireModes(currentStats.FireModes),
            projectedStats != null ? FormatFireModes(projectedStats.FireModes) : null);
        string magCount = FormatComparedNumber(
            currentStats.MagCount,
            projectedStats?.MagCount,
            "0");

        weaponStatsFooterLabel.text =
            $"FIRE MODES: {fireModes}    |    MAGAZINES: {magCount}\n" +
            $"VEHICLE DAMAGE: {currentStats.VehicleDamage:0.#}    |    " +
            $"HEADSHOT: {currentStats.HeadshotMultiplier:0.#}x\n" +
            $"DESTRUCTION: {currentStats.DestructionForce:0.#}";
        weaponStatsFooterLabel.color = StatTextColor;
    }

    private static string FormatComparedText(string currentValue, string projectedValue)
    {
        if (string.IsNullOrEmpty(projectedValue) || currentValue == projectedValue) return currentValue;
        return $"{currentValue} → {projectedValue}";
    }

    private static string FormatComparedNumber(
        float currentValue,
        float? projectedValue,
        string format)
    {
        if (!projectedValue.HasValue || Mathf.Approximately(currentValue, projectedValue.Value))
            return currentValue.ToString(format);

        return $"{currentValue.ToString(format)} → {projectedValue.Value.ToString(format)}";
    }

    private void UpdateStatSliderAnimations()
    {
        if (weaponStatsContainer == null || !weaponStatsContainer.gameObject.activeInHierarchy) return;

        float maxDelta = Mathf.Max(0.1f, statSliderAnimationSpeed) * Time.unscaledDeltaTime;
        foreach (WeaponStatRow row in weaponStatRows)
        {
            if (!row.Root.gameObject.activeSelf || !row.IsInitialized) continue;

            row.AnimatedBaseNormalized = Mathf.MoveTowards(
                row.AnimatedBaseNormalized,
                row.TargetBaseNormalized,
                maxDelta);
            row.Slider.SetValueWithoutNotify(Mathf.Lerp(
                row.Slider.minValue,
                row.Slider.maxValue,
                row.AnimatedBaseNormalized));

            if (!row.DifferenceVisible) continue;

            row.AnimatedDifferenceNormalized = Mathf.MoveTowards(
                row.AnimatedDifferenceNormalized,
                row.TargetDifferenceNormalized,
                maxDelta);

            float differenceMin = Mathf.Min(
                row.DifferenceStartNormalized,
                row.AnimatedDifferenceNormalized);
            float differenceMax = Mathf.Max(
                row.DifferenceStartNormalized,
                row.AnimatedDifferenceNormalized);
            row.DifferenceRect.anchorMin = new Vector2(differenceMin, 0f);
            row.DifferenceRect.anchorMax = new Vector2(differenceMax, 1f);
            row.DifferenceRect.offsetMin = new Vector2(0f, 1f);
            row.DifferenceRect.offsetMax = new Vector2(0f, -1f);

            if (!row.ComparisonActive &&
                Mathf.Abs(row.AnimatedDifferenceNormalized - row.TargetDifferenceNormalized) < 0.0005f)
            {
                row.DifferenceVisible = false;
                row.DifferenceFill.gameObject.SetActive(false);
            }
        }
    }

    private static string FormatFireModes(List<Firing.FireMode> fireModes)
    {
        return fireModes == null || fireModes.Count == 0
            ? "--"
            : string.Join(" / ", fireModes);
    }

    private void EnsureWeaponStatRows(int count)
    {
        while (weaponStatRows.Count < count)
        {
            int rowIndex = weaponStatRows.Count;
            GameObject rowObject = new GameObject($"Stat Row {rowIndex}", typeof(RectTransform));
            rowObject.layer = weaponStatsContainer.gameObject.layer;
            rowObject.transform.SetParent(weaponStatsContainer, false);

            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -rowIndex * statRowHeight);
            rowRect.sizeDelta = new Vector2(0f, statRowHeight - 2f);

            TextMeshProUGUI label = CreateStatsText("Label", rowRect, statRowFontSize, FontStyles.Normal);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.52f, 1f);
            labelRect.offsetMin = new Vector2(0f, 8f);
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            TextMeshProUGUI value = CreateStatsText("Value", rowRect, statRowFontSize, FontStyles.Bold);
            RectTransform valueRect = value.rectTransform;
            valueRect.anchorMin = new Vector2(0.45f, 0f);
            valueRect.anchorMax = Vector2.one;
            valueRect.offsetMin = new Vector2(0f, 8f);
            valueRect.offsetMax = Vector2.zero;
            value.alignment = TextAlignmentOptions.MidlineRight;

            GameObject sliderObject = new GameObject(
                "Slider",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Slider));
            sliderObject.layer = rowObject.layer;
            sliderObject.transform.SetParent(rowRect, false);

            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = new Vector2(1f, 0f);
            sliderRect.pivot = new Vector2(0.5f, 0f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = new Vector2(0f, 6f);

            Image track = sliderObject.GetComponent<Image>();
            track.color = StatTrackColor;
            track.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.layer = rowObject.layer;
            fillObject.transform.SetParent(sliderRect, false);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);

            Image fill = fillObject.GetComponent<Image>();
            fill.color = StatFillColor;
            fill.raycastTarget = false;

            GameObject differenceObject = new GameObject(
                "Difference",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            differenceObject.layer = rowObject.layer;
            differenceObject.transform.SetParent(sliderRect, false);

            RectTransform differenceRect = differenceObject.GetComponent<RectTransform>();
            differenceRect.anchorMin = Vector2.zero;
            differenceRect.anchorMax = Vector2.zero;
            differenceRect.offsetMin = new Vector2(0f, 1f);
            differenceRect.offsetMax = new Vector2(0f, -1f);

            Image differenceFill = differenceObject.GetComponent<Image>();
            differenceFill.color = PositiveStatColor;
            differenceFill.raycastTarget = false;
            differenceObject.SetActive(false);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = fill;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;

            weaponStatRows.Add(new WeaponStatRow(
                rowRect,
                label,
                value,
                slider,
                fill,
                differenceRect,
                differenceFill));
        }
    }

    private TextMeshProUGUI CreateStatsText(
        string objectName,
        Transform parent,
        float fontSize,
        FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = itemStatusText != null ? itemStatusText.font : TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = StatTextColor;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static List<WeaponStatView> CreateStatViews(
        WeaponStatsSnapshot current,
        WeaponStatsSnapshot projected)
    {
        float? ProjectedValue(float value) => projected != null ? value : (float?)null;

        return new List<WeaponStatView>
        {
            new WeaponStatView("Rate of Fire", current.RateOfFire, ProjectedValue(projected?.RateOfFire ?? 0f), 0f,
                GetDisplayMaximum(1200f, current.RateOfFire, projected?.RateOfFire), true, "0", " RPM"),
            new WeaponStatView("ADS Time", current.AdsSpeed, ProjectedValue(projected?.AdsSpeed ?? 0f), 0f,
                GetDisplayMaximum(2f, current.AdsSpeed, projected?.AdsSpeed), false, "0.00", " s"),
            new WeaponStatView("Primary Zoom", current.Zoom, ProjectedValue(projected?.Zoom ?? 0f), 0f,
                Sight.MAX_ZOOM_LEVEL, true, "0.#", "x"),
            new WeaponStatView("Canted Zoom", current.CantedZoom, ProjectedValue(projected?.CantedZoom ?? 0f), 0f,
                Sight.MAX_ZOOM_LEVEL, true, "0.#", "x"),
            new WeaponStatView("Spread per Shot", current.SpreadIncreaser, ProjectedValue(projected?.SpreadIncreaser ?? 0f),
                Spread.MIN_SPREAD_VALUE, Spread.MAX_SPREAD_VALUE, false, "0.00", ""),
            new WeaponStatView("Maximum Spread", current.MaxSpread, ProjectedValue(projected?.MaxSpread ?? 0f),
                Spread.MIN_SPREAD_VALUE, Spread.MAX_SPREAD_VALUE, false, "0.00", ""),
            new WeaponStatView("Horizontal Recoil", current.HorizontalRecoil, ProjectedValue(projected?.HorizontalRecoil ?? 0f),
                Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE, false, "0.00", ""),
            new WeaponStatView("Vertical Recoil", current.VerticalRecoil, ProjectedValue(projected?.VerticalRecoil ?? 0f),
                Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE, false, "0.00", ""),
            new WeaponStatView("First Shot Recoil", current.FirstShotRecoil, ProjectedValue(projected?.FirstShotRecoil ?? 0f),
                Recoil.MIN_FIRTSHOTINCREASER_VALUE, Recoil.MAX_FIRTSHOTINCREASER_VALUE, false, "0.0", "x"),
            new WeaponStatView("Damage", current.Damage, ProjectedValue(projected?.Damage ?? 0f), 0f,
                GetDisplayMaximum(100f, current.Damage, projected?.Damage), true, "0.0", ""),
            new WeaponStatView("Projectile Speed", current.MuzzleVelocity, ProjectedValue(projected?.MuzzleVelocity ?? 0f), 0f,
                GetDisplayMaximum(1200f, current.MuzzleVelocity, projected?.MuzzleVelocity), true, "0", " m/s"),
            new WeaponStatView("Rounds per Magazine", current.BulletsPerMag, ProjectedValue(projected?.BulletsPerMag ?? 0f), 0f,
                GetDisplayMaximum(100f, current.BulletsPerMag, projected?.BulletsPerMag), true, "0", ""),
            new WeaponStatView("Reload Time", current.ReloadTime, ProjectedValue(projected?.ReloadTime ?? 0f), 0f,
                GetDisplayMaximum(6f, current.ReloadTime, projected?.ReloadTime), false, "0.00", " s")
        };
    }

    private static float GetDisplayMaximum(float configuredMaximum, float currentValue, float? projectedValue)
    {
        float greatestValue = Mathf.Max(currentValue, projectedValue ?? currentValue);
        if (greatestValue <= configuredMaximum) return configuredMaximum;

        float increment = configuredMaximum >= 100f ? 100f : 1f;
        return Mathf.Ceil(greatestValue / increment) * increment;
    }

    private static WeaponStatsSnapshot CreateProjectedStats(
        WeaponProperties weaponProperties,
        Attatchment attachment,
        WeaponStatsSnapshot current)
    {
        WeaponStatsSnapshot projected = current.Copy();
        AttatchmentManager manager = weaponProperties.GetComponent<AttatchmentManager>();
        if (manager == null) return projected;

        if (attachment is CantedSight cantedSight)
        {
            projected.CantedZoom = cantedSight.zoomChange;
        }
        else if (attachment is Sight sight)
        {
            AttatchmentManager.AttachmentData oldSight = manager.GetCurrentSight();
            projected.Zoom = ApplyReplacementDelta(current.Zoom, oldSight?.zoomChange ?? 0f, sight.zoomChange);
        }
        else if (attachment is Nozzle nozzle)
        {
            AttatchmentManager.AttachmentData oldNozzle = manager.GetCurrentNozzle();
            projected.HorizontalRecoil = CalculateProjectedAverageRecoil(
                weaponProperties, oldNozzle?.horizontalRecoilChange ?? 0f, nozzle.horizontalRecoilChange, true);
            projected.VerticalRecoil = CalculateProjectedAverageRecoil(
                weaponProperties, oldNozzle?.verticalRecoilChange ?? 0f, nozzle.verticalRecoilChange, false);
            projected.FirstShotRecoil = ApplyReplacementDelta(
                current.FirstShotRecoil, oldNozzle?.firstShootChange ?? 0f, nozzle.firstShootRecoilChange);
            projected.MuzzleVelocity = ApplyReplacementDelta(
                current.MuzzleVelocity, oldNozzle?.muzzleVelocityChange ?? 0f, nozzle.muzzleVelocityChange);
            projected.SpreadIncreaser = ApplyReplacementDelta(
                current.SpreadIncreaser, oldNozzle?.spreadChange ?? 0f, nozzle.spreadChange);
        }
        else if (attachment is Barrel barrel)
        {
            AttatchmentManager.AttachmentData oldBarrel = manager.GetCurrentBarrel();
            projected.HorizontalRecoil = CalculateProjectedAverageRecoil(
                weaponProperties, oldBarrel?.horizontalRecoilChange ?? 0f, barrel.horizontalRecoilChange, true);
            projected.VerticalRecoil = CalculateProjectedAverageRecoil(
                weaponProperties, oldBarrel?.verticalRecoilChange ?? 0f, barrel.verticalRecoilChange, false);
            projected.FirstShotRecoil = ApplyReplacementDelta(
                current.FirstShotRecoil, oldBarrel?.firstShootChange ?? 0f, barrel.firstShootRecoilChange);
            projected.MuzzleVelocity = ApplyReplacementDelta(
                current.MuzzleVelocity, oldBarrel?.muzzleVelocityChange ?? 0f, barrel.muzzleVelocityChange);
            projected.AdsSpeed = ApplyReplacementDelta(
                current.AdsSpeed, oldBarrel?.adsSpeedChange ?? 0f, barrel.adsSpeedChange);
        }
        else if (attachment is Mag mag)
        {
            AttatchmentManager.AttachmentData oldMag = manager.GetCurrentMag();
            projected.AdsSpeed = ApplyReplacementDelta(
                current.AdsSpeed, oldMag?.adsSpeedChange ?? 0f, mag.adsSpeedChange);
            projected.ReloadTime = ApplyReplacementDelta(
                current.ReloadTime, oldMag?.reloadSpeedChanger ?? 0f, mag.reloadValues.reloadTime);
            projected.BulletsPerMag = mag.reloadValues.bulletsPerMag;
            projected.MagCount = ApplyReplacementDelta(
                current.MagCount, oldMag?.magCountChange ?? 0f, mag.reloadValues.magCount);
        }
        else if (attachment is Grip grip)
        {
            AttatchmentManager.AttachmentData oldGrip = manager.GetCurrentGrip();
            projected.HorizontalRecoil = CalculateProjectedAverageRecoil(
                weaponProperties, oldGrip?.horizontalRecoilChange ?? 0f, grip.horizontalRecoilChange, true);
            projected.VerticalRecoil = CalculateProjectedAverageRecoil(
                weaponProperties, oldGrip?.verticalRecoilChange ?? 0f, grip.verticalRecoilChange, false);
            projected.FirstShotRecoil = ApplyReplacementDelta(
                current.FirstShotRecoil, oldGrip?.firstShootChange ?? 0f, grip.firstShootChange);
            projected.AdsSpeed = ApplyReplacementDelta(
                current.AdsSpeed, oldGrip?.adsSpeedChange ?? 0f, grip.adsSpeedChange);
            projected.ReloadTime = ApplyReplacementDelta(
                current.ReloadTime, oldGrip?.reloadSpeedChange ?? 0f, grip.reloadSpeedChange);
        }
        else if (attachment is Ergonomics ergonomics)
        {
            AttatchmentManager.AttachmentData oldErgonomics = manager.GetCurrentErgonomics();
            projected.RateOfFire = ApplyReplacementDelta(
                current.RateOfFire, oldErgonomics?.rateOfFireChange ?? 0f, ergonomics.rafeOfFireChange);
            projected.AdsSpeed = ApplyReplacementDelta(
                current.AdsSpeed, oldErgonomics?.adsSpeedChange ?? 0f, ergonomics.adsSpeedChange);
            projected.ReloadTime = ApplyReplacementDelta(
                current.ReloadTime, oldErgonomics?.reloadSpeedChange ?? 0f, ergonomics.reloadSpeedChange);

            if (oldErgonomics?.fireModesChange != null)
            {
                foreach (Firing.FireMode fireMode in oldErgonomics.fireModesChange)
                    projected.FireModes.Remove(fireMode);
            }

            if (ergonomics.fireModesChange != null)
            {
                foreach (Firing.FireMode fireMode in ergonomics.fireModesChange)
                {
                    if (!projected.FireModes.Contains(fireMode)) projected.FireModes.Add(fireMode);
                }
            }
        }

        return projected;
    }

    private static float CalculateProjectedAverageRecoil(
        WeaponProperties weaponProperties,
        float oldChange,
        float newChange,
        bool horizontal)
    {
        Recoil.RecoilPattern[] pattern = weaponProperties.recoilValues.recoilPattern;
        if (pattern == null || pattern.Length == 0) return 0f;

        float total = 0f;
        foreach (Recoil.RecoilPattern recoil in pattern)
        {
            float currentValue = horizontal ? recoil.horizontalRecoil.value : recoil.verticalRecoil.value;
            float withoutOldAttachment = Mathf.Clamp(
                currentValue - oldChange,
                Recoil.MIN_RECOIL_VALUE,
                Recoil.MAX_RECOIL_VALUE);
            total += Mathf.Clamp(
                withoutOldAttachment + newChange,
                Recoil.MIN_RECOIL_VALUE,
                Recoil.MAX_RECOIL_VALUE);
        }

        return total / pattern.Length;
    }

    private static float ApplyReplacementDelta(float currentValue, float oldChange, float newChange)
    {
        return currentValue - oldChange + newChange;
    }

    private sealed class WeaponStatRow
    {
        public readonly RectTransform Root;
        public readonly TextMeshProUGUI Label;
        public readonly TextMeshProUGUI Value;
        public readonly Slider Slider;
        public readonly Image Fill;
        public readonly RectTransform DifferenceRect;
        public readonly Image DifferenceFill;
        public float AnimatedBaseNormalized;
        public float TargetBaseNormalized;
        public float DifferenceStartNormalized;
        public float AnimatedDifferenceNormalized;
        public float TargetDifferenceNormalized;
        public bool IsInitialized;
        public bool ComparisonActive;
        public bool DifferenceVisible;

        public WeaponStatRow(
            RectTransform root,
            TextMeshProUGUI label,
            TextMeshProUGUI value,
            Slider slider,
            Image fill,
            RectTransform differenceRect,
            Image differenceFill)
        {
            Root = root;
            Label = label;
            Value = value;
            Slider = slider;
            Fill = fill;
            DifferenceRect = differenceRect;
            DifferenceFill = differenceFill;
        }
    }

    private sealed class WeaponStatView
    {
        public readonly string Label;
        public readonly float CurrentValue;
        public readonly float? ProjectedValue;
        public readonly float Minimum;
        public readonly float Maximum;
        public readonly bool HigherIsBetter;
        private readonly string numberFormat;
        private readonly string suffix;

        public WeaponStatView(
            string label,
            float currentValue,
            float? projectedValue,
            float minimum,
            float maximum,
            bool higherIsBetter,
            string numberFormat,
            string suffix)
        {
            Label = label;
            CurrentValue = currentValue;
            ProjectedValue = projectedValue;
            Minimum = minimum;
            Maximum = Mathf.Max(minimum + Mathf.Epsilon, maximum);
            HigherIsBetter = higherIsBetter;
            this.numberFormat = numberFormat;
            this.suffix = suffix;
        }

        public string Format(float value) => value.ToString(numberFormat) + suffix;
    }

    private sealed class WeaponStatsSnapshot
    {
        public float RateOfFire;
        public float AdsSpeed;
        public float Zoom;
        public float CantedZoom;
        public float SpreadIncreaser;
        public float MaxSpread;
        public float HorizontalRecoil;
        public float VerticalRecoil;
        public float FirstShotRecoil;
        public float Damage;
        public float MuzzleVelocity;
        public float BulletsPerMag;
        public float ReloadTime;
        public float MagCount;
        public float VehicleDamage;
        public float HeadshotMultiplier;
        public float DestructionForce;
        public List<Firing.FireMode> FireModes;

        public static WeaponStatsSnapshot Capture(WeaponProperties weaponProperties)
        {
            CantedSight cantedSight = weaponProperties.GetComponentInChildren<CantedSight>();

            return new WeaponStatsSnapshot
            {
                RateOfFire = weaponProperties.firing.rateOfFire,
                AdsSpeed = weaponProperties.adsSpeed,
                Zoom = weaponProperties.zoom,
                CantedZoom = cantedSight != null ? cantedSight.zoomChange : 0f,
                SpreadIncreaser = weaponProperties.spreadValues.spreadIncreaser,
                MaxSpread = weaponProperties.spreadValues.maxSpread,
                HorizontalRecoil = CalculateAverageRecoil(weaponProperties, true),
                VerticalRecoil = CalculateAverageRecoil(weaponProperties, false),
                FirstShotRecoil = weaponProperties.recoilValues.firstShootRecoilMultiplier,
                Damage = weaponProperties.projectileValues.infantryDamage,
                MuzzleVelocity = weaponProperties.projectileValues.muzzleVelocity,
                BulletsPerMag = weaponProperties.reloadValues.bulletsPerMag,
                ReloadTime = weaponProperties.reloadValues.reloadTime,
                MagCount = weaponProperties.reloadValues.magCount,
                VehicleDamage = weaponProperties.projectileValues.vehicleDamage,
                HeadshotMultiplier = weaponProperties.projectileValues.headshotMultiplier,
                DestructionForce = weaponProperties.projectileValues.destructionRadius,
                FireModes = weaponProperties.firing.fireModes != null
                    ? new List<Firing.FireMode>(weaponProperties.firing.fireModes)
                    : new List<Firing.FireMode>()
            };
        }

        public WeaponStatsSnapshot Copy()
        {
            return new WeaponStatsSnapshot
            {
                RateOfFire = RateOfFire,
                AdsSpeed = AdsSpeed,
                Zoom = Zoom,
                CantedZoom = CantedZoom,
                SpreadIncreaser = SpreadIncreaser,
                MaxSpread = MaxSpread,
                HorizontalRecoil = HorizontalRecoil,
                VerticalRecoil = VerticalRecoil,
                FirstShotRecoil = FirstShotRecoil,
                Damage = Damage,
                MuzzleVelocity = MuzzleVelocity,
                BulletsPerMag = BulletsPerMag,
                ReloadTime = ReloadTime,
                MagCount = MagCount,
                VehicleDamage = VehicleDamage,
                HeadshotMultiplier = HeadshotMultiplier,
                DestructionForce = DestructionForce,
                FireModes = new List<Firing.FireMode>(FireModes)
            };
        }

        private static float CalculateAverageRecoil(WeaponProperties weaponProperties, bool horizontal)
        {
            Recoil.RecoilPattern[] pattern = weaponProperties.recoilValues.recoilPattern;
            if (pattern == null || pattern.Length == 0) return 0f;

            float total = 0f;
            foreach (Recoil.RecoilPattern recoil in pattern)
                total += horizontal ? recoil.horizontalRecoil.value : recoil.verticalRecoil.value;

            return total / pattern.Length;
        }
    }
}
