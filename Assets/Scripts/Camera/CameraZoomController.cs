using UnityEngine;

public class CameraZoomController : CameraModifiers
{
    [Header("Instances")]
    [SerializeField] private Camera _camera;


    [Header("Smooth Settings")]
    [SerializeField, Min(0f)] private float fovLerpSpeed = 12f;

    private float zoomMultiplier = 1f;
    private bool zoomRequested;
    private float baseFov;

    public void SetZoomMultiplier(float zoomMultiplier) => this.zoomMultiplier = zoomMultiplier > 0f ? zoomMultiplier : 1f;
    public void SetBaseFOV(float fov)
    {
        baseFov = fov;
        _camera.fieldOfView = baseFov;
    }
    public override void SetActive(bool state) => zoomRequested = state;

    private void LateUpdate()
    {
        if (_camera == null || Settings.Instance == null) return;

        float targetFov = zoomRequested ? baseFov / zoomMultiplier : baseFov;

        _camera.fieldOfView = Mathf.Lerp(
            _camera.fieldOfView,
            targetFov,
            1f - Mathf.Exp(-fovLerpSpeed * Time.deltaTime)
        );
    }
}