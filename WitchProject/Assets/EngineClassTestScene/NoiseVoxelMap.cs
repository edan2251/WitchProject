using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class NoiseVoxelMap : MonoBehaviour
{
    public GameObject[] BlockPrefab; //0 == Dirt / 1 == Grass / 2 == Water

    public int width = 40;          //가로 x
    public int depth = 40;          //깊이 z
    public int maxHeight = 16;      //최대 높이 y

    public int waterLevel = 5;      //물높이

    [SerializeField] float noiseScale = 16f;    //변화도 - 값이 높을수록 평평한 지형
    void Start()
    {
        float offsetX = Random.Range(-9999f, 9999f);
        float offsetZ = Random.Range(-9999f, 9999f);

        for (int x = 0; x < width; x++)
        {
            for(int z = 0; z < depth; z++)
            {
                float nx = (x + offsetX) / noiseScale;
                float nz = (z + offsetZ) / noiseScale;

                float noise = Mathf.PerlinNoise(nx, nz);    //0-1사이 랜덤한 값  출력

                int h = Mathf.FloorToInt(noise * maxHeight);    //최대 높이만큼 곱해주고 소수점 버림

                if (h <= 0) continue;       //0 이하일땐 생성 안함

                for (int y = 0; y <= h; y++)
                {
                    if ( y <= h - 1)
                    {
                        PlaceBlock(BlockPrefab[0], x, y, z);
                    }

                    if ( y == h)
                    {
                        PlaceBlock(BlockPrefab[1], x, y, z);

                    }
                }

                for (int waterY = h + 1; waterY <= waterLevel; waterY++)
                {
                    PlaceBlock(BlockPrefab[2], x, waterY, z);
                }

            }
        }
    }

    private void PlaceBlock(GameObject prefab, int x, int y, int z)
    {
        var go = Instantiate(prefab, new Vector3(x, y, z), Quaternion.identity, transform);
        go.name = $"{prefab.name}_{x}_{y}_{z}";
    }

}
