using UnityEngine;

public class EnterJet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Jet jet;
    void Start()
    {
        jet = GetComponentInParent<Jet>();
    }

    void OnTriggerStay(Collider collision)
    {

        if (collision.gameObject.CompareTag("Player"))
        {
            jet.Enter_Exit(collision.gameObject);
        }

    }
}
