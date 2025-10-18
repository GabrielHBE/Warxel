using System;
using UnityEngine;

public class WeaponAnimation : MonoBehaviour
{
    [HideInInspector] public AnimationClip fireClip;
    private AnimationClip reloadClip;

    private Animator anim;
    private WeaponProperties weaponProperties;

    RuntimeAnimatorController rac;
    public float reload_animation_timer;
    private float shoot_animation_timer;
    private float elapsed;
    public bool is_in_fire_animation;

    bool restarted;

    void Start()
    {
        restarted = false;
    }

    void Update()
    {
        if (!restarted) return;

        // Controlar o tempo da animação de disparo
        if (is_in_fire_animation)
        {
            elapsed += Time.deltaTime;

            // Se o tempo da animação foi concluído, parar a animação
            if (elapsed >= shoot_animation_timer)
            {
                StopFireAnimation();
            }
        }
    }

    public void Restart()
    {
        anim = GetComponentInChildren<Animator>();
        weaponProperties = GetComponentInChildren<WeaponProperties>();
        anim.Play("Stop");

        rac = anim.runtimeAnimatorController;

        foreach (AnimationClip clip in rac.animationClips)
        {
            if (clip.name.Contains("Firing") || clip.name.Contains("Pump"))
            {
                fireClip = clip;
                shoot_animation_timer = fireClip.length;
                anim.Play("Stop", -1, 0f);
                anim.Update(0f); // aplica imediatamente
                break;
            }
            else
            {
                fireClip = null;
            }
        }
        restarted = true;
    }

    public void StartReloadAnimation()
    {
        anim.SetFloat("Reload_speed", weaponProperties.reload_time);

        if (anim != null)
        {
            if (weaponProperties.mags[^1] != 0)
            {
                foreach (AnimationClip clip in rac.animationClips)
                {
                    if (clip.name == "Reload")
                    {
                        reloadClip = clip;
                        reload_animation_timer = reloadClip.length;
                        anim.SetBool("Last_bullet", false);
                        break;
                    }
                    else
                    {
                        reloadClip = null;
                    }
                }
            }
            else
            {
                foreach (AnimationClip clip in rac.animationClips)
                {
                    if (clip.name == "Reload2")
                    {
                        reloadClip = clip;
                        reload_animation_timer = reloadClip.length;
                        anim.SetBool("Last_bullet", true);
                        break;
                    }
                    else
                    {
                        reloadClip = null;
                    }
                }
            }
        }

        anim.SetBool("Is_reloading", true);
    }

    public void FinishReloadAnimation()
    {
        if (anim != null)
        {
            if (weaponProperties.mags[^1] != 0)
            {
                foreach (AnimationClip clip in rac.animationClips)
                {
                    if (clip.name == "Reload")
                    {
                        reloadClip = clip;
                        reload_animation_timer = reloadClip.length;
                        anim.SetBool("Last_bullet", false);
                        break;
                    }
                    else
                    {
                        reloadClip = null;
                    }
                }
            }
            else
            {
                foreach (AnimationClip clip in rac.animationClips)
                {
                    if (clip.name == "Reload2")
                    {
                        reloadClip = clip;
                        reload_animation_timer = reloadClip.length;
                        anim.SetBool("Last_bullet", true);
                        break;
                    }
                    else
                    {
                        reloadClip = null;
                    }
                }
            }
        }

        anim.SetBool("Is_reloading", false);
    }

    public void StartFireAnimation()
    {
        if (fireClip == null) return;

        StartCoroutine(ExecuteFireAnimationDelayed());
    }

    private System.Collections.IEnumerator ExecuteFireAnimationDelayed()
    {
        yield return new WaitForSeconds(weaponProperties.delay_to_shoot_animation);

        anim.SetBool("Is_firing", true);
        is_in_fire_animation = true;
        elapsed = 0f;
    }
    public void StopFireAnimation()
    {
        if (fireClip == null) return;

        anim.SetBool("Is_firing", false);
        is_in_fire_animation = false;
        elapsed = 0f;
    }
}