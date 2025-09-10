using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelCube : MonoBehaviour
{
    public bool neighborLeft;
    public bool neighborRight;
    public bool neighborTop;
    public bool neighborBottom;
    public bool neighborFront;
    public bool neighborBack;
    public Material material;

    void Start()
    {

        GetComponent<MeshRenderer>().material = material;
        GenerateCube();
        CastRays();
    }

    void CastRays()
    {
        Vector3 origin = transform.position;
        float distance = 5f;

        RaycastHit hit;

        // Cima
        if (Physics.Raycast(origin, Vector3.up, out hit, distance) && Physics.Raycast(origin, Vector3.down, out hit, distance) && Physics.Raycast(origin, Vector3.right, out hit, distance) && Physics.Raycast(origin, Vector3.left, out hit, distance) && Physics.Raycast(origin, Vector3.forward, out hit, distance) && Physics.Raycast(origin, Vector3.back, out hit, distance))
        {
            Debug.Log("Acertou em cima: " + hit.collider.name);

            gameObject.SetActive(false);


        }

    }


    public string hexColor = "FFFFFF";

    private void OnDrawGizmos()
    {
        Color c;
        if (!ColorUtility.TryParseHtmlString("#" + hexColor, out c))
            c = Color.white;

        Gizmos.color = c;
        Gizmos.DrawCube(transform.position, Vector3.one); // mostra um cubinho 1x1
    }

    void GenerateCube()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Faces
        if (!neighborRight) AddFace(vertices, triangles, uvs, Vector3.right);   // +X
        if (!neighborLeft) AddFace(vertices, triangles, uvs, Vector3.left);    // -X
        if (!neighborTop) AddFace(vertices, triangles, uvs, Vector3.up);      // +Y
        if (!neighborBottom) AddFace(vertices, triangles, uvs, Vector3.down);    // -Y
        if (!neighborFront) AddFace(vertices, triangles, uvs, Vector3.forward); // +Z
        if (!neighborBack) AddFace(vertices, triangles, uvs, Vector3.back);    // -Z

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    void AddFace(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 normal)
    {
        int startIndex = vertices.Count;

        // Define vértices da face dependendo da normal
        Vector3 up = Vector3.zero, right = Vector3.zero;

        if (normal == Vector3.up || normal == Vector3.down)
        {
            right = Vector3.right;
            up = Vector3.forward;
        }
        else if (normal == Vector3.right || normal == Vector3.left)
        {
            right = Vector3.forward;
            up = Vector3.up;
        }
        else if (normal == Vector3.forward || normal == Vector3.back)
        {
            right = Vector3.right;
            up = Vector3.up;
        }

        if (normal == Vector3.left || normal == Vector3.down || normal == Vector3.back)
        {
            right = -right;
            up = -up;
        }

        // Tamanho do cubo (1x1x1)
        Vector3 center = normal * 0.5f;
        vertices.Add(center - right * 0.5f - up * 0.5f);
        vertices.Add(center - right * 0.5f + up * 0.5f);
        vertices.Add(center + right * 0.5f + up * 0.5f);
        vertices.Add(center + right * 0.5f - up * 0.5f);

        // Triângulos (duas faces)
        triangles.Add(startIndex);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 3);

        // UV simples
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0));
    }
}
