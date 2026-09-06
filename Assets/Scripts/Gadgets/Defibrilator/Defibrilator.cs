using UnityEngine;

public class Defibrilator : Gadget
{
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask hitLayer;

    [Header("Sounds")]
    [SerializeField] private SoundManager.SoundComponents holdingButtonSound;
    [SerializeField] private SoundManager.SoundComponents reviveButtonSound;

    [Header("Sparkle Effects")]
    [SerializeField] private ParticleSystem leftDefibParticle;
    [SerializeField] private ParticleSystem rightDefibParticle;

    private float hpToRestore;
    private const float CHARGE_TIMER = 2;
    private bool canChangeUp;

    public override void Initialize()
    {
        base.Initialize();
        equippableItemAnimator.Setup();
    }

    public override void Restart()
    {
        base.Restart();
        adsBehaviour.DisableAim();
    }

    void Update()
    {
        if(!is_active) return;
        
        bool isHoldingLeftClick = InputManager.GetKey(Settings.Instance._keybinds.WEAPON_shootKey);

        animator.SetBool("Is_firing", isHoldingLeftClick);

        if (isHoldingLeftClick && canChangeUp)
        {
            // Garante que não divida por zero caso chargeTimer seja configurado como 0 no Inspector
            hpToRestore = Mathf.Clamp(hpToRestore + ((100f / CHARGE_TIMER) * Time.deltaTime), 5f, 100f);
        }

        soldierHudManager.currentHeat = hpToRestore;
        soldierHudManager.SetCurrentAmmo("");
    }

    public void Activate()
    {
        cameraShake.RequestShake();
        if (leftDefibParticle != null) leftDefibParticle.Play();
        if (rightDefibParticle != null) rightDefibParticle.Play();

        if (reviveButtonSound.clip != null)
        {
            SoundManager.Instance.RequestPlay3dSound(reviveButtonSound.clip.name, reviveButtonSound.properties, transform.position, false);
            SoundManager.Play2dSoundLocal(reviveButtonSound.clip, reviveButtonSound.properties);
        }

        RaycastHit[] hits = Physics.SphereCastAll(playerController.playerCamera.transform.position, InteractiveButton.INTERACT_RADIOUS, playerController.playerCamera.transform.forward, InteractiveButton.INTERACT_DISTANCE, hitLayer);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            SkinPart skinPart = hit.transform.gameObject.GetComponent<SkinPart>();
            if (skinPart == null) continue;

            PlayerController hitPlayerController = skinPart.playerController;
            if (hitPlayerController == null) continue;

            if (hitPlayerController == playerController) continue;

            if ( skinPart.GetPlayerFaction() == AccountManager.Instance.selectedFaction)
            {
                if (skinPart.IsPlayerDead()) skinPart.RequestRevive(hpToRestore,  hit.transform.position + Vector3.up * 1.5f);
            }
            else ProcessHit.PlayerHit(skinPart.gameObject, hpToRestore, 2, gameObject);

            break;
        }

        DisableChangeUp();

        hpToRestore = 0f;
    }

    public void EnableChangeUp() => canChangeUp = true;
    
    public void DisableChangeUp() => canChangeUp = false;
}