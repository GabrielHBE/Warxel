using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchWeapon : MonoBehaviour
{
    [Header("Keycodes")]
    public KeyCode weapon1 = KeyCode.Alpha1;
    public KeyCode weapon2 = KeyCode.Alpha2;
    public KeyCode weapon3 = KeyCode.Alpha3;
    public KeyCode weapon4 = KeyCode.Alpha4;

    [Header("Weapons")]
    public GameObject primary;
    public GameObject secondary;
    public GameObject gadget1;
    public GameObject gadget2;

    [Header("Sounds")]
    public AudioSource zipper;

    [Header("Instances")]
    public MagCounter magCounter;
    public Reticle reticle;
    public Transform save;



    private int current_weapon = 1;
    [HideInInspector] public bool _switch = false;
    bool _return = false;
    float saving_timer = 0;
    private Weapon weapon;
    private SwayNBobScript sway;
    private WeaponAnimation weapon_animation;
    private WeaponProperties weaponProperties;
    private PlayerController playerController;
    private PlayerProperties playerProperties;
    private WeaponHolder weaponHolder;
    private Shell Shell;
    private Vector3 original_pos;
    private Vector3 save_pos;
    private Quaternion original_rot;
    private Quaternion save_rot;
    float switch_timer = 0.6f;


    bool do_once;

    float pick_up_weapon_timer = 0.6f;


    public void Start()
    {
        //weaponProperties = GetComponentInChildren<WeaponProperties>();
        playerController = GetComponentInParent<PlayerController>();
        playerProperties = GetComponentInParent<PlayerProperties>();
        do_once = true;
        save_pos = new Vector3(transform.localPosition.x, transform.localPosition.y - 30, transform.localPosition.z + 1);
        save_rot = new Quaternion(transform.localRotation.x + 4, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w);

        //save_pos = save.localPosition;
        //save_rot = save.localRotation;

        weapon_animation = GetComponentInChildren<WeaponAnimation>();

        weapon = GetComponentInChildren<Weapon>();
        sway = GetComponentInChildren<SwayNBobScript>();
        ////magCounter.Restart();

        //playerController.ChangeWeaponVelocitySpeed(weaponProperties.speed_change);
        _switch = true;
        playerProperties.is_reloading = false;
        playerProperties.is_firing = false;
        current_weapon = 1;


    }


    void Update()
    {

        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
        float scrollY = scrollDelta.y;


        if (!_switch && !playerProperties.is_reloading && !playerProperties.is_firing)
        {
            if (scrollY > 0f)
            {

                if (current_weapon == 4)
                {
                    current_weapon = 1;
                }
                else
                {
                    current_weapon += 1;
                }
                _switch = true;

            }
            else if (scrollY < 0f)
            {
                if (current_weapon == 1)
                {
                    current_weapon = 4;
                }
                else
                {
                    current_weapon -= 1;
                }
                _switch = true;
            }

            if (Input.GetKeyDown(weapon1) && primary != null && current_weapon != 1)
            {
                current_weapon = 1;
                _switch = true;
            }

            if (Input.GetKeyDown(weapon2) && secondary != null && current_weapon != 2)
            {
                current_weapon = 2;
                _switch = true;
            }

            if (Input.GetKeyDown(weapon3) && gadget1 != null && current_weapon != 3)
            {
                current_weapon = 3;
                _switch = true;
            }

            if (Input.GetKeyDown(weapon4) && gadget2 != null && current_weapon != 4)
            {
                current_weapon = 4;
                _switch = true;
            }
        }



        if (_switch)
        {
            weapon.can_shoot = false;
            original_pos = transform.localPosition;
            original_rot = transform.localRotation;
            zipper.Play();
            sway.enabled = false;

            weapon.can_aim = false;

            switch (current_weapon)
            {
                case 1:
                    weapon.is_active = true;
                    saving_timer += Time.deltaTime;

                    if (saving_timer <= switch_timer)
                    {
                        transform.localPosition = Vector3.Lerp(transform.localPosition,
                        save_pos,
                        Time.deltaTime * 0.1f);

                        transform.localRotation = Quaternion.Lerp(transform.localRotation, save_rot, Time.deltaTime * 0.1f);
                    }
                    else
                    {
                        if (do_once)
                        {
                            if (primary != null)
                            {
                                primary.SetActive(true);
                            }

                            if (secondary != null)
                            {
                                secondary.SetActive(false);
                            }

                            if (gadget1 != null)
                            {
                                gadget1.SetActive(false);
                            }

                            if (gadget2 != null)
                            {
                                gadget2.SetActive(false);
                            }
                            do_once = false;
                            weaponProperties = GetComponentInChildren<WeaponProperties>();
                            Shell = GetComponentInChildren<Shell>();
                            Shell = GetComponentInChildren<Shell>();
                            if (Shell != null)
                            {
                                Shell.Restart(weaponProperties);
                            }
                            weaponProperties.Restart();
                            weapon.Restart();
                            weapon.can_shoot = false;
                            sway.Restart();
                            //magCounter.Restart();
                            weapon_animation.Restart();
                            reticle.Restart();
                            playerController.ChangeWeaponVelocitySpeed(weaponProperties.speed_change);
                            weaponHolder = GetComponentInChildren<WeaponHolder>();
                            weaponHolder.SetHandsToWeapon(0);


                        }


                        saving_timer = 0;
                        _return = true;
                        _switch = false;

                    }
                    break;

                case 2:
                    weapon.is_active = true;
                    saving_timer += Time.deltaTime;

                    if (saving_timer <= switch_timer)
                    {
                        transform.localPosition = Vector3.Lerp(transform.localPosition,
                        save_pos,
                        Time.deltaTime * 0.1f);

                        transform.localRotation = Quaternion.Lerp(transform.localRotation, save_rot, Time.deltaTime * 0.1f);
                    }
                    else
                    {
                        if (do_once)
                        {
                            if (primary != null)
                            {
                                primary.SetActive(false);
                            }

                            if (secondary != null)
                            {
                                secondary.SetActive(true);
                            }

                            if (gadget1 != null)
                            {
                                gadget1.SetActive(false);
                            }

                            if (gadget2 != null)
                            {
                                gadget2.SetActive(false);
                            }
                            do_once = false;
                            weaponProperties = GetComponentInChildren<WeaponProperties>();
                            weaponProperties.Restart();
                            Shell = GetComponentInChildren<Shell>();
                            Shell = GetComponentInChildren<Shell>();
                            if (Shell != null)
                            {
                                Shell.Restart(weaponProperties);
                            }
                            weapon.Restart();
                            weapon.can_shoot = false;
                            sway.Restart();
                            //magCounter.Restart();
                            weapon_animation.Restart();
                            reticle.Restart();
                            playerController.ChangeWeaponVelocitySpeed(weaponProperties.speed_change);
                            weaponHolder = GetComponentInChildren<WeaponHolder>();
                            weaponHolder.SetHandsToWeapon(0);

                        }


                        saving_timer = 0;
                        _return = true;
                        _switch = false;

                    }
                    break;

                case 3:
                    weapon.is_active = false;
                    saving_timer += Time.deltaTime;

                    if (saving_timer <= switch_timer)
                    {
                        transform.localPosition = Vector3.Lerp(transform.localPosition,
                        save_pos,
                        Time.deltaTime * 0.1f);

                        transform.localRotation = Quaternion.Lerp(transform.localRotation, save_rot, Time.deltaTime * 0.1f);
                    }
                    else
                    {

                        if (do_once)
                        {
                            if (primary != null)
                            {
                                primary.SetActive(false);
                            }

                            if (secondary != null)
                            {
                                secondary.SetActive(false);
                            }

                            if (gadget1 != null)
                            {
                                gadget1.SetActive(true);
                            }

                            if (gadget2 != null)
                            {
                                gadget2.SetActive(false);
                            }
                            do_once = false;

                            C4 c4 = GetComponentInChildren<C4>();
                            if (c4 != null)
                            {
                                c4.is_active = true;
                            }
                            
                


                        }

                        saving_timer = 0;
                        _return = true;
                        _switch = false;


                    }
                    break;

                case 4:
                    saving_timer += Time.deltaTime;

                    if (saving_timer <= switch_timer)
                    {
                        transform.localPosition = Vector3.Lerp(transform.localPosition,
                        save_pos,
                        Time.deltaTime * 0.1f);

                        transform.localRotation = Quaternion.Lerp(transform.localRotation, save_rot, Time.deltaTime * 0.1f);
                    }
                    else
                    {

                        if (do_once)
                        {
                            if (primary != null)
                            {
                                primary.SetActive(false);
                            }

                            if (secondary != null)
                            {
                                secondary.SetActive(false);
                            }

                            if (gadget1 != null)
                            {
                                gadget1.SetActive(false);
                            }

                            if (gadget2 != null)
                            {
                                gadget2.SetActive(true);
                            }
                            do_once = false;
                            weaponProperties = GetComponentInChildren<WeaponProperties>();
                            Shell = GetComponentInChildren<Shell>();
                            Shell = GetComponentInChildren<Shell>();
                            if (Shell != null)
                            {
                                Shell.Restart(weaponProperties);
                            }
                            weaponProperties.Restart();
                            weapon.Restart();
                            weapon.can_shoot = false;
                            sway.Restart();
                            //magCounter.Restart();
                            weapon_animation.Restart();
                            reticle.Restart();
                            playerController.ChangeWeaponVelocitySpeed(weaponProperties.speed_change);
                            weaponHolder = GetComponentInChildren<WeaponHolder>();
                            weaponHolder.SetHandsToWeapon(0);

                        }



                        saving_timer = 0;
                        _return = true;
                        _switch = false;

                    }
                    break;

                default:
                    break;

            }

        }

        if (_return)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, original_pos, pick_up_weapon_timer * Time.deltaTime * 100);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, original_rot, pick_up_weapon_timer * Time.deltaTime * 100);

            if (transform.localRotation == original_rot)
            {
                //StartCoroutine(cameraShake.PickWeaponShake());
                weapon.can_shoot = true;
                _return = false;
                weapon.can_aim = true;
                do_once = true;
                sway.enabled = true;

            }

        }
    }


}
