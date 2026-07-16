using FishNet.Object;
using UnityEngine;

public class PlayerNetworkObjectSpawner : NetworkBehaviour
{
    #region  Gadget
    [ServerRpc(RequireOwnership = false)]
    public void ServerSpawnAirStrike(GameObject airStrikePrefab, Vector3 goToPos)
    {
        Vector3 pos = new Vector3(Random.Range(-500, 500), MapSettings.Instance.max_altitude, Random.Range(-500, 500));
        GameObject instantiatedAirStrike = Instantiate(airStrikePrefab, pos, Quaternion.identity);
        Spawn(instantiatedAirStrike);

        AirStrikeMissile airStrikeMissile = instantiatedAirStrike.GetComponent<AirStrikeMissile>();
        if (airStrikeMissile != null)
        {
            airStrikeMissile.EnableMissile(goToPos);
        }

    }
    #endregion
}