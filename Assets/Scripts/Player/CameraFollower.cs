using System.Collections;
using UnityEngine;
public class CameraFollower : MonoBehaviour
{
    [SerializeField] private Transform parent;
    [SerializeField] private GameObject neck;
    [SerializeField] private float position;
    [SerializeField] private PlayerProperties playerProperties;

    Quaternion original_rotation;

    void Start()
    {
        original_rotation = transform.localRotation;
    }

    private bool wasRolling = false;
    private bool wasDead = false;


    void Update()
    {

        bool setparent = playerProperties.roll || playerProperties.is_dead;

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

        transform.position = new Vector3(
                neck.transform.position.x,
                neck.transform.position.y + position,
                neck.transform.position.z + 0.01f
            );

        // Atualiza os estados anteriores
        wasRolling = playerProperties.roll;
        wasDead = playerProperties.is_dead;

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