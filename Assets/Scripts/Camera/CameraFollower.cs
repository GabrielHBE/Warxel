using System.Collections;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkinApplier skinApplier;
    [SerializeField] private Transform parent;
    [SerializeField] private GameObject neck;
    [SerializeField] private PlayerProperties playerProperties;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.15f;
    [SerializeField, Min(0f)] private float collisionSkin = 0.02f;

    private readonly RaycastHit[] collisionHits = new RaycastHit[12];
    private Quaternion original_rotation;
    private bool wasRolling = false;

    private Coroutine deadPlayerCameraFollowerCoroutine;

    void Start()
    {
        playerProperties.isDead.OnChange += OnIsDeadChange;
        original_rotation = transform.localRotation;
    }

    void OnDestroy()
    {
        if (deadPlayerCameraFollowerCoroutine != null) StopCoroutine(deadPlayerCameraFollowerCoroutine);

        playerProperties.isDead.OnChange -= OnIsDeadChange;
    }

    void LateUpdate()
    {
        if (playerProperties.isDead.Value) return;

        bool setparent = playerProperties.roll;

        if (setparent && !wasRolling) transform.SetParent(neck.transform);
        else if (!setparent && wasRolling)
        {
            // Saiu do estado de roll/dead
            transform.SetParent(parent);
            StartCoroutine(ResetRotation());
        }

        if (neck != null) MoveToCollisionSafePosition(neck.transform.position);

        wasRolling = playerProperties.roll;
    }

    private void MoveToCollisionSafePosition(Vector3 desiredPosition)
    {
        Transform playerRoot = parent != null ? parent : transform.root;
        Vector3 safeOrigin = new Vector3(
            playerRoot.position.x,
            desiredPosition.y,
            playerRoot.position.z
        );

        Vector3 offset = desiredPosition - safeOrigin;
        float distance = offset.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            transform.position = desiredPosition;
            return;
        }

        Vector3 direction = offset / distance;
        int hitCount = Physics.SphereCastNonAlloc(
            safeOrigin,
            collisionRadius,
            direction,
            collisionHits,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );

        float allowedDistance = distance;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = collisionHits[i];
            collisionHits[i] = default;

            if (hit.collider == null || hit.collider.transform.IsChildOf(playerRoot)) continue;

            allowedDistance = Mathf.Min(
                allowedDistance,
                Mathf.Max(0f, hit.distance - collisionSkin)
            );
        }

        transform.position = safeOrigin + direction * allowedDistance;
    }

    private IEnumerator ResetRotation()
    {
        while (transform.localRotation != original_rotation)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, original_rotation, Time.deltaTime * 7);
            yield return null;
        }

        transform.localRotation = original_rotation;
    }

    private void OnIsDeadChange(bool prev, bool next, bool asServer)
    {
        if (prev == next) return;

        if (next)
        {
            deadPlayerCameraFollowerCoroutine = StartCoroutine(DeadPlayerCameraFollowerCoroutine());
        }
        else
        {
            if (deadPlayerCameraFollowerCoroutine != null)
            {
                StopCoroutine(deadPlayerCameraFollowerCoroutine);
                deadPlayerCameraFollowerCoroutine = null;
            }
            transform.localRotation = original_rotation;
        }
    }

    private IEnumerator DeadPlayerCameraFollowerCoroutine()
    {
        while (true)
        {
            if (skinApplier == null || skinApplier.instantiatedHead == null) yield break;

            if (skinApplier.instantiatedHead != null)
            {
                MoveToCollisionSafePosition(skinApplier.instantiatedHead.transform.position);
                transform.rotation = skinApplier.instantiatedHead.transform.rotation;
            }
            else yield break;

            yield return null;
        }
    }

}
