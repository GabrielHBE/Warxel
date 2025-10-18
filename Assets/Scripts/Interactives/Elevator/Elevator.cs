using UnityEngine;

public class Elevator : MonoBehaviour
{
    [SerializeField] private GameObject door;
    [SerializeField] private ElevatorController elevator_controller;
    [SerializeField] private AudioSource elevator_sound;
    bool can_move = true;
    public bool isMoving = false;
    
    public int currentFloor = 0;

    public System.Collections.IEnumerator MoveElevator(Vector3 targetPosition, int floor)
    {

        isMoving = true;
        Debug.Log("Moving Elevator to: " + targetPosition);

        // Fecha a porta
        if (door != null)
        {
            yield return new WaitForSeconds(1f);
        }

        // Move o elevador
        float speed = 5f;
        Vector3 horizontalPosition = new Vector3(transform.localPosition.x, targetPosition.y, transform.localPosition.z);

        while (Vector3.Distance(transform.localPosition, horizontalPosition) > 0.01f && can_move)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                horizontalPosition,
                speed * Time.deltaTime
            );
            yield return null;
        }

        elevator_sound.Stop();

        currentFloor = floor;

        // Garante que chegou na posição final mesmo se can_move ficar false
        if (can_move)
        {
            transform.localPosition = horizontalPosition;
        }

        isMoving = false;
    }

}