using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    public Camera player_camera;
    public GameObject neck;
    public float position;
    void Start()
    {
        
    }

    void Update()
    {
        player_camera.transform.localPosition = new Vector3(neck.transform.localPosition.x, neck.transform.localPosition.y + position, neck.transform.localPosition.z + 0.01f);
    }

}
