using UnityEngine;

public class RotateRotationFlaps : MonoBehaviour
{
    public bool is_right;
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
        float rotationAmount = -jet.mouseX  * Time.deltaTime;

        float current_rotation = transform.localRotation.z;

        if (is_right)
        {
            if (jet.mouseX != 0)
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
        else
        {
            if (jet.mouseX != 0)
            {
                if (current_rotation > -0.15 && current_rotation < 0.15)
                {
                    transform.Rotate(0, 0f, -rotationAmount, Space.Self);
                }
            }
            else
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, original_flap_rotation, Time.deltaTime * 5);
            }
        }

    }

}
