using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneralHudAlertMessages : MonoBehaviour
{
    public static GeneralHudAlertMessages Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI message;
    [SerializeField] private Image message_image;
    private Coroutine current_message;

    void Awake()
    {
        Instance = this;
    }

    public void CreateMessage(string msg, float duration)
    {
        if (current_message != null) StopCoroutine(current_message);
        current_message = StartCoroutine(HandleMessage(msg, duration));
    }

    private IEnumerator HandleMessage(string message, float d)
    {
        this.message.text = message;
        message_image.enabled = true;
        yield return new WaitForSeconds(d);
        this.message.text = "";
        message_image.enabled = false;
    }

}
