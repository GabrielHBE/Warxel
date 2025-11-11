
using UnityEngine;

public interface Vehicle
{
    public void EnterVehicle(GameObject _player);
    void ExitHevicle();
    void Explode(Collider[] colliders, ContactPoint contact, Collision collision, float explosionForce);
    void Damage(float damage);
    void Start_Stop_Engine();
    void CameraController();

}
