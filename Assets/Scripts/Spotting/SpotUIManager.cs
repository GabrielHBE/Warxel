using UnityEngine;
using System.Collections.Generic;

public class SpotUIManager : MonoBehaviour
{
    public static SpotUIManager Instance { get; private set; }

    public GameObject spotMarkerPrefab;
    public Transform spotCanvasContainer;
    
    // Um simples pool para reutilizar os marcadores
    private List<SpotMarker> markerPool = new List<SpotMarker>();

    private void Awake()
    {
        Instance = this;
    }

    public void ShowSpot(Transform spotPosition)
    {
        // Tenta achar um marcador inativo no pool
        foreach (SpotMarker marker in markerPool)
        {
            if (!marker.uiElement.gameObject.activeSelf)
            {
                marker.Initialize(spotPosition);
                return;
            }
        }

        // Se não achar, cria um novo
        GameObject newMarkerObj = Instantiate(spotMarkerPrefab, spotCanvasContainer);
        SpotMarker newMarker = newMarkerObj.GetComponent<SpotMarker>();
        markerPool.Add(newMarker);
        newMarker.Initialize(spotPosition);
    }
}