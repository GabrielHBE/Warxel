using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class DamageCurveGraph : MaskableGraphic
{
    private const int CURVE_SAMPLE_COUNT = 96;
    private const int AXIS_DIVISIONS = 4;

    private static readonly Vector4 GRAPH_PADDING = new Vector4(54f, 44f, 18f, 34f);

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.015f, 0.015f, 0.017f, 0.96f);
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color axisColor = new Color(0.72f, 0.72f, 0.75f, 1f);
    [SerializeField] private Color curveColor = new Color(0.94f, 0.94f, 0.96f, 1f);
    [SerializeField] private Color pointColor = Color.white;

    [Header("Line Settings")]
    [SerializeField, Min(0.5f)] private float gridThickness = 1f;
    [SerializeField, Min(0.5f)] private float axisThickness = 2f;
    [SerializeField, Min(0.5f)] private float curveThickness = 3f;
    [SerializeField, Min(1f)] private float pointSize = 7f;

    private readonly List<TextMeshProUGUI> tickLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> pointLabels = new List<TextMeshProUGUI>();

    private AnimationCurve damageCurve;
    private TMP_FontAsset labelFont;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI xAxisLabel;
    private TextMeshProUGUI yAxisLabel;
    private Rect plotRect;
    private float maximumDistance = 1f;
    private float maximumDamage = 1f;

    public bool HasCurve => damageCurve != null && damageCurve.length > 0;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    public void SetFont(TMP_FontAsset font)
    {
        labelFont = font;
        ApplyFontToExistingLabels();
    }

    public void SetCurve(AnimationCurve curve)
    {
        damageCurve = curve;
        gameObject.SetActive(HasCurve);

        if (!HasCurve) return;

        CalculateGraphBounds();
        EnsureLabels();
        UpdateLabels();
        SetVerticesDirty();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();

        if (!HasCurve) return;

        CalculateGraphBounds();
        UpdateLabels();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (!HasCurve) return;

        CalculateGraphBounds();
        AddRectangle(vertexHelper, rectTransform.rect, backgroundColor);
        DrawGrid(vertexHelper);
        DrawCurve(vertexHelper);
        DrawCurvePoints(vertexHelper);
    }

    private void CalculateGraphBounds()
    {
        Rect rect = rectTransform.rect;
        plotRect = new Rect(
            rect.xMin + GRAPH_PADDING.x,
            rect.yMin + GRAPH_PADDING.y,
            Mathf.Max(1f, rect.width - GRAPH_PADDING.x - GRAPH_PADDING.z),
            Mathf.Max(1f, rect.height - GRAPH_PADDING.y - GRAPH_PADDING.w));

        maximumDistance = Mathf.Max(1f, damageCurve[damageCurve.length - 1].time);

        float greatestDamage = 0f;
        for (int i = 0; i < damageCurve.length; i++)
            greatestDamage = Mathf.Max(greatestDamage, damageCurve[i].value);

        for (int i = 0; i <= CURVE_SAMPLE_COUNT; i++)
        {
            float distance = maximumDistance * i / CURVE_SAMPLE_COUNT;
            greatestDamage = Mathf.Max(greatestDamage, damageCurve.Evaluate(distance));
        }

        maximumDamage = GetRoundedMaximum(Mathf.Max(1f, greatestDamage));
    }

    private void DrawGrid(VertexHelper vertexHelper)
    {
        for (int i = 0; i <= AXIS_DIVISIONS; i++)
        {
            float normalized = i / (float)AXIS_DIVISIONS;
            float x = Mathf.Lerp(plotRect.xMin, plotRect.xMax, normalized);
            float y = Mathf.Lerp(plotRect.yMin, plotRect.yMax, normalized);

            Color verticalColor = i == 0 ? axisColor : gridColor;
            Color horizontalColor = i == 0 ? axisColor : gridColor;
            float verticalThickness = i == 0 ? axisThickness : gridThickness;
            float horizontalThickness = i == 0 ? axisThickness : gridThickness;

            AddLine(vertexHelper, new Vector2(x, plotRect.yMin), new Vector2(x, plotRect.yMax), verticalThickness, verticalColor);
            AddLine(vertexHelper, new Vector2(plotRect.xMin, y), new Vector2(plotRect.xMax, y), horizontalThickness, horizontalColor);
        }
    }

    private void DrawCurve(VertexHelper vertexHelper)
    {
        Vector2 previousPoint = GetGraphPosition(0f, damageCurve.Evaluate(0f));

        for (int i = 1; i <= CURVE_SAMPLE_COUNT; i++)
        {
            float distance = maximumDistance * i / CURVE_SAMPLE_COUNT;
            Vector2 currentPoint = GetGraphPosition(distance, damageCurve.Evaluate(distance));
            AddLine(vertexHelper, previousPoint, currentPoint, curveThickness, curveColor);
            previousPoint = currentPoint;
        }
    }

    private void DrawCurvePoints(VertexHelper vertexHelper)
    {
        for (int i = 0; i < damageCurve.length; i++)
        {
            Keyframe key = damageCurve[i];
            Vector2 position = GetGraphPosition(key.time, key.value);
            Rect pointRect = new Rect(position.x - pointSize * 0.5f, position.y - pointSize * 0.5f, pointSize, pointSize);
            AddRectangle(vertexHelper, pointRect, pointColor);
        }
    }

    private Vector2 GetGraphPosition(float distance, float damage)
    {
        float normalizedDistance = Mathf.Clamp01(distance / maximumDistance);
        float normalizedDamage = Mathf.Clamp01(damage / maximumDamage);

        return new Vector2(
            Mathf.Lerp(plotRect.xMin, plotRect.xMax, normalizedDistance),
            Mathf.Lerp(plotRect.yMin, plotRect.yMax, normalizedDamage));
    }

    private void EnsureLabels()
    {
        titleLabel = EnsureFixedLabel(titleLabel, "Title", 20f, FontStyles.Bold);
        xAxisLabel = EnsureFixedLabel(xAxisLabel, "X Axis", 17f, FontStyles.Normal);
        yAxisLabel = EnsureFixedLabel(yAxisLabel, "Y Axis", 17f, FontStyles.Normal);

        EnsureLabelPool(tickLabels, (AXIS_DIVISIONS + 1) * 2, "Tick", 14f);
        EnsureLabelPool(pointLabels, damageCurve.length, "Point", 14f);
    }

    private void UpdateLabels()
    {
        if (!HasCurve) return;

        EnsureLabels();

        Rect graphRect = rectTransform.rect;
        ConfigureLabel(titleLabel, "Damage by Distance", new Vector2(plotRect.center.x, graphRect.yMax - 14f), new Vector2(240f, 26f));
        ConfigureLabel(xAxisLabel, "Distance (m)", new Vector2(plotRect.center.x, graphRect.yMin + 12f), new Vector2(160f, 24f));
        ConfigureLabel(yAxisLabel, "Damage", new Vector2(graphRect.xMin + 13f, plotRect.center.y), new Vector2(100f, 24f));
        yAxisLabel.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f);

        int labelIndex = 0;
        for (int i = 0; i <= AXIS_DIVISIONS; i++)
        {
            float normalized = i / (float)AXIS_DIVISIONS;
            float x = Mathf.Lerp(plotRect.xMin, plotRect.xMax, normalized);
            float distance = maximumDistance * normalized;
            ConfigureLabel(tickLabels[labelIndex++], FormatNumber(distance), new Vector2(x, plotRect.yMin - 15f), new Vector2(64f, 22f));
        }

        for (int i = 0; i <= AXIS_DIVISIONS; i++)
        {
            float normalized = i / (float)AXIS_DIVISIONS;
            float y = Mathf.Lerp(plotRect.yMin, plotRect.yMax, normalized);
            float damage = maximumDamage * normalized;
            TextMeshProUGUI label = tickLabels[labelIndex++];
            ConfigureLabel(label, FormatNumber(damage), new Vector2(plotRect.xMin - 29f, y), new Vector2(48f, 22f));
            label.alignment = TextAlignmentOptions.MidlineRight;
        }

        for (int i = 0; i < pointLabels.Count; i++)
        {
            bool shouldShow = i < damageCurve.length;
            pointLabels[i].gameObject.SetActive(shouldShow);
            if (!shouldShow) continue;

            Keyframe key = damageCurve[i];
            Vector2 pointPosition = GetGraphPosition(key.time, key.value);
            float verticalOffset = pointPosition.y > plotRect.yMax - 30f ? -21f : 21f;
            float labelX = Mathf.Clamp(pointPosition.x, plotRect.xMin + 44f, plotRect.xMax - 44f);
            string text = $"{FormatNumber(key.time)}m | {FormatNumber(key.value)}";

            TextMeshProUGUI label = pointLabels[i];
            label.color = curveColor;
            ConfigureLabel(label, text, new Vector2(labelX, pointPosition.y + verticalOffset), new Vector2(104f, 22f));
        }
    }

    private TextMeshProUGUI EnsureFixedLabel(TextMeshProUGUI label, string objectName, float fontSize, FontStyles fontStyle)
    {
        if (label != null) return label;

        label = CreateLabel(objectName, fontSize);
        label.fontStyle = fontStyle;
        return label;
    }

    private void EnsureLabelPool(List<TextMeshProUGUI> labels, int count, string namePrefix, float fontSize)
    {
        while (labels.Count < count)
            labels.Add(CreateLabel($"{namePrefix} {labels.Count}", fontSize));

        for (int i = 0; i < labels.Count; i++)
            labels[i].gameObject.SetActive(i < count);
    }

    private TextMeshProUGUI CreateLabel(string objectName, float fontSize)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(transform, false);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = labelFont;
        label.fontSize = fontSize;
        label.color = axisColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);

        return label;
    }

    private static void ConfigureLabel(TextMeshProUGUI label, string text, Vector2 position, Vector2 size)
    {
        label.text = text;
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = size;
    }

    private void ApplyFontToExistingLabels()
    {
        if (titleLabel != null) titleLabel.font = labelFont;
        if (xAxisLabel != null) xAxisLabel.font = labelFont;
        if (yAxisLabel != null) yAxisLabel.font = labelFont;

        foreach (TextMeshProUGUI label in tickLabels)
            label.font = labelFont;

        foreach (TextMeshProUGUI label in pointLabels)
            label.font = labelFont;
    }

    private static float GetRoundedMaximum(float value)
    {
        float magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(value)));
        float normalized = value / magnitude;
        float roundedNormalized;

        if (normalized <= 1f) roundedNormalized = 1f;
        else if (normalized <= 2f) roundedNormalized = 2f;
        else if (normalized <= 5f) roundedNormalized = 5f;
        else roundedNormalized = 10f;

        return roundedNormalized * magnitude;
    }

    private static string FormatNumber(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.Round(value).ToString("0")
            : value.ToString("0.#");
    }

    private static void AddLine(VertexHelper vertexHelper, Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 direction = end - start;
        if (direction.sqrMagnitude <= Mathf.Epsilon) return;

        Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (thickness * 0.5f);
        int startIndex = vertexHelper.currentVertCount;

        vertexHelper.AddVert(start - normal, color, Vector2.zero);
        vertexHelper.AddVert(start + normal, color, Vector2.zero);
        vertexHelper.AddVert(end + normal, color, Vector2.zero);
        vertexHelper.AddVert(end - normal, color, Vector2.zero);

        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }

    private static void AddRectangle(VertexHelper vertexHelper, Rect rect, Color color)
    {
        int startIndex = vertexHelper.currentVertCount;

        vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMin), color, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMax), color, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.zero);
        vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.zero);

        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }
}
