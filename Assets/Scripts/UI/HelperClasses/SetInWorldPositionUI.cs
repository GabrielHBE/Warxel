using UnityEngine;
using System.Collections.Generic;

public class SetInWorldPositionUI : MonoBehaviour
{
    [System.Serializable]
    public struct UIWorldMapping
    {
        public RectTransform uiElement;
        public Transform worldPos;

        public UIWorldMapping(RectTransform uiElement, Transform worldPos)
        {
            this.uiElement = uiElement;
            this.worldPos = worldPos;
        }
    }

    [SerializeField] private Updatemehtod updateMethod;

    [SerializeField] private bool disableWithinDistance;
    [SerializeField] private float viewDistance;

    [SerializeField] private List<UIWorldMapping> elements = new List<UIWorldMapping>();

    private enum Updatemehtod
    {
        Update,
        LateUpdate
    }

    private void Update()
    {
        if(updateMethod == Updatemehtod.Update) MovePosition();
    }
    
    private void LateUpdate()
    {
        if(updateMethod == Updatemehtod.LateUpdate) MovePosition();
    }

    private void MovePosition()
    {
        if(Camera.main == null)
        {
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                UIWorldMapping map = elements[i];
                if (map.uiElement.gameObject.activeSelf) map.uiElement.gameObject.SetActive(false);
            }
        
            return;
        } 

        for (int i = elements.Count - 1; i >= 0; i--)
        {
            UIWorldMapping map = elements[i];

            // Se o elemento de UI ou a posição de mundo sumirem/forem destruídos, remove da lista
            if (map.uiElement == null || map.worldPos == null)
            {
                elements.RemoveAt(i);
                continue;
            }

            float distanceToCamera = Vector3.Distance(Camera.main.transform.position, map.worldPos.position);

            if ((distanceToCamera > viewDistance && disableWithinDistance)|| SettingsHUD.Instance.is_menu_settings_active)
            {
                
                if (map.uiElement.gameObject.activeSelf) map.uiElement.gameObject.SetActive(false);
                continue;
            }

            Vector3 screenPoint = Camera.main.WorldToScreenPoint(map.worldPos.position);

            if (screenPoint.z > 0)
            {
                if (!map.uiElement.gameObject.activeSelf) map.uiElement.gameObject.SetActive(true);
                map.uiElement.position = screenPoint;
            }
            else
            {
                if (map.uiElement.gameObject.activeSelf) map.uiElement.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Permite que outros scripts (como FlagsUI e InteractiveButtonUI) registrem elementos gerados via código.
    /// </summary>
    public void AddElement(RectTransform uiElement, Transform posicaoMundo)
    {
        if (uiElement == null || posicaoMundo == null) return;
        elements.Add(new UIWorldMapping(uiElement, posicaoMundo));
    }
}