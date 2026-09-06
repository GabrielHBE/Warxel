using ProcessReload;
using UnityEngine;

public class EquippableItemAnimator : MonoBehaviour
{
    [Header("Arms animation")]
    [SerializeField] private AnimationClip armReload1AnimationClip;
    [SerializeField] private AnimationClip armReload2AnimationClip;
    [SerializeField] private AnimationClip armFireAnimationClip;

    [HideInInspector] public AnimationClip fireClip;
    [HideInInspector] public bool isInFireAnimation;

    // Clipes Padrão
    private AnimationClip reload1Clip;
    private AnimationClip reload2Clip;

    // Clipes do Single Reload
    private AnimationClip startReloadClip;
    private AnimationClip reloadingClip;
    private AnimationClip finishReloadClip;

    // Novas variáveis para os clipes de guardar e sacar
    private AnimationClip storeClip;
    private AnimationClip drawClip;

    private Animator anim;
    RuntimeAnimatorController rac;
    private Coroutine fireAnimationRoutine;
    private int fireStateHash;
    private float delayToShootAnimation;
    private Reload.ReloadValues reloadValues;
    private bool changeShootAnimationSpeed;
    private Firing.FiringValues firingValues;
    private bool hasLastBulletParam;
    private Vector3 idleLocalPosition;
    private Quaternion idleLocalRotation;
    private Vector3 idleLocalScale;
    private bool hasCachedIdleTransform;

    public void Setup()
    {
        anim = GetComponent<Animator>();
        rac = anim.runtimeAnimatorController;
        CacheIdleTransform();

        SetFiringAnimation();
        SetReload1Animation();
        SetReload2Animation();
        SetSingleReloadAnimations();

        SetStoreAndDrawAnimations();

        hasLastBulletParam = HasAnimatorParameter("Last_bullet", AnimatorControllerParameterType.Bool);
        ResetFireAnimationState();
    }

    public void Setup(float delayToShootAnimation, Reload.ReloadValues reloadValues, bool changeShootAnimationSpeed, Firing.FiringValues firingValues)
    {
        this.delayToShootAnimation = delayToShootAnimation;
        this.reloadValues = reloadValues;
        this.changeShootAnimationSpeed = changeShootAnimationSpeed;
        this.firingValues = firingValues;

        Setup();
    }

    private void SetStoreAndDrawAnimations()
    {
        foreach (AnimationClip clip in rac.animationClips)
        {
            string clipName = clip.name.ToLower();
            if (clipName.Contains("store") || clipName.Contains("holster") || clipName.Contains("guardar"))
                storeClip = clip;
            else if (clipName.Contains("draw") || clipName.Contains("equip") || clipName.Contains("sacar") || clipName.Contains("pickup"))
                drawClip = clip;
        }
    }

    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType paramType)
    {
        if (anim == null) return false;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName && param.type == paramType) return true;
        }
        return false;
    }

    private void SetFiringAnimation()
    {
        foreach (AnimationClip clip in rac.animationClips)
        {
            if (clip.name.Contains("Firing") || clip.name.Contains("Pump"))
            {
                fireClip = clip;
                fireStateHash = Animator.StringToHash(clip.name.Contains("Pump") ? "Pump" : "Firing");
                break;
            }
            else fireClip = null;
        }
    }

    private void SetReload1Animation()
    {
        foreach (AnimationClip clip in rac.animationClips)
        {
            if (clip.name == "Reload")
            {
                reload1Clip = clip;
                break;
            }
            else reload1Clip = null;
        }
    }

    private void SetReload2Animation()
    {
        foreach (AnimationClip clip in rac.animationClips)
        {
            if (clip.name == "Reload2")
            {
                reload2Clip = clip;
                break;
            }
            else reload2Clip = null;
        }
    }

    // Procura os clipes de Single Reload pelo nome
    private void SetSingleReloadAnimations()
    {
        foreach (AnimationClip clip in rac.animationClips)
        {
            string clipName = clip.name.ToLower();

            if (clipName.Contains("start") && clipName.Contains("reload"))
                startReloadClip = clip;
            else if (clipName.Contains("reloading") || clipName.Contains("process"))
                reloadingClip = clip;
            else if (clipName.Contains("finish") && clipName.Contains("reload"))
                finishReloadClip = clip;
        }
    }

    public void StartReloadAnimation()
    {
        if (anim == null) return;

        StopFireAnimation();

        bool isEmpty = !reloadValues.isSingleReload && reloadValues.IsMagazineEmpty();
        float targetDuration = Reload.ReloadLogic.CalculateReloadTime(reloadValues, isEmpty);
        float speedMultiplier = 1f;

        // SE FOR STANDARD RELOAD
        if (!reloadValues.isSingleReload)
        {
            bool isLastBullet;
            AnimationClip currentAnimationClip;

            if (!isEmpty)
            {
                isLastBullet = false;
                if (hasLastBulletParam) anim.SetBool("Last_bullet", false);
                currentAnimationClip = reload1Clip != null ? reload1Clip : reload2Clip;
            }
            else
            {
                isLastBullet = true;
                if (hasLastBulletParam)
                {
                    anim.SetBool("Last_bullet", true);
                    currentAnimationClip = reload2Clip != null ? reload2Clip : reload1Clip;
                }
                else
                {
                    currentAnimationClip = reload1Clip;
                }
            }

            if (currentAnimationClip != null && targetDuration > 0) speedMultiplier = currentAnimationClip.length / targetDuration;
            
            if (isLastBullet && armReload2AnimationClip != null)
                FirstPersonArms.Instance.StartAnimation(armReload2AnimationClip, targetDuration);
            else if (!isLastBullet && armReload1AnimationClip != null)
                FirstPersonArms.Instance.StartAnimation(armReload1AnimationClip, targetDuration);
        }

        // SE FOR SINGLE RELOAD
        else
        {
            float totalLength = 0f;

            // Soma o tempo das 3 animações juntas
            if (startReloadClip != null) totalLength += startReloadClip.length;
            if (reloadingClip != null) totalLength += reloadingClip.length;
            if (finishReloadClip != null) totalLength += finishReloadClip.length;

            if (totalLength > 0f && targetDuration > 0f) speedMultiplier = totalLength / targetDuration;
            

            // Toca a animação dos braços (se você usar para o single reload também)
            if (armReload1AnimationClip != null) FirstPersonArms.Instance.StartAnimation(armReload1AnimationClip, targetDuration);
        }

        // Aplica a velocidade calculada para o Animator
        anim.SetFloat("Reload_speed", speedMultiplier);
        anim.SetBool("Is_reloading", true);
    }

    public void FinishReloadAnimation() => anim.SetBool("Is_reloading", false);

    public void StartFireAnimation()
    {
        if (anim == null || fireClip == null) return;

        // Quando a velocidade nao e adaptada ao intervalo de tiro, deixa o clip
        // atual terminar. Caso contrario, tiros rapidos reiniciariam o mesmo clip
        // antes que ele alcancasse o ultimo frame.
        if (!changeShootAnimationSpeed && fireAnimationRoutine != null) return;

        CancelFireAnimationRoutine();

        float targetDuration;
        float speedMultiplier;

        if (changeShootAnimationSpeed)
        {
            targetDuration = firingValues.interval - delayToShootAnimation;
            if (targetDuration <= 0.01f) targetDuration = 0.01f;
            speedMultiplier = fireClip.length / targetDuration;
        }
        else
        {
            targetDuration = fireClip.length;
            speedMultiplier = 1f;
        }

        fireAnimationRoutine = StartCoroutine(ExecuteFireAnimation(targetDuration, speedMultiplier));
    }

    private System.Collections.IEnumerator ExecuteFireAnimation(float targetDuration, float speedMultiplier)
    {
        if (delayToShootAnimation > 0f) yield return new WaitForSeconds(delayToShootAnimation);

        if (anim == null || !isActiveAndEnabled)
        {
            fireAnimationRoutine = null;
            yield break;
        }

        anim.SetFloat("Fire_speed", speedMultiplier);
        anim.SetBool("Is_firing", true);

        // SetBool(true) nao reinicia um estado que ja esta ativo. Play em zero
        // garante que cada disparo adaptado comece no primeiro frame do estado.
        if (fireStateHash != 0 && anim.HasState(0, fireStateHash))
            anim.Play(fireStateHash, 0, 0f);

        isInFireAnimation = true;

        bool enteredFireState = false;
        float safetyTimer = 0f;
        float safetyDuration = Mathf.Max(targetDuration, fireClip.length) + 1f;

        // Aguarda o progresso real do Animator. Assim o ultimo frame e avaliado
        // antes de Is_firing ser desligado, mesmo com variacao de frame rate.
        while (anim != null && isActiveAndEnabled && safetyTimer < safetyDuration)
        {
            if (!anim.IsInTransition(0))
            {
                AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                bool isFireState = stateInfo.shortNameHash == fireStateHash;

                if (isFireState)
                {
                    enteredFireState = true;
                    if (stateInfo.normalizedTime >= 1f) break;
                }
                else if (enteredFireState)
                {
                    break;
                }
            }

            safetyTimer += Time.deltaTime;
            yield return null;
        }

        ResetFireAnimationState();
        fireAnimationRoutine = null;
    }

    public void StopFireAnimation()
    {
        CancelFireAnimationRoutine();
        ResetFireAnimationState();
    }

    private void CancelFireAnimationRoutine()
    {
        if (fireAnimationRoutine == null) return;

        StopCoroutine(fireAnimationRoutine);
        fireAnimationRoutine = null;
    }

    private void ResetFireAnimationState()
    {
        if (anim != null) anim.SetBool("Is_firing", false);
        isInFireAnimation = false;
    }

    // Novos métodos para Guardar e Sacar a arma
    public void StartStoreAnimation(float targetDuration)
    {
        if (anim == null) return;

        StopFireAnimation();

        float speedMultiplier = 1f;
        if (storeClip != null && targetDuration > 0f)
            speedMultiplier = storeClip.length / targetDuration;

        ResetWeaponSwitchTriggers();
        anim.SetFloat("Store_speed", speedMultiplier);
        anim.SetTrigger("Store");
    }

    public void StartDrawAnimation(float targetDuration)
    {
        if (anim == null) return;

        PrepareAnimatorBeforeDraw();

        float speedMultiplier = 1f;
        if (drawClip != null && targetDuration > 0f)
            speedMultiplier = drawClip.length / targetDuration;

        anim.SetFloat("Draw_speed", speedMultiplier);
        anim.SetTrigger("Draw");
    }

    public void FinishDrawAnimation()
    {
        RestoreIdleTransform();
        ResetWeaponSwitchTriggers();
    }

    private void PrepareAnimatorBeforeDraw()
    {
        RestoreIdleTransform();
        StopFireAnimation();
        ResetWeaponSwitchTriggers();
    }

    private void OnDisable()
    {
        CancelFireAnimationRoutine();
        ResetFireAnimationState();
    }

    private void CacheIdleTransform()
    {
        if (hasCachedIdleTransform) return;

        idleLocalPosition = transform.localPosition;
        idleLocalRotation = transform.localRotation;
        idleLocalScale = transform.localScale;
        hasCachedIdleTransform = true;
    }

    private void RestoreIdleTransform()
    {
        if (!hasCachedIdleTransform) return;

        transform.localPosition = idleLocalPosition;
        transform.localRotation = idleLocalRotation;
        transform.localScale = idleLocalScale;
    }

    private void ResetWeaponSwitchTriggers()
    {
        anim.ResetTrigger("Store");
        anim.ResetTrigger("Draw");
    }
}
