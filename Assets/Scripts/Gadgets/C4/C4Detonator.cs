using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class C4Detonator : Gadget
{
    [SerializeField] private C4Explosive c4Explosive;
    [SerializeField] private int c4_qtd;
    [SerializeField] private float throw_c4_force;
    [SerializeField] private float pick_up_c4_distance;
    [SerializeField] private GameObject right_hand_pos;
    [SerializeField] private AudioSource beepSound;
    private List<C4Explosive> c4_list = new List<C4Explosive>();
    [SerializeField] private Transform detonator_position;


    [Header("Detonation timing (seconds)")]
    [SerializeField] private float initialDetonateDelay;
    [SerializeField] private float perC4Delay;

    private bool isDetonating = false;
    private float detonateTimer = 0f;
    private int detonateIndex = 0;


    void Update()
    {
        if (Input.GetKeyDown(gadgetComponents.keyBinds.PLAYER_interactKey))
        {
            TryPickUpC4();
        }

        if (!is_active) return;
        if (gadgetComponents.soldierHudManager.mag_counter_hud != null) gadgetComponents.soldierHudManager.mag_counter_hud.UpdateMagCount(c4_qtd);

        gadgetComponents.left_hand.transform.position = detonator_position.position;
        gadgetComponents.right_hand.transform.position = right_hand_pos.transform.position;

        if (c4_qtd > 0 && Input.GetKeyDown(gadgetComponents.keyBinds.GADGET_throwC4Key))
        {
            Throw_C4();
        }

        if (Input.GetKeyDown(gadgetComponents.keyBinds.GADGET_detonateC4Key) && !isDetonating)
        {
            CleanupDestroyedC4s();

            if (c4_list.Count > 0)
            {
                beepSound?.Play();
                StartDetonationSequence();
            }
        }

        if (isDetonating)
        {
            detonateTimer += Time.deltaTime;

            if (detonateIndex == 0)
            {
                if (detonateTimer >= initialDetonateDelay)
                {
                    DetonateNextC4();
                }
            }
            else
            {
                if (detonateTimer >= perC4Delay)
                {
                    DetonateNextC4();
                }
            }
        }
    }

    private void StartDetonationSequence()
    {
        isDetonating = true;
        detonateTimer = 0f;
        detonateIndex = 0;
    }

    private void EndDetonationSequence()
    {
        isDetonating = false;
        detonateTimer = 0f;
        detonateIndex = 0;
        CleanupDestroyedC4s();
    }

    private void DetonateNextC4()
    {
        if (detonateIndex >= c4_list.Count || c4_list[detonateIndex] == null)
        {
            EndDetonationSequence();
            return;
        }

        C4Explosive c4ToDetonate = c4_list[detonateIndex];
        if (c4ToDetonate != null)
        {
            c4ToDetonate.Detonate();
        }

        detonateIndex++;
        detonateTimer = 0f;

        if (detonateIndex >= c4_list.Count)
        {
            EndDetonationSequence();
        }
    }

    private void CleanupDestroyedC4s()
    {
        c4_list.RemoveAll(c4 => c4 == null || c4.gameObject == null);
    }

    public void TryPickUpC4()
    {
        RaycastHit hit;

        if (Physics.Raycast(gadgetComponents.player_camera.transform.position, gadgetComponents.player_camera.transform.forward, out hit, pick_up_c4_distance))
        {
            C4Explosive c4 = hit.collider.GetComponent<C4Explosive>();
            if (c4 != null)
            {
                PickUpC4();

                if (c4_list.Contains(c4))
                {
                    c4_list.Remove(c4);
                }

                Destroy(hit.collider.gameObject);
            }
        }
    }

    private void PickUpC4()
    {
        c4_qtd += 1;
    }

    private void Throw_C4()
    {
        c4_qtd -= 1;

        GameObject c4Instance = Instantiate(c4Explosive.gameObject, gadgetComponents.player_camera.transform.position, gadgetComponents.player_camera.transform.rotation);
        C4Explosive newC4 = c4Instance.GetComponent<C4Explosive>();
        c4_list.Add(newC4);

        c4Instance.SetActive(true);

        Rigidbody rb = c4Instance.GetComponent<Rigidbody>();

        rb.AddForce(gadgetComponents.player_camera.transform.forward * throw_c4_force, ForceMode.Impulse);

    }

    /*
    private void OnDrawGizmos()
    {
        if (gadgetComponents.player_camera.transform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(gadgetComponents.player_camera.transform.position, 0.1f);
            Gizmos.DrawLine(gadgetComponents.player_camera.transform.position, gadgetComponents.player_camera.transform.position + gadgetComponents.player_camera.transform.forward * 2f);
        }
    }
    */
}