using FishNet.Object;
using UnityEngine;

public class ParticlesBehaviour : MonoBehaviour
{
    [SerializeField] protected float destroyTimer = 5f;
    protected virtual void Awake()
    {
        Destroy(gameObject, destroyTimer);
    }

}