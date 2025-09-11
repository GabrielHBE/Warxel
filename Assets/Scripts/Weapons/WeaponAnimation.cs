using System;
using UnityEngine;

public class WeaponAnimation : MonoBehaviour
{
    [HideInInspector] public AnimationClip fireClip;
    private AnimationClip reloadClip;

    private Animator anim;
    private PlayerProperties player;
    private WeaponProperties weaponProperties;

    private Weapon weapon;
    private float delay_to_shoot_timer = 0f;
    private float animation_timer = 0f;
    private bool fired;
    RuntimeAnimatorController rac;
    public float reload_animation_timer;
    public float shoot_animation_timer;
    public float elapsed;
    public bool is_in_fire_animation;


    bool restarted;

    void Start()
    {
        restarted = false;
    }

    public void Restart()
    {
        anim = GetComponentInChildren<Animator>();
        player = GetComponentInParent<PlayerProperties>();
        weaponProperties = GetComponentInChildren<WeaponProperties>();
        weapon = GetComponent<Weapon>();
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

    void Update()
    {

        if (!restarted)
        {
            return;
        }

        if (weapon.did_shoot)
        {
            fired = true;
        }

        //Verifica se a animação nã oé nula
        if (anim != null)
        {
            //Verifica se tem uma animaçã ode atirar
            if (fireClip != null)
            {
                //Ferifica se ativou
                if (fired)
                {
                    if (delay_to_shoot_timer >= weaponProperties.delay_to_shoot_animation)
                    {
                        is_in_fire_animation = true;
                        anim.SetBool("Is_firing", is_in_fire_animation);
                        animation_timer = fireClip.length / anim.speed;
                        fired = false;
                        delay_to_shoot_timer = 0f;
                    }
                    else
                    {
                        delay_to_shoot_timer += Time.deltaTime;
                    }
                }
                else if (animation_timer > 0f)
                {
                    animation_timer -= Time.deltaTime;
                    if (animation_timer <= 0f)
                    {
                        is_in_fire_animation = false;
                        anim.SetBool("Is_firing", is_in_fire_animation);

                    }
                }
                else
                {
                    anim.SetBool("Is_firing", false);
                }
            }
            ReloadAnimation();
            anim.SetBool("Is_reloading", player.is_reloading);
            anim.SetFloat("Reload_speed", weaponProperties.reload_time);

        }



    }

    void ReloadAnimation()
    {

        try
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
        }
        catch (Exception)
        {

        }



    }

    


}
