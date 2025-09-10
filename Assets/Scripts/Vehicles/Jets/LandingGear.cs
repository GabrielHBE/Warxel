using System.Runtime.InteropServices;
using UnityEngine;

public class LandingGear : MonoBehaviour
{
    public char retraction_rotation;
    public float rotation;
    private Jet jet;
    private Quaternion original_rotation;
    private Collider wheel_collider;

    void Start()
    {
        original_rotation = transform.localRotation;
        jet = GetComponentInParent<Jet>();

        wheel_collider = GetComponent<Collider>();
    }

    void Update()
    {
        if (jet.is_in_jet)
        {
            if (jet.currentSpeed >= 110)
            {
                wheel_collider.enabled = false;
                Retract();
            }
            else
            {
                wheel_collider.enabled = true;
                Return();
            }
        }


    }


    void Retract()
    {
        if (retraction_rotation == 'x')
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation,
            new Quaternion(rotation, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w),
            Time.deltaTime * 0.1f
            );
        }
        else if (retraction_rotation == 'y')
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation,
            new Quaternion(transform.localRotation.x, rotation, transform.localRotation.z, transform.localRotation.w),
            Time.deltaTime * 0.1f
            );
        }
        else
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation,
            new Quaternion(transform.localRotation.x, transform.localRotation.y, rotation, transform.localRotation.w),
            Time.deltaTime * 0.1f
            );
        }

    }

    void Return()
    {
        transform.localRotation = Quaternion.Lerp(transform.localRotation, original_rotation, 5 * Time.deltaTime);
    }

}
