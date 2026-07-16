using UnityEngine;

public class VehicleStartEngineUI : MonoBehaviour
{
    private Vehicle vehicle;

    [SerializeField] private GameObject start_engine_indicator;

    void Start()
    {
        vehicle = GetComponentInParent<Vehicle>();
        if (vehicle == null)
        {
            Debug.LogError("VehicleStartEngineUI: Não foi possível encontrar um componente Vehicle no objeto pai.");
            return;
        }

        // Inicialmente, o indicador de start engine está oculto
        HideStartEnginePrompt();
    }

    void Update()
    {
        if (vehicle.startEngine.Value)
        {
            HideStartEnginePrompt();
        }
        else
        {
            ShowStartEnginePrompt();
        }
    }

    public void ShowStartEnginePrompt()
    {
        if (start_engine_indicator != null)
        {
            start_engine_indicator.SetActive(true);
        }
    }

    public void HideStartEnginePrompt()
    {
        if (start_engine_indicator != null)
        {
            start_engine_indicator.SetActive(false);
        }
    }

}
