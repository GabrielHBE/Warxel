using FishNet.Object;
using UnityEngine;

public abstract class InteractiveButton : NetworkBehaviour
{   
    [SerializeField] private string interactionButtonText;

    private void OnEnable()
    {
        if (InteractiveButtonUI.Instance != null)
        {
            InteractiveButtonUI.Instance.CreateButtonUI(this);
        } 
    }

    public abstract void Interact(PlayerController player);

    public string GetInteractionButtonText()
    {
        return interactionButtonText;
    }
    
}
