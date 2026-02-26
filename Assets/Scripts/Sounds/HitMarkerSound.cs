using UnityEngine;

public class HitMarkerSound : MonoBehaviour
{
    [SerializeField] private AudioSource body_hit;
    [SerializeField] private AudioSource head_hit;
    [SerializeField] private AudioSource vehicle_hit;

    private Audio volume;

    void Start()
    {
        volume = GameObject.FindGameObjectWithTag("Settings").GetComponent<Audio>();
    }

    public void CrateBodyHitMarkerSound()
    {
        if (body_hit == null) return;
        body_hit.volume = volume.hit_volume;
        body_hit.PlayOneShot(body_hit.clip);
    }

    public void CreateHeadShotMarkerSound()
    {
        if (head_hit == null) return;
        head_hit.volume = volume.hit_volume;
        head_hit.PlayOneShot(head_hit.clip);
    }

    public void CreateVehicleShotMarkerSound()
    {
        if (vehicle_hit == null) return;
        vehicle_hit.volume = volume.hit_volume;
        vehicle_hit.PlayOneShot(vehicle_hit.clip);
    }


}
