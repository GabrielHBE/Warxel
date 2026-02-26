using UnityEngine;

public class TankTracksController : MonoBehaviour
{

    [System.Serializable]
    public class TrackWheel
    {
        public WheelCollider wheelCollider;
        public Transform visualWheel;
        public bool isDriveWheel; // Roda motriz
    }

    public TrackWheel[] trackWheels;
    public Transform[] trackSegments; // Ou um LineRenderer para a esteira

    void Update()
    {
        foreach (TrackWheel trackWheel in trackWheels)
        {
            if (trackWheel.visualWheel != null)
            {
                // Atualiza rotação da roda visual baseada no WheelCollider
                trackWheel.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
                trackWheel.visualWheel.rotation = rot;

                // Se for roda motriz, calcula movimento da esteira
                if (trackWheel.isDriveWheel)
                {
                    float wheelRPM = trackWheel.wheelCollider.rpm;
                    float trackSpeed = (wheelRPM * Mathf.PI * 2f) / 60f;
                    UpdateTrackPosition(trackSpeed);
                }
            }
        }
    }

    void UpdateTrackPosition(float speed)
    {
        // Implemente o movimento dos segmentos da esteira aqui
    }
}
