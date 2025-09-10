using UnityEngine;

public class Reticle : MonoBehaviour
{
    [Header("Instances")]
    public Weapon weapon;
    public SwayNBobScript swayNBob;
    private Sight active_sight;
    private PlayerProperties playerProperties;
    private GameObject reticle;
    private Material material;
    private MeshRenderer mesh;
    [SerializeField] private SwitchWeapon switchWeapon;


    [Header("Customization")]
    public float reticle_size;
    public Color sight_color;


    Vector3 original_pos;
    float magnitude = 0.07f;

    void Start()
    {
        transform.localScale = new Vector3(reticle_size, reticle_size, reticle_size);
        playerProperties = GetComponentInParent<PlayerProperties>();

    }

    public void Restart()
    {

        active_sight = transform.root.GetComponentInChildren<Sight>();

        if (active_sight == null || active_sight.reticle == "")
        {
            reticle = null;
            return;
        }

        foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
        {
            if (child.gameObject.name == active_sight.reticle)
            {
                reticle = child.gameObject;

                original_pos = reticle.transform.localPosition;

                MeshRenderer rend = reticle.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    material = rend.material;
                    material.color = sight_color;
                    material.SetColor("_EmissionColor", sight_color);
                    material.EnableKeyword("_EMISSION");

                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    rend.receiveShadows = false;
                    mesh = reticle.GetComponent<MeshRenderer>();

                }
            }

        }

    }


    void Update()
    {

        if (reticle != null)
        {

            if (weapon.dot_position && playerProperties.isGrounded && !playerProperties.is_reloading && !switchWeapon._switch)
            {
                if (active_sight != null)
                {
                    if (playerProperties.is_firing)
                    {
                        Shake();
                    }
                    else
                    {
                        reticle.transform.localPosition = original_pos;
                    }
                    mesh.enabled = true;
                }

            }
            else
            {
                if (active_sight != null)
                {
                    mesh.enabled = false;
                }
            }
        }

    }

    void Shake()
    {
        float x = Random.Range(-0.1f, 0.1f) * magnitude;
        float y = Random.Range(-0.1f, 0.1f) * magnitude;

        reticle.transform.localPosition = new Vector3(original_pos.x + x, original_pos.y + y, original_pos.z);
    }


}
