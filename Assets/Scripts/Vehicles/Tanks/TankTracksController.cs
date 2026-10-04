using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Tank))]
public class TankTracksController : MonoBehaviour
{
    [SerializeField] private Shader trackShader;
    [SerializeField, Min(0f)] private float teleportDistance = 5f;

    private readonly List<TrackSide> trackSides = new List<TrackSide>();
    private Tank tank;

    private sealed class TrackSide
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public Material[] animatedMaterials;
        public WheelCollider[] wheels;
        public float perimeter;
        public float meshUnitsPerWorldMeter;
        public Vector3 previousPosition;
        public float phase;
        public bool hasPreviousPosition;
    }

    private void Awake()
    {
        tank = GetComponent<Tank>();
        if (trackShader == null)
            trackShader = Shader.Find("Warxel/TankTrackMotion");

        if (trackShader == null)
        {
            Debug.LogError("TankTracksController: shader Warxel/TankTrackMotion não encontrado.", this);
            return;
        }

        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;

            string meshName = filter.name + " " + filter.sharedMesh.name;
            WheelCollider[] wheels = null;
            if (meshName.IndexOf("Esteira_E", StringComparison.OrdinalIgnoreCase) >= 0)
                wheels = tank.leftWheels;
            else if (meshName.IndexOf("Esteira_D", StringComparison.OrdinalIgnoreCase) >= 0)
                wheels = tank.rightWheels;

            if (wheels == null) continue;

            Renderer trackRenderer = filter.GetComponent<Renderer>();
            if (trackRenderer == null) continue;

            Bounds bounds = filter.sharedMesh.bounds;
            float radius = bounds.extents.y;
            float straightHalfLength = Mathf.Max(0f, bounds.extents.z - radius);
            if (radius <= 0.001f) continue;

            Material[] originals = trackRenderer.sharedMaterials;
            Material[] animated = new Material[originals.Length];
            Vector4 trackBounds = new Vector4(bounds.center.y, bounds.center.z, radius, straightHalfLength);
            for (int i = 0; i < originals.Length; i++)
            {
                Material source = originals[i];
                if (source == null) continue;

                Material material = new Material(trackShader) { name = source.name + " (Moving Track)" };
                Texture texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : null;
                string textureProperty = "_BaseMap";
                if (texture == null && source.HasProperty("_MainTex"))
                {
                    texture = source.GetTexture("_MainTex");
                    textureProperty = "_MainTex";
                }

                if (texture != null)
                {
                    material.SetTexture("_BaseMap", texture);
                    material.SetTextureScale("_BaseMap", source.GetTextureScale(textureProperty));
                    material.SetTextureOffset("_BaseMap", source.GetTextureOffset(textureProperty));
                }

                if (source.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                else if (source.HasProperty("_Color"))
                    material.SetColor("_BaseColor", source.GetColor("_Color"));

                material.SetVector("_TrackBounds", trackBounds);
                animated[i] = material;
            }

            float worldScale = filter.transform.TransformVector(Vector3.forward).magnitude;
            trackSides.Add(new TrackSide
            {
                renderer = trackRenderer,
                originalMaterials = originals,
                animatedMaterials = animated,
                wheels = wheels,
                perimeter = 4f * straightHalfLength + 2f * Mathf.PI * radius,
                meshUnitsPerWorldMeter = worldScale > 0.0001f ? 1f / worldScale : 1f
            });

            trackRenderer.sharedMaterials = animated;
        }

        if (trackSides.Count < 2)
            Debug.LogWarning($"TankTracksController encontrou {trackSides.Count} malha(s) de esteira; esperado: Esteira_E e Esteira_D.", this);
    }

    private void OnEnable()
    {
        foreach (TrackSide side in trackSides)
            side.hasPreviousPosition = false;
    }

    private void LateUpdate()
    {
        foreach (TrackSide side in trackSides)
        {
            Vector3 position = GetSidePosition(side);
            if (side.hasPreviousPosition)
            {
                Vector3 movement = position - side.previousPosition;
                if (movement.sqrMagnitude <= teleportDistance * teleportDistance)
                {
                    float worldTravel = Vector3.Dot(movement, transform.forward);
                    if (tank.IsOwner && tank.rb != null)
                    {
                        float physicsTravel = Vector3.Dot(tank.rb.GetPointVelocity(position), transform.forward) * Time.deltaTime;
                        if (Mathf.Abs(physicsTravel) > 0.0001f)
                            worldTravel = physicsTravel;
                    }

                    side.phase = Mathf.Repeat(side.phase + worldTravel * side.meshUnitsPerWorldMeter, side.perimeter);
                    foreach (Material material in side.animatedMaterials)
                        if (material != null) material.SetFloat("_TrackPhase", side.phase);
                }
            }

            side.previousPosition = position;
            side.hasPreviousPosition = true;
        }
    }

    private static Vector3 GetSidePosition(TrackSide side)
    {
        Vector3 position = Vector3.zero;
        int count = 0;
        foreach (WheelCollider wheel in side.wheels)
        {
            if (wheel == null) continue;
            position += wheel.transform.position;
            count++;
        }

        return count > 0 ? position / count : side.renderer.bounds.center;
    }

    private void OnDestroy()
    {
        foreach (TrackSide side in trackSides)
        {
            if (side.renderer != null) side.renderer.sharedMaterials = side.originalMaterials;
            foreach (Material material in side.animatedMaterials)
                if (material != null) Destroy(material);
        }
    }
}
