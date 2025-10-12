using UnityEngine;

public class DestroyAfterDelay : MonoBehaviour
{
    public float delay = 1.0f;

    void Start()
    {
        Destroy(gameObject, delay);
    }
}