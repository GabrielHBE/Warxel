using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.VoxelObjects;

public class Mod_DestroyAfterAll : MonoBehaviour
{
    [SerializeField] private bool can_collapse;
    [SerializeField] private AudioSource collapsesound;
    [SerializeField] private GameObject fallParticles;
    [SerializeField] private float distance_step = 1f;
    [SerializeField] private float dmg_to_collapse = 100f;
    [SerializeField] private GameObject collapse_smoke;
    [SerializeField] private Transform collapse_smoke_position;
    [SerializeField] private float collapseSpeed;
    [SerializeField] private float delay_to_collapse;

    [Header("Performance Settings")]
    [SerializeField] private float max_destructionRadius = 30f;
    [SerializeField] private float destructionDelay = 0.5f;
    [SerializeField] private float sampleProbability = 0.5f;

    private DynamicVoxelObj[] childScripts;
    private List<Vector3> traversalPoints = new List<Vector3>();
    private HashSet<Vector3Int> visitedCells = new HashSet<Vector3Int>();
    public bool isCollapsing = false;
    private bool doOnce = true;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    [HideInInspector] public float totalDamage = 0f;
    private List<Collider> collidersCache = new List<Collider>();
    private WaitForSeconds destructionWait;
    private int voxelLayerMask;
    private Collider[] colliderArrayCache = new Collider[50];
    private bool pointsCalculated = false;

    bool can_instantiate_fall_itens = true;

    void Start()
    {
        targetPosition = new Vector3(transform.position.x, transform.position.y - Random.Range(40, 80), transform.position.z);
        targetRotation = new Quaternion(transform.rotation.x + Random.Range(-0.2f, 0.2f), transform.rotation.y, transform.rotation.z + Random.Range(-0.2f, 0.2f), transform.rotation.w);
        childScripts = GetComponentsInChildren<DynamicVoxelObj>();
        destructionWait = new WaitForSeconds(destructionDelay);
        voxelLayerMask = LayerMask.GetMask("Voxel");

        Pre_load_collapse();

    }

    void Update()
    {

        if (isCollapsing && can_collapse)
        {
            delay_to_collapse -= Time.deltaTime;

            if (delay_to_collapse <= 0)
            {
                HandleCollapse();
                return;
            }
        }

        if (totalDamage >= dmg_to_collapse && doOnce)
        {

            isCollapsing = true;
            doOnce = false;

            if (pointsCalculated && traversalPoints.Count > 0)
            {
                StartCoroutine(ApplyDestructionWithDelayOptimized());
            }
            else
            {
                Debug.LogError("Nenhum ponto de destruição calculado!");
            }
        }

    }


    private void HandleCollapse()
    {
        transform.position = Vector3.Lerp(transform.position,
            targetPosition,
            Time.deltaTime * collapseSpeed);

        transform.rotation = Quaternion.Lerp(transform.rotation,
            targetRotation,
            Time.deltaTime * collapseSpeed);

        if (Vector3.Distance(transform.position, targetPosition) < 0.3f)
        {
            isCollapsing = false;
        }

        if (can_instantiate_fall_itens)
        {
            if (collapse_smoke_position != null)
            {
                collapse_smoke_position.SetParent(null);
                GameObject smoke = Instantiate(collapse_smoke, collapse_smoke_position.position, Quaternion.identity);
                smoke.transform.localScale *= 100;
            }
            if (collapsesound != null)
            {
                collapsesound.Play();
            }
            can_instantiate_fall_itens = false;
        }
    }

    private void Pre_load_collapse()
    {
        traversalPoints.Clear();
        visitedCells.Clear();

        foreach (DynamicVoxelObj script in childScripts)
        {
            if (script != null && script.transform.childCount > 0)
            {
                MeshCollider meshCollider = script.transform.GetChild(0).GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    CollapseStructureOptimized(meshCollider, distance_step);
                }
                else
                {
                    Debug.LogWarning($"MeshCollider não encontrado em: {script.name}");
                }
            }

        }

        pointsCalculated = traversalPoints.Count > 0;

    }

    public void CollapseStructureOptimized(MeshCollider mesh, float step)
    {

        Bounds worldBounds = mesh.bounds;

        worldBounds.Expand(step * 2f);

        float inverseStep = 1f / step;

        Vector3[] sampleOffsets = GenerateSampleOffsets();
        int pointsAdded = 0;

        for (float x = worldBounds.min.x; x <= worldBounds.max.x; x += step)
        {
            for (float y = worldBounds.min.y; y <= worldBounds.max.y; y += step)
            {
                for (float z = worldBounds.min.z; z <= worldBounds.max.z; z += step)
                {
                    if (Random.value <= sampleProbability)
                    {
                        Vector3 worldPoint = new Vector3(x, y, z);

                        Vector3 samplePoint = worldPoint + sampleOffsets[Random.Range(0, sampleOffsets.Length)] * step * 0.3f;

                        if (IsPointInsideMeshCollider(samplePoint, mesh))
                        {
                            Vector3 localPoint = transform.InverseTransformPoint(samplePoint);

                            Vector3Int cell = new Vector3Int(
                                Mathf.RoundToInt(localPoint.x * inverseStep),
                                Mathf.RoundToInt(localPoint.y * inverseStep),
                                Mathf.RoundToInt(localPoint.z * inverseStep)
                            );

                            if (visitedCells.Add(cell))
                            {
                                traversalPoints.Add(localPoint);
                                pointsAdded++;
                            }
                        }
                    }
                }
            }
        }


    }

    private Vector3[] GenerateSampleOffsets()
    {
        return new Vector3[]
        {
            new Vector3(0.1f, 0.1f, 0.1f),
            new Vector3(0.1f, 0.1f, -0.1f),
            new Vector3(0.1f, -0.1f, 0.1f),
            new Vector3(0.1f, -0.1f, -0.1f),
            new Vector3(-0.1f, 0.1f, 0.1f),
            new Vector3(-0.1f, 0.1f, -0.1f),
            new Vector3(-0.1f, -0.1f, 0.1f),
            new Vector3(-0.1f, -0.1f, -0.1f),
            Vector3.zero
        };
    }

    private bool IsPointInsideMeshCollider(Vector3 worldPoint, MeshCollider meshCollider)
    {

        if (!meshCollider.bounds.Contains(worldPoint))
            return false;

        Vector3 direction = Vector3.up;
        float maxDistance = 100f;

        Ray ray = new Ray(worldPoint - direction * maxDistance * 0.5f, direction);
        RaycastHit hit;

        if (meshCollider.Raycast(ray, out hit, maxDistance))
        {
            return Vector3.Distance(hit.point, worldPoint) < 1f;
        }

        return false;
    }

    private IEnumerator ApplyDestructionWithDelayOptimized()
    {

        var pointsCopy = new List<Vector3>(traversalPoints);

        // Embaralha os pontos
        for (int i = pointsCopy.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Vector3 temp = pointsCopy[i];
            pointsCopy[i] = pointsCopy[randomIndex];
            pointsCopy[randomIndex] = temp;
        }

        foreach (Vector3 localPos in pointsCopy)
        {
            Vector3 worldPos = transform.TransformPoint(localPos);
            ApplyDestructionAtPosition(worldPos);
            yield return destructionWait;
        }

    }

    private void ApplyDestructionAtPosition(Vector3 worldPos)
    {
        float destructionRadius = Random.Range(5, max_destructionRadius);

        int count = Physics.OverlapSphereNonAlloc(worldPos, destructionRadius, colliderArrayCache);

        for (int i = 0; i < count; i++)
        {
            if (colliderArrayCache[i] != null)
            {
                DynamicVoxelObj voxelObj = colliderArrayCache[i].GetComponentInParent<DynamicVoxelObj>();
                if (voxelObj != null)
                {
                    voxelObj.AddDestruction_Sphere(worldPos, destructionRadius);

                }
            }
        }
    }


    public IEnumerator FallUpperVoxels(float radius, Vector3 startPos, bool multiple_raycasts)
    {

        if (radius > 2f && radius != 0f)
        {
            float rayDistance = 20;
            float adjustedRadius = radius * 0.9f;
            RaycastHit hit;

            if (multiple_raycasts)
            {
                Vector3[] rayOrigins = GenerateRaycastOrigins(startPos, radius);

                foreach (Vector3 origin in rayOrigins)
                {

                    Debug.DrawLine(origin, origin + Vector3.up * rayDistance, Color.green, 2);
                    Debug.DrawRay(origin, Vector3.up * rayDistance, Color.green, 2);

                    if (Physics.Raycast(origin, Vector3.up, out hit, rayDistance, voxelLayerMask))
                    {
                        yield return ProcessHitOptimized(hit, adjustedRadius, rayDistance);
                    }
                }
            }
            else
            {
                Debug.DrawLine(startPos, startPos + Vector3.up * rayDistance, Color.green, 2);
                Debug.DrawRay(startPos, Vector3.up * rayDistance, Color.green, 2);


                if (Physics.Raycast(startPos, Vector3.up, out hit, rayDistance, voxelLayerMask))
                {
                    yield return ProcessHitOptimized(hit, adjustedRadius, rayDistance);
                }
            }


        }
    }

    private Vector3[] GenerateRaycastOrigins(Vector3 startPos, float radius)
    {
        float offset = radius / 10;
        return new Vector3[]
        {
            startPos,
            new Vector3(startPos.x + offset, startPos.y, startPos.z),
            new Vector3(startPos.x - offset, startPos.y, startPos.z),
            new Vector3(startPos.x, startPos.y, startPos.z + offset),
            new Vector3(startPos.x, startPos.y, startPos.z - offset)
        };
    }

    private IEnumerator ProcessHitOptimized(RaycastHit hit, float radius, float rayDistance)
    {
        Vector3 hitPoint = hit.point;
        Vector3 checkCenter = new Vector3(hitPoint.x, hitPoint.y + radius * 0.5f, hitPoint.z);

        collidersCache.Clear();
        int count = Physics.OverlapSphereNonAlloc(checkCenter, radius, colliderArrayCache);

        for (int i = 0; i < count; i++)
        {
            collidersCache.Add(colliderArrayCache[i]);
        }

        DynamicVoxelObj voxel = hit.collider.GetComponentInParent<DynamicVoxelObj>();

        yield return new WaitForSeconds(3f);

        if (voxel != null)
        {
            ApplyUpperVoxelDestruction(hit, radius, collidersCache, rayDistance);

            if (radius > 20f)
            {
                StartCoroutine(FallUpperVoxels(radius, hitPoint, false));
            }
        }
    }

    private void ApplyUpperVoxelDestruction(RaycastHit hit, float radius, List<Collider> colliders, float rayDistance)
    {
        Vector3 secondRayOrigin = hit.point + Vector3.up * 0.1f;
        RaycastHit secondHit;

        if (Physics.Raycast(secondRayOrigin, Vector3.down, out secondHit, rayDistance, voxelLayerMask) && secondHit.collider != null)
        {
            foreach (Collider collider in colliders)
            {
                if (collider == null) continue;

                DynamicVoxelObj voxel = collider.GetComponentInParent<DynamicVoxelObj>();
                if (voxel != null)
                {
                    voxel.AddDestruction_Sphere(hit.point, radius);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (traversalPoints != null && traversalPoints.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (Vector3 point in traversalPoints)
            {
                Gizmos.DrawSphere(point, 0.1f);
            }
        }
    }
}