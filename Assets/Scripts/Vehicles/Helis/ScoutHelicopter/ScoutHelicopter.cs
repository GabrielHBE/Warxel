using UnityEngine;

public class ScoutHelicopter : Helicopter
{
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsSpawned)
        {
            Debug.LogError($"{gameObject.name} : ScoutHelicopter not spawned in network yet");
            return;
        }

        SetHpProperties(heliProperties.hp, heliProperties.resistance);
    }

    protected override void OnVehicleEntered(int seatIndex, GameObject _player)
    {
        base.OnVehicleEntered(seatIndex, _player);

        if (currentSeat == null || currentSeat.playerController == null)
        {
            Debug.LogError("Error: Player references were not populated by the base class.");
            return;
        }
    }
}
