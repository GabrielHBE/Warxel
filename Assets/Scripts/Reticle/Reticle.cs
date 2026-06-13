using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Reticle : MonoBehaviour
{

    [Header("Instances")]
    [SerializeField] private Camera playerCamera;
    public AdsBehaviour adsBehaviour;
    [SerializeField] private Transform parent;
    [SerializeField] private SwitchWeapon switchWeapon;
    [SerializeField] private List<Sprite> reticleList = new List<Sprite>();
    [SerializeField] private Image reticleImage;
    [SerializeField] private PlayerProperties playerProperties;

    private Sight active_sight;

    public void Restart()
    {
        active_sight = parent.GetComponentInChildren<Sight>();
        if (active_sight != null)
        {
            if (active_sight.reticle != null)
            {
                foreach (Sprite r in reticleList)
                {
                    if (r.name == active_sight.reticle)
                    {
                        reticleImage.sprite = r;
                        reticleImage.color = Color.red;
                        reticleImage.enabled = true;
                        return;
                    }

                }
            }

        }

        reticleImage.sprite = null;
        reticleImage.enabled = false;

    }

    void LateUpdate()
    {

        if (adsBehaviour.dot_position && !playerProperties.is_reloading && !switchWeapon._switch && active_sight != null && reticleImage.sprite != null)
        {
            transform.position = playerCamera.WorldToScreenPoint(active_sight.adsPosition.position);
            reticleImage.transform.localScale = new Vector3(Settings.Instance._gameplay.sight_reticle_size, Settings.Instance._gameplay.sight_reticle_size, 1f);
            reticleImage.enabled = true;
        }
        else
        {
            reticleImage.enabled = false;
        }


    }

}
