using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class EliminationMarker : PersistentLocalSingleton<EliminationMarker>
{
    //public static EliminationMarker Instance { get; private set; }
    [Header("Images")]
    [SerializeField] private Sprite infantryKillImage;
    [SerializeField] private Sprite vehicleKillImage;

    [SerializeField] private Sprite infantryAssistImage;
    [SerializeField] private Sprite vehicleAssistImage;

    [Header("Settings")]
    
    [SerializeField] private Transform images_container;
    [SerializeField] private float imageLifetime = 1f;
    [SerializeField] private float fadeDuration = 0.5f;

    // Pooling para melhor performance
    private Queue<GameObject> imagePool = new Queue<GameObject>();
    private List<GameObject> activeImages = new List<GameObject>();
    private float imagesDistance = 100;

    private float nextImagePositionX = 0f;

    protected override void Awake()
    {
        base.Awake();
        if (images_container == null) images_container = transform;
    }

    public void InstantiateInfantryKillImage()
    {
        if (infantryKillImage != null) CreateImage(infantryKillImage);
    }

    public void InstantiateVehicleKillImage()
    {
        if (vehicleKillImage != null) CreateImage(vehicleKillImage);
    }

    public void InstantiateInfantryAssistImage()
    {
        if (infantryAssistImage != null) CreateImage(infantryAssistImage);
    }

    public void InstantiateVehicleAssistImage()
    {
        if (vehicleAssistImage != null) CreateImage(vehicleAssistImage);
    }

    private void CreateImage(Sprite sprite)
    {
        GameObject imageObject = GetOrCreateImageObject();
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;

        CanvasGroup canvasGroup = imageObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        // Posicionar
        imageObject.transform.SetParent(images_container);
        imageObject.transform.localPosition = new Vector3(nextImagePositionX, 0f, 0f);
        imageObject.transform.localScale = Vector3.one;
        imageObject.SetActive(true);

        // Adicionar à lista de ativas
        activeImages.Add(imageObject);

        // Atualizar posição para próxima imagem
        nextImagePositionX -= imagesDistance;

        // Iniciar fade out
        StartCoroutine(FadeAndRecycleImage(imageObject, canvasGroup));
    }

    private GameObject GetOrCreateImageObject()
    {
        if (imagePool.Count > 0)  return imagePool.Dequeue();
        
        GameObject newObj = new GameObject("KillImage");
        Image image = newObj.AddComponent<Image>();
        image.preserveAspect = true;
        newObj.AddComponent<CanvasGroup>();

        return newObj;
    }

    private IEnumerator FadeAndRecycleImage(GameObject imageObject, CanvasGroup canvasGroup)
    {
        yield return new WaitForSeconds(imageLifetime);

        // Fade out
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        // Remover da lista de ativas
        activeImages.Remove(imageObject);

        // Desativar e adicionar ao pool
        imageObject.SetActive(false);
        imagePool.Enqueue(imageObject);

        // Reorganizar imagens restantes
        ReorganizeRemainingImages();
    }

    private void ReorganizeRemainingImages()
    {
        float currentX = 0f;

        for (int i = 0; i < activeImages.Count; i++)
        {
            activeImages[i].transform.localPosition = new Vector3(currentX, 0f, 0f);
            currentX -= imagesDistance;
        }

        // Atualizar próxima posição
        nextImagePositionX = currentX;
    }
}