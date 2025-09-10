using UnityEngine;

public class DestroyAfterAll : MonoBehaviour
{
    private float delay;

    void Start()
    {
        delay = Random.Range(4, 6);
        Destroy(gameObject, delay);
    }
}
