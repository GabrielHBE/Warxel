using UnityEngine;

public class MainMenuSingletonManager : MonoBehaviour
{
    [SerializeField] private GameObject accountManager;
    [SerializeField] private GameObject generalHUD;
    [SerializeField] private GameObject settings;

    void Start()
    {
        SpawnClientObjects();
    }

    private void SpawnClientObjects()
    {
        if (settings != null)
        {
            Instantiate(settings); 
            print("Settings Initialized");
        }
        if (accountManager != null)
        {
            Instantiate(accountManager);
        }
        
        if (generalHUD != null)
        {
            Instantiate(generalHUD);
        }
            
    }
}
