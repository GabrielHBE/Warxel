using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsColorPickerPopup : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private SettingsColorPlane colorPlane;
    [SerializeField] private SettingsHueRing hueRing;
    [SerializeField] private Slider alphaSlider;
    [SerializeField] private Image originalColorPreview;
    [SerializeField] private Image selectedColorPreview;
    [SerializeField] private TMP_InputField hexInput;
    [SerializeField] private TextMeshProUGUI saturationValue;
    [SerializeField] private TextMeshProUGUI brightnessValue;
    [SerializeField] private TextMeshProUGUI alphaValue;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;

    private Action<Color> onApplied;
    private float hue;
    private float saturation;
    private float brightness;
    private float alpha = 1f;
    private bool initialized;
    private bool synchronizing;

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private void Awake()
    {
        EnsureInitialized();
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    public void Open(Color initialColor, Action<Color> applyCallback)
    {
        EnsureInitialized();
        onApplied = applyCallback;
        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
            popupRoot.transform.SetAsLastSibling();
        }

        if (originalColorPreview != null) originalColorPreview.color = initialColor;
        SetColor(initialColor);
    }

    public void CloseWithoutApply()
    {
        onApplied = null;
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        if (colorPlane != null) colorPlane.SelectionChanged += OnPlaneChanged;
        if (hueRing != null) hueRing.HueChanged += OnHueChanged;
        alphaSlider?.onValueChanged.AddListener(OnAlphaChanged);
        hexInput?.onEndEdit.AddListener(OnHexChanged);
        applyButton?.onClick.AddListener(Apply);
        cancelButton?.onClick.AddListener(CloseWithoutApply);
    }

    private void SetColor(Color color)
    {
        Color.RGBToHSV(color, out hue, out saturation, out brightness);
        alpha = color.a;
        SynchronizeControls();
    }

    private void OnPlaneChanged(float newSaturation, float newBrightness)
    {
        if (synchronizing) return;
        saturation = newSaturation;
        brightness = newBrightness;
        RefreshVisuals();
    }

    private void OnHueChanged(float value)
    {
        if (synchronizing) return;
        hue = Mathf.Repeat(value, 1f);
        colorPlane?.SetHue(hue);
        RefreshVisuals();
    }

    private void OnAlphaChanged(float value)
    {
        if (synchronizing) return;
        alpha = Mathf.Clamp01(value);
        RefreshVisuals();
    }

    private void OnHexChanged(string html)
    {
        if (synchronizing) return;
        string normalized = string.IsNullOrWhiteSpace(html) ? string.Empty : html.Trim();
        if (!normalized.StartsWith("#")) normalized = "#" + normalized;

        if (ColorUtility.TryParseHtmlString(normalized, out Color color))
        {
            SetColor(color);
        }
        else if (hexInput != null)
        {
            hexInput.SetTextWithoutNotify("#" + ColorUtility.ToHtmlStringRGBA(CurrentColor()));
        }
    }

    private void SynchronizeControls()
    {
        synchronizing = true;
        hueRing?.SetHue(hue);
        alphaSlider?.SetValueWithoutNotify(alpha);
        colorPlane?.SetHue(hue);
        colorPlane?.SetSelection(saturation, brightness);
        synchronizing = false;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        Color color = CurrentColor();
        if (selectedColorPreview != null) selectedColorPreview.color = color;
        hexInput?.SetTextWithoutNotify("#" + ColorUtility.ToHtmlStringRGBA(color));
        if (saturationValue != null) saturationValue.text = $"SATURATION  {Mathf.RoundToInt(saturation * 100f)}%";
        if (brightnessValue != null) brightnessValue.text = $"BRIGHTNESS  {Mathf.RoundToInt(brightness * 100f)}%";
        if (alphaValue != null) alphaValue.text = $"{Mathf.RoundToInt(alpha * 100f)}%";
    }

    private Color CurrentColor()
    {
        Color color = Color.HSVToRGB(hue, saturation, brightness);
        color.a = alpha;
        return color;
    }

    private void Apply()
    {
        Action<Color> callback = onApplied;
        Color color = CurrentColor();
        CloseWithoutApply();
        callback?.Invoke(color);
    }

    private void OnDestroy()
    {
        if (colorPlane != null) colorPlane.SelectionChanged -= OnPlaneChanged;
        if (hueRing != null) hueRing.HueChanged -= OnHueChanged;
        alphaSlider?.onValueChanged.RemoveListener(OnAlphaChanged);
        hexInput?.onEndEdit.RemoveListener(OnHexChanged);
        applyButton?.onClick.RemoveListener(Apply);
        cancelButton?.onClick.RemoveListener(CloseWithoutApply);
    }
}
