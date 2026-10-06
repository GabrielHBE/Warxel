using System;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]
public class VehicleLockInMissileController : VehicleMissileController
{
    [Header("Lock-On Settings")]
    [SerializeField] private Transform lockInFowardReference;
    [SerializeField] private SoundManager.SoundComponents lockingInSound;
    [SerializeField] private Vehicle.VehicleType lockInVehicleType;
    [SerializeField] private float lockOnTimeRequired = 2f;

    [Header("Detection Settings")]
    [Min(0f)]
    [SerializeField] private float maxLockDistance = 5000f;
    [Tooltip("Solid layers that can block lock-on. Include Vehicle and world geometry. Triggers and the firing vehicle are ignored.")]
    [SerializeField] private LayerMask lockObstructionMask = ~0;

    [Header("Locked Target Zoom")]
    [SerializeField] private bool zoomOnLockedTarget = true;
    [Tooltip("Fraction of the shortest viewport dimension used to frame the whole target. Limited to the lock area to preserve detection while zooming.")]
    [SerializeField, Range(0.1f, 0.9f)] private float lockedTargetScreenFraction = 0.35f;
    [SerializeField, Range(1f, 179f)] private float minimumLockFieldOfView = 1f;
    [SerializeField, Range(1f, 179f)] private float maximumLockFieldOfView = 90f;
    [SerializeField, Min(0.01f)] private float lockZoomSpeed = 6f;

    [Header("Locked Target Camera Tracking")]
    [SerializeField] private bool followLockedTarget = true;
    [Tooltip("Minimum mouse-axis movement that releases camera tracking.")]
    [SerializeField, Min(0f)] private float mouseTrackingReleaseThreshold = 0.001f;

    [Header("HUD Lock Indicator")]
    [SerializeField, Min(8f)] private float indicatorSize = 64f;
    [SerializeField, Min(1f)] private float indicatorLineWidth = 3f;
    [SerializeField, Range(0.1f, 1f)] private float lockedIndicatorScale = 0.7f;
    [SerializeField, Min(0.01f)] private float indicatorTransitionSpeed = 8f;
    [SerializeField, Min(0.01f)] private float indicatorPositionSpeed = 20f;

    [Header("HUD Lock Area")]
    [Tooltip("Side length of the detection square as a fraction of the camera's shortest viewport dimension.")]
    [SerializeField, Range(0.1f, 1f)] private float lockAreaScreenFraction = 0.45f;
    [SerializeField, Min(1f)] private float lockAreaCornerLength = 32f;
    [SerializeField] private Color lockAreaColor => Color.limeGreen;

    private Vehicle targetVehicle;
    private Transform currentTarget;
    private float currentLockTimer = 0f;
    private bool canShoot = false;
    private float lockingInSoundDelay = 0;
    private RectTransform indicatorRect;
    private RectTransform lockAreaRect;
    private RectTransform indicatorCanvasRect;
    private Canvas indicatorCanvas;
    private Image[] indicatorBorders;
    private Camera zoomCamera;
    private float originalArmoryFieldOfView;
    private bool cameraTrackingActive;
    private bool cameraTrackingInterrupted;

    public override bool CanRotateCamera => followLockedTarget || base.CanRotateCamera;

    protected override void Update()
    {
        base.Update();

        if (!IsOwner || !isActive) return;
        HandleLockingInSound();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (!IsOwner || !isActive)
        {
            if (currentTarget != null) ResetLock();
            RestoreArmoryFieldOfView();
            HideLockArea();
            HideLockIndicator();
            return;
        }

        Vehicle hostVehicle = GetComponentInParent<Vehicle>();
        VehicleSeats seat = hostVehicle != null ? hostVehicle.currentSeat : null;
        Camera camera = GetLockCamera(seat);
        float mouseX = InputManager.GetAxisRaw("Mouse X");
        float mouseY = InputManager.GetAxisRaw("Mouse Y");
        float releaseThreshold = Mathf.Max(0f, mouseTrackingReleaseThreshold);
        bool mouseMoved = mouseX * mouseX + mouseY * mouseY > releaseThreshold * releaseThreshold;

        UpdateCameraTrackingState(CanTrackLockedTarget(camera), mouseMoved);
        ProcessLockOn(camera);
        UpdateCameraTrackingState(CanTrackLockedTarget(camera), mouseMoved);
        if (cameraTrackingActive)
        {
            AimCameraAtLockedTarget(camera);
            ValidateTrackedTargetLock(camera);
        }
        UpdateArmoryFieldOfView(camera);
        UpdateLockArea(seat, camera);
        UpdateLockIndicator(seat, camera);
    }

    public override void ActivateArmory()
    {
        RestoreArmoryFieldOfView();
        ResetLock();
        base.ActivateArmory();
    }

    public override void DeactivateArmory()
    {
        RestoreArmoryFieldOfView();
        ResetLock();
        HideLockArea();
        base.DeactivateArmory();
    }

    public override void SetCameraActive(bool active)
    {
        if (!active)
        {
            RestoreArmoryFieldOfView();
            ResetLock();
            HideLockArea();
        }

        base.SetCameraActive(active);
    }

    private void OnDestroy()
    {
        RestoreArmoryFieldOfView();
        if (indicatorRect != null) Destroy(indicatorRect.gameObject);
        if (lockAreaRect != null) Destroy(lockAreaRect.gameObject);
    }

    private Collider[] lockCandidates = new Collider[32];
    private RaycastHit[] lockSightHits = new RaycastHit[32];
    private readonly Plane[] lockAreaPlanes = new Plane[6];
    private readonly HashSet<Vehicle> checkedVehicles = new HashSet<Vehicle>();
    private readonly List<Renderer> vehicleRenderers = new List<Renderer>();
    private readonly List<Collider> vehicleColliders = new List<Collider>();

    private void UpdateCameraTrackingState(bool canTrack, bool mouseMoved)
    {
        if (!canTrack)
        {
            cameraTrackingActive = false;
            return;
        }

        if (mouseMoved)
        {
            if (cameraTrackingActive)
            {
                cameraTrackingActive = false;
                cameraTrackingInterrupted = true;
            }
            return;
        }

        if (!cameraTrackingInterrupted) cameraTrackingActive = true;
    }

    private bool CanTrackLockedTarget(Camera camera)
    {
        return followLockedTarget && canShoot && targetVehicle != null &&
            targetVehicle.isActiveAndEnabled && !targetVehicle.vehicle_destroyed.Value &&
            camera != null && camera == armoryCamera && camera.isActiveAndEnabled;
    }

    private void AimCameraAtLockedTarget(Camera camera)
    {
        Vector3 targetPosition = TryGetTargetBounds(out Bounds bounds)
            ? bounds.center
            : targetVehicle.spot_position != null
                ? targetVehicle.spot_position.position
                : targetVehicle.transform.position;
        Vector3 direction = targetPosition - camera.transform.position;
        if (direction.sqrMagnitude <= 0.0001f) return;

        Transform pivot = CameraRotationPivot;
        if (pivot == null || !camera.transform.IsChildOf(pivot)) pivot = camera.transform;

        // Rotate the same pivot used by manual mouse control, preserving the camera's child offset.
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        Quaternion rotationDelta = targetRotation * Quaternion.Inverse(camera.transform.rotation);
        Quaternion desiredWorldRotation = rotationDelta * pivot.rotation;
        Quaternion desiredLocalRotation = pivot.parent != null
            ? Quaternion.Inverse(pivot.parent.rotation) * desiredWorldRotation
            : desiredWorldRotation;

        // Tracking obeys the same local pitch/yaw limits as manual mouse rotation.
        Vector3 localAngles = desiredLocalRotation.eulerAngles;
        Vector2 clampedAngles = ClampCameraRotation(new Vector2(
            Mathf.DeltaAngle(0f, localAngles.x), Mathf.DeltaAngle(0f, localAngles.y)));
        pivot.localRotation = Quaternion.Euler(clampedAngles.x, clampedAngles.y, localAngles.z);
    }

    private void ResetCameraTracking()
    {
        cameraTrackingActive = false;
        cameraTrackingInterrupted = false;
    }

    private void ValidateTrackedTargetLock(Camera camera)
    {
        // Check the target after applying the camera clamps, before zoom can widen the view.
        // Keep partial-body detection: only lose lock when no valid part remains in the area.
        Transform castReference = lockInFowardReference != null ? lockInFowardReference : transform;
        Vector3 origin = castReference.position;
        Rect lockArea = GetLockAreaScreenRect(camera);
        UpdateLockAreaPlanes(camera, lockArea, origin);
        if (!TryGetVehicleLockPart(camera, targetVehicle, lockArea, origin,
            maxLockDistance * maxLockDistance, out _, out _, out _, out _))
            ResetLock();
    }

    private void UpdateArmoryFieldOfView(Camera camera)
    {
        if (!zoomOnLockedTarget || camera == null || camera != armoryCamera ||
            !camera.isActiveAndEnabled || camera.orthographic)
        {
            RestoreArmoryFieldOfView();
            return;
        }

        if (zoomCamera != camera)
        {
            RestoreArmoryFieldOfView();
            zoomCamera = camera;
            originalArmoryFieldOfView = camera.fieldOfView;
        }

        float targetFieldOfView = originalArmoryFieldOfView;
        if (canShoot && targetVehicle != null && TryGetTargetBounds(out Bounds bounds) &&
            TryGetTargetFieldOfView(camera, bounds, out float framingFieldOfView))
        {
            float minimum = Mathf.Clamp(minimumLockFieldOfView, 1f, 179f);
            float maximum = Mathf.Clamp(maximumLockFieldOfView, minimum, 179f);
            targetFieldOfView = Mathf.Clamp(framingFieldOfView, minimum, maximum);
        }

        float blend = 1f - Mathf.Exp(-Mathf.Max(lockZoomSpeed, 0.01f) * Time.deltaTime);
        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFieldOfView, blend);
    }

    private bool TryGetTargetBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        targetVehicle.GetComponentsInChildren(false, vehicleRenderers);
        foreach (Renderer body in vehicleRenderers)
        {
            if (body == null || !body.enabled ||
                !(body is MeshRenderer || body is SkinnedMeshRenderer) ||
                body.GetComponentInParent<Vehicle>() != targetVehicle ||
                body.GetComponentInParent<PlayerProperties>() != null) continue;

            if (hasBounds) bounds.Encapsulate(body.bounds);
            else bounds = body.bounds;
            hasBounds = true;
        }

        if (hasBounds) return true;

        targetVehicle.GetComponentsInChildren(false, vehicleColliders);
        foreach (Collider body in vehicleColliders)
        {
            if (body == null || !body.enabled || body.isTrigger ||
                body.GetComponentInParent<Vehicle>() != targetVehicle ||
                body.GetComponentInParent<PlayerProperties>() != null) continue;

            if (hasBounds) bounds.Encapsulate(body.bounds);
            else bounds = body.bounds;
            hasBounds = true;
        }

        return hasBounds;
    }

    private bool TryGetTargetFieldOfView(Camera camera, Bounds bounds, out float fieldOfView)
    {
        fieldOfView = 0f;
        if (camera.aspect <= 0f) return false;

        // Fit every corner, including targets away from the center of the view.
        // Keep the whole vehicle inside the detection square so zoom cannot break its lock.
        float screenFraction = Mathf.Max(0.01f,
            Mathf.Min(lockedTargetScreenFraction, lockAreaScreenFraction * 0.9f));
        float frameFraction = screenFraction * Mathf.Min(camera.aspect, 1f);
        float requiredTangent = 0f;
        Vector3 minimum = bounds.min;
        Vector3 maximum = bounds.max;
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 worldPoint = new Vector3(
                (corner & 1) == 0 ? minimum.x : maximum.x,
                (corner & 2) == 0 ? minimum.y : maximum.y,
                (corner & 4) == 0 ? minimum.z : maximum.z);
            Vector3 cameraPoint = camera.transform.InverseTransformPoint(worldPoint);
            if (cameraPoint.z <= Mathf.Max(camera.nearClipPlane, 0.01f)) return false;

            float tangent = Mathf.Max(Mathf.Abs(cameraPoint.x), Mathf.Abs(cameraPoint.y)) /
                (cameraPoint.z * frameFraction);
            requiredTangent = Mathf.Max(requiredTangent, tangent);
        }

        fieldOfView = 2f * Mathf.Atan(requiredTangent) * Mathf.Rad2Deg;
        return true;
    }

    private void RestoreArmoryFieldOfView()
    {
        if (zoomCamera != null) zoomCamera.fieldOfView = originalArmoryFieldOfView;
        zoomCamera = null;
    }

    private void ProcessLockOn(Camera camera)
    {
        int vehicleLayer = LayerMask.NameToLayer("Vehicle");
        if (camera == null || !camera.isActiveAndEnabled || camera.pixelWidth <= 0 ||
            camera.pixelHeight <= 0 || vehicleLayer < 0 || maxLockDistance <= 0f)
        {
            ResetLock();
            return;
        }

        Transform castReference = lockInFowardReference != null ? lockInFowardReference : transform;
        Vector3 origin = castReference.position;

        // Let tracking catch up to a target outside the previous view cone.
        // ValidateTrackedTargetLock checks the lock area after the clamped camera rotation.
        if (cameraTrackingActive && CanTrackLockedTarget(camera))
        {
            Vector3 closestPoint = TryGetTargetBounds(out Bounds trackedBounds)
                ? trackedBounds.ClosestPoint(origin)
                : targetVehicle.transform.position;
            if ((closestPoint - origin).sqrMagnitude <= maxLockDistance * maxLockDistance &&
                IsTargetVisible(camera, targetVehicle)) return;

            ResetLock();
        }

        int hitCount;
        while (true)
        {
            // Gather vehicles in range, then use the same projected region as the HUD.
            // A fixed-radius SphereCast cannot cover a fixed screen area at every distance.
            hitCount = Physics.OverlapSphereNonAlloc(origin, maxLockDistance, lockCandidates,
                1 << vehicleLayer, QueryTriggerInteraction.Collide);

            // O resultado não é ordenado e pode ser truncado se houver muitos colliders.
            if (hitCount < lockCandidates.Length) break;
            Array.Resize(ref lockCandidates, lockCandidates.Length * 2);
        }

        Vehicle bestTarget = null;
        Collider bestCollider = null;
        Renderer bestRenderer = null;
        float bestAlignment = float.PositiveInfinity;
        float bestDistance = float.PositiveInfinity;
        Rect lockArea = GetLockAreaScreenRect(camera);
        float maxDistanceSquared = maxLockDistance * maxLockDistance;
        UpdateLockAreaPlanes(camera, lockArea, origin);
        checkedVehicles.Clear();

        // Keep progress attached to the vehicle, even when its visible part changes.
        // This also applies during acquisition and when only the seat camera is available.
        if (IsValidLockVehicle(targetVehicle) &&
            TryGetVehicleLockPart(camera, targetVehicle, lockArea, origin, maxDistanceSquared,
                out Collider retainedCollider, out Renderer retainedRenderer, out _, out _))
        {
            UpdateLockProgress(targetVehicle, retainedCollider, retainedRenderer);
            return;
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = lockCandidates[i];
            if (collider == null || collider.transform.root == transform.root) continue;

            Vehicle candidate = collider.GetComponentInParent<Vehicle>();
            if (!IsValidLockVehicle(candidate) ||
                !checkedVehicles.Add(candidate)) continue;

            if (!TryGetVehicleLockPart(camera, candidate, lockArea, origin, maxDistanceSquared,
                out Collider partCollider, out Renderer partRenderer,
                out float alignment, out float distance)) continue;
            if (alignment < bestAlignment ||
                (Mathf.Approximately(alignment, bestAlignment) && distance < bestDistance))
            {
                bestTarget = candidate;
                bestRenderer = partRenderer;
                bestCollider = partCollider;
                bestAlignment = alignment;
                bestDistance = distance;
            }
        }

        if (bestTarget == null)
        {
            ResetLock();
            return;
        }

        UpdateLockProgress(bestTarget, bestCollider, bestRenderer);
    }

    private bool IsValidLockVehicle(Vehicle candidate)
    {
        return candidate != null && candidate.isActiveAndEnabled && !candidate.vehicle_destroyed.Value &&
            candidate.vehicleType == lockInVehicleType && candidate.transform.root != transform.root;
    }

    private bool TryGetVehicleLockPart(Camera camera, Vehicle candidate, Rect lockArea,
        Vector3 origin, float maxDistanceSquared, out Collider bestCollider,
        out Renderer bestRenderer, out float bestAlignment, out float bestDistance)
    {
        bestCollider = null;
        bestRenderer = null;
        bestAlignment = float.PositiveInfinity;
        bestDistance = float.PositiveInfinity;
        candidate.GetComponentsInChildren(false, vehicleRenderers);
        foreach (Renderer body in vehicleRenderers)
        {
            if (body == null || !body.enabled ||
                !(body is MeshRenderer || body is SkinnedMeshRenderer) ||
                body.GetComponentInParent<Vehicle>() != candidate ||
                body.GetComponentInParent<PlayerProperties>() != null) continue;

            Bounds bounds = body.bounds;
            if (!TryGetLockPartScore(camera, lockArea, bounds,
                bounds.ClosestPoint(origin), origin, maxDistanceSquared,
                out float alignment, out float distance) ||
                !HasLineOfSight(camera, candidate, bounds.center)) continue;
            if (alignment < bestAlignment ||
                (Mathf.Approximately(alignment, bestAlignment) && distance < bestDistance))
            {
                bestRenderer = body;
                bestAlignment = alignment;
                bestDistance = distance;
            }
        }

        // A combined mesh can have its center in empty space between physical parts.
        // Always consider all colliders too, even when the vehicle has visible meshes.
        candidate.GetComponentsInChildren(false, vehicleColliders);
        foreach (Collider body in vehicleColliders)
        {
            if (body == null || !body.enabled || body.isTrigger ||
                body.GetComponentInParent<Vehicle>() != candidate ||
                body.GetComponentInParent<PlayerProperties>() != null) continue;
            if (!TryGetLockPartScore(camera, lockArea, body.bounds,
                body.ClosestPoint(origin), origin, maxDistanceSquared,
                out float alignment, out float distance) ||
                !HasLineOfSight(camera, candidate, body.bounds.center)) continue;
            if (alignment < bestAlignment ||
                (Mathf.Approximately(alignment, bestAlignment) && distance < bestDistance))
            {
                bestCollider = body;
                bestRenderer = null;
                bestAlignment = alignment;
                bestDistance = distance;
            }
        }
        return bestCollider != null || bestRenderer != null;
    }

    private void UpdateLockProgress(Vehicle selectedVehicle, Collider selectedCollider, Renderer selectedRenderer)
    {
        if (selectedVehicle != targetVehicle)
        {
            ResetLock();
            targetVehicle = selectedVehicle;
            currentTarget = selectedVehicle.transform;
        }
        currentLockTimer += Time.deltaTime;
        canShoot = currentLockTimer >= lockOnTimeRequired;
        lockingInSoundDelay = canShoot ? 0.1f : 0.5f;
    }

    private bool HasLineOfSight(Camera camera, Vehicle candidate, Vector3 aimPoint)
    {
        return LockInTargeting.HasLineOfSight(camera.transform.position, aimPoint,
            transform.root, candidate, lockObstructionMask, ref lockSightHits);
    }

    private bool IsTargetVisible(Camera camera, Vehicle candidate)
    {
        candidate.GetComponentsInChildren(false, vehicleRenderers);
        foreach (Renderer body in vehicleRenderers)
        {
            if (body == null || !body.enabled ||
                !(body is MeshRenderer || body is SkinnedMeshRenderer) ||
                body.GetComponentInParent<Vehicle>() != candidate ||
                body.GetComponentInParent<PlayerProperties>() != null) continue;

            if (HasLineOfSight(camera, candidate, body.bounds.center)) return true;
        }

        candidate.GetComponentsInChildren(false, vehicleColliders);
        foreach (Collider body in vehicleColliders)
        {
            if (body == null || !body.enabled || body.isTrigger ||
                body.GetComponentInParent<Vehicle>() != candidate ||
                body.GetComponentInParent<PlayerProperties>() != null) continue;
            if (HasLineOfSight(camera, candidate, body.bounds.center)) return true;
        }
        return false;
    }

    private void ResetLock()
    {
        ResetCameraTracking();
        targetVehicle = null;
        canShoot = false;
        currentTarget = null;
        currentLockTimer = 0f;
        lockingInSoundDelay = 0f;
        soundTimer = 0f;
        HideLockIndicator();
        if (indicatorRect != null)
        {
            indicatorRect.localRotation = Quaternion.identity;
            indicatorRect.localScale = Vector3.one;
        }
    }

    private Camera GetLockCamera(VehicleSeats seat)
    {
        if (seat == null || seat.GetCurrentArmory() != this) return null;

        if (armoryCamera != null && armoryCamera.isActiveAndEnabled) return armoryCamera;

        Camera camera = seat.seatCameras != null && seat.seatCameras.Length > 0
            ? seat.GetCurrentCamera()
            : null;
        if (camera == null || !camera.isActiveAndEnabled)
            camera = lockInFowardReference != null ? lockInFowardReference.GetComponent<Camera>() : null;

        return camera != null && camera.isActiveAndEnabled ? camera : null;
    }

    private Rect GetLockAreaScreenRect(Camera camera)
    {
        Rect viewport = camera.pixelRect;
        float side = Mathf.Min(viewport.width, viewport.height) * lockAreaScreenFraction;
        return new Rect(viewport.center - Vector2.one * (side * 0.5f), Vector2.one * side);
    }

    private void UpdateLockAreaPlanes(Camera camera, Rect lockArea, Vector3 origin)
    {
        // Crop the camera frustum to the exact screen rectangle drawn by the HUD.
        // This tests the collider volume instead of requiring its center inside it.
        Rect viewport = camera.pixelRect;
        Matrix4x4 crop = Matrix4x4.identity;
        crop.m00 = viewport.width / lockArea.width;
        crop.m11 = viewport.height / lockArea.height;
        crop.m03 = 2f * (viewport.center.x - lockArea.center.x) / lockArea.width;
        crop.m13 = 2f * (viewport.center.y - lockArea.center.y) / lockArea.height;
        GeometryUtility.CalculateFrustumPlanes(
            crop * camera.projectionMatrix * camera.worldToCameraMatrix, lockAreaPlanes);

        // Range is measured from the armory, independently of the camera far clip.
        float farDistance = maxLockDistance + Vector3.Distance(camera.transform.position, origin);
        lockAreaPlanes[5] = new Plane(-camera.transform.forward,
            camera.transform.position + camera.transform.forward * farDistance);
    }

    private bool TryGetLockPartScore(Camera camera, Rect lockArea, Bounds bounds,
        Vector3 closestPoint, Vector3 origin, float maxDistanceSquared,
        out float alignment, out float distance)
    {
        alignment = float.PositiveInfinity;
        distance = (closestPoint - origin).sqrMagnitude;
        if (distance > maxDistanceSquared ||
            !GeometryUtility.TestPlanesAABB(lockAreaPlanes, bounds)) return false;

        Vector3 screenPoint = camera.WorldToScreenPoint(bounds.center);
        alignment = ((Vector2)screenPoint - lockArea.center).sqrMagnitude;
        return true;
    }

    private void UpdateLockArea(VehicleSeats seat, Camera camera)
    {
        if (camera == null || !camera.isActiveAndEnabled || maxLockDistance <= 0f ||
            seat == null || !EnsureLockIndicator(seat))
        {
            HideLockArea();
            return;
        }

        Rect lockArea = GetLockAreaScreenRect(camera);
        Vector2 center = lockArea.center;
        float halfSide = lockArea.width * 0.5f;
        if (!TryScreenToCanvasLocal(center, camera, out Vector2 localCenter) ||
            !TryScreenToCanvasLocal(center + Vector2.right * halfSide, camera, out Vector2 localRight) ||
            !TryScreenToCanvasLocal(center + Vector2.up * halfSide, camera, out Vector2 localTop))
        {
            HideLockArea();
            return;
        }

        lockAreaRect.anchoredPosition = localCenter;
        lockAreaRect.sizeDelta = new Vector2(
            Mathf.Abs(localRight.x - localCenter.x) * 2f,
            Mathf.Abs(localTop.y - localCenter.y) * 2f);
        if (!lockAreaRect.gameObject.activeSelf) lockAreaRect.gameObject.SetActive(true);
    }

    private void UpdateLockIndicator(VehicleSeats seat, Camera camera)
    {
        if (targetVehicle == null)
        {
            HideLockIndicator();
            return;
        }

        if (seat == null || seat.GetCurrentArmory() != this || !EnsureLockIndicator(seat))
        {
            HideLockIndicator();
            return;
        }

        if (camera == null || !camera.isActiveAndEnabled)
        {
            HideLockIndicator();
            return;
        }

        // The indicator belongs to the vehicle, not the collider currently used to detect it.
        Vector3 targetPosition = TryGetTargetBounds(out Bounds targetBounds)
            ? targetBounds.center
            : targetVehicle.spot_position != null
                ? targetVehicle.spot_position.position
                : targetVehicle.transform.position;
        Vector3 screenPoint = camera.WorldToScreenPoint(targetPosition);
        if (screenPoint.z <= 0f || !camera.pixelRect.Contains(new Vector2(screenPoint.x, screenPoint.y)))
        {
            HideLockIndicator();
            return;
        }

        if (!TryScreenToCanvasLocal(screenPoint, camera, out Vector2 localPosition))
        {
            HideLockIndicator();
            return;
        }

        float positionBlend = 1f - Mathf.Exp(-indicatorPositionSpeed * Time.deltaTime);
        indicatorRect.anchoredPosition = indicatorRect.gameObject.activeSelf
            ? Vector2.Lerp(indicatorRect.anchoredPosition, localPosition, positionBlend)
            : localPosition;
        float transition = 1f - Mathf.Exp(-indicatorTransitionSpeed * Time.deltaTime);
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, canShoot ? 45f : 0f);
        Vector3 targetScale = Vector3.one * (canShoot ? lockedIndicatorScale : 1f);
        indicatorRect.localRotation = Quaternion.Lerp(indicatorRect.localRotation, targetRotation, transition);
        indicatorRect.localScale = Vector3.Lerp(indicatorRect.localScale, targetScale, transition);
        Color color = canShoot ? Color.red : Color.green;
        foreach (Image border in indicatorBorders) border.color = color;
        if (!indicatorRect.gameObject.activeSelf) indicatorRect.gameObject.SetActive(true);
    }

    private bool EnsureLockIndicator(VehicleSeats seat)
    {
        if (indicatorRect != null) return true;
        if (seat.seatHUD == null) return false;

        Canvas[] canvases = seat.seatHUD.GetComponentsInChildren<Canvas>(true);
        indicatorCanvas = Array.Find(canvases, canvas => canvas.renderMode == RenderMode.ScreenSpaceOverlay);
        if (indicatorCanvas == null)
            indicatorCanvas = Array.Find(canvases, canvas => canvas.renderMode == RenderMode.ScreenSpaceCamera);
        if (indicatorCanvas == null && canvases.Length > 0) indicatorCanvas = canvases[0];
        if (indicatorCanvas == null) return false;

        // Screen coordinates map to the root canvas rect, rather than a nested HUD rect.
        if (indicatorCanvas.rootCanvas.renderMode != RenderMode.WorldSpace)
            indicatorCanvas = indicatorCanvas.rootCanvas;

        indicatorCanvasRect = indicatorCanvas.transform as RectTransform;
        if (indicatorCanvasRect == null) return false;

        GameObject areaObject = new GameObject("Missile Lock Area", typeof(RectTransform));
        areaObject.layer = indicatorCanvas.gameObject.layer;
        lockAreaRect = areaObject.GetComponent<RectTransform>();
        lockAreaRect.SetParent(indicatorCanvasRect, false);
        lockAreaRect.anchorMin = lockAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        lockAreaRect.pivot = new Vector2(0.5f, 0.5f);
        CreateLockAreaCorners();
        areaObject.SetActive(false);

        GameObject indicatorObject = new GameObject("Missile Lock Indicator", typeof(RectTransform));
        indicatorObject.layer = indicatorCanvas.gameObject.layer;
        indicatorRect = indicatorObject.GetComponent<RectTransform>();
        indicatorRect.SetParent(indicatorCanvasRect, false);
        indicatorRect.anchorMin = indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRect.pivot = new Vector2(0.5f, 0.5f);
        indicatorRect.sizeDelta = new Vector2(indicatorSize, indicatorSize);
        indicatorRect.SetAsLastSibling();

        indicatorBorders = new Image[4];
        CreateIndicatorBorder(0, "Top", new Vector2(indicatorSize, indicatorLineWidth),
            new Vector2(0f, indicatorSize * 0.5f));
        CreateIndicatorBorder(1, "Bottom", new Vector2(indicatorSize, indicatorLineWidth),
            new Vector2(0f, -indicatorSize * 0.5f));
        CreateIndicatorBorder(2, "Left", new Vector2(indicatorLineWidth, indicatorSize),
            new Vector2(-indicatorSize * 0.5f, 0f));
        CreateIndicatorBorder(3, "Right", new Vector2(indicatorLineWidth, indicatorSize),
            new Vector2(indicatorSize * 0.5f, 0f));

        indicatorObject.SetActive(false);
        return true;
    }

    private bool TryScreenToCanvasLocal(Vector2 screenPoint, Camera camera, out Vector2 localPosition)
    {
        if (indicatorCanvas.renderMode != RenderMode.WorldSpace)
        {
            // Screen-space HUD placement must not depend on the vehicle's world pose.
            // Intersecting a ray with the canvas plane can jitter as that plane/camera moves.
            Rect viewport = indicatorCanvas.renderMode == RenderMode.ScreenSpaceCamera &&
                indicatorCanvas.worldCamera != null
                ? indicatorCanvas.worldCamera.pixelRect
                : indicatorCanvas.pixelRect;
            Rect canvasRect = indicatorCanvasRect.rect;
            if (viewport.width <= 0f || viewport.height <= 0f ||
                canvasRect.width <= 0f || canvasRect.height <= 0f)
            {
                localPosition = default;
                return false;
            }

            localPosition = new Vector2(
                canvasRect.xMin + (screenPoint.x - viewport.xMin) / viewport.width * canvasRect.width,
                canvasRect.yMin + (screenPoint.y - viewport.yMin) / viewport.height * canvasRect.height);
            return true;
        }

        Camera uiCamera = indicatorCanvas.worldCamera != null ? indicatorCanvas.worldCamera : camera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            indicatorCanvasRect, screenPoint, uiCamera, out localPosition);
    }

    private void CreateIndicatorBorder(int index, string name, Vector2 size, Vector2 position)
    {
        GameObject borderObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        borderObject.layer = indicatorCanvas.gameObject.layer;
        RectTransform borderRect = borderObject.GetComponent<RectTransform>();
        borderRect.SetParent(indicatorRect, false);
        borderRect.anchorMin = borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = size;
        borderRect.anchoredPosition = position;
        Image border = borderObject.GetComponent<Image>();
        border.raycastTarget = false;
        border.color = Color.green;
        indicatorBorders[index] = border;
    }

    private void CreateLockAreaCorners()
    {
        float lineWidth = indicatorLineWidth;
        float length = lockAreaCornerLength;
        CreateLockAreaLine("Top Left Horizontal", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(length, lineWidth));
        CreateLockAreaLine("Top Left Vertical", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(lineWidth, length));
        CreateLockAreaLine("Top Right Horizontal", Vector2.one, Vector2.one, new Vector2(length, lineWidth));
        CreateLockAreaLine("Top Right Vertical", Vector2.one, Vector2.one, new Vector2(lineWidth, length));
        CreateLockAreaLine("Bottom Left Horizontal", Vector2.zero, Vector2.zero, new Vector2(length, lineWidth));
        CreateLockAreaLine("Bottom Left Vertical", Vector2.zero, Vector2.zero, new Vector2(lineWidth, length));
        CreateLockAreaLine("Bottom Right Horizontal", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(length, lineWidth));
        CreateLockAreaLine("Bottom Right Vertical", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(lineWidth, length));
    }

    private void CreateLockAreaLine(string name, Vector2 corner, Vector2 pivot, Vector2 size)
    {
        GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.layer = indicatorCanvas.gameObject.layer;
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.SetParent(lockAreaRect, false);
        lineRect.anchorMin = lineRect.anchorMax = corner;
        lineRect.pivot = pivot;
        lineRect.sizeDelta = size;
        lineRect.anchoredPosition = Vector2.zero;
        Image line = lineObject.GetComponent<Image>();
        line.color = lockAreaColor;
        line.raycastTarget = false;
    }

    private void HideLockArea()
    {
        if (lockAreaRect != null && lockAreaRect.gameObject.activeSelf)
            lockAreaRect.gameObject.SetActive(false);
    }

    private void HideLockIndicator()
    {
        if (indicatorRect != null && indicatorRect.gameObject.activeSelf)
            indicatorRect.gameObject.SetActive(false);
    }

    float soundTimer = 0;
    private void HandleLockingInSound()
    {
        if (lockingInSoundDelay == 0) return;

        soundTimer += Time.deltaTime;

        if (soundTimer >= lockingInSoundDelay)
        {
            SoundManager.Play2dSoundLocal(lockingInSound.clip, lockingInSound.properties);
            soundTimer = 0;
        }
    }

    protected override bool CanPlayFireAudio() => canShoot;

    protected override void ExecuteShot()
    {
        if (!canShoot || targetVehicle == null) return;
        Camera camera = GetLockCamera(vehicle != null ? vehicle.currentSeat : null);
        if (camera == null || !IsTargetVisible(camera, targetVehicle))
        {
            ResetLock();
            return;
        }
        int spawnIndex = currentSpawnPointShootIndex.Value;
        if (initializeDummyMissiles) RequestActivateDummyMissile(spawnIndex, false);

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = spawnPoints[spawnIndex].position,
            rotation = spawnPoints[spawnIndex].rotation,
            ignoredObject = transform.root,
            root = transform.root.gameObject,
            target = currentTarget.GetComponent<NetworkObject>()
        };

        if (ProjectileSpawner.Instance != null)
        {
            Vehicle firingVehicle = vehicle != null ? vehicle : GetComponentInParent<Vehicle>();
            float initialSpeed = properties.projectileValues.muzzleVelocity;
            if (firingVehicle != null && firingVehicle.rb != null) initialSpeed += firingVehicle.rb.linearVelocity.magnitude;

            Projectile.ProjectileValues shotValues = properties.projectileValues.WithMuzzleVelocity(initialSpeed);
            ProjectileSpawner.Instance.CreateProjectile(properties.bulletPref, properties.dummyBullet.gameObject, prop, shotValues);
        }

        PlayShotEffects();
        UpdateAmmoAfterShot();
        UpdateCurrentSpawnPointShootIndex();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Transform castReference = lockInFowardReference != null ? lockInFowardReference : transform;
        Vector3 origin = castReference.position;
        Camera camera = GetLockCamera(vehicle != null ? vehicle.currentSeat : null);
        if (camera == null) camera = armoryCamera;
        if (camera == null) camera = castReference.GetComponent<Camera>();

        // Project the same HUD corners into the detection volume, clipped to the range.
        if (camera != null && maxLockDistance > 0f)
        {
            Rect area = GetLockAreaScreenRect(camera);
            Vector2[] corners =
            {
                new Vector2(area.xMin, area.yMin), new Vector2(area.xMin, area.yMax),
                new Vector2(area.xMax, area.yMax), new Vector2(area.xMax, area.yMin)
            };
            Vector3[] endPoints = new Vector3[4];
            Gizmos.color = Color.yellow;
            for (int i = 0; i < corners.Length; i++)
            {
                Ray ray = camera.ScreenPointToRay(corners[i]);
                Vector3 offset = ray.origin - origin;
                float alongRay = Vector3.Dot(offset, ray.direction);
                float discriminant = alongRay * alongRay - offset.sqrMagnitude +
                    maxLockDistance * maxLockDistance;
                if (discriminant < 0f) return;
                float distance = -alongRay + Mathf.Sqrt(discriminant);
                if (distance < 0f) return;
                endPoints[i] = ray.GetPoint(distance);
                Gizmos.DrawLine(ray.origin, endPoints[i]);
            }
            for (int i = 0; i < endPoints.Length; i++)
                Gizmos.DrawLine(endPoints[i], endPoints[(i + 1) % endPoints.Length]);
        }

        // Desenha o alvo se existir
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 3f);

            // Mostra o tempo de lock atual
            UnityEditor.Handles.Label(
                currentTarget.position + Vector3.up * 5f,
                $"Lock: {currentLockTimer:F1}s / {lockOnTimeRequired:F1}s"
            );
        }

        // Mostra informações no editor
        UnityEditor.Handles.Label(
            origin + Vector3.up * 3f,
            $"Lock Area: {lockAreaScreenFraction:P0}\nMax Distance: {maxLockDistance}\nCan Shoot: {canShoot}"
        );
    }
#endif

}
