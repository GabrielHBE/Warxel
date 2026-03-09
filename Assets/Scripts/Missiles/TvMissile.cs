using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TvMissile : Missiles
{
    [SerializeField] private Camera missile_camera;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private Volume volume;
    private Camera original_camera;

    [Header("Post-Processing Effects")]
    [SerializeField] private Texture filmGrainTexture;


    protected override void Update()
    {


        if (!didShoot) return;

        DestroyTimer();

        // Garante que a câmera seja restaurada quando o tempo acabar
        if (time_to_explode <= 0)
        {
            RestoreOriginalCamera();
            //RemoveFilmGrainEffect();
        }


        float mouseX = Math.Clamp(Input.GetAxis("Mouse X"), -turnSpeed, turnSpeed) * (turnSpeed / 2);
        float mouseY = Math.Clamp(Input.GetAxis("Mouse Y"), -turnSpeed, turnSpeed) * (turnSpeed / 2);

        rb.AddTorque(transform.forward * -mouseX / 15, ForceMode.Force);
        rb.AddTorque(transform.right * -mouseY, ForceMode.Force);

        transform.position += transform.forward * travel_speed * Time.deltaTime;

    }

    protected override void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject != parent_gameobject)
        {
            RestoreOriginalCamera();
            //RemoveFilmGrainEffect();
            base.OnCollisionEnter(collision);
        }

    }
    private void RestoreOriginalCamera()
    {
        if (original_camera != null)
        {
            original_camera.enabled = true;
        }

        if (missile_camera != null)
        {
            missile_camera.enabled = false;
        }
    }

    public void Shoot(Camera original_camera)
    {
        // IMPORTANTE: Salva a referência da câmera original
        transform.SetParent(null);
        this.original_camera = original_camera;

        //AddFilmGrainEffect();

        // Ativa a câmera do míssil
        if (missile_camera != null)
        {
            missile_camera.enabled = true;
        }

        didShoot = true;


        CreateSound(shoot_sound);
        missile_collider.enabled = true;

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.isKinematic = false;
            rb.useGravity = false;
        }

        trail.gameObject.SetActive(true);
    }


}
