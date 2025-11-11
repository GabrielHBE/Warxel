using System;
using UnityEngine;

public class LeverAction : MonoBehaviour
{
    [Header("Hand")]
    public Transform left_hand;
    public Transform right_hand;
    [Header("Lever")]
    public GameObject left_lever;
    public GameObject center_lever;
    public GameObject right_lever;

    [Header("Buttons")]
    public Transform EjectButton;

    [Header("Hand rotation")]
    public Transform center_lever_handPosition;
    public Transform left_lever_handPosition;
    public Transform right_lever_handPosition;


    private Jet jet;
    private Quaternion original_left_lever_rotation;
    private Quaternion original_center_lever_rotation;
    private Quaternion original_right_lever_rotation;

    void Start()
    {
        jet = GetComponent<Jet>();

        original_left_lever_rotation = left_lever.transform.localRotation;
        original_center_lever_rotation = center_lever.transform.localRotation;
        original_right_lever_rotation = right_lever.transform.localRotation;
    }

    void Update()
    {
        if(!jet.is_in_jet) return;
        
        left_hand.position = left_lever_handPosition.position;
        right_hand.position = center_lever_handPosition.position;

        CenterLever();

        LeftLever();
        

    }


    void CenterLever()
    {
        float rotationAmountY = -jet.mouseY / 40;
        float rotationAmountX = -jet.mouseX / 40;

        Vector3 currentRotation = center_lever.transform.localEulerAngles;

        if (currentRotation.x > 180f) currentRotation.x -= 360f;
        if (currentRotation.z > 180f) currentRotation.z -= 360f;

        float newXRotation = currentRotation.x;
        float newZRotation = currentRotation.z;

        if (jet.mouseY == 0)
        {
            // Voltar eixo X à rotação original
            newXRotation = Mathf.LerpAngle(currentRotation.x, original_center_lever_rotation.eulerAngles.x > 180 ? original_center_lever_rotation.eulerAngles.x - 360f : original_center_lever_rotation.eulerAngles.x, Time.deltaTime * 5f);
        }
        else
        {
            newXRotation = Mathf.Clamp(currentRotation.x + rotationAmountY, -20f, 20f);
        }

        if (jet.mouseX == 0)
        {
            // Voltar eixo Z à rotação original
            newZRotation = Mathf.LerpAngle(currentRotation.z, original_center_lever_rotation.eulerAngles.z > 180 ? original_center_lever_rotation.eulerAngles.z - 360f : original_center_lever_rotation.eulerAngles.z, Time.deltaTime * 5f);
        }
        else
        {
            newZRotation = Mathf.Clamp(currentRotation.z + rotationAmountX, -30f, 30f);
        }

        center_lever.transform.localEulerAngles = new Vector3(
            newXRotation,
            currentRotation.y,
            newZRotation
        );
    }


    void LeftLever()
    {
        float rotationAmountY = jet.moveForward/5;
        float rotationAmountX = -jet.lean_value/5;

        Vector3 currentRotation = left_lever.transform.localEulerAngles;

        if (currentRotation.x > 180f) currentRotation.x -= 360f;
        if (currentRotation.z > 180f) currentRotation.z -= 360f;

        float newXRotation = currentRotation.x;
        float newZRotation = currentRotation.z;

        if (jet.moveForward == 0)
        {
            // Voltar eixo X à rotação original
            newXRotation = Mathf.LerpAngle(currentRotation.x, original_left_lever_rotation.eulerAngles.x > 180 ? original_left_lever_rotation.eulerAngles.x - 360f : original_left_lever_rotation.eulerAngles.x, Time.deltaTime * 5f);
        }
        else
        {
            newXRotation = Mathf.Clamp(currentRotation.x + rotationAmountY, -20f, 20f);
        }

        if (jet.lean_value == 0)
        {
            // Voltar eixo Z à rotação original
            newZRotation = Mathf.LerpAngle(currentRotation.z, original_left_lever_rotation.eulerAngles.z > 180 ? original_left_lever_rotation.eulerAngles.z - 360f : original_left_lever_rotation.eulerAngles.z, Time.deltaTime * 5f);
        }
        else
        {
            newZRotation = Mathf.Clamp(currentRotation.z + rotationAmountX, -30f, 30f);
        }

        left_lever.transform.localEulerAngles = new Vector3(
            newXRotation,
            currentRotation.y,
            newZRotation
        );

    }



}
