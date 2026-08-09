using FishNet.Object;

public class ServerSingleton<T> : NetworkBehaviour where T : NetworkBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake() => SetInstance();
    protected void SetInstance()
    {
        if (Instance != null && Instance != this)
        {
            if (IsServerInitialized) Despawn();
            return;
        }

        Instance = this as T;
    }
}
