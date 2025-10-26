using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;

    // 1. 랜덤 스폰 시간 설정
    public float minSpawnInterval = 5f;  // <-- 최소 스폰 시간 (5초)
    public float maxSpawnInterval = 10f; // <-- 최대 스폰 시간 (10초)

    // 2. 스폰 양 조절
    public int spawnAmount = 1;          // <-- 한 번에 스폰할 양

    public float spawnRange = 5f;

    private float timer = 0f;
    private float nextSpawnTime; // <-- 다음 스폰까지 걸릴 랜덤 시간

    void Start()
    {
        // 1. 시작할 때 첫 랜덤 스폰 시간 설정
        SetNextSpawnTime();
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 3. 타이머가 설정된 '다음 스폰 시간'을 넘었는지 확인
        if (timer >= nextSpawnTime)
        {
            // 4. spawnAmount 만큼 스폰 로직 반복
            for (int i = 0; i < spawnAmount; i++)
            {
                Vector3 spawnPos = new Vector3(
                    transform.position.x + Random.Range(-spawnRange, spawnRange),
                    transform.position.y,
                    transform.position.z + Random.Range(-spawnRange, spawnRange)
                );
                Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
            }

            timer = 0f; // 타이머 초기화
            SetNextSpawnTime(); // 5. 다음 스폰 시간 다시 랜덤으로 설정
        }
    }

    // 다음 스폰 시간을 랜덤으로 설정하는 함수
    void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        // 6. 기즈모 크기 수정 (정사각형)
        // 기존 코드(spawnRange * spawnRange * 2)는 오타였습니다.
        // -spawnRange ~ +spawnRange 까지의 총 길이는 'spawnRange * 2' 입니다.
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnRange * 2, 0.1f, spawnRange * 2));
    }
}