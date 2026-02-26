using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TvMissile : Missiles
{
    [SerializeField] private Camera missile_camera;
    [SerializeField] private float turnSpeed = 5f;
    private Volume volume;
    private Camera original_camera;

    [Header("Post-Processing Effects")]
    [SerializeField] private Texture filmGrainTexture;
    [SerializeField] private float filmGrainIntensity = 0.442f;
    [SerializeField] private float filmGrainResponse = 0.496f;
    private FilmGrain filmGrainComponent;



    protected override void Start()
    {
        base.Start();
        volume = GetVolume();
    }
    protected override void Update()
    {

        if (!didShoot) return;
        DestroyTimer();

        if (time_to_explode <= 0)
        {
            original_camera.enabled = true;
            missile_camera.enabled = false;
            RemoveFilmGrainEffect();
        }

        float mouseX = Math.Clamp(Input.GetAxis("Mouse X"), -turnSpeed, turnSpeed) * (turnSpeed/2);
        float mouseY = Math.Clamp(Input.GetAxis("Mouse Y"), -turnSpeed, turnSpeed) * (turnSpeed/2);

        rb.AddTorque(transform.forward * -mouseX / 15, ForceMode.Force);
        rb.AddTorque(transform.right * -mouseY, ForceMode.Force);

        transform.position += transform.forward * travel_speed * Time.deltaTime;

    }



    protected override void OnCollisionEnter(Collision collision)
    {
        original_camera.enabled = true;
        missile_camera.enabled = false; 
        RemoveFilmGrainEffect();

        base.OnCollisionEnter(collision);

    }

    public void Shoot(Camera original_camera)
    {
        transform.SetParent(null);
        AddFilmGrainEffect();
        missile_camera.enabled = true;
        this.original_camera = original_camera;
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

    Volume GetVolume()
    {

        GameObject globalVolumeObj = GameObject.FindGameObjectWithTag("GlobalVolume");
        if (globalVolumeObj != null)
        {
            return globalVolumeObj.GetComponent<Volume>();
        }

        return null;

    }

    public void AddFilmGrainEffect()
    {
        if (volume == null || volume.profile == null) return;

        // Verificar se já existe o componente
        if (!volume.profile.TryGet<FilmGrain>(out filmGrainComponent))
        {
            filmGrainComponent = volume.profile.Add<FilmGrain>();
        
        }
        ConfigureFilmGrain();
    }

    private void ConfigureFilmGrain()
    {
        if (filmGrainComponent == null) return;
        

        // Tipo Custom
        filmGrainComponent.type.overrideState = true;
        filmGrainComponent.type.value = FilmGrainLookup.Custom;

        // Texture
        if (filmGrainTexture != null)
        {
            filmGrainComponent.texture.overrideState = true;
            filmGrainComponent.texture.value = filmGrainTexture;
        }

        // Intensity
        filmGrainComponent.intensity.overrideState = true;
        filmGrainComponent.intensity.value = filmGrainIntensity;

        // Response
        filmGrainComponent.response.overrideState = true;
        filmGrainComponent.response.value = filmGrainResponse;
    }

    // Remover efeito Film Grain
    public void RemoveFilmGrainEffect()
    {
        if (volume == null || volume.profile == null) return;

        if (volume.profile.TryGet<FilmGrain>(out filmGrainComponent))
        {
            volume.profile.Remove<FilmGrain>();
            filmGrainComponent = null;
        }
    }
}
