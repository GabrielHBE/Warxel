using System;
using UnityEngine;

public class MainCannon : MonoBehaviour
{
    private Jet jet;
    private JetProperties jetProperties;

    float rotation_value;

    void Start()
    {
        jet = GetComponentInParent<Jet>();
        jetProperties = GetComponentInParent<JetProperties>();
    }

    void Update()
    {   
        if(!jet.is_in_jet) return;

        if (Input.GetKey(jet.shoot_key))
        {
            rotation_value += 0.01f;
        }
        else
        {
            rotation_value -= 0.01f;
        }

        rotation_value = Math.Clamp(rotation_value, 0, 1);
        Rotate();

    }

    void Rotate()
    {
        transform.Rotate(Vector3.left * jetProperties.fire_rate * Time.deltaTime * rotation_value);
    }
}
