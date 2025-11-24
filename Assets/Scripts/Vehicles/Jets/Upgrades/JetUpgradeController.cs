using UnityEngine;

public interface JetUpgradeController
{
    void UseCamera(bool active);
    void Shoot();
    bool CanShoot();
    void SetActive(bool active);

}
