using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SummonerEnemy : MonoBehaviour
{
    // ---------------------------------------------------------------------
    // [1] 체력 및 UI 관련 변수
    // ---------------------------------------------------------------------

    [Header("Enemy Data")]
    public EnemyData enemyData;

    [Header("Health Settings (To Be Implemented)")]
    public int currentHP;
    // private HealthBarManager healthBarManager; // [TODO: UI]

    // ---------------------------------------------------------------------
    // [2] 공통 변수 (AI/렌더러)
    // ---------------------------------------------------------------------
    private NavMeshAgent agent;
    private Transform player;
    [SerializeField] private float traceRange = 15f;
    [SerializeField] private float moveSpeed = 3.5f;

    // 기타 (피격 관련)
    private Renderer enemyRenderer;
    private Color originalColor;
    [SerializeField] private float flashDuration = 0.1f;

    // ---------------------------------------------------------------------
    // [3] 소환술사 전용 변수
    // ---------------------------------------------------------------------
    public enum EnemyState { Idle, Trace, Summon_Charge, Summon_Action }
    public EnemyState currentState = EnemyState.Idle;

    [Header("Summon Settings")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionCount = 3;
    [SerializeField] private float minionSpawnRange = 3f;
    [SerializeField] private float summonCooldown = 10f;
    private float lastSummonTime = -10f;
    [SerializeField] private float chargeDuration = 1.0f;


    // [제거됨]: [Header("Visual Effects")], [SerializeField] private GameObject summonEffectPrefab;

    void Start()
    {
        // 체력 초기화
        if (enemyData != null)
        {
            currentHP = enemyData.maxHealth; // ScriptableObject에서 체력 가져옴
        }
        else
        {
            currentHP = 10;
            Debug.LogError(gameObject.name + ": EnemyData가 할당되지 않았습니다!");
        } // [TODO: UI]

        // UI 매니저 등록은 나중에 Health Component를 만들 때 구현합니다. 
        // [TODO: UI]

        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponentInChildren<Renderer>();
        }

        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.HasProperty("_BaseColor") ?
                                enemyRenderer.material.GetColor("_BaseColor") :
                                enemyRenderer.material.color;
        }

        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
        ChangeState(EnemyState.Idle);
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 상태 로직 처리
        switch (currentState)
        {
            case EnemyState.Idle:
                IdleUpdate(distanceToPlayer);
                break;
            case EnemyState.Trace:
                TraceUpdate(distanceToPlayer);
                break;
            case EnemyState.Summon_Charge:
                // Coroutine에서 기 모으기 시간 처리
                break;
            case EnemyState.Summon_Action:
                // Coroutine에서 소환 후 상태 전환 처리
                break;
        }
    }

    // --- 상태 전환 메서드 ---
    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        // 이전 상태 종료 로직
        if (currentState == EnemyState.Summon_Charge)
        {
            StopCoroutine("SummonSequence");
        }

        currentState = newState;

        // 새 상태 진입 로직 - NavMeshAgent 제어
        if (agent != null)
        {
            if (newState == EnemyState.Idle || newState == EnemyState.Summon_Charge || newState == EnemyState.Summon_Action)
            {
                agent.isStopped = true; // 이동 멈춤
                if (newState == EnemyState.Summon_Charge) agent.velocity = Vector3.zero; // 소환 시작 시 관성 제거
            }
            else
            {
                agent.isStopped = false;
            }
        }

        if (newState == EnemyState.Summon_Charge)
        {
            StartCoroutine("SummonSequence");
        }
    }

    // --- 상태별 Update 로직 ---
    void IdleUpdate(float distanceToPlayer)
    {
        if (distanceToPlayer <= traceRange)
        {
            ChangeState(EnemyState.Trace);
        }
        else if (Time.time >= lastSummonTime + summonCooldown)
        {
            ChangeState(EnemyState.Summon_Charge);
        }
    }

    void TraceUpdate(float distanceToPlayer)
    {
        // 소환 쿨다운 확인
        if (Time.time >= lastSummonTime + summonCooldown)
        {
            ChangeState(EnemyState.Summon_Charge);
        }
        else if (distanceToPlayer <= traceRange)
        {
            TracePlayer(); // 플레이어를 계속 추적
        }
        else
        {
            ChangeState(EnemyState.Idle);
        }
    }

    // --- 소환 로직 (Coroutine 사용) ---
    IEnumerator SummonSequence()
    {
        // 1. 소환 준비 (기 모으기) 상태
        ChangeState(EnemyState.Summon_Charge);
        if (agent != null) agent.velocity = Vector3.zero;

        // [제거됨]: 데칼 생성 로직

        // 시각적 피드백: 파란색으로 변경
        if (enemyRenderer != null)
        {
            enemyRenderer.material.SetColor("_BaseColor", Color.blue);
        }

        yield return new WaitForSeconds(chargeDuration); // 1초 대기

        // 2. 소환 실행 상태
        ChangeState(EnemyState.Summon_Action);

        // 몬스터 소환
        PerformSummon();

        // 소환 직후 색상 복원
        if (enemyRenderer != null)
        {
            enemyRenderer.material.SetColor("_BaseColor", originalColor);
        }

        // 3. 쿨다운 업데이트
        lastSummonTime = Time.time;

        // 4. 다음 상태로 복귀
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= traceRange)
        {
            ChangeState(EnemyState.Trace);
        }
        else
        {
            ChangeState(EnemyState.Idle);
        }
    }

    void PerformSummon()
    {
        if (minionPrefab == null) return;

        for (int i = 0; i < minionCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * minionSpawnRange;
            Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPosition, out hit, minionSpawnRange, NavMesh.AllAreas))
            {
                spawnPosition = hit.position;
            }

            // 미니언 소환: MinionRise 호출 대신 즉시 생성
            GameObject minion = Instantiate(minionPrefab, spawnPosition, Quaternion.identity);

            // [제거됨]: StartCoroutine(MinionRise(...));
        }
    }


    // [제거됨]: IEnumerator MinionRise(...) 코루틴 전체 삭제

    void TracePlayer()
    {
        if (agent == null) return;
        agent.speed = moveSpeed;
        agent.SetDestination(player.position);
    }

    // --- 피격 및 사망 로직 ---

    public void FlashOnHit()
    {
        StopAllCoroutines();
        StartCoroutine(FlashColor());
    }

    IEnumerator FlashColor()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.SetColor("_BaseColor", Color.red);
            yield return new WaitForSeconds(flashDuration);
            enemyRenderer.material.SetColor("_BaseColor", originalColor);
        }
    }

    public void TakeDamage(int damage)
    {
        FlashOnHit();
        currentHP -= damage;

        // [TODO: UI]
        // if (healthBarManager != null) { healthBarManager.UpdateEnemyHealth(this); } 

        if (currentHP <= 0) Die();
    }

    void Die()
    {
        // 몬스터의 경험치 데이터를 가져와서
        int expAmount = enemyData.experienceGained;

        // 몬스터 처치 시 경험치 부여 (싱글톤 Instance를 통해 접근)
        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.AddExperience(expAmount);
        }

        // [TODO: UI]
        // if (healthBarManager != null) { healthBarManager.UnregisterEnemy(this); } 

        Destroy(gameObject);
    }

    // --- 에디터 시각화 ---
    private void OnDrawGizmosSelected()
    {
        // 추적 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, traceRange);

        // 미니언 소환 범위
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minionSpawnRange);
    }
}