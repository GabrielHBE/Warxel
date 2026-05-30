using UnityEngine;

public abstract class InteractiveButton : MonoBehaviour
{   
    [SerializeField] private string interactionButtonText;

    public abstract void Interact();

    public string GetInteractionButtonText()
    {
        return interactionButtonText;
    }
    
}
