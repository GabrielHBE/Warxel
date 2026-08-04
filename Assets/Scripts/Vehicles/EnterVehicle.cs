using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EnterVehicle : InteractiveButton
{
    [SerializeField] private Vehicle vehicle;

    void Start() => GetComponent<BoxCollider>().isTrigger = true;
    
    public override void Interact(PlayerController player)
    {
        player.ResetWeaponAnimation();
        RequestEnterVehicle(player);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEnterVehicle(PlayerController player)
    {
        if (vehicle == null || !vehicle.IsSpawned) return;

        NetworkObject conn = player.GetComponent<NetworkObject>();
        //vehicle.NetworkObject.GiveOwnership(Owner);
        vehicle.EnterVehicle(conn.Owner, player.gameObject);
    }
}
