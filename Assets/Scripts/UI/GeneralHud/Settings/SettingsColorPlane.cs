using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public sealed class SettingsColorPlane : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private RectTransform cursor;

    private const int TextureSize = 64;
    private RawImage planeImage;
    private Texture2D texture;
    private float hue = -1f;
    private float saturation;
    private float brightness = 1f;

    public event Action<float, float> SelectionChanged;

    private void Awake()
    {
        EnsureTexture();
        UpdateCursor();
    }

    public void SetHue(float value)
    {
        float normalized = Mathf.Repeat(value, 1f);
        if (Mathf.Approximately(hue, normalized)) return;

        hue = normalized;
        RebuildTexture();
    }

    public void SetSelection(float newSaturation, float newBrightness)
    {
        saturation = Mathf.Clamp01(newSaturation);
        brightness = Mathf.Clamp01(newBrightness);
        UpdateCursor();
    }

    public void OnPointerDown(PointerEventData eventData) => SelectAt(eventData);

    public void OnDrag(PointerEventData eventData) => SelectAt(eventData);

    private void SelectAt(PointerEventData eventData)
    {
        RectTransform rectTransform = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint)) return;

        Rect rect = rectTransform.rect;
        saturation = Mathf.Clamp01(Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x));
        brightness = Mathf.Clamp01(Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y));
        UpdateCursor();
        SelectionChanged?.Invoke(saturation, brightness);
    }

    private void EnsureTexture()
    {
        if (planeImage == null) planeImage = GetComponent<RawImage>();
        if (texture != null) return;

        texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGB24, false)
        {
            name = "Settings Color Plane",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        planeImage.texture = texture;
    }

    private void RebuildTexture()
    {
        EnsureTexture();
        Color[] pixels = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            float value = y / (TextureSize - 1f);
            for (int x = 0; x < TextureSize; x++)
            {
                float sat = x / (TextureSize - 1f);
                pixels[y * TextureSize + x] = Color.HSVToRGB(hue, sat, value);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
    }

    private void UpdateCursor()
    {
        if (cursor == null) return;

        Rect rect = ((RectTransform)transform).rect;
        cursor.anchoredPosition = new Vector2(
            Mathf.Lerp(rect.xMin, rect.xMax, saturation),
            Mathf.Lerp(rect.yMin, rect.yMax, brightness));
    }

    private void OnRectTransformDimensionsChange() => UpdateCursor();

    private void OnDestroy()
    {
        if (texture != null) Destroy(texture);
    }
}
