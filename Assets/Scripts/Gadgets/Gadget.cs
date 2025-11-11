using UnityEngine;

public interface Gadget
{
    void SetActive(bool is_active);
    float GetBobWalkExageration();
    float GetBobSprintExageration();
    float GetBobCrouchExageration();
    float GetBobAimExageration();
    Vector3 GetWalkMultiplier();
    Vector3 GetSprintMultiplier();
    Vector3 GetAimMultiplier();
    Vector3 GetCrouchMultiplier();
    float[] GetVector3Values();
    float[] GetQuaternionValues();
    Transform GetTransform();

}
