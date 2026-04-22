using UnityEngine;

public class HitSound : MonoBehaviour
{
    [SerializeField] private AudioClip body_hit;
    [SerializeField] private AudioClip head_hit;
    [SerializeField] private AudioClip vehicle_hit;
    [SerializeField] private AudioSource audioSource;

    public void CrateBodyHitMarkerSound()
    {
        if (body_hit == null) return;
        audioSource.PlayOneShot(body_hit);
    }

    public void CreateHeadShotMarkerSound()
    {
        if (head_hit == null) return;
        audioSource.PlayOneShot(head_hit);
    }

    public void CreateVehicleShotMarkerSound()
    {
        if (vehicle_hit == null) return;
        audioSource.PlayOneShot(vehicle_hit);
    }


}
