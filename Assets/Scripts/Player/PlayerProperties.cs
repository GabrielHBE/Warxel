using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayerProperties : NetworkBehaviour
{
    private const float INFANTRY_HP = 100;

    public readonly SyncVar<string> playerName = new SyncVar<string>();
    public readonly SyncVar<ClassManager.Class> selectedClass = new SyncVar<ClassManager.Class>();
    public readonly SyncVar<FactionManager.Faction> faction = new SyncVar<FactionManager.Faction>();
    public readonly SyncVar<bool> isDead = new SyncVar<bool>() { Value = false };
    public readonly SyncVar<float> hp = new SyncVar<float>() { Value = INFANTRY_HP };
    public readonly SyncVar<float> resistance = new SyncVar<float>();
    public readonly SyncVar<bool> spotted = new SyncVar<bool>();
    public bool crouched;
    public bool sprinting;
    public bool aiming;
    public bool reloading;
    public bool firing;
    public bool grounded;
    public bool proned;
    public bool isProneTransition;
    public bool applyProneImpulse;
    public bool roll;
    public bool isComposingBullets;
    public float proneImpulseLockTime;
    public float maxHp => INFANTRY_HP;
    public bool isInVehicle;
    public float deathTimer => 15;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner && AccountManager.Instance.selectedClass == ClassManager.Class.Support) resistance.Value = 25;
    }
}