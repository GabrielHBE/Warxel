using UnityEngine;

public class CameraZoomController : MonoBehaviour
{
    [Header("Instances")]
    [SerializeField] private Camera _camera;
    [SerializeField] private PlayerProperties playerProperties;

    [Header("Smooth Settings")]
    [SerializeField, Min(0f)] private float fovLerpSpeed = 12f;

    private float zoomMultiplier = 1f;
    private bool zoomRequested;
    private float baseFov;

    public void SetZoomMultiplier(float zoomMultiplier) =>  this.zoomMultiplier = zoomMultiplier > 0f ? zoomMultiplier : 1f;
    public void SetBaseFOV(float fov) => baseFov = fov;
    public void ZoomIn() => zoomRequested = true;
    public void ZoomOut() => zoomRequested = false;
    
    private void LateUpdate()
    {
        if (_camera == null || Settings.Instance == null) return;
        if (playerProperties != null && playerProperties.isInVehicle) return;

        bool canApplyZoom = zoomRequested && (playerProperties == null || (playerProperties.aiming && !playerProperties.reloading));
        float targetFov = canApplyZoom ? baseFov / zoomMultiplier : baseFov;

        _camera.fieldOfView = Mathf.Lerp(
            _camera.fieldOfView,
            targetFov,
            1f - Mathf.Exp(-fovLerpSpeed * Time.deltaTime)
        );
    }


}
