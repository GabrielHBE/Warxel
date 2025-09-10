using UnityEngine;

public class RotateLeanFlaps : MonoBehaviour
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

        Rotate();

    }

    void Rotate()
    {
        float rotationAmount = -jet.lean_value * 600 * Time.deltaTime;

        float current_rotation = transform.localRotation.y;
        
        if (jet.lean_value != 0)
        {
            if (current_rotation > -0.15 && current_rotation < 0.15)
            {
                transform.Rotate(0, rotationAmount, 0, Space.Self);
            }

        }
        else
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, original_flap_rotation, Time.deltaTime * 5);
        }


    }
}
