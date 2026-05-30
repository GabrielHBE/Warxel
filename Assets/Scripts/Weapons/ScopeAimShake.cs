using UnityEngine;

public class ScopeAimShake : MonoBehaviour
{
    [Header("Aim Shake Settings")]
    [SerializeField] private float tension = 1f;

    [Header("Hold Breath Settings")]
    [SerializeField] private float maxBreathDuration = 5f; // Tempo máximo segurando a respiração
    [SerializeField] private float breathCooldown = 10f;   // Tempo de penalidade caso a respiração acabe
    [SerializeField] private float breathRecoveryRate = 1f;// Velocidade que recupera o fôlego quando não está apertando

    private PlayerProperties playerProperties;
    private CameraShake cameraShake;

    private bool isHoldingBreath = false;
    private float currentBreath;
    private float currentCooldown;

    void Awake()
    {
        playerProperties = GetComponentInParent<PlayerProperties>();
        cameraShake = GetComponentInParent<CameraShake>();

        // Inicia com o pulmão cheio
        currentBreath = maxBreathDuration;
    }

    void Update()
    {
        if (playerProperties == null || cameraShake == null || !gameObject.activeSelf) return;

        // O jogador quer segurar a respiração se estiver mirando E apertando o botão
        bool wantsToHoldBreath = playerProperties.is_aiming && Input.GetKey(Settings.Instance._keybinds.PLAYER_holdBreathKey);

        // Atualiza a matemática do fôlego e cooldown
        UpdateBreathMechanic(wantsToHoldBreath);

        // Aplica a tremedeira com base nos estados atuais
        if (playerProperties.is_aiming)
        {
            if (isHoldingBreath)
            {
                // Zera o shake (ou deixa ele estabilizado) enquanto segura a respiração
                cameraShake.ResetAimShake(); 
            }
            else
            {
                // Se não está segurando a respiração (ou está em cooldown), treme a mira
                cameraShake.CalculateScopeAimShake(tension);
            }
        }
        else
        {
            cameraShake.ResetAimShake();
        }
    }

    private void UpdateBreathMechanic(bool wantsToHoldBreath)
    {
        // 1. CHECAGEM DE COOLDOWN
        if (currentCooldown > 0)
        {
            isHoldingBreath = false;
            currentCooldown -= Time.deltaTime;
            
            RecoverBreath(); // Permite que o fôlego volte mesmo enquanto em cooldown
            return;
        }

        // 2. SEGURANDO A RESPIRAÇÃO
        if (wantsToHoldBreath && currentBreath > 0)
        {
            isHoldingBreath = true;
            currentBreath -= Time.deltaTime;

            // Se o fôlego chegar a zero, ativa a punição de cooldown
            if (currentBreath <= 0)
            {
                isHoldingBreath = false;
                currentCooldown = breathCooldown;
            }
        }
        // 3. NÃO ESTÁ APERTANDO O BOTÃO
        else
        {
            isHoldingBreath = false;
            RecoverBreath();
        }
    }

    private void RecoverBreath()
    {
        // Recupera o fôlego até o limite máximo
        if (currentBreath < maxBreathDuration)
        {
            currentBreath += Time.deltaTime * breathRecoveryRate;
            
            // Garante que não ultrapasse o limite
            if (currentBreath > maxBreathDuration)
            {
                currentBreath = maxBreathDuration;
            }
        }
    }
}