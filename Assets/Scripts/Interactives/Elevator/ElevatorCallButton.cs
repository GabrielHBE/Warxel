using UnityEngine;

public class ElevatorCallButton : MonoBehaviour
{
    public int floor;
    [SerializeField] private GameObject door;
    [SerializeField] private Elevator elevator;
    [SerializeField] private Vector3 elevatorPosition;
    [SerializeField] private KeyCode Key_callbutton = KeyCode.E;
    [SerializeField] private float button_distance = 10f;

    bool can_move_door = false;
    
    public void Interact()
    {
        can_move_door = true;
        if (elevator != null)
        {
            StartCoroutine(elevator.MoveElevator(elevatorPosition, floor));
        }
    }
    
    void Update()
    {   

        if (door != null && elevator != null && can_move_door)
        {
            if (elevator.currentFloor == floor && !elevator.isMoving)
            {
                StartCoroutine(OpenDoor());
            }
            else
            {
                StartCoroutine(CloseDoor());
            }
        }

    }

    public System.Collections.IEnumerator CloseDoor()
    {
        
        yield return new WaitForSeconds(1f);

        float duration = 1f;
        float timer = 0;
        Vector3 startScale = door.transform.localScale;
        Vector3 targetScale = new Vector3(startScale.x, 1f, startScale.z);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            door.transform.localScale = Vector3.Lerp(startScale, targetScale, timer / duration);
            yield return null;
        }

        door.transform.localScale = targetScale;
        can_move_door = false;
        Debug.Log("Door fully closed");
    }

    public System.Collections.IEnumerator OpenDoor()
    {
        yield return new WaitForSeconds(1f);

        float duration = 1f;
        float timer = 0;
        Vector3 startScale = door.transform.localScale;
        Vector3 targetScale = new Vector3(startScale.x, 0.05f, startScale.z);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            door.transform.localScale = Vector3.Lerp(startScale, targetScale, timer / duration);
            yield return null;
        }

        door.transform.localScale = targetScale;
        can_move_door = false;
        Debug.Log("Door fully opened");
    }



}
