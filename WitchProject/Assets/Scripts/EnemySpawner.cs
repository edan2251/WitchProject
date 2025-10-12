using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 3f;
    public float spawnRange = 5f;

    private float timer = 0f;

   
    void Update()
    {
        timer += Time.deltaTime;

        if(timer >= spawnInterval)
        {
            Vector3 spawnPos = new Vector3(
                transform.position.x + Random.Range(-spawnRange, spawnRange),
                transform.position.y,
                transform.position.z + Random.Range(-spawnRange, spawnRange)
                );
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            timer = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnRange * spawnRange * 2, 0.1f, spawnRange * 2));
    }
}
