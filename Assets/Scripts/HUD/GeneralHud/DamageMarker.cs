using TMPro;
using UnityEngine;
using System.Collections;

public class DamageMarker : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damage_text;
    [SerializeField] private float fadeDuration = 0.5f; // Duração do fade out
    [SerializeField] private float idleTimeToFade = 1f; // Tempo sem chamadas para começar a desaparecer
    
    private float currentDamage = 0f;
    private Coroutine fadeCoroutine;
    private float lastUpdateTime;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        // Garantir que temos um CanvasGroup para controlar a transparência
        canvasGroup = damage_text.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = damage_text.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Inicializar como invisível
        canvasGroup.alpha = 0f;
        damage_text.text = "0";
        currentDamage = 0f;
    }

    public void UpdateDamage(float damage)
    {
        // Atualizar tempo da última chamada
        lastUpdateTime = Time.time;
        
        // Resetar e somar o dano
        currentDamage += damage;
        damage_text.text = currentDamage.ToString("F0"); // Formato sem casas decimais
        
        // Tornar visível instantaneamente
        canvasGroup.alpha = 1f;
        
        // Parar qualquer fade em andamento
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        // Iniciar nova coroutine para verificar se ficou idle
        fadeCoroutine = StartCoroutine(CheckIdleAndFade());
    }

    private IEnumerator CheckIdleAndFade()
    {
        // Esperar o tempo de idle
        yield return new WaitForSeconds(idleTimeToFade);
        
        // Verificar se realmente ficou idle (se não houve novas chamadas)
        if (Time.time - lastUpdateTime >= idleTimeToFade)
        {
            // Iniciar fade out
            yield return StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeDuration;
            
            // Interpolar a transparência
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalizedTime);
            
            yield return null;
        }
        
        // Garantir que ficou completamente transparente
        canvasGroup.alpha = 0f;
        
        // Resetar o texto e o dano acumulado
        damage_text.text = "0";
        currentDamage = 0f;
        
        fadeCoroutine = null;
    }

    // Opcional: Método para resetar manualmente
    public void ResetMarker()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        canvasGroup.alpha = 0f;
        damage_text.text = "0";
        currentDamage = 0f;
    }
}