using System.Collections;
using UnityEngine;

public class FirstPersonArms : MonoBehaviour
{
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private GameObject first_person_player_components;

    private Coroutine rightHandCoroutine;
    private Coroutine leftHandCoroutine;

    public void Enable()
    {
        first_person_player_components.SetActive(true);
    }

    public void Disable()
    {
        first_person_player_components.SetActive(false);
    }

    public void MoveRightHand(Transform destiny, float duracao)
    {
        if (destiny == null)
        {
            Debug.LogWarning("O transform de destino é nulo!");
            return;
        }

        if (rightHandCoroutine != null)
        {
            StopCoroutine(rightHandCoroutine);
        }
        rightHandCoroutine = StartCoroutine(Interpolate(rightHand, destiny, duracao));
    }

    public void MoveLeftHand(Transform destiny, float duracao)
    {
        if (destiny == null)
        {
            Debug.LogWarning("O transform de destino é nulo!");
            return;
        }

        if (leftHandCoroutine != null)
        {
            StopCoroutine(leftHandCoroutine);
        }
        leftHandCoroutine = StartCoroutine(Interpolate(leftHand, destiny, duracao));
    }

    private IEnumerator Interpolate(Transform origin, Transform target, float duration)
    {
        float timer = 0f;

        duration *= 3;

        if (duration > 0)
        {
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float porcentagem = timer / duration;

                origin.position = Vector3.Lerp(origin.position, target.position, porcentagem);
                origin.rotation = Quaternion.Lerp(origin.rotation, target.rotation, porcentagem);

                yield return null;
            }
        }

        while (true)
        {
            origin.position = target.position;
            origin.rotation = target.rotation;

            yield return null;
        }
    }
}