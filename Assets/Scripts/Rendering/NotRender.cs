using UnityEngine;

public class NotRender : MonoBehaviour
{

    void Start()
    {
        Vector3 origin = transform.position;
        float distance = 5f;

        RaycastHit hit;

        // Cima
        if (Physics.Raycast(origin, Vector3.up, out hit, distance) && Physics.Raycast(origin, Vector3.down, out hit, distance) && Physics.Raycast(origin, Vector3.right, out hit, distance) && Physics.Raycast(origin, Vector3.left, out hit, distance) && Physics.Raycast(origin, Vector3.forward, out hit, distance) && Physics.Raycast(origin, Vector3.back, out hit, distance))
        {
            
            gameObject.SetActive(false);

        }
    }

}
