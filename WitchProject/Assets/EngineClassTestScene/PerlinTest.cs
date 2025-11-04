using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Perlin Noise Value: " + Mathf.PerlinNoise(0.5f, 0.1f));
        Debug.Log("Perlin Noise Value: " + Mathf.PerlinNoise(0.5f, 0.2f));
        Debug.Log("Perlin Noise Value: " + Mathf.PerlinNoise(0.5f, 0.3f));
        Debug.Log("Perlin Noise Value: " + Mathf.PerlinNoise(0.5f, 0.4f));
        Debug.Log("Perlin Noise Value: " + Mathf.PerlinNoise(0.5f, 0.5f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
