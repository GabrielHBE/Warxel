using System.Collections;
using UnityEngine;
public class CameraFollower : MonoBehaviour
{
    [SerializeField] private Transform parent;
    [SerializeField] private GameObject neck;
    [SerializeField] private float position;
    [SerializeField] private PlayerProperties playerProperties;
    Quaternion original_rotation;
    private bool wasRolling = false;
    private bool wasDead = false;

    void Start() => original_rotation = transform.localRotation;

    void LateUpdate()
    {

        bool setparent = playerProperties.roll || playerProperties.is_dead.Value;

        // Verifica se o estado mudou
        if (setparent && !(wasRolling || wasDead))
        {
            // Entrou no estado de roll/dead
            transform.SetParent(neck.transform);
        }
        else if (!setparent && (wasRolling || wasDead))
        {
            // Saiu do estado de roll/dead
            transform.SetParent(parent);

            StartCoroutine(ResetRotation());
        }

        transform.position = neck.transform.position;
        wasRolling = playerProperties.roll;
        wasDead = playerProperties.is_dead.Value;

    }

    private IEnumerator ResetRotation()
    {
        while (transform.localRotation != original_rotation)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, original_rotation, Time.deltaTime * 7);
            yield return null;
        }

        transform.localRotation = original_rotation;
    }
}