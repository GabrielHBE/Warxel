using UnityEngine;
using System.Collections;

public class BulletExtractor : MonoBehaviour
{
    public GameObject bullet;
    //public GameObject bullet_extractor;

    public void CreateBullet()
    {
        float random = Random.Range(1f,3f);
        StartCoroutine(FireBulletWithDrop(gameObject.transform.position, gameObject.transform.right * random, 2, 20));
    }

    //this whole function works in meters
    IEnumerator FireBulletWithDrop(Vector3 StartingPosition, Vector3 ForwardVector, float BulletSpeed, float MaxBulletDistance, float Gravity = -20f)
    {
        // Cria uma rotação aleatória em Euler angles
        float randomX = Random.Range(0f, 360f);
        float randomY = Random.Range(0f, 360f);
        float randomZ = Random.Range(0f, 360f);
        Quaternion randomRotation = Quaternion.Euler(randomX, randomY, randomZ);

        GameObject bulletInstance = Instantiate(bullet, StartingPosition, randomRotation);

        float TimeSpace = 0.01f;
        Vector3 StartPos = StartingPosition;
        Vector3 EndPos = StartingPosition + (BulletSpeed * TimeSpace * ForwardVector) + new Vector3(0, Gravity * TimeSpace * TimeSpace, 0);
        float CurrentTime = 0;
        
        do
        {
            bulletInstance.transform.position = StartPos;
            
            yield return new WaitForSeconds(TimeSpace);
            CurrentTime += TimeSpace;
            StartPos = EndPos;
            EndPos = StartingPosition + (BulletSpeed * CurrentTime * ForwardVector) + new Vector3(0, Gravity * CurrentTime * CurrentTime, 0);
        }
        while (MaxBulletDistance > Vector3.Distance(StartingPosition, EndPos));

        Destroy(bulletInstance);
    }
}