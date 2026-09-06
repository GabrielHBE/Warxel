using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class SettingsHueRing : MaskableGraphic, IPointerDownHandler, IDragHandler
{
    [SerializeField, Range(0.55f, 0.9f)] private float innerRadiusRatio = 0.79f;
    [SerializeField, Range(24, 128)] private int segments = 72;
    [SerializeField] private RectTransform cursor;

    private float hue;

    public event Action<float> HueChanged;

    public void SetHue(float value)
    {
        hue = Mathf.Repeat(value, 1f);
        UpdateCursor();
    }

    public void OnPointerDown(PointerEventData eventData) => SelectAt(eventData);

    public void OnDrag(PointerEventData eventData) => SelectAt(eventData);

    public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
    {
        if (!base.Raycast(screenPoint, eventCamera) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 localPoint)) return false;

        Vector2 centered = localPoint - rectTransform.rect.center;
        float outerRadius = Radius();
        float distance = centered.magnitude;
        return distance >= outerRadius * innerRadiusRatio && distance <= outerRadius;
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        float outerRadius = Radius();
        float innerRadius = outerRadius * innerRadiusRatio;
        Vector2 center = rectTransform.rect.center;

        for (int i = 0; i < segments; i++)
        {
            float hue0 = i / (float)segments;
            float hue1 = (i + 1f) / segments;
            float angle0 = hue0 * Mathf.PI * 2f;
            float angle1 = hue1 * Mathf.PI * 2f;
            Vector2 direction0 = new Vector2(Mathf.Cos(angle0), Mathf.Sin(angle0));
            Vector2 direction1 = new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1));
            Color color0 = Color.HSVToRGB(hue0, 1f, 1f);
            Color color1 = Color.HSVToRGB(hue1, 1f, 1f);
            int start = vertexHelper.currentVertCount;

            AddVertex(vertexHelper, center + direction0 * innerRadius, color0);
            AddVertex(vertexHelper, center + direction0 * outerRadius, color0);
            AddVertex(vertexHelper, center + direction1 * outerRadius, color1);
            AddVertex(vertexHelper, center + direction1 * innerRadius, color1);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }
    }

    private void SelectAt(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint)) return;
        Vector2 direction = localPoint - rectTransform.rect.center;
        if (direction.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(direction.y, direction.x);
        if (angle < 0f) angle += Mathf.PI * 2f;
        hue = angle / (Mathf.PI * 2f);
        UpdateCursor();
        HueChanged?.Invoke(hue);
    }

    private void UpdateCursor()
    {
        if (cursor == null) return;
        float angle = hue * Mathf.PI * 2f;
        float radius = Radius() * (1f + innerRadiusRatio) * 0.5f;
        cursor.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private float Radius()
    {
        Rect rect = rectTransform.rect;
        return Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f);
    }

    private static void AddVertex(VertexHelper helper, Vector2 position, Color color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        helper.AddVert(vertex);
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        UpdateCursor();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        segments = Mathf.Clamp(segments, 24, 128);
        innerRadiusRatio = Mathf.Clamp(innerRadiusRatio, 0.55f, 0.9f);
        SetVerticesDirty();
        UpdateCursor();
    }
#endif
}
