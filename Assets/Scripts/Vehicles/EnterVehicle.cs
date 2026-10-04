using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EnterVehicle : InteractiveButton
{
    [SerializeField] private Vehicle vehicle;

    void Start() => GetComponent<BoxCollider>().isTrigger = true;
    
    public override void Interact(PlayerController player)
    {
        if (!IsLocallyAvailable || player == null || !player.BeginVehicleEntry()) return;

        player.ResetWeaponAnimation();
        RequestEnterVehicle(player);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEnterVehicle(PlayerController player)
    {
        if (player == null) return;

        if (vehicle == null || !vehicle.IsSpawned)
        {
            player.SetVehicleCollisionProtectionServer(false);
            return;
        }

        NetworkObject conn = player.GetComponent<NetworkObject>();
        if (conn == null)
        {
            player.SetVehicleCollisionProtectionServer(false);
            return;
        }

        //vehicle.NetworkObject.GiveOwnership(Owner);
        vehicle.EnterVehicle(conn.Owner, player.gameObject);
    }
}
