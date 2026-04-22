using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class EliminationMarker : MonoBehaviour
{
    public static EliminationMarker Instance {get; private set;}
    [SerializeField] private Sprite infantary_kill_image;
    [SerializeField] private Sprite vehicle_kill_image;
    [SerializeField] private float images_distance = 2f;
    [SerializeField] private Transform images_container;
    [SerializeField] private float imageLifetime = 1f;
    [SerializeField] private float fadeDuration = 0.5f;
    
    // Pooling para melhor performance
    private Queue<GameObject> imagePool = new Queue<GameObject>();
    private List<GameObject> activeImages = new List<GameObject>();
    
    private float nextImagePositionX = 0f;

    private void Awake()
    {
        Instance = this;
        if (images_container == null)
        {
            images_container = transform;
        }
    }

    public void InstantiateInfantaryImage()
    {
        CreateImage(infantary_kill_image);
    }

    public void InstantiateVehicleImage()
    {
        CreateImage(vehicle_kill_image);
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
        nextImagePositionX -= images_distance;
        
        // Iniciar fade out
        StartCoroutine(FadeAndRecycleImage(imageObject, canvasGroup));
    }
    
    private GameObject GetOrCreateImageObject()
    {
        if (imagePool.Count > 0)
        {
            return imagePool.Dequeue();
        }
        
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
            currentX -= images_distance;
        }
        
        // Atualizar próxima posição
        nextImagePositionX = currentX;
    }
    
    public void ClearAllImages()
    {
        StopAllCoroutines();
        
        // Mover todas as imagens ativas para o pool
        foreach (var image in activeImages)
        {
            image.SetActive(false);
            imagePool.Enqueue(image);
        }
        
        activeImages.Clear();
        nextImagePositionX = 0f;
    }
    
    public void ForceReorganize()
    {
        ReorganizeRemainingImages();
    }
}