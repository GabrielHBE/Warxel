using System;
using UnityEngine;

public class JetCameraShake : MonoBehaviour
{
    private Jet jet;
    float magnitude;
    Vector3 original_pos;

    void Start()
    {
        
        jet = GetComponentInParent<Jet>();
        original_pos = transform.localPosition;
    }

    void Update()
    {
        if (jet.is_in_jet && (jet.mouseY != 0 && jet.currentSpeed>=100)  && jet.current_camera==1) 
        {
            magnitude += 0.01f;
        }   
        else
        {
            magnitude -= 0.04f;

        }
        magnitude = Math.Clamp(magnitude, 0, 2);

        Shake();
    }

    void Shake()
    {
        float x = UnityEngine.Random.Range(-0.1f, 0.1f) * magnitude;
        float y = UnityEngine.Random.Range(-0.1f, 0.1f) * magnitude;

        transform.localPosition = new Vector3(original_pos.x + x, original_pos.y + y, original_pos.z);
    }
}
