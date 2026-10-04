using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VehicleStartEngineUI : MonoBehaviour
{
    private static VehicleStartEngineUI instance;

    private Vehicle vehicle;
    private GameObject indicator;
    private TextMeshProUGUI promptText;

    public static void ShowFor(Vehicle target)
    {
        if (target == null) return;

        if (instance == null)
        {
            GameObject host = new GameObject(nameof(VehicleStartEngineUI));
            instance = host.AddComponent<VehicleStartEngineUI>();
            instance.CreateIndicator();
        }

        instance.vehicle = target;
        instance.Refresh();
    }

    public static void HideFor(Vehicle target)
    {
        if (instance == null || instance.vehicle != target) return;

        instance.vehicle = null;
        instance.Refresh();
    }

    private void Update() => Refresh();

    private void Refresh()
    {
        Settings settings = Settings.Instance;
        bool visible = vehicle != null && vehicle.isInVehicle &&
                       vehicle.currentSeat != null &&
                       vehicle.currentSeat.seatType == VehicleSeats.SeatType.Pilot &&
                       !vehicle.startEngine.Value && !vehicle.vehicle_destroyed.Value &&
                       settings != null && settings._keybinds != null;

        if (indicator.activeSelf != visible) indicator.SetActive(visible);
        if (!visible) return;

        string message = $"[{settings._keybinds.VEHICLE_startEngineKey}] Start Engine";
        if (promptText.text != message) promptText.text = message;
    }

    private void CreateIndicator()
    {
        GameObject canvasObject = new GameObject("Start Engine Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panelObject = new GameObject("Start Engine Indicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.25f);
        panel.anchorMax = panel.anchorMin;
        panel.sizeDelta = new Vector2(400f, 64f);
        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.07f, 0.09f, 0.8f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Start Engine Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 4f);
        textRect.offsetMax = new Vector2(-12f, -4f);

        promptText = textObject.GetComponent<TextMeshProUGUI>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 26f;
        promptText.color = Color.white;
        promptText.raycastTarget = false;

        indicator = canvasObject;
        indicator.SetActive(false);
    }
}
