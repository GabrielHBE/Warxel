using UnityEngine;

public class JetBombsAndMissiles : MonoBehaviour
{
    [SerializeField] private GameObject parent_jet;
    public JetBomb bombs;
    public JetMissile missile;

    void Awake()
    {
        if (missile != null)
        {
            missile.parent_gameobject = parent_jet;
        }

    }

}
