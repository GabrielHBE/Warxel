using UnityEngine;

public class Mod_DestroyBelowTerrain : MonoBehaviour
{
    public Terrain terrain;          // Referência ao Terrain
    public float maxBelowDistance = 10f; // Distância máxima abaixo do terreno antes de destruir

    void Update()
    {
        if (terrain == null) return;

        // Posição atual do objeto
        Vector3 pos = transform.position;

        // Altura do terreno no ponto (x, z)
        float terrainHeight = terrain.SampleHeight(pos) + terrain.GetPosition().y;

        // Verifica se o objeto está muito abaixo
        if (pos.y < terrainHeight - maxBelowDistance)
        {
            Destroy(gameObject);
        }
    }
}
