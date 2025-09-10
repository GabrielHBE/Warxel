using UnityEngine;

public class Laser : MonoBehaviour
{
    public float distance = 100f;
    private LineRenderer line;

    [SerializeField] private Color color;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        if (line == null)
        {
            line = gameObject.AddComponent<LineRenderer>();
        }

        line.positionCount = 2;
        line.startWidth = 0.005f;
        line.endWidth = 0.002f;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.color = color;
    }
    void LateUpdate()
    {
        UpdateLaser();
    }

    void UpdateLaser()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        Vector3 endPosition;
        if (Physics.Raycast(ray, out hit, distance))
        {
            endPosition = hit.point;
        }
        else
        {
            endPosition = ray.origin + ray.direction * distance;
        }

        line.SetPosition(0, transform.position);
        line.SetPosition(1, endPosition);
    }

}
