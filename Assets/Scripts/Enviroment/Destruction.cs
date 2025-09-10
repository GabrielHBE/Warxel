using UnityEngine;
using System.Collections.Generic;

public class Destruction : MonoBehaviour
{
    public int max_depth = 1;
    public int current_depth = 0;
    public float explosion_force = 100;
    public float explosion_radious = 2f;
    public float explosion_upward_modifier = 0.5f;
    private bool id_divided = false;
    private List<GameObject> children = new List<GameObject>();
    private Material parent_material;

    void Start()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            parent_material = renderer.sharedMaterial;
        }

        if (current_depth > 0)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                gameObject.AddComponent<Rigidbody>();
            }
        }
    }

    private void SubDivide()
    {
        if (id_divided || current_depth == max_depth)
        {
            return;
        }

        id_divided = true;
        int vox_layer = LayerMask.NameToLayer("Ground");

        if (vox_layer == -1)
        {
            Debug.LogError("Erro no layer");
            return;
        }

        Vector3 child_scale = transform.localScale / 4;

        Vector3[] childOffsets = new Vector3[]
        {
            //new Vector3(-0.5f, -0.5f, -0.5f), // Bottom-Left-Front
            //new Vector3( 0.5f, -0.5f, -0.5f), // Bottom-Right-Front
            //new Vector3(-0.5f,  0.5f, -0.5f), // Top-Left-Front
            //new Vector3( 0.5f,  0.5f, -0.5f), // Top-Right-Front
            //new Vector3(-0.5f, -0.5f,  0.5f), // Bottom-Left-Back
            //new Vector3( 0.5f, -0.5f,  0.5f), // Bottom-Right-Back
            //new Vector3(-0.5f,  0.5f,  0.5f), // Top-Left-Back
            new Vector3( 0.5f,  0.5f,  0.5f)  // Top-Right-Back
        };

        for (int i = 0; i < childOffsets.Length; i++)
        {
            GameObject child_object = GameObject.CreatePrimitive(PrimitiveType.Cube);

            child_object.transform.position = transform.position + childOffsets[i] * child_scale.x;
            child_object.transform.localScale = transform.localScale / 10;

            MeshRenderer child_renderer = child_object.GetComponent<MeshRenderer>();

            child_renderer.material = parent_material;


            child_object.layer = vox_layer;

            // Adiciona componente Rigidbody apenas ao objeto filho
            Rigidbody childRb = child_object.AddComponent<Rigidbody>();

            // Adiciona o script do nó da octree e configura os parâmetros
            Destruction childNode = child_object.AddComponent<Destruction>();
            childNode.max_depth = max_depth;                      // Profundidade máxima da árvore
            childNode.current_depth = current_depth + 1;              // Profundidade atual (filho)
            childNode.explosion_force = explosion_force;                // Força da explosão aplicada
            childNode.explosion_radious = explosion_radious;               // Raio de alcance da explosão
            childNode.explosion_upward_modifier = explosion_upward_modifier;       // Modificador de explosão para direção vertical

            // Salva a referência ao objeto filho na lista
            children.Add(child_object);

            childRb.AddExplosionForce(explosion_force, transform.position, explosion_radious, explosion_upward_modifier);

            child_object.AddComponent<DestroyAfterAll>();


        }

        Destroy(gameObject);

    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("oi");
        if (collision.gameObject.CompareTag("Projectile"))
        {

            if (current_depth <= max_depth)
            {
                SubDivide();
            }
            else
            {
                Destroy(gameObject);
            }

            Destroy(collision.gameObject);
        }
    }

    public void DestroyNode()
    {
        foreach (var child in children)
        {
            if (child != null)
            {
                child.GetComponent<Destruction>().DestroyNode();
            }
        }

        Destroy(gameObject);
    }



}
