using UnityEngine;

public class FPS_Display : MonoBehaviour
{
    private float fps;
    float timer=0.2f;
    public TMPro.TextMeshProUGUI fps_counter_text;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            GetFps();
            timer = 0.2f;
        }
        
    }
    
    void GetFps()
    {
        fps = (int)(1f / Time.unscaledDeltaTime);
        fps_counter_text.text = fps.ToString();
    }

}
