
using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    public GameObject rightHand_origin;
    public GameObject leftHand_origin;
    public GameObject rightHand_destiny;
    public GameObject leftHand_destiny;
    public Transform weapon_mag;
    public GameObject weapon_extractor;
    private Quaternion leftHand_initialRot;


    private float MoveTimer = 0f;
    [HideInInspector] public int func = 0;

    private bool can_save_left_hand_position;
    private bool can_save_right_hand_position;
    private string left_hand_position_saver;
    private string right_hand_position_saver;

    private CameraShake cameraShake;

    //float moveTime = 0.7f;
    float moveTime;

    void Start()
    {
        cameraShake = GetComponentInParent<CameraShake>();
        func = 0;
        weapon_mag = GetChildMag(transform, "MagHandPosition");

        //mag_position = new Vector3(weapon_mag.transform.position.x, weapon_mag.transform.position.y-0.00f, weapon_mag.transform.position.z);
    }

    Transform GetChildMag(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }


            Transform found = GetChildMag(child, name);
            if (found != null)
            {
                return found;
            }

        }

        return null;
    }

    void Update()
    {

        if (func == 0)
        {

            can_save_left_hand_position = true;
            left_hand_position_saver = "weapon";

            can_save_right_hand_position = true;
            right_hand_position_saver = "weapon";


        }
        else if (func == 1) //Mão esquerda arma para o magazine
        {

            MoveTimer += Time.deltaTime;

            if (MoveTimer <= moveTime)
            {
                // Indo até o magazine
                float t = MoveTimer / moveTime;
                leftHand_origin.transform.position = Vector3.Lerp(leftHand_destiny.transform.position, weapon_mag.position, t);
                leftHand_origin.transform.rotation = Quaternion.Lerp(leftHand_origin.transform.rotation, weapon_mag.rotation, t);
                can_save_left_hand_position = false;


            }
            else
            {
                can_save_left_hand_position = true;
                left_hand_position_saver = "mag";
            }
        }
        else if (func == 2) // Mão esquerda magazine para arma
        {

            MoveTimer += Time.deltaTime;

            if (MoveTimer <= moveTime)
            {
                // Voltando da posição do mag
                float t = MoveTimer / moveTime;
                leftHand_origin.transform.position = Vector3.Lerp(leftHand_origin.transform.position, leftHand_destiny.transform.position, t);
                leftHand_origin.transform.rotation = Quaternion.Lerp(leftHand_origin.transform.rotation, leftHand_initialRot, t);
                can_save_left_hand_position = false;
            }
            else
            {
                can_save_left_hand_position = true;
                left_hand_position_saver = "weapon";
            }
        }
        else if (func == 3) //Mão direita arma para extrator
        {

            MoveTimer += Time.deltaTime;

            if (MoveTimer <= moveTime)
            {
                // Indo até o magazine
                float t = MoveTimer / moveTime;
                rightHand_origin.transform.position = Vector3.Lerp(rightHand_origin.transform.position, weapon_extractor.transform.position, t);
                rightHand_origin.transform.rotation = Quaternion.Lerp(leftHand_initialRot, weapon_extractor.transform.rotation, t);
                can_save_right_hand_position = false;

            }
            else
            {
                can_save_right_hand_position = true;
                right_hand_position_saver = "extractor";
            }
        }
        else if (func == 4) //Mão direita, extrator para arma
        {

            MoveTimer += Time.deltaTime;

            if (MoveTimer <= moveTime)
            {
                // Voltando da posição do mag
                float t = MoveTimer / moveTime;
                rightHand_origin.transform.position = Vector3.Lerp(weapon_extractor.transform.position, rightHand_destiny.transform.position, t);
                rightHand_origin.transform.rotation = Quaternion.Lerp(weapon_extractor.transform.rotation, rightHand_destiny.transform.rotation, t);
                can_save_right_hand_position = false;

            }
            else
            {
                can_save_right_hand_position = true;
                right_hand_position_saver = "weapon";
            }
        }
        else if (func == 5) //Mão esquerda, arma para extrator
        {

            MoveTimer += Time.deltaTime;

            if (MoveTimer <= moveTime)
            {
                // Voltando da posição do mag
                float t = MoveTimer / moveTime;
                leftHand_origin.transform.position = Vector3.Lerp(leftHand_destiny.transform.position, weapon_extractor.transform.position, t);
                leftHand_origin.transform.rotation = Quaternion.Lerp(leftHand_destiny.transform.rotation, weapon_extractor.transform.rotation, t);
                can_save_left_hand_position = false;

            }
            else
            {
                can_save_left_hand_position = true;
                left_hand_position_saver = "extractor";

            }
        }
        else if (func == 6) //Mão esquerda, extrator para arma
        {

            MoveTimer += Time.deltaTime;

            if (MoveTimer <= moveTime)
            {
                // Voltando da posição do mag
                float t = MoveTimer / moveTime;
                leftHand_origin.transform.position = Vector3.Lerp(weapon_extractor.transform.position, leftHand_destiny.transform.position, t);
                leftHand_origin.transform.rotation = Quaternion.Lerp(weapon_extractor.transform.rotation, leftHand_initialRot, t);
                can_save_left_hand_position = false;

            }
            else
            {
                can_save_left_hand_position = true;
                left_hand_position_saver = "weapon";

            }
        }
        else if (func == 7) // Mão esquerda, mag para extrator
        {

            MoveTimer += Time.deltaTime;

            if (MoveTimer <= moveTime)
            {
                // Voltando da posição do mag
                float t = MoveTimer / moveTime;
                leftHand_origin.transform.position = Vector3.Lerp(leftHand_origin.transform.position, weapon_extractor.transform.position, t);
                leftHand_origin.transform.rotation = Quaternion.Lerp(leftHand_origin.transform.rotation, weapon_extractor.transform.rotation, t);
                can_save_left_hand_position = false;

            }
            else
            {
                can_save_left_hand_position = true;
                left_hand_position_saver = "extractor";

            }
        }
        else if (func == 8) // Mão esquerda, extractor para mag
        {
            MoveTimer += Time.deltaTime;

            if (MoveTimer <= moveTime)
            {
                // Voltando da posição do mag
                float t = MoveTimer / moveTime;
                leftHand_origin.transform.position = Vector3.Lerp(leftHand_origin.transform.position, weapon_mag.transform.position, t);
                leftHand_origin.transform.rotation = Quaternion.Lerp(leftHand_origin.transform.rotation, weapon_mag.transform.rotation, t);
                can_save_left_hand_position = false;

            }
            else
            {
                can_save_left_hand_position = true;
                left_hand_position_saver = "mag";

            }
        }

        //Assegura as posições
        if (can_save_left_hand_position && left_hand_position_saver == "mag") //Left hand mag
        {
            leftHand_origin.transform.position = weapon_mag.position;
            leftHand_origin.transform.rotation = weapon_mag.rotation;
        }

        if (can_save_left_hand_position && left_hand_position_saver == "weapon") // left hand weapon
        {
            leftHand_origin.transform.position = leftHand_destiny.transform.position;
            leftHand_origin.transform.rotation = leftHand_destiny.transform.rotation;
        }

        if (can_save_left_hand_position && left_hand_position_saver == "extractor") // left hand extractor
        {
            leftHand_origin.transform.position = weapon_extractor.transform.position;
            leftHand_origin.transform.rotation = weapon_extractor.transform.rotation;
        }

        if (can_save_right_hand_position && right_hand_position_saver == "extractor") //Righ hand extractor
        {
            rightHand_origin.transform.position = weapon_extractor.transform.position;
            rightHand_origin.transform.rotation = weapon_extractor.transform.rotation;
        }

        if (can_save_right_hand_position && right_hand_position_saver == "weapon") // right hand weapon
        {
            rightHand_origin.transform.position = rightHand_destiny.transform.position;
            rightHand_origin.transform.rotation = rightHand_destiny.transform.rotation;

        }

    }

    public void LeftHand_WeaponToMag(float moveTime)
    {
        this.moveTime = moveTime;
        func = 1;
        MoveTimer = 0;
        StartCoroutine(cameraShake.ReloadShake());

    }


    public void LeftHand_MagToWeapon(float moveTime)
    {
        this.moveTime = moveTime;
        func = 2;
        MoveTimer = 0;
        StartCoroutine(cameraShake.ReloadShake());
    }

    public void LeftHand_WeaponToExtractor(float moveTime)
    {
        this.moveTime = moveTime;
        func = 5;
        MoveTimer = 0;
        StartCoroutine(cameraShake.ReloadShake());

    }

    public void LeftHand_ExtractorToWeapon(float moveTime)
    {
        this.moveTime = moveTime;
        func = 6;
        MoveTimer = 0;
        StartCoroutine(cameraShake.ReloadShake());

    }

    public void LeftHand_MagToExtractor(float moveTime)
    {
        this.moveTime = moveTime;
        func = 7;
        MoveTimer = 0;
        StartCoroutine(cameraShake.ReloadShake());
    }

    public void LeftHand_ExtractorToMag(float moveTime)
    {
        this.moveTime = moveTime;
        func = 8;
        MoveTimer = 0;
        StartCoroutine(cameraShake.ReloadShake());
    }

    public void RightHand_WeaponToExtractor(float moveTime)
    {
        this.moveTime = moveTime;
        func = 3;
        MoveTimer = 0;
        StartCoroutine(cameraShake.ReloadShake());
    }

    public void RightHand_ExtractorToWeapon(float moveTime)
    {
        this.moveTime = moveTime;
        func = 4;
        MoveTimer = 0;
        StartCoroutine(cameraShake.ReloadShake());

    }

    public void ReturnWeaponToOriginalPos(float moveTime)
    {
        this.moveTime = moveTime;
        func = 8;
        MoveTimer = 0;
        StartCoroutine(cameraShake.ReloadShake());

    }

    public void SetHandsToWeapon(float moveTime)
    {
        this.moveTime = moveTime;
        func = 0;
        MoveTimer = 0;
    }


}
