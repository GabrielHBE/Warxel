using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlagsUI : MonoBehaviour
{
    public static FlagsUI Instance { get; private set; }

    [SerializeField] private SetInWorldPositionUI setInWorldPositionUI;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector2 imageSize = new Vector2(50f, 50f); // Tamanho da imagem na UI

    private class FlagUIElement
    {
        public FlagCapture Flag;
        public Image UIImage;
        public RectTransform Rect;
    }

    private List<FlagUIElement> flagElements = new List<FlagUIElement>();

    void Awake()
    {
        Instance = this;
        GameObject[] flagsGo = GameObject.FindGameObjectsWithTag("CaptureFlag");

        foreach (GameObject f in flagsGo)
        {
            FlagCapture flagCapture = f.GetComponent<FlagCapture>();
            if (flagCapture != null)
            {
                CreateFlagUI(flagCapture);
            }
        }
    }

    private void CreateFlagUI(FlagCapture flag)
    {
        // 1. Cria um novo GameObject para a UI
        GameObject uiObject = new GameObject($"FlagUI_{flag.gameObject.name}");

        // 2. Define o Canvas como pai (false para manter transformações relativas)
        uiObject.transform.SetParent(canvas.transform, false);

        // 3. Adiciona o componente Image e o Sprite da FlagCapture
        Image imageComponent = uiObject.AddComponent<Image>();
        if (flag.UI_Image != null)
        {
            imageComponent.sprite = flag.UI_Image;
        }
        else
        {
            Debug.LogWarning($"A Flag {flag.name} não possui uma UI_Image definida!");
        }

        // 4. Configura o tamanho do RectTransform
        RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = imageSize;

        // 5. Adiciona à nossa lista para podermos atualizar a posição
        FlagUIElement element = new FlagUIElement
        {
            Flag = flag,
            UIImage = imageComponent,
            Rect = rectTransform
        };

        flagElements.Add(element);

        setInWorldPositionUI.AddElement(element.Rect, flag.InWorldUIPosition);
    }

    void LateUpdate()
    {

        foreach (var element in flagElements)
        {
            if (element.Flag != null && element.Flag.InWorldUIPosition != null)
            {
                ChangeColor(element);
                ChangeOpacity(element);

                /*
                Vector3 flagWorldPos = element.Flag.InWorldUIPosition.position;

                // <-- NOVA LÓGICA DE DISTÂNCIA -->
                // Calcula a distância entre a câmera e a bandeira
                float distanceToCamera = Vector3.Distance(camPos, flagWorldPos);

                // Se a distância for maior que o limite definido, esconde a UI e pula para a próxima
                if (distanceToCamera > maxViewDistance || PlayerController.Instance == null || SettingsHUD.Instance.is_menu_settings_active)
                {
                    element.UIImage.enabled = false;
                    continue;
                }
                

                // Converte a posição 3D (InWorldUIPosition) para espaço 2D da tela
                Vector3 screenPos = Camera.main.WorldToScreenPoint(flagWorldPos);

                // Se o Z for maior que 0, a bandeira está na frente da câmera (visível)
                if (screenPos.z > 0)
                {
                    element.UIImage.enabled = true;
                    element.Rect.position = screenPos;
                }
                else
                {
                    // Esconde a imagem se o jogador estiver olhando para a direção oposta
                    element.UIImage.enabled = false;
                }
                */
            }
        }
    }

    private void ChangeColor(FlagUIElement element)
    {
        if (AccountManager.Instance == null) return;

        if (AccountManager.Instance.faction == element.Flag.GetFactionInControl())
        {
            element.UIImage.color = Settings.Instance._gameplay.ally_color;
        }
        else if (element.Flag.GetFactionInControl() == FactionManager.Faction.Neutral)
        {
            element.UIImage.color = Settings.Instance._gameplay.neutral_color;
        }
        else
        {
            element.UIImage.color = Settings.Instance._gameplay.enemy_color;
        }
    }

    private void ChangeOpacity(FlagUIElement element)
    {
        if (AccountManager.Instance == null || PlayerController.Instance == null) return;

        if (AccountManager.Instance.faction == element.Flag.GetFactionInControl())
        {
            Color c = element.UIImage.color;
            c.a = PlayerController.Instance.playerProperties.is_aiming ? Settings.Instance._gameplay.ally_indicator_aim_opacity : Settings.Instance._gameplay.ally_indicator_opacity;
            element.UIImage.color = c;
        }
        else if (element.Flag.GetFactionInControl() == FactionManager.Faction.Neutral)
        {
            Color c = element.UIImage.color;
            c.a = PlayerController.Instance.playerProperties.is_aiming ? Settings.Instance._gameplay.neutral_indicator_aim_opacity : Settings.Instance._gameplay.neutral_indicator_opacity;
            element.UIImage.color = c;
        }
        else
        {
            Color c = element.UIImage.color;
            c.a = PlayerController.Instance.playerProperties.is_aiming ? Settings.Instance._gameplay.enemy_indicator_aim_opacity : Settings.Instance._gameplay.enemy_indicator_opacity;
            element.UIImage.color = c;
        }

    }
}