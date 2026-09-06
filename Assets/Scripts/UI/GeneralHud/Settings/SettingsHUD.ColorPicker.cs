using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class SettingsHUD
{
    private void RefreshColorPreviews()
    {
        RefreshColorPreview(bodyShotColorInput, bodyShotColorPreview);
        RefreshColorPreview(headShotColorInput, headShotColorPreview);
        RefreshColorPreview(vehicleMarkerColorInput, vehicleMarkerColorPreview);
        RefreshColorPreview(sightReticleColorInput, sightReticleColorPreview);
        RefreshColorPreview(enemyColorInput, enemyColorPreview);
        RefreshColorPreview(allyColorInput, allyColorPreview);
        RefreshColorPreview(squadColorInput, squadColorPreview);
        RefreshColorPreview(neutralColorInput, neutralColorPreview);
    }

    private void OpenColorPicker(TMP_InputField input, Image preview, Action<Color> onSelected)
    {
        if (colorPickerPopup == null) return;

        Color current = preview != null ? preview.color : Color.white;
        if (input != null && TryParseHtmlColor(input.text, out Color parsed))
        {
            current = parsed;
        }

        colorPickerPopup.Open(current, onSelected);
    }

    private static void ApplyPickedColor(TMP_InputField input, Image preview, Color color, Action<Color> apply)
    {
        apply?.Invoke(color);
        input?.SetTextWithoutNotify("#" + ColorUtility.ToHtmlStringRGBA(color));
        if (preview != null) preview.color = color;
    }

    private static void ApplyHexColor(string html, TMP_InputField input, Image preview, Action<string> apply)
    {
        apply?.Invoke(html);
        RefreshColorPreview(input, preview);
    }

    private static void RefreshColorPreview(TMP_InputField input, Image preview)
    {
        if (input == null || preview == null || !TryParseHtmlColor(input.text, out Color color)) return;
        preview.color = color;
    }

    private static bool TryParseHtmlColor(string html, out Color color)
    {
        string normalized = string.IsNullOrWhiteSpace(html) ? string.Empty : html.Trim();
        if (!normalized.StartsWith("#")) normalized = "#" + normalized;
        return ColorUtility.TryParseHtmlString(normalized, out color);
    }
}
