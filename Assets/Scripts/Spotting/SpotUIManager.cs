using UnityEngine;
using System.Collections.Generic;

public class SpotUIManager : InMatchClientSingleton<SpotUIManager>
{
    public GameObject spotMarkerPrefab;
    public Transform spotCanvasContainer;
    private List<SpotMarker> markerPool = new List<SpotMarker>();

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