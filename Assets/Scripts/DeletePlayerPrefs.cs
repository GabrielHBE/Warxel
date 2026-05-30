using UnityEngine;

public class DeletePlayerPrefs : MonoBehaviour
{
    [SerializeField] private bool deletePlayerPrefs;
    void Awake()
    {
        if (!deletePlayerPrefs) return;

        // Deleta todas as chaves e valores do PlayerPrefs
        PlayerPrefs.DeleteAll();

        // (Opcional) Salva imediatamente as alterações no disco
        PlayerPrefs.Save();

        Debug.Log("Todos os dados do PlayerPrefs foram deletados com sucesso!");
    }
}
