using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// [추가] 소환 웨이브를 인스펙터에서 설정하기 위한 클래스
[System.Serializable]
public class SummonWaveEntry
{
    public GameObject minionPrefab;
    [Min(1)] // 최소 1마리
    public int count = 1;
}

// [수정] MonoBehaviour 대신 BaseEnemy를 상속
public class SummonerEnemy : BaseEnemy
{
    // ... (체력 관련 변수 삭제됨) ...

    // --- 공통 변수 (AI) ---
    //private NavMeshAgent agent;
    [SerializeField] private float traceRange = 15f;
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Summoner AI")]
    [SerializeField] private float orbitDistance = 13f; // 이 거리 안으로 들어오면 서성거리기 시작
    [SerializeField] private float orbitWanderRadius = 10f; // 서성거리는 반경
    [SerializeField] private float orbitWanderTimer = 5f;  // 새 서성거리기 위치를 찍는 주기
    private float lastOrbitWanderTime; // 마지막 서성거리기 시간

    // --- 소환술사 전용 변수 ---
    public enum EnemyState { Idle, Trace, Summon_Charge, Summon_Action }
    public EnemyState currentState = EnemyState.Idle;

    [Header("Summon Settings")]
    [SerializeField] private List<SummonWaveEntry> summonWave;

    [SerializeField] private float minionSpawnRange = 3f;
    [SerializeField] private float summonCooldown = 10f;
    private float lastSummonTime = -10f;
    [SerializeField] private float chargeDuration = 1.0f;

    [Header("Idle Wandering")]
    [SerializeField] private float idleWanderRadius = 25f; // Idle 상태에서 배회하는 반경
    [SerializeField] private float idleWanderTimer = 8f;   // 새 배회 지점을 찍는 주기
    private float lastIdleWanderTime; // 마지막 배회 시간



    protected override void Awake()
    {
        base.Awake();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    protected override void OnDisable()
    {
        base.OnDisable();
    }

    public override void Start()
    {
        base.Start(); // 부모(BaseEnemy)의 Start() 호출

        //agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
        ChangeState(EnemyState.Idle);

        lastIdleWanderTime = -idleWanderTimer;

    }

    // ... (Update, ChangeState, IdleUpdate, TraceUpdate 함수는 이전과 동일) ...
    void Update()
    {
        UpdateTarget();

        if (currentTarget == null && currentState != EnemyState.Idle)
        {
            // 타겟이 없는데 Idle 상태가 아니라면 Idle로 전환 (안전장치)
            ChangeState(EnemyState.Idle);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                IdleUpdate(distanceToTarget);
                break;
            case EnemyState.Trace:
                TraceUpdate(distanceToTarget);
                break;
            case EnemyState.Summon_Charge:
                break;
            case EnemyState.Summon_Action:
                break;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        // --- 상태 나가기 (Exit) ---
        if (currentState == EnemyState.Summon_Charge)
        {
            StopCoroutine("SummonSequence");
        }

        currentState = newState; // 상태 변경

        // --- 상태 들어가기 (Enter) ---
        if (agent != null)
        {
            // [★수정★] 상태별 isStopped 설정
            switch (newState)
            {
                case EnemyState.Idle:
                    agent.isStopped = false; // Idle 상태에서는 배회하므로 false
                    lastIdleWanderTime = -idleWanderTimer; // Idle 상태 시작 시 즉시 새 배회 지점 찾도록
                    break;
                case EnemyState.Summon_Charge:
                case EnemyState.Summon_Action:
                    agent.isStopped = true; // 소환 중에는 멈춤
                    if (newState == EnemyState.Summon_Charge) agent.velocity = Vector3.zero;
                    break;
                case EnemyState.Trace:
                    agent.isStopped = false; // 추적/서성임 상태에서는 움직임
                    break;
            }
        }

        if (newState == EnemyState.Summon_Charge)
        {
            StartCoroutine("SummonSequence");
        }
    }

    void IdleUpdate(float distanceToTarget)
    {
        // 1. 디펜스 모드면 즉시 추적
        if (EnemyTargetManager.IsDefenseStageActive)
        {
            ChangeState(EnemyState.Trace);
            return;
        }
        // 2. 일반 타겟이 감지 범위 안에 들어오면 추적
        if (distanceToTarget <= traceRange)
        {
            ChangeState(EnemyState.Trace);
            return;
        }
        // 3. 소환 쿨타임이 다 됐으면 소환 준비
        if (Time.time >= lastSummonTime + summonCooldown)
        {
            ChangeState(EnemyState.Summon_Charge);
            return;
        }

        // 4. [★추가★] 위의 어떤 조건에도 해당하지 않으면 배회
        WanderIdle();
    }

    void WanderIdle()
    {
        if (agent == null) return;

        // [★추가★] 혹시 멈춰있다면 다시 움직이도록 설정
        if (agent.isStopped) agent.isStopped = false;

        // 타이머가 다 됐거나, 목적지에 도착했다면 새 목적지 설정
        if (Time.time > lastIdleWanderTime + idleWanderTimer || (!agent.pathPending && agent.remainingDistance < 0.5f))
        {
            lastIdleWanderTime = Time.time;

            // 현재 위치 기준으로 랜덤 방향 및 거리 설정
            Vector3 randomDirection = Random.insideUnitSphere * idleWanderRadius;
            randomDirection += transform.position;

            // NavMesh 상의 유효 위치 탐색
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, idleWanderRadius, NavMesh.AllAreas))
            {
                // 새 목적지로 이동
                agent.SetDestination(hit.position);
            }
        }
    }

    void TraceUpdate(float distanceToTarget)
    {
        if (Time.time >= lastSummonTime + summonCooldown)
        {
            ChangeState(EnemyState.Summon_Charge);
        }
        else if (!EnemyTargetManager.IsDefenseStageActive && distanceToTarget > traceRange)
        {
            ChangeState(EnemyState.Idle);
        }
        else
        {
            if (distanceToTarget > orbitDistance)
            {
                TraceTarget();
            }
            else
            {
                WanderNearTarget();
            }
        }
    }

    IEnumerator SummonSequence()
    {
        ChangeState(EnemyState.Summon_Charge);
        if (agent != null) agent.velocity = Vector3.zero;

        if (enemyRenderer != null)
        {
            enemyRenderer.material.SetColor("_BaseColor", Color.blue);
        }

        yield return new WaitForSeconds(chargeDuration);

        ChangeState(EnemyState.Summon_Action);

        PerformSummon();

        if (enemyRenderer != null)
        {
            enemyRenderer.material.SetColor("_BaseColor", originalColor);
        }

        lastSummonTime = Time.time;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distanceToTarget <= traceRange)
        {
            ChangeState(EnemyState.Trace);
        }
        else
        {
            ChangeState(EnemyState.Idle);
        }
    }


    // [★수정★] PerformSummon 함수
    void PerformSummon()
    {
        // [★추가★] 미니언 매니저에서 부모 Transform 가져오기
        Transform parentTransform = null;
        if (MinionManager.Instance != null)
        {
            parentTransform = MinionManager.Instance.MinionsParent;
        }
        else
        {
            Debug.LogError("SummonerEnemy: MinionManager 인스턴스를 찾을 수 없습니다!", this);
            // 부모 없이 그냥 소환하거나, 여기서 리턴할 수 있습니다.
            // 여기서는 일단 루트에 소환되도록 둡니다.
        }

        foreach (SummonWaveEntry entry in summonWave)
        {
            if (entry.minionPrefab == null) { /* ... 경고 ... */ continue; }

            for (int i = 0; i < entry.count; i++)
            {
                // ... (spawnPosition 계산 로직 동일) ...
                Vector2 randomCircle = Random.insideUnitCircle * minionSpawnRange;
                Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(spawnPosition, out hit, minionSpawnRange, NavMesh.AllAreas))
                {
                    spawnPosition = hit.position;
                }

                GameObject spawnedMinion = Instantiate(entry.minionPrefab, spawnPosition, Quaternion.identity);

                // [★수정★] MinionManager에서 가져온 parentTransform을 부모로 설정
                if (parentTransform != null)
                {
                    spawnedMinion.transform.SetParent(parentTransform);
                }
            }
        }
    }

    void TraceTarget()
    {
        if (agent == null) return;
        agent.speed = moveSpeed;
        agent.SetDestination(currentTarget.position);
    }

    void WanderNearTarget()
    {
        if (agent == null) return;

        agent.speed = moveSpeed;

        if (Time.time > lastOrbitWanderTime + orbitWanderTimer || (!agent.pathPending && agent.remainingDistance < 0.5f))
        {
            lastOrbitWanderTime = Time.time;

            Vector3 randomDirection = Random.insideUnitSphere * orbitWanderRadius;
            randomDirection += transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, orbitWanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, traceRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minionSpawnRange);
    }
}