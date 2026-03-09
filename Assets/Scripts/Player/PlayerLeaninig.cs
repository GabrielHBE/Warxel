using UnityEngine;

public class PlayerLeaninig : MonoBehaviour
{

    [SerializeField] private GameObject spine;
    [SerializeField] private float rotation_value;
    private Quaternion rotation_left_total;
    private Quaternion rotation_right_total;
    private Quaternion original_rotation;
    private bool is_leaning_left = false;
    private bool is_leaning_right = false;
    [SerializeField] private float lean_timer;
    private float elapsed_timer;
    PlayerProperties playerProperties;

    // Adicione estas variáveis
    [SerializeField] private Animator animator;
    private int leanLayerIndex;
    [SerializeField] private float leanLayerWeight = 1f; // Peso total para sobrescrever

    void Start()
    {
        Settings.Instance._keybinds = GameObject.FindGameObjectWithTag("Settings").GetComponent<KeyBinds>();
        playerProperties = GetComponent<PlayerProperties>();

        // Obter o Animator
        //animator = GetComponent<Animator>();
        if (animator != null)
        {
            // Criar uma layer específica para o lean
            for (int i = 0; i < animator.layerCount; i++)
            {
                if (animator.GetLayerName(i) == "Lean Layer")
                {
                    leanLayerIndex = i;
                    break;
                }
            }

            // Se não existir, você precisa criar no Animator Controller
        }

        rotation_left_total = Quaternion.Euler(
            spine.transform.localRotation.eulerAngles.x,
            spine.transform.localRotation.eulerAngles.y,
            spine.transform.localRotation.eulerAngles.z + rotation_value
        );

        rotation_right_total = Quaternion.Euler(
            spine.transform.localRotation.eulerAngles.x,
            spine.transform.localRotation.eulerAngles.y,
            spine.transform.localRotation.eulerAngles.z - rotation_value
        );

        original_rotation = spine.transform.localRotation;
        original_rotation = spine.transform.localRotation;
    }

    void Update()
    {
        // Seu código de input existente...
        if (Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_leanLeftKey))
        {
            elapsed_timer = 0;
            is_leaning_left = !is_leaning_left;
            is_leaning_right = false;
        }

        if (Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_leanRightKey))
        {
            elapsed_timer = 0;
            is_leaning_right = !is_leaning_right;
            is_leaning_left = false;
        }

        if (playerProperties.sprinting)
        {
            is_leaning_left = false;
            is_leaning_right = false;
        }

        // Aplicar o peso da layer de lean
        if (animator != null && leanLayerIndex > 0)
        {
            animator.SetLayerWeight(leanLayerIndex, leanLayerWeight);
        }

        // Executar leaning
        if (is_leaning_left)
        {
            is_leaning_right = false;
            LeanLeft();
        }
        else if (is_leaning_right)
        {
            is_leaning_left = false;
            LeanRight();
        }
        else
        {
            ResetLeaning();
        }
    }

    void ResetLeaning()
    {
        elapsed_timer += Time.deltaTime;

        if (elapsed_timer <= lean_timer)
        {
            float t = elapsed_timer / lean_timer;
            spine.transform.localRotation = Quaternion.Lerp(spine.transform.localRotation, new Quaternion(spine.transform.localRotation.x,
                                                        spine.transform.localRotation.y,
                                                        original_rotation.z,
                                                        spine.transform.localRotation.w), t);
        }
        else
        {
            /*
            spine.transform.localRotation = new Quaternion(spine.transform.localRotation.x,
                                                        spine.transform.localRotation.y,
                                                        original_rotation.z,
                                                        spine.transform.localRotation.w);
                                                        */
        }

    }

    void LeanLeft()
    {
        elapsed_timer += Time.deltaTime;

        if (elapsed_timer <= lean_timer)
        {
            float t = elapsed_timer / lean_timer;
            spine.transform.localRotation = Quaternion.Lerp(spine.transform.localRotation, rotation_left_total, t);
        }
        else
        {
            spine.transform.localRotation = rotation_left_total;
        }

    }

    void LeanRight()
    {
        elapsed_timer += Time.deltaTime;

        if (elapsed_timer <= lean_timer)
        {
            float t = elapsed_timer / lean_timer;
            spine.transform.localRotation = Quaternion.Lerp(spine.transform.localRotation, rotation_right_total, t);
        }
        else
        {
            spine.transform.localRotation = rotation_right_total;
        }

    }

}
