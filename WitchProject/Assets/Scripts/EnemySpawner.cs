using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지정된 범위 내에서 주기적으로 적을 스폰합니다.
/// 스폰 시간, 스폰 양, 스폰 범위를 조절할 수 있습니다.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("스폰 대상 설정")]
    public GameObject enemyPrefab;

    [Header("스폰 시간 설정 (랜덤)")]
    public float minSpawnInterval = 5f;  // 최소 스폰 시간
    public float maxSpawnInterval = 10f; // 최대 스폰 시간

    [Header("스폰 수량 및 범위")]
    public int spawnAmount = 1;          // 한 번에 스폰할 양
    public float spawnRange = 5f;        // 스폰 범위 (정사각형)

    // --- 내부 변수 ---
    private float timer = 0f;
    private float nextSpawnTime; // 다음 스폰까지 걸릴 랜덤 시간


    // --- Unity 생명주기 함수 ---

    void Start()
    {
        // 시작할 때 첫 랜덤 스폰 시간 설정
        SetNextSpawnTime();
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 타이머가 설정된 '다음 스폰 시간'을 넘었는지 확인
        if (timer >= nextSpawnTime)
        {
            SpawnEnemies();     // 스폰 함수 호출
            timer = 0f;         // 타이머 초기화
            SetNextSpawnTime(); // 다음 스폰 시간 다시 랜덤으로 설정
        }
    }


    // --- 커스텀 함수 ---

    /// <summary>
    /// 설정된 spawnAmount만큼 적을 스폰합니다.
    /// </summary>
    void SpawnEnemies()
    {
        for (int i = 0; i < spawnAmount; i++)
        {
            // 스폰 위치 계산 (정사각형 범위)
            Vector3 spawnPos = new Vector3(
                transform.position.x + Random.Range(-spawnRange, spawnRange),
                transform.position.y,
                transform.position.z + Random.Range(-spawnRange, spawnRange)
            );

            // 스포너(자신)를 부모로 하여 적 생성
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
        }
    }

    /// <summary>
    /// 다음 스폰 시간을 min/max 사이의 랜덤 값으로 설정합니다.
    /// </summary>
    void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
    }


    // --- 에디터 전용 함수 ---

    /// <summary>
    /// 씬(Scene) 뷰에서 스폰 범위를 시각적으로 표시합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // 스폰 범위를 정사각형 와이어 큐브로 표시
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnRange * 2, 0.1f, spawnRange * 2));
    }
}