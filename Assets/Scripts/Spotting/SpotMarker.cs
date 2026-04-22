using UnityEngine;
using UnityEngine.UI;

public class SpotMarker : MonoBehaviour
{
    public RectTransform uiElement;
    public float spotDuration = 5f; // Tempo que o spot dura na tela
    
    private Transform targetSpotPosition;
    private float timer;

    public void Initialize(Transform target)
    {
        targetSpotPosition = target;
        timer = spotDuration;
        uiElement.gameObject.SetActive(true);
    }

    void LateUpdate()
    {
        if (targetSpotPosition == null)
        {
            uiElement.gameObject.SetActive(false);
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            uiElement.gameObject.SetActive(false);
            return;
        }

        // Converte a posição 3D para 2D na tela
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(targetSpotPosition.position);

        if (screenPosition.z > 0)
        {
            if (!uiElement.gameObject.activeSelf) uiElement.gameObject.SetActive(true);
            uiElement.position = screenPosition;
        }
        else
        {
            if (uiElement.gameObject.activeSelf) uiElement.gameObject.SetActive(false);
        }
    }
}