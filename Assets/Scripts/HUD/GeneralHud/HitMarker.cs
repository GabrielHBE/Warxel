using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HitMarker : MonoBehaviour
{
    public static HitMarker Instance {get; private set;}
    private float show_time = 0.5f;
    [SerializeField] private HitMarkerSound hitMarkerSound;
    [SerializeField] private Image hitMarker;
    private Coroutine currentCoroutine;


    [Header("Opacity Settings")]
    [SerializeField] private bool fadeOut = true;
    [SerializeField] private float fadeDuration = 0.2f;

    void Start()
    {
        Instance = this;

        DeactivateHitMarker();
    }

    public void CreateBodyShotMarker()
    {
        if (!Settings.Instance._gameplay.show_hit_marker)
        {
            return;
        }

        hitMarkerSound.CrateBodyHitMarkerSound();
        ShowHitMarker(Settings.Instance._gameplay.body_shot_marker_colour, Settings.Instance._gameplay.hit_marker_size);
    }

    public void CreateHeadShotMarker()
    {
        if (!Settings.Instance._gameplay.show_hit_marker)
        {
            return;
        }
        hitMarkerSound.CreateHeadShotMarkerSound();
        ShowHitMarker(Settings.Instance._gameplay.head_shot_marker_colour, Settings.Instance._gameplay.hit_marker_size);
    }

    public void CreateVehicleMarker()
    {
        if (!Settings.Instance._gameplay.show_hit_marker)
        {
            
            return;
        }
        hitMarkerSound.CreateVehicleShotMarkerSound();
        ShowHitMarker(Settings.Instance._gameplay.vehicle_marker_colour, Settings.Instance._gameplay.hit_marker_size);
    }

    private void ShowHitMarker(Color color, float scale)
    {
        if (currentCoroutine != null)
        {

            StopCoroutine(currentCoroutine);
        }

        ActivateHitMarker(color, scale);

        if (fadeOut)
        {
            currentCoroutine = StartCoroutine(FadeOutCoroutine());
        }
        else
        {
            currentCoroutine = StartCoroutine(HideAfterDelay());
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(show_time);
        DeactivateHitMarker();
    }

    private IEnumerator FadeOutCoroutine()
    {

        // Mostrar com opacidade total
        Color currentColor = hitMarker.color;
        currentColor.a = Settings.Instance._gameplay.hit_marker_opacity;
        hitMarker.color = currentColor;
        // Aguardar tempo visível
        float visibleTime = show_time - fadeDuration;
        if (visibleTime > 0)
        {

            yield return new WaitForSeconds(visibleTime);
        }


        float elapsed = 0f;
        float startAlpha = Settings.Instance._gameplay.hit_marker_opacity;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            float currentAlpha = Mathf.Lerp(startAlpha, 0f, t);

            SetOpacity(currentAlpha);
            yield return null;
        }

        DeactivateHitMarker();
    }

    // Ativar com opacidade configurada
    private void ActivateHitMarker(Color color, float scale = 1f)
    {

        if (hitMarker != null)
        {
            // Aplicar rotação aleatória
            float randomRotation = Random.Range(-100f, 100f);
            hitMarker.transform.localRotation = Quaternion.Euler(0, 0, randomRotation);

            // Ativar objeto
            hitMarker.gameObject.SetActive(true);

            // Aplicar escala
            hitMarker.transform.localScale = Vector3.one * scale;

            // Aplicar cor com opacidade inicial
            Color newColor = color;
            newColor.a = Settings.Instance._gameplay.hit_marker_opacity;
            hitMarker.color = newColor;

        }

    }

    // Método para alterar opacidade
    public void SetOpacity(float opacity)
    {
        if (hitMarker != null && hitMarker.gameObject.activeSelf)
        {
            Color currentColor = hitMarker.color;
            currentColor.a = Mathf.Clamp01(opacity);
            hitMarker.color = currentColor;
        }

    }

    // Desativar o hitMarker
    private void DeactivateHitMarker()
    {
        if (hitMarker != null)
        {
            hitMarker.gameObject.SetActive(false);
        }
    }

    // Método para testar via teclas (temporário)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            CreateBodyShotMarker();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            CreateHeadShotMarker();
        }
    }
}