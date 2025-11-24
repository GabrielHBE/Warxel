using System;
using Unity.VisualScripting;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class C4Explosive : MonoBehaviour
{
    [SerializeField] private GameObject smokeEffect;
    [SerializeField] private C4Detonator c4;
    [SerializeField] private GameObject throw_hand;
    [SerializeField] private GameObject throw_hand_original_pos;
    [SerializeField] private AudioSource explosion_sound;
    [SerializeField] private AudioSource beepSound;

    public float explosionRadius = 10f;
    public float explosionForce = 20f;

    public DestructionData.DestructionType destructionType = DestructionData.DestructionType.Sphere;

    

    void CreateSound()
    {
        GetComponent<MeshRenderer>().enabled = false;


        GameObject soundObject = new GameObject("TemporarySoundObject");

        soundObject.transform.position = transform.position;
        soundObject.transform.rotation = transform.rotation;

        AudioSource tempAudioSource = soundObject.AddComponent<AudioSource>();

        tempAudioSource.clip = explosion_sound.clip;
        tempAudioSource.volume = explosion_sound.volume;
        tempAudioSource.pitch = explosion_sound.pitch;
        tempAudioSource.spatialBlend = explosion_sound.spatialBlend;
        tempAudioSource.maxDistance = explosion_sound.maxDistance;
        tempAudioSource.rolloffMode = explosion_sound.rolloffMode;

        tempAudioSource.Play();

        Destroy(soundObject, tempAudioSource.clip.length);
    }

    public void Detonate()
    {
        bool do_once = true;
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        for (int i = 0; i < colliders.Length; i++)
        {
            PlayerProperties player = colliders[i].GetComponentInParent<PlayerProperties>();

            if (do_once && player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                float damage = Mathf.Clamp(explosionForce * (1 - (distance / explosionRadius)), 0, explosionForce);
                player.Damage(damage * 8);

                CameraShake cameraShake = player.GetComponentInChildren<CameraShake>();
                if (cameraShake != null)
                {

                    cameraShake.StartCoroutine(cameraShake.ExplosionShake(damage / 10, 1f));
                }

                do_once = false;
            }

            DynamicVoxelObj vox = colliders[i].GetComponentInParent<DynamicVoxelObj>();

            if (vox == null)
                continue;

            if (destructionType == DestructionData.DestructionType.Sphere)
                vox.AddDestruction_Sphere(transform.position, explosionForce);
            else
                vox.AddDestruction_Cube(transform.position, explosionForce);
        }
        Instantiate(smokeEffect, transform.position, Quaternion.identity);

        CreateSound();
        Destroy(gameObject);
    }



    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Detonate();

        }
        else if (!collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Player"))
        {

            // Destroy Rigidbody to prevent further physics interactions
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
                Destroy(rb);

            transform.parent = collision.transform;

        }


    }

}