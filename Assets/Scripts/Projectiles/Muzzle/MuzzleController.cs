using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class MuzzleController : NetworkBehaviour
{
    [SerializeField] private GameObject muzzlePrefab;

    private float muzzleLifetime;
    private readonly List<GameObject> instantiatedMuzzles = new List<GameObject>();

    #region Network Methods
    public void RequestClearMuzzles() => ClearMuzzles();

    public void RequestSetMuzzleLifetime(float muzzleLifetime) => SetMuzzleLifetime(muzzleLifetime);

    public void RequestSetupMuzzle(Transform parent) => SetupMuzzle(parent);

    [ServerRpc]
    public void ServerRpcCreateMuzzle() => CmdCreateMuzzle();

    [ObserversRpc(ExcludeOwner = true)]
    private void CmdCreateMuzzle() => CreateMuzzle();
    #endregion

    #region Ordinary Methods
    private void ClearMuzzles()
    {
        foreach (GameObject muzzle in instantiatedMuzzles)
        {
            if (muzzle != null) Destroy(muzzle);
        }

        instantiatedMuzzles.Clear();
    }
    private void SetMuzzleLifetime(float muzzleLifetime) => this.muzzleLifetime = Mathf.Clamp(muzzleLifetime * 0.6f, 0, 0.15f);

    private void SetupMuzzle(Transform parent)
    {
        if (muzzlePrefab == null)
        {
            Debug.LogWarning($"{nameof(MuzzleController)} has no muzzle prefab assigned.", this);
            return;
        }

        if (parent == null)
        {
            Debug.LogWarning($"Cannot setup muzzle without a {nameof(WeaponProperties.shootPos)}.", this);
            return;
        }

        GameObject im = Instantiate(muzzlePrefab, parent, false);

        // Zera a posição e rotação locais para ficar exatamente no mesmo lugar do parent
        im.transform.localPosition = Vector3.zero;
        im.transform.localRotation = Quaternion.identity;

        im.SetActive(false);
        instantiatedMuzzles.Add(im);
    }

    private void CreateMuzzle()
    {
        foreach (GameObject m in instantiatedMuzzles)
        {
            if (m != null) StartCoroutine(MuzzleLifeTime(m));
        }
    }

    private IEnumerator MuzzleLifeTime(GameObject m)
    {
        m.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        m.SetActive(true);
        yield return new WaitForSeconds(muzzleLifetime);
        m.SetActive(false);
    }

    public void RequestCreateMuzzle()
    {
        if (IsOwner) CreateMuzzle();
        else ServerRpcCreateMuzzle();
    }
    #endregion
}
