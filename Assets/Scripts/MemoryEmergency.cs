using UnityEngine;

public class MemoryEmergency : MonoBehaviour
{
    [SerializeField] private KeyCode cleanupKey = KeyCode.F7;
    
    void Update()
    {
        if(Input.GetKeyDown(cleanupKey))
        {
            EmergencyCleanup();
        }
    }
    
    void EmergencyCleanup()
    {
        // Forçar garbage collection
        System.GC.Collect();
        
        // Descarrregar assets não utilizados
        Resources.UnloadUnusedAssets();
        
        Debug.Log("Emergency cleanup performed!");
    }
}
