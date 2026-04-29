using UnityEngine;

public class HelicopterMissileManager : MonoBehaviour
{
    [SerializeField] private Vehicle helicopter;
    [SerializeField] private HelicopterPilotHUD helicopterPilotHUD;
    public MissileController main_missile;
    public MissileController secondary_missile;

    public void SetCamera(Camera camera)
    {
        TowMissileController tow = secondary_missile.GetComponent<TowMissileController>();
        if (tow != null)
        {
            tow.camera_transform = camera.transform;
            return;
        } 

        TvMissileController tv_missile = secondary_missile.GetComponent<TvMissileController>();
        if (tv_missile != null)
        {
           tv_missile.original_camera = camera;
           return; 
        } 
    }

    
    
}
