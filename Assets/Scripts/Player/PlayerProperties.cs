using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerProperties : NetworkBehaviour
{
    public readonly SyncVar<string> player_name = new SyncVar<string>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    public ClassManager.Class selected_class;
    public readonly SyncVar<FactionManager.Faction> faction = new SyncVar<FactionManager.Faction>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    public bool crouched;
    public bool sprinting;
    public bool is_aiming;
    public bool is_reloading;
    public bool is_firing;
    public bool isGrounded;
    public bool is_proned;
    public bool isProneTransition;
    public bool applyProneImpulse;
    public bool roll;
    public bool is_composing_bullets;
    public float proneImpulseLockTime;
    public readonly SyncVar<bool> is_dead = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    public readonly SyncVar<float> hp = new SyncVar<float>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    public readonly SyncVar<float> resistance = new SyncVar<float>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    public float max_hp;
    public bool is_in_vehicle;
    public float death_timer;
    public readonly SyncVar<bool> spotted = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));

    public override void OnStartClient()
    {
        base.OnStartClient();

        hp.Value = 100;
        is_dead.Value = false;
        if (IsOwner)
        {
            if (AccountManager.Instance.selected_class == ClassManager.Class.Support)
            {
                resistance.Value = 25;
            }
        }

    }

}

