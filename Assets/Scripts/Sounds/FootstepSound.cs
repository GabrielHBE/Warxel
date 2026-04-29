using FishNet.Object;
using UnityEngine;

public class FootstepSound : NetworkBehaviour
{
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private LayerMask layers;
    [SerializeField] private float raycast_distance;
    [Header("Concrete")]
    [SerializeField] private AudioClip[] concrete_steps;

    [Header("Grass")]
    [SerializeField] private AudioClip[] grass_steps;

    [Header("Sand / Dirt")]
    [SerializeField] private AudioClip[] sand_dirt_steps;

    private Steps current_step;
    public float original_volume;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner) audioSource.volume = 0.5f;

        original_volume = audioSource.volume;
    }

    void Update()
    {   
        if(!IsOwner) return;
        
        if (playerProperties.crouched || playerProperties.is_proned)
        {
            original_volume = audioSource.volume / 2;
        }
        else
        {
            original_volume = audioSource.volume;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, raycast_distance, layers))
        {
            if (hit.transform.tag == "Concrete")
            {
                current_step = Steps.Concrete;
            }
            else if (hit.transform.tag == "Sand / Dirt")
            {
                current_step = Steps.Sand_dirt;
            }
            else if (hit.transform.tag == "Grass")
            {
                current_step = Steps.Grass;
            }
        }
    }

    private AudioClip GetSound()
    {
        int i = 0;
        if (current_step == Steps.Concrete)
        {
            i = Random.Range(0, concrete_steps.Length);
            return concrete_steps[i];

        }
        else if (current_step == Steps.Grass)
        {
            i = Random.Range(0, grass_steps.Length);
            return grass_steps[i];
        }
        else if (current_step == Steps.Sand_dirt)
        {
            i = Random.Range(0, sand_dirt_steps.Length);
            return sand_dirt_steps[i];
        }

        i = Random.Range(0, grass_steps.Length);

        return grass_steps[i];
    }

    // 1. The Client calls this method to ask the server to play the sound
    [ServerRpc(RequireOwnership = false)]
    public void CmdPlayStepSound()
    {
        RpcPlayStepSound();
    }

    // 2. The Server calls this method to broadcast to all clients
    [ObserversRpc]
    private void RpcPlayStepSound()
    {
        AudioClip audioClip = GetSound();
        if (audioClip == null) return;
        audioSource.clip = audioClip;
        audioSource.PlayOneShot(audioClip);
    }


    private enum Steps
    {
        Concrete,
        Grass,
        Sand_dirt

    }

}

