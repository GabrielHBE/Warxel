using UnityEngine;
using System.Collections;


public class BulletExtractor : MonoBehaviour
{

    public GameObject bullet;
    //public GameObject bullet_extractor;

    public void CreateBullet()
    {
        float random = Random.Range(1f,10f);
        StartCoroutine(FireBulletWithDrop(gameObject.transform.position, gameObject.transform.right *random, 5, 20));
    }



    //this whole function works in meters
    IEnumerator FireBulletWithDrop(Vector3 StartingPosition, Vector3 ForwardVector, float BulletSpeed, float MaxBulletDistance, float Gravity = 40f)
    {
        float random = Random.Range(-180, 180);
        Quaternion _rotation = new Quaternion(random, random, random, random);

        GameObject bulletInstance = Instantiate(bullet, StartingPosition, _rotation);
        Destroy(bulletInstance, 2f);
        float TimeSpace = 0.01f; // this variable is how long it will wait before calculating the next raycast, that one is how much the bullet will move in the time that's then calculated(this calculation doest take into accout gravity because gravity is a force), you can replace that one for a smaller number if you want a more acurate raycasts though more performace expensive
        Gravity /= -2;//in all the calculation that will be done with gravity it will be divided by 2 so instead of dividing it everytime i just divide it up here once and that should improve performance by a tiny bit
        Vector3 StartPos = StartingPosition;//this variable holds the start point of each raycast
        Vector3 EndPos = StartingPosition + (BulletSpeed * TimeSpace * ForwardVector) + new Vector3(0, Gravity * TimeSpace * TimeSpace, 0);//this variable holds the end point of each raycast
        float CurrentTime = 0;//this variable holds how much time has passed
        do
        {
            Debug.DrawLine(StartPos, EndPos,Color.blue,500);//this is just for displaying the path of the bullet on the scene, you can remove it if you want
            bulletInstance.transform.position = StartPos;

            /*
            RaycastHit[] hits = Physics.RaycastAll(StartPos, EndPos - StartPos, Vector3.Distance(StartPos, EndPos));//this casts the actual raycast and retunrs the objects it has hit
            //use this space bellow to do watever you want wiht the objects that were hit, they're stored in the hits variable
            foreach (var ObjectHit in hits)//this loop goes through each object that was hit with this raycast
            {
                Debug.Log(ObjectHit.transform.name);//this prints the name of each object that was hit, replace it with whatever you want to happen with each object that was hit
            }
            */
            ///////////////////////
            yield return new WaitForSeconds(TimeSpace);//Time for waiting
            CurrentTime += TimeSpace;//add to current time
            StartPos = EndPos;//the next raycast should start and the end the last one
            EndPos = StartingPosition + (BulletSpeed * CurrentTime * ForwardVector) + new Vector3(0, Gravity * CurrentTime * CurrentTime, 0);//calculate the end pos of the next raycast
        }
        while (MaxBulletDistance > Vector3.Distance(StartingPosition, EndPos));//it will keep calculating raycast until it reachs a distance greater than the max bullet distance

    }

}
