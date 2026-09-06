using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

public class FirstPersonArms : InMatchClientSingleton<FirstPersonArms>
{
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Ik Rigs")]
    [SerializeField] private TwoBoneIKConstraint rightHandRig;
    [SerializeField] private TwoBoneIKConstraint leftHandRig;
    private const float CHANGE_WEIGHT_TIMER = 0.1f;

    [Header("Animation")]
    [SerializeField] private Animator anim;

    private Coroutine rightHandCoroutine;
    private Coroutine leftHandCoroutine;
    private Coroutine changeRigWeightCoroutine;
    private Coroutine animationRoutine;
    private PlayableGraph currentGraph;

    public void MoveRightHand(Transform destiny, float duracao)
    {
        if (destiny == null)
        {
            Debug.LogWarning("The target transform is null!");
            return;
        }
        if (rightHandCoroutine != null) StopCoroutine(rightHandCoroutine);

        rightHandCoroutine = StartCoroutine(Interpolate(rightHand, destiny, duracao));
    }

    public void MoveLeftHand(Transform destiny, float duracao)
    {
        if (destiny == null)
        {
            Debug.LogWarning("The target transform is null!");
            return;
        }

        if (leftHandCoroutine != null) StopCoroutine(leftHandCoroutine);

        leftHandCoroutine = StartCoroutine(Interpolate(leftHand, destiny, duracao));
    }

    private IEnumerator Interpolate(Transform origin, Transform target, float duration)
    {
        float timer = 0f;

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

    public void StartAnimation(AnimationClip clip, float duration = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("The animation clip is null!");
            return;
        }

        // Prevenção contra divisão por zero ou durações negativas
        if (duration <= 0f)
        {
            Debug.LogWarning("The duration must be greater than zero. Adjusting it to 0.01f.");
            duration = 0.01f;
        }

        if (animationRoutine != null) StopCoroutine(animationRoutine);

        if (changeRigWeightCoroutine != null) StopCoroutine(changeRigWeightCoroutine);
        changeRigWeightCoroutine = StartCoroutine(ChangeRigWeight(0));

        if (currentGraph.IsValid()) currentGraph.Destroy();

        // PlayClip retorna o AnimationClipPlayable, que nos permite modificar a velocidade
        var clipPlayable = AnimationPlayableUtilities.PlayClip(anim, clip, out currentGraph);

        // Calcula a velocidade: se o clip tem 2s e queremos que rode em 1s, a velocidade será 2x.
        float speedMultiplier = clip.length / duration;
        clipPlayable.SetSpeed(speedMultiplier);

        // Agora esperamos o tempo do 'duration' em vez do tamanho original do clipe
        animationRoutine = StartCoroutine(WaitAndStopAnimation(duration));
    }

    private IEnumerator WaitAndStopAnimation(float duration)
    {
        yield return new WaitForSeconds(duration);

        StopAnimation();

        animationRoutine = null;
    }

    private void StopAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        // 1. Destroi a Playable Graph atual
        if (currentGraph.IsValid()) currentGraph.Destroy();

        // 2. Reseta o Animator e força a avaliação da pose base no mesmo frame
        anim.Rebind();
        anim.Update(0f);

        // 3. Suaviza a volta do peso do Rig para 1
        if (changeRigWeightCoroutine != null) StopCoroutine(changeRigWeightCoroutine);

        changeRigWeightCoroutine = StartCoroutine(ChangeRigWeight(1f));
    }

    private IEnumerator ChangeRigWeight(float targetWeight)
    {
        float timer = 0f;
        float startRightWeight = rightHandRig.weight;
        float startLeftWeight = leftHandRig.weight;

        while (timer < CHANGE_WEIGHT_TIMER)
        {
            timer += Time.deltaTime;
            float progress = timer / CHANGE_WEIGHT_TIMER;

            rightHandRig.weight = Mathf.Lerp(startRightWeight, targetWeight, progress);
            leftHandRig.weight = Mathf.Lerp(startLeftWeight, targetWeight, progress);

            yield return null;
        }

        // Garante o peso exato no final
        rightHandRig.weight = targetWeight;
        leftHandRig.weight = targetWeight;
    }
}
