using System.Collections;
using System.Linq;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class VoxelPartialCollapse : VoxelDestruction
{
    [SerializeField] protected VoxelPartialCollapse[] chainCollapse;
    [SerializeField] protected DamageModelSwap[] damageModelSwap;

    [Header("Debris performance")]
    [Tooltip("Prebuilt shape used after collapse. Empty keeps the legacy convex mesh.")]
    [SerializeField] private Collider debrisCollider;
    [Tooltip("Optional layer whose collision matrix controls rubble-to-rubble contacts.")]
    [SerializeField] private string debrisLayer = "";
    [SerializeField] private bool settleWhenResting;
    [Min(0.25f)] [SerializeField] private float restingDuration = 1.5f;
    [Min(0f)] [SerializeField] private float restingLinearSpeed = 0.15f;
    [Min(0f)] [SerializeField] private float restingAngularSpeed = 0.15f;
    [Tooltip("Zero preserves the Rigidbody's solver settings.")]
    [Range(0, 12)] [SerializeField] private int debrisSolverIterations;

    private readonly SyncVar<bool> isSettled = new SyncVar<bool>();
    private static readonly WaitForSeconds RestCheckInterval = new WaitForSeconds(0.25f);
    private Coroutine settleRoutine;
    private NetworkTransform networkTransform;
    private bool initialized;
    private int intactLayer;
    private int collapsedLayer;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    protected override void Start() => EnsureInitialized();

    private void EnsureInitialized()
    {
        if (initialized) return;
        base.Start();
        intactLayer = gameObject.layer;
        collapsedLayer = string.IsNullOrEmpty(debrisLayer) ? intactLayer : LayerMask.NameToLayer(debrisLayer);
        if (collapsedLayer < 0) collapsedLayer = intactLayer;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        networkTransform = GetComponent<NetworkTransform>();

        if (debrisCollider == null)
        {
            debrisCollider = GetComponents<MeshCollider>().FirstOrDefault(collider => collider.convex);
            if (debrisCollider == null)
            {
                MeshCollider convexCollider = gameObject.AddComponent<MeshCollider>();
                convexCollider.enabled = false;
                convexCollider.sharedMesh = meshFilter.sharedMesh;
                convexCollider.convex = true;
                debrisCollider = convexCollider;
            }
        }
        debrisCollider.enabled = false;
        if (debrisSolverIterations > 0) rb.solverIterations = debrisSolverIterations;
        damageTaken.OnChange += OnDamageTakenChanged;
        isDestroyed.OnChange += OnDestroyedChanged;
        isSettled.OnChange += OnSettledChanged;
        initialized = true;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        EnsureInitialized();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ApplyCollapseState();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Also restore initial state for clients joining after a collapse.
        UpdateModel(damageTaken.Value);
        ApplyCollapseState();
    }

    public override void OnStopNetwork()
    {
        StopSettling();
        if (rb != null) rb.isKinematic = true;
        base.OnStopNetwork();
    }

    private void OnEnable()
    {
        if (initialized && NetworkObject != null && IsSpawned) ApplyCollapseState();
    }

    private void OnDisable() => StopSettling();

    private void OnDestroy()
    {
        damageTaken.OnChange -= OnDamageTakenChanged;
        isDestroyed.OnChange -= OnDestroyedChanged;
        isSettled.OnChange -= OnSettledChanged;
    }

    private void OnDamageTakenChanged(float prev, float next, bool asServer)
    {
        if (!asServer && IsServerInitialized) return;
        UpdateModel(next);
    }

    private void OnDestroyedChanged(bool prev, bool next, bool asServer)
    {
        if (!asServer && IsServerInitialized) return;
        ApplyCollapseState();
    }

    private void OnSettledChanged(bool prev, bool next, bool asServer)
    {
        if (!asServer && IsServerInitialized) return;
        ApplyCollapseState();
    }

    private void ApplyCollapseState()
    {
        EnsureInitialized();
        bool collapsed = isDestroyed.Value;
        bool simulate = collapsed && !isSettled.Value && IsServerInitialized;

        // Become kinematic BEFORE restoring the non-convex collider.
        if (!simulate && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        meshCollider.enabled = !collapsed;
        debrisCollider.enabled = collapsed;
        gameObject.layer = collapsed ? collapsedLayer : intactLayer;
        rb.isKinematic = !simulate;

        if (simulate && settleWhenResting && settleRoutine == null && isActiveAndEnabled)
            settleRoutine = StartCoroutine(SettleWhenResting());
        else if (!simulate)
            StopSettling();
    }

    private void UpdateModel(float currentDamage)
    {
        if (damageModelSwap == null) return;
        foreach (var swap in damageModelSwap)
        {
            if (swap.damageRange == null || swap.damageRange.Length == 0) continue;
            float minDamage = swap.damageRange[0];
            float maxDamage = swap.damageRange.Length > 1 ? swap.damageRange[1] : float.MaxValue;
            if (currentDamage < minDamage || currentDamage > maxDamage) continue;

            // Avoid reassigning the same collision mesh on every damage update.
            if (swap.mesh != null && meshFilter.sharedMesh != swap.mesh)
            {
                meshFilter.sharedMesh = swap.mesh;
                meshCollider.sharedMesh = swap.mesh;
            }
            if (swap.material != null && meshRenderer.sharedMaterial != swap.material)
                meshRenderer.sharedMaterial = swap.material;
            break;
        }
    }

    [Server]
    public override void Destroy()
    {
        if (isDestroyed.Value) return;
        EnsureInitialized();
        damageTaken.Value = damageToDestroy;
        isDestroyed.Value = true;
        ApplyCollapseState();
        ApplyRandomTorque();

        if (chainCollapse == null) return;
        foreach (VoxelPartialCollapse vox in chainCollapse)
        {
            if (vox != null) vox.ApplyDamageOnServer(vox.damageToDestroy);
        }
    }

    private IEnumerator SettleWhenResting()
    {
        float restingTime = 0f;
        float linearLimit = restingLinearSpeed * restingLinearSpeed;
        float angularLimit = restingAngularSpeed * restingAngularSpeed;
        while (true)
        {
            yield return RestCheckInterval;
            bool slow = rb.linearVelocity.sqrMagnitude <= linearLimit && rb.angularVelocity.sqrMagnitude <= angularLimit;
            // A timeout alone would freeze airborne pieces. Require sleep or
            // sustained low velocity with stationary support beneath the collider.
            bool resting = rb.IsSleeping() || (slow && HasStationarySupport());
            restingTime = resting ? restingTime + 0.25f : 0f;
            if (restingTime < restingDuration) continue;

            settleRoutine = null;
            isSettled.Value = true;
            ApplyCollapseState();
            if (networkTransform != null) networkTransform.ForceSend();
            yield break;
        }
    }

    private bool HasStationarySupport()
    {
        Bounds bounds = debrisCollider.bounds;
        int mask = Physics.DefaultRaycastLayers;
        // Optimized debris intentionally does not stack.
        if (collapsedLayer != intactLayer) mask &= ~(1 << collapsedLayer);
        if (!Physics.Raycast(bounds.center, Vector3.down, out RaycastHit hit,
                bounds.extents.y + 0.1f, mask, QueryTriggerInteraction.Ignore)) return false;
        return hit.rigidbody != rb && (hit.rigidbody == null || hit.rigidbody.isKinematic);
    }

    private void StopSettling()
    {
        if (settleRoutine == null) return;
        StopCoroutine(settleRoutine);
        settleRoutine = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (NetworkObject == null || !IsServerInitialized || !isDestroyed.Value || isSettled.Value) return;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Vehicle"))
            ProcessHit.VehicleHit(collision.gameObject, rb.linearVelocity.magnitude, gameObject);
        if (collision.gameObject.layer == LayerMask.NameToLayer("PlayerHitBox"))
            ProcessHit.PlayerHit(collision.gameObject, rb.linearVelocity.magnitude, 1, gameObject);
    }

    [Server]
    public void ResetCollapse()
    {
        EnsureInitialized();
        StopSettling();
        isDestroyed.Value = false;
        isSettled.Value = false;
        damageTaken.Value = 0;
        ApplyCollapseState();
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
        rb.position = transform.position;
        rb.rotation = transform.rotation;
        if (networkTransform != null)
        {
            networkTransform.Teleport();
            networkTransform.ForceSend();
        }
    }

    [System.Serializable]
    public struct DamageModelSwap
    {
        public Mesh mesh;
        public Material material;
        public float[] damageRange;
    }
}