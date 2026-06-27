using UnityEngine;

public class FpsDisplay : PersistentLocalSingleton<FpsDisplay>
{
    //public static FpsDisplay Instance {get; private set;}
    private float fps;
    float timer = 0.2f;
    public TMPro.TextMeshProUGUI fps_counter_text;

    void Update()
    {
        if (!Settings.Instance._gameplay.show_fps)
        {
            fps_counter_text.text = "";
            return;
        }
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
        fps_counter_text.text = "FPS: " + fps.ToString();
    }

}
