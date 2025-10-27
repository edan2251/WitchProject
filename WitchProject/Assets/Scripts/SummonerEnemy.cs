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
    private NavMeshAgent agent;
    //private Transform player;
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
    // [수정] 단일 프리팹 대신 '소환 웨이브' 리스트를 사용
    [SerializeField] private List<SummonWaveEntry> summonWave;


    [SerializeField] private float minionSpawnRange = 3f;
    [SerializeField] private float summonCooldown = 10f;
    private float lastSummonTime = -10f;
    [SerializeField] private float chargeDuration = 1.0f;


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

    // [수정] Start() 함수
    public override void Start()
    {
        base.Start(); // 부모(BaseEnemy)의 Start() 호출

        agent = GetComponent<NavMeshAgent>();
        //player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
        ChangeState(EnemyState.Idle);
    }

    // ... (Update, ChangeState, IdleUpdate, TraceUpdate 함수는 이전과 동일) ...
    void Update()
    {
        UpdateTarget();

        if (currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                IdleUpdate(distanceToTarget); // [수정] distanceToTarget 전달
                break;
            case EnemyState.Trace:
                TraceUpdate(distanceToTarget); // [수정] distanceToTarget 전달
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
        if (currentState == EnemyState.Summon_Charge)
        {
            StopCoroutine("SummonSequence");
        }
        currentState = newState;
        if (agent != null)
        {
            if (newState == EnemyState.Idle || newState == EnemyState.Summon_Charge || newState == EnemyState.Summon_Action)
            {
                agent.isStopped = true;
                if (newState == EnemyState.Summon_Charge) agent.velocity = Vector3.zero;
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

    void IdleUpdate(float distanceToTarget)
    {
        if (EnemyTargetManager.IsDefenseStageActive)
        {
            ChangeState(EnemyState.Trace);
            return;
        }

        // [수정] distanceToPlayer -> distanceToTarget (이제 정상 작동)
        if (distanceToTarget <= traceRange)
        {
            ChangeState(EnemyState.Trace);
        }
        else if (Time.time >= lastSummonTime + summonCooldown)
        {
            ChangeState(EnemyState.Summon_Charge);
        }
    }

    void TraceUpdate(float distanceToTarget)
    {
        // 1. 소환 쿨타임이 최우선
        if (Time.time >= lastSummonTime + summonCooldown)
        {
            ChangeState(EnemyState.Summon_Charge);
        }
        // 2. 디펜스 모드가 "아닌데" 타겟이 너무 멀어졌을 때만 Idle로 복귀
        else if (!EnemyTargetManager.IsDefenseStageActive && distanceToTarget > traceRange)
        {
            ChangeState(EnemyState.Idle);
        }
        // 3. [★수정★] 그 외 모든 추적/서성임 경우
        else
        {
            // 3a. 타겟이 orbitDistance(7f)보다 멀리 있으면, 접근
            if (distanceToTarget > orbitDistance)
            {
                TraceTarget(); // 기존 로직: 타겟에게 접근
            }
            // 3b. 타겟이 orbitDistance(7f) 안에 들어왔으면, 서성거림
            else
            {
                WanderNearTarget(); // 신규 로직: 주변 배회
            }
        }
    }

    // ... (SummonSequence 코루틴은 이전과 동일) ...
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

        PerformSummon(); // [수정] 이 함수가 새 로직을 사용

        if (enemyRenderer != null)
        {
            enemyRenderer.material.SetColor("_BaseColor", originalColor);
        }

        lastSummonTime = Time.time;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distanceToTarget <= traceRange) // [수정]
        {
            ChangeState(EnemyState.Trace);
        }
        else
        {
            ChangeState(EnemyState.Idle);
        }
    }


    // [수정] PerformSummon 함수가 리스트를 순회하도록 변경
    void PerformSummon()
    {
        // 1. 'summonWave' 리스트에 등록된 몬스터 종류(Entry)를 하나씩 순회
        foreach (SummonWaveEntry entry in summonWave)
        {
            if (entry.minionPrefab == null)
            {
                Debug.LogWarning(gameObject.name + ": 소환 웨이브에 프리팹이 비어있습니다.");
                continue; // 이 항목은 건너뛰고 다음 항목으로
            }

            // 2. 해당 몬스터를 'entry.count' 만큼 반복해서 소환
            for (int i = 0; i < entry.count; i++)
            {
                // 3. 소환 위치 계산 (기존 로직과 동일)
                Vector2 randomCircle = Random.insideUnitCircle * minionSpawnRange;
                Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                NavMeshHit hit;
                if (NavMesh.SamplePosition(spawnPosition, out hit, minionSpawnRange, NavMesh.AllAreas))
                {
                    spawnPosition = hit.position;
                }

                // 4. 'entry.minionPrefab'을 사용해 소환
                Instantiate(entry.minionPrefab, spawnPosition, Quaternion.identity);
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

        // 1. 속도 설정 (혹시 모르니)
        agent.speed = moveSpeed;

        // 2. 타이머가 다 됐거나, 이미 목적지에 도착했다면
        if (Time.time > lastOrbitWanderTime + orbitWanderTimer || (!agent.pathPending && agent.remainingDistance < 0.5f))
        {
            lastOrbitWanderTime = Time.time;

            // 3. 현재 위치(transform.position)를 기준으로 랜덤한 방향을 정함
            Vector3 randomDirection = Random.insideUnitSphere * orbitWanderRadius;
            randomDirection += transform.position;

            // 4. NavMesh 상의 유효한 위치를 찾음
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, orbitWanderRadius, NavMesh.AllAreas))
            {
                // 5. 새 목적지로 이동
                agent.SetDestination(hit.position);
            }
        }
    }

    // ... (OnDrawGizmosSelected 함수는 이전과 동일) ...
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, traceRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minionSpawnRange);
    }
}