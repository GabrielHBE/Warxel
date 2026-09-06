using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Collider)), RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(ProcessInfantryDamage))]
public class SkinPart : MonoBehaviour
{
    public Rigidbody rb { get; private set; }
    public Collider col { get; private set; }
    public MeshRenderer meshRenderer { get; private set; }
    public PlayerController playerController => processInfantryDamage.playerController;
    public ProcessInfantryDamage processInfantryDamage {get; private set;}

    private float timerToDestroy = 0;

    void Awake()
    {
        processInfantryDamage = GetComponent<ProcessInfantryDamage>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();
    }
    
    void Update()
    {
        if (playerController.playerProperties.isDead.Value)
        {
            timerToDestroy += Time.deltaTime;
            if(timerToDestroy >= playerController.playerProperties.deathTimer) Destroy(gameObject);
        }
        else timerToDestroy = 0;
        
    }

    public bool IsPlayerDead() => processInfantryDamage.IsPlayerDead();
    public void RequestRevive(float hp) => playerController.Revive(hp);
    public void RequestRevive(float hp, Vector3 position) => playerController.Revive(hp, position);
    public FactionManager.Faction GetPlayerFaction() => playerController.playerProperties.faction.Value;

}