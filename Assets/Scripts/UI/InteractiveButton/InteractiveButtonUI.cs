using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractiveButtonUI : MonoBehaviour
{
    public static InteractiveButtonUI Instance { get; private set; }

    [SerializeField] private InteractiveButtonUIIndicator InteractiveButtonIndicator;
    private float maxViewDistance = 10f;
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

    private List<InteractiveButtonUIElement> interactiveButtonElements = new List<InteractiveButtonUIElement>();

    void Awake()
    {
        Instance = this;
        GameObject[] interactiveButtonsGo = GameObject.FindGameObjectsWithTag("InteractiveButton");

        foreach (GameObject i in interactiveButtonsGo)
        {
            InteractiveButton interactiveButton = i.GetComponent<InteractiveButton>();
            if (interactiveButton != null)
            {
                CreateFlagUI(interactiveButton);
            }
        }
    }

    private void CreateFlagUI(InteractiveButton interactiveButton)
    {

        InteractiveButtonUIIndicator uiObject = Instantiate(InteractiveButtonIndicator);

        uiObject.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = uiObject.GetComponent<RectTransform>();

        interactiveButtonElements.Add(new InteractiveButtonUIElement
        (
            uiObject.gameObject,
            interactiveButton,
            rectTransform,
            interactiveButton.GetInteractionButtonText()
        ));

        uiObject.SetInteractiDesciption(interactiveButton.GetInteractionButtonText());
    }

    void LateUpdate()
    {
        // Garante que temos uma câmera para calcular a posição na tela
        if (Camera.main == null) return;

        // Guarda a posição da câmera em uma variável para evitar chamar transform.position várias vezes no loop
        Vector3 camPos = Camera.main.transform.position;

        foreach (var element in interactiveButtonElements)
        {
            if (element.InteractiveButton != null)
            {
                Vector3 flagWorldPos = element.InteractiveButton.transform.position;

                // <-- NOVA LÓGICA DE DISTÂNCIA -->
                // Calcula a distância entre a câmera e a bandeira
                float distanceToCamera = Vector3.Distance(camPos, flagWorldPos);

                // Se a distância for maior que o limite definido, esconde a UI e pula para a próxima
                if (distanceToCamera > maxViewDistance || PlayerController.Instance == null || SettingsHUD.Instance.is_menu_settings_active)
                {
                    element.gameObject.SetActive(false);
                    continue;
                }
                // Converte a posição 3D (InWorldUIPosition) para espaço 2D da tela
                Vector3 screenPos = Camera.main.WorldToScreenPoint(flagWorldPos);

                // Se o Z for maior que 0, a bandeira está na frente da câmera (visível)
                if (screenPos.z > 0)
                {
                    element.gameObject.SetActive(true);
                    element.Rect.position = screenPos;
                }
                else
                {
                    // Esconde a imagem se o jogador estiver olhando para a direção oposta
                    element.gameObject.SetActive(false);
                }
            }
        }
    }
}
