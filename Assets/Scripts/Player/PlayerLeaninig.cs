using UnityEngine;

public class PlayerLeaninig : MonoBehaviour
{

    public GameObject spine;
    public KeyCode lean_left_key;
    public KeyCode lean_right_key;
    public float rotation_value;
    private Quaternion rotation_left_total;
    private Quaternion rotation_right_total;
    private Quaternion original_rotation;
    private bool is_leaning_left = false;
    private bool is_leaning_right = false;
    public float lean_timer;
    private float elapsed_timer;

    PlayerProperties playerProperties;

    void Start()
    {
        playerProperties = GetComponent<PlayerProperties>();

        rotation_left_total = new Quaternion(spine.transform.localRotation.x,
        spine.transform.localRotation.y,
        spine.transform.localRotation.z + rotation_value,
        spine.transform.localRotation.w);

        rotation_right_total = new Quaternion(spine.transform.localRotation.x,
        spine.transform.localRotation.y,
        spine.transform.localRotation.z - rotation_value,
        spine.transform.localRotation.w);

        original_rotation = spine.transform.localRotation;
    }

    void Update()
    {
        if (Input.GetKeyDown(lean_left_key))
        {
            elapsed_timer = 0;
            is_leaning_left = !is_leaning_left;
            is_leaning_right = false;
        }

        if (Input.GetKeyDown(lean_right_key))
        {
            elapsed_timer = 0;
            is_leaning_right = !is_leaning_right;
            is_leaning_left = false;
        }

        if (is_leaning_left)
        {
            is_leaning_right = false;
            LeanLeft();
        }
        else if (is_leaning_right)
        {
            is_leaning_left = false;
            LeanRight();
        }
        else
        {
            ResetLeaning();
        }

    }

    void ResetLeaning()
    {
        elapsed_timer += Time.deltaTime;

        if (elapsed_timer <= lean_timer)
        {
            float t = elapsed_timer / lean_timer;
            spine.transform.localRotation = Quaternion.Lerp(spine.transform.localRotation, new Quaternion(spine.transform.localRotation.x,
                                                        spine.transform.localRotation.y,
                                                        original_rotation.z,
                                                        spine.transform.localRotation.w), t);
        }
        else
        {
            spine.transform.localRotation = new Quaternion(spine.transform.localRotation.x,
                                                        spine.transform.localRotation.y,
                                                        original_rotation.z,
                                                        spine.transform.localRotation.w);
        }

    }

    void LeanLeft()
    {
        elapsed_timer += Time.deltaTime;

        if (elapsed_timer <= lean_timer)
        {
            float t = elapsed_timer / lean_timer;
            spine.transform.localRotation = Quaternion.Lerp(spine.transform.localRotation, rotation_left_total, t);
        }
        else
        {
            spine.transform.localRotation = rotation_left_total;
        }

    }

    void LeanRight()
    {
        elapsed_timer += Time.deltaTime;

        if (elapsed_timer <= lean_timer)
        {
            float t = elapsed_timer / lean_timer;
            spine.transform.localRotation = Quaternion.Lerp(spine.transform.localRotation, rotation_right_total, t);
        }
        else
        {
            spine.transform.localRotation = rotation_right_total;
        }

    }

}
