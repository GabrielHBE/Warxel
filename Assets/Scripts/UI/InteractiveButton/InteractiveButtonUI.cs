using UnityEngine;

public class InteractiveButtonUI : InMatchClientSingleton<InteractiveButtonUI>
{
    [SerializeField] private SetInWorldPositionUI setInWorldPositionUI;
    [SerializeField] private InteractiveButtonUIIndicator InteractiveButtonIndicator;
    [SerializeField] private Canvas canvas;

    private class InteractiveButtonUIElement
    {
        public GameObject gameObject;
        public InteractiveButton InteractiveButton;
        public RectTransform Rect;
        public string Text;

        public InteractiveButtonUIElement(GameObject gameObject, InteractiveButton InteractiveButton, RectTransform Rect, string Text)
        {
            this.gameObject = gameObject;
            this.InteractiveButton = InteractiveButton;
            this.Rect = Rect;
            this.Text = Text;
        }
    }

    public void CreateButtonUI(InteractiveButton interactiveButton)
    {

        InteractiveButtonUIIndicator uiObject = Instantiate(InteractiveButtonIndicator);

        uiObject.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = uiObject.GetComponent<RectTransform>();

        InteractiveButtonUIElement element = new InteractiveButtonUIElement
        (
            uiObject.gameObject,
            interactiveButton,
            rectTransform,
            interactiveButton.GetInteractionButtonText()
        );

        uiObject.SetInteractiDesciption(interactiveButton.GetInteractionButtonText());

        setInWorldPositionUI.AddElement(element.Rect, element.InteractiveButton.transform, false);
    }

}
