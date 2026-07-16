using UnityEngine;

public class AttackHelicopter : Helicopter
{
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsSpawned)
        {
            Debug.LogError($"{gameObject.name} : AttackHelicopter not spawned in network yet");
            return;
        }

        throttle.Value = 0;
        SetHpProperties(heliProperties.hp, heliProperties.resistance);
    }

    protected override void OnVehicleEntered(int seatIndex, GameObject _player)
    {
        base.OnVehicleEntered(seatIndex, _player);

        if (currentSeat == null || currentSeat.playerController == null)
        {
            Debug.LogError("Erro: Referências do Player não foram preenchidas pela classe base.");
            return;
        }
    }
}