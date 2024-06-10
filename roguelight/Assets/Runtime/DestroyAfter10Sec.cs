using UnityEngine;

public class DestroyAfter10Sec : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 30f);
    }
}
