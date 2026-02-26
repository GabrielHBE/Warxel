using System;
using Unity.VisualScripting;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class C4Explosive : MonoBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] VoxCollider voxCollider;
    [SerializeField] private GameObject smokeEffect;
    [SerializeField] private C4Detonator c4;
    [SerializeField] private GameObject throw_hand;
    [SerializeField] private GameObject throw_hand_original_pos;
    [SerializeField] private AudioSource explosion_sound;
    [SerializeField] private AudioSource beepSound;

    public float explosionRadius = 10f;
    public float explosionForce = 20f;

    void CreateSound()
    {
        explosion_sound.transform.SetParent(null);
        explosion_sound.GetComponent<AudioDistanceController>().StartGrowth();
    }

    public void Detonate()
    {
        voxCollider.SphereExplosion(transform.position, damage);

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