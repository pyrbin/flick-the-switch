using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    void Start()
    {
        Look();
    }
    public void Look()
    {
        const float maxRandomAngle = 15f;
        Vector3 directionToCamera = Camera.main.transform.position - transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
        float randomYaw = Freya.Random.Range(-maxRandomAngle, maxRandomAngle);
        float randomPitch = Freya.Random.Range(-maxRandomAngle, maxRandomAngle);
        float randomRoll = Freya.Random.Range(-maxRandomAngle, maxRandomAngle);
        Quaternion randomRotation = Quaternion.Euler(randomPitch, randomYaw, randomRoll);
        Quaternion finalRotation = lookRotation * randomRotation;
        transform.rotation = finalRotation;
    }
}
