using System;
using UnityEngine;

public class RotatePitchFlaps : MonoBehaviour
{

    private Quaternion original_flap_rotation;
    private Jet jet;

    void Start()
    {
        jet = GetComponentInParent<Jet>();

        original_flap_rotation = transform.localRotation;
    }

    void Update()
    {
        if (jet.is_in_jet)
        {
            Rotate();
        }
        
    }

    void Rotate()
    {
        float rotationAmount = jet.mouseY * 100 * Time.deltaTime;

        float current_rotation = transform.localRotation.z;
        
        if (jet.mouseY != 0)
        {
            if (current_rotation > -0.15 && current_rotation < 0.15)
            {
                transform.Rotate(0, 0f, rotationAmount, Space.Self);
            }

        }
        else
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, original_flap_rotation, Time.deltaTime * 5);
        }


    }
}
