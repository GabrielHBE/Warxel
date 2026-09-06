using FishNet.Object;
using UnityEngine;

public abstract class InteractiveButton : NetworkBehaviour
{   
    public const float INTERACT_DISTANCE = 10f;
    public const float INTERACT_RADIOUS = 0.5f;
    [SerializeField] private string interactionButtonText;

    private void OnEnable()
    {
        if (InteractiveButtonUI.Instance != null) InteractiveButtonUI.Instance.CreateButtonUI(this);
    }

    public abstract void Interact(PlayerController player);
    public string GetInteractionButtonText() => interactionButtonText;
    
}
