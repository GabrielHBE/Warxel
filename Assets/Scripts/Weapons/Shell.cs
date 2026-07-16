
using UnityEngine;

public class Shell : Mag
{
    [Header("Positions")]
    [SerializeField] private Transform put_shell_pos;

    [Header("Sound")]
    [SerializeField] private SoundManager.SoundComponents put_shell;

    [Header("Instances")]

    [SerializeField] private WeaponHolder weaponHolder;
    [SerializeField] private MeshRenderer mesh;

    Vector3 original_pos;
    Quaternion original_rot;


    bool hand_to_shell;
    bool hand_to_weapon;

    // Variáveis para controlar o movimento de alternância
    private bool isMovingToShellPos = true;
    private bool hasPlayedSound = false;

    private Weapon weapon;
    private PlayerProperties playerProperties;

    public override void Initialize()
    {
        InitializeWeaponProperties();
        weapon = GetComponentInParent<Weapon>();
        playerProperties = GetComponentInParent<PlayerProperties>();
        hand_to_shell = true;
        original_pos = transform.localPosition;
        original_rot = transform.localRotation;
    }

    void HandToShell()
    {
        weaponHolder.LeftHandToMag(0.3f);
    }

    public void Reload()
    {
        if (hand_to_shell)
        {

            hand_to_weapon = true;
            HandToShell();
            hand_to_shell = false;
        }

        // Movimento de alternância entre as posições
        if (isMovingToShellPos)
        {
            // Movendo em direção à posição de colocar a munição
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, put_shell_pos.localPosition, Time.deltaTime * (weaponProperties.reloadValues.reloadTime / 2));
            transform.localRotation = Quaternion.Lerp(transform.localRotation, put_shell_pos.localRotation, Time.deltaTime * (weaponProperties.reloadValues.reloadTime / 2));

            // Verificar se chegou na posição de colocar a munição
            if (Vector3.Distance(transform.localPosition, put_shell_pos.localPosition) < 0.01f)
            {
                // Executar ações apenas uma vez quando chegar na posição
                if (!hasPlayedSound)
                {
                    mesh.enabled = false;

                    SoundManager.Play2dSoundLocal(put_shell.clip, put_shell.properties);
                    hasPlayedSound = true;
                }

                weapon.ApplyMagAmmo(weaponProperties.reloadValues.mags[^1] + 1);

                for (int i = 0; i < weaponProperties.reloadValues.magCount; i++)
                {
                    if (weaponProperties.reloadValues.mags[i] > 0)
                    {
                        weapon.RemoveMagAmmo(1, i);
                        break;
                    }
                }

                // Inverter a direção do movimento
                isMovingToShellPos = false;
            }
        }
        else
        {
            // Movendo de volta para a posição original
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, original_pos, Time.deltaTime * (weaponProperties.reloadValues.reloadTime / 2));
            transform.localRotation = Quaternion.Lerp(transform.localRotation, original_rot, Time.deltaTime * (weaponProperties.reloadValues.reloadTime / 2));

            // Verificar se chegou na posição original
            if (Vector3.Distance(transform.localPosition, original_pos) < 0.01f)
            {
                mesh.enabled = true;
                // Resetar para permitir tocar o som novamente na próxima vez
                hasPlayedSound = false;

                // Inverter a direção do movimento
                isMovingToShellPos = true;
            }
        }

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_shootKey) || weaponProperties.reloadValues.mags[^1] == weaponProperties.reloadValues.bulletsPerMag)
        {
            playerProperties.is_reloading = false;
            weapon.weaponAnimation.FinishReloadAnimation();
            ReturnHand();
        }
    }

    public void ReturnHand()
    {
        if (hand_to_weapon)
        {
            transform.localPosition = original_pos;
            transform.localRotation = original_rot;
            mesh.enabled = true;
            hand_to_shell = true;
            weaponHolder.LeftHandToWeapon(0.5f);
            hand_to_weapon = false;
        }
    }
}