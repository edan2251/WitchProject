using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlaceCharacter : MonoBehaviour
{
    public GameObject Character;

    [SerializeField] NoiseVoxelMap NoiseVoxelMap;

    void Start()
    {
        
    }

    private void SpawnCharacter(int x, int y, int z)
    {
        var go = Instantiate(Character, new Vector3(x, y, z), Quaternion.identity, transform);
        go.name = $"{Character.name}_{x}_{y}_{z}";
    }
}
