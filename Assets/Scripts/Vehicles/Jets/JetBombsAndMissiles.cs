using UnityEngine;

public class JetBombsAndMissiles : MonoBehaviour
{
    [SerializeField] private GameObject parent_jet;
    public BombsController bombs;
    public MissileController missile;

    void Awake()
    {
        missile.parent_gameobject = parent_jet;
    }
    
}
