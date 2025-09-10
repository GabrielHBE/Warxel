
using UnityEngine;

public class Shell : MonoBehaviour
{
    [Header("Changes")]
    public float spread_change;
    public float reload_speed_change;
    public float damage_change;


    [Header("Positions")]
    [SerializeField] private Transform put_shell_pos;
    [SerializeField] private Transform weapon;

    [Header("Sound")]
    private AudioSource put_shell;
    [SerializeField] private PlayerProperties playerProperties;
    private WeaponHolder weaponHolder;
    private MeshRenderer mesh;

    Vector3 original_pos;
    Quaternion original_rot;
    Vector3 original_weapon_pos;
    Quaternion original_weapon_rot;
    Vector3 reload_pos;
    Quaternion reload_rot;

    bool hand_to_shell;
    bool hand_to_weapon;

    // Variáveis para controlar o movimento de alternância
    private bool isMovingToShellPos = true;
    private bool hasPlayedSound = false;

    private Transform current_shell;

    void Start()
    {
        hand_to_shell = true;

        original_weapon_pos = weapon.localPosition;
        original_weapon_rot = weapon.localRotation;


        original_pos = transform.localPosition;
        original_rot = transform.localRotation;
    }

    public void Restart(WeaponProperties weaponProperties)
    {
        reload_pos = new Vector3(weaponProperties.initial_potiion.x, weaponProperties.initial_potiion.y + 0.2f, weaponProperties.initial_potiion.z);
        reload_rot = new Quaternion(weaponProperties.inicial_rotation.x + 0.05f, weaponProperties.inicial_rotation.y + 0.05f, weaponProperties.inicial_rotation.z + 0.2f, weapon.localRotation.w);

        weaponHolder = GetComponentInParent<WeaponHolder>();
        // Resetar estado de movimento quando reiniciar
        isMovingToShellPos = true;
        hasPlayedSound = false;

        foreach (Transform child in gameObject.transform)
        {
            if (child.gameObject.activeSelf) // verifica se o filho está ativo
            {
                current_shell = child;
                put_shell = current_shell.GetComponent<AudioSource>();
                mesh = current_shell.GetComponent<MeshRenderer>();
                mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                if (current_shell.name.Contains("Hight"))
                {
                    weaponProperties.reload_time += -1;
                    weaponProperties.spread_increaser += 10;
                    weaponProperties.damage += 8;
                }
                else if (current_shell.name.Contains("Light"))
                {
                    weaponProperties.reload_time += 1;
                    weaponProperties.spread_increaser += -5;
                    weaponProperties.damage += -8;
                }

                break;
            }
        }
    }

    void Update()
    {
        if (!playerProperties.is_reloading)
        {
            weapon.transform.localPosition = Vector3.Lerp(weapon.transform.localPosition, original_weapon_pos, Time.deltaTime * 2);
            weapon.transform.localRotation = Quaternion.Lerp(weapon.transform.localRotation, original_weapon_rot, Time.deltaTime * 2);
            transform.localPosition = original_pos;

            // Resetar estados quando não estiver recarregando
            isMovingToShellPos = true;
            hasPlayedSound = false;
        }
    }

    void HandToShell()
    {
        weaponHolder.LeftHand_WeaponToMag(0.3f);
    }

    public void Reload(WeaponProperties weaponProperties)
    {
        if (hand_to_shell)
        {

            hand_to_weapon = true;
            HandToShell();
            hand_to_shell = false;
        }

        weapon.transform.localPosition = Vector3.MoveTowards(weapon.transform.localPosition, reload_pos, Time.deltaTime);
        weapon.transform.localRotation = Quaternion.Lerp(weapon.transform.localRotation, reload_rot, Time.deltaTime);

        // Movimento de alternância entre as posições
        if (isMovingToShellPos)
        {
            // Movendo em direção à posição de colocar a munição
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, put_shell_pos.localPosition, Time.deltaTime * weaponProperties.reload_time / 2);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, put_shell_pos.localRotation, Time.deltaTime * weaponProperties.reload_time / 2);

            // Verificar se chegou na posição de colocar a munição
            if (Vector3.Distance(transform.localPosition, put_shell_pos.localPosition) < 0.01f)
            {
                // Executar ações apenas uma vez quando chegar na posição
                if (!hasPlayedSound)
                {
                    mesh.enabled = false;
                    weaponProperties.mags[^1] += 1;
                    weaponProperties.shells -= 1;
                    put_shell.Play();
                    hasPlayedSound = true;
                }

                // Inverter a direção do movimento
                isMovingToShellPos = false;
            }
        }
        else
        {
            // Movendo de volta para a posição original
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, original_pos, Time.deltaTime * weaponProperties.reload_time / 2);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, original_rot, Time.deltaTime * weaponProperties.reload_time / 2);

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
    }

    public void ReturnHand()
    {
        if (hand_to_weapon)
        {
            mesh.enabled = true;
            hand_to_shell = true;
            weaponHolder.LeftHand_MagToWeapon(0.5f);
            hand_to_weapon = false;
        }
    }
}