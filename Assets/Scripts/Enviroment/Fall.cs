using Unity.VisualScripting;
using UnityEngine;

public class Fall : MonoBehaviour
{
    RaycastHit hit;
    float distance_max = 0.01f;
    void Update()
    {

        if (Physics.Raycast(transform.position, Vector3.down, out hit, distance_max))
        {
            Debug.Log("Objeto abaixo: " + hit.collider.gameObject.name);
        }
        else
        {
            gameObject.AddComponent<Rigidbody>();
        }
    }

}
