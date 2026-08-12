using UnityEngine;

public class ProcessInfantryDamage : MonoBehaviour
{
    private PlayerController playerController;
    [SerializeField] private LimbMultiplier limbMultiplier;

    public void Damage(float dmg) => playerController.TakeDamage(dmg);
    public LimbMultiplier GetLimbMultiplier() => limbMultiplier;
    public bool IsPlayerDead() => playerController.playerProperties.is_dead.Value;
    public float GetResistance() => playerController.playerProperties.resistance.Value;
    public string GetPlayerName() => playerController.playerProperties.playerName.Value;
    public float GetHP() => playerController.playerProperties.hp.Value;
    public void SetPlayerController(PlayerController playerController) => this.playerController = playerController;

    public enum LimbMultiplier
    {
        Torso,
        Leg,
        Arm,
        Hand,
        Foot,
        Head
    }
}
