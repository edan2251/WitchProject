using UnityEngine;
using System.Collections; // [★추가★] IEnumerator 사용 시 필요 (지금은 사용 안 함)

/// <summary>
/// 지정된 범위 내에서 주기적으로 적을 스폰합니다.
/// 최대 자식 수, 최대 소환 횟수 제한 기능 추가.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("스폰 대상 설정")]
    public GameObject enemyPrefab;

    [Header("스폰 시간 설정 (랜덤)")]
    public float minSpawnInterval = 5f;
    public float maxSpawnInterval = 10f;

    [Header("스폰 수량 및 범위")]
    public int spawnAmount = 1;
    public float spawnRange = 5f;

    // [★추가★] 스폰 제한 설정
    [Header("스폰 제한")]
    [Tooltip("스포너 하위에 동시에 존재할 수 있는 최대 자식(적) 수")]
    public int maxChildren = 10;
    [Tooltip("스포너가 스폰 동작을 실행할 수 있는 최대 횟수")]
    public int maxSpawnEvents = 6;
    private int currentSpawnEvents = 0; // 현재까지 스폰한 횟수

    // --- 내부 변수 ---
    private float timer = 0f;
    private float nextSpawnTime;
    private bool canSpawn = true; // [★추가★] 스폰 가능 여부 플래그

    // --- Unity 생명주기 함수 ---

    void Start()
    {
        SetNextSpawnTime();
        currentSpawnEvents = 0; // 시작 시 스폰 횟수 초기화
        canSpawn = true; // 시작 시 스폰 가능
    }

    void Update()
    {
        // 스폰 가능 상태가 아니면 Update 로직 중지
        if (!canSpawn) return;

        timer += Time.deltaTime;

        // [★수정★] 스폰 조건 변경
        // 1. 최대 스폰 횟수를 넘지 않았고,
        // 2. 현재 자식 수가 최대치보다 "적고", (같으면 스폰 안 함, 요청하신 예외 규칙 반영)
        // 3. 타이머가 다음 스폰 시간에 도달했는지 확인
        if (currentSpawnEvents < maxSpawnEvents &&
            transform.childCount < maxChildren && // 현재 자식 수가 maxChildren "미만"일 때만 통과
            timer >= nextSpawnTime)
        {
            SpawnEnemies();      // 스폰 함수 호출
            timer = 0f;          // 타이머 초기화
            SetNextSpawnTime();  // 다음 스폰 시간 설정

            // [★추가★] 최대 스폰 횟수에 도달했는지 확인하고 스폰 중지
            if (currentSpawnEvents >= maxSpawnEvents)
            {
                canSpawn = false; // 더 이상 스폰하지 않음
            }
        }
    }


    // --- 커스텀 함수 ---

    /// <summary>
    /// 설정된 spawnAmount만큼 적을 스폰하고 스폰 횟수를 증가시킵니다.
    /// </summary>
    void SpawnEnemies()
    {
        // [★추가★] 실제 스폰 전에 한 번 더 최대 스폰 횟수 체크 (안전 장치)
        if (currentSpawnEvents >= maxSpawnEvents)
        {
            canSpawn = false; // 혹시 모르니 여기서도 막음
            return;
        }


        for (int i = 0; i < spawnAmount; i++)
        {
            // 스폰 위치 계산 (정사각형 범위)
            Vector3 spawnPos = new Vector3(
                transform.position.x + Random.Range(-spawnRange, spawnRange),
                transform.position.y, // Y축은 스포너와 동일하게
                transform.position.z + Random.Range(-spawnRange, spawnRange)
            );

            // [★중요★] 스포너(자신)를 부모로 하여 적 생성 (기존과 동일)
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
        }

        // [★추가★] 스폰 동작 1회 완료 후 스폰 횟수 증가
        currentSpawnEvents++;
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
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnRange * 2, 0.1f, spawnRange * 2));

        // [★추가★] 현재 자식 수와 스폰 횟수를 씬 뷰에 표시 (디버깅용)
#if UNITY_EDITOR // 에디터에서만 실행되도록 전처리기 사용
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"Children: {transform.childCount}/{maxChildren}");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.0f, $"Spawns: {currentSpawnEvents}/{maxSpawnEvents}");
        if (!canSpawn)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "SPAWN DISABLED");
        }
#endif
    }
}