using FishNet.Object;
using UnityEngine;

public class EnterVehicle : InteractiveButton
{
    [SerializeField] private Vehicle vehicle;

    public override void Interact(PlayerController player)
    {

        player.ResetWeaponAnimation();
        player.DisableNightVison();

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
