using Unity.VisualScripting;
using UnityEngine;

public class LandingGear : MonoBehaviour
{
    public char retraction_rotation;
    public float rotation;
    [SerializeField] private Jet jet;
    private Quaternion original_rotation;
    [SerializeField] private Collider wheel_collider;
    [SerializeField] private LayerMask targetLayer;

    void Start()
    {
        original_rotation = transform.localRotation;
        //jet = GetComponentInParent<Jet>();
        //Return();
    }

    void Update()
    {

        if (jet.is_in_vehicle)
        {

            if (jet.retractLandingGear)
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

    public void Retract()
    {
        if (retraction_rotation == 'x')
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation,
            new Quaternion(rotation, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w),
            Time.deltaTime * 0.1f
            );

            if (transform.localRotation == new Quaternion(rotation, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w)) jet.isNearGround = false;
        }
        else if (retraction_rotation == 'y')
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation,
            new Quaternion(transform.localRotation.x, rotation, transform.localRotation.z, transform.localRotation.w),
            Time.deltaTime * 0.1f
            );

            if (transform.localRotation == new Quaternion(transform.localRotation.x, rotation, transform.localRotation.z, transform.localRotation.w)) jet.isNearGround = false;
        }
        else
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation,
            new Quaternion(transform.localRotation.x, transform.localRotation.y, rotation, transform.localRotation.w),
            Time.deltaTime * 0.1f
            );

            if (transform.localRotation == new Quaternion(transform.localRotation.x, transform.localRotation.y, rotation, transform.localRotation.w)) jet.isNearGround = false;
        }

        jet.isWheelTouchingGround = false;

    }

    public void Return()
    {
        
        transform.localRotation = Quaternion.Lerp(transform.localRotation, original_rotation, 7 * Time.deltaTime);
        if (transform.localRotation == original_rotation) jet.isNearGround = true;
    }

    void OnCollisionStay(Collision collision)
    {

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            jet.isWheelTouchingGround = true;
        }

    }


}
