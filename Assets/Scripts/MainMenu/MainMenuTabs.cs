using UnityEngine;

public class MainMenuTabs : MonoBehaviour
{
    protected bool is_active;

    public virtual void Activate()
    {
        is_active = true;
    }
    public virtual void Deactivate()
    {
        is_active = false;
    }

}
