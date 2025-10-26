using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

// [수정] MonoBehaviour 대신 BaseEnemy를 상속
public class Enemy : BaseEnemy
{
    // [수정] 상태 변경: Wander(순찰), MoveToAttack(공격이동), Attack(공격)
    public enum EnemyState { Wander, MoveToAttack, Attack }
    public EnemyState state = EnemyState.Wander; // 기본 상태를 Wander로

    // [수정] EnemyData, HP, FlashDuration 등 공통 변수는 BaseEnemy에 있으므로 여기선 삭제

    [Header("Goblin AI")]
    public float moveSpeed = 2f;      // 순찰 속도
    public float chaseSpeed = 4f;     // 추격 속도
    public float attackRange = 6f;
    public float attackCooldown = 1.5f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    // --- 비공개 변수 ---
    private Transform player;
    private NavMeshAgent agent;
    private float lastAttackTime;

    [Header("Wandering & Separation")]
    public float wanderRadius = 20f; // 순찰 반경
    public float wanderTimer = 5f;   // 새 순찰 지점을 찍는 주기
    private float lastWanderTime;
    public float separationDistance = 5f; // 다른 고블린과 유지할 최소 거리

    // [추가] 모든 고블린 인스턴스를 추적하는 static 리스트
    private static List<Enemy> allGoblins = new List<Enemy>();

    [Header("Sight Check")]
    [SerializeField] private LayerMask sightObstructionLayers; // 시야를 가리는 벽/장애물 레이어

    protected override void Awake()
    {
        base.Awake();
    }

    // [추가] 오브젝트 활성화 시 리스트에 추가
    protected override void OnEnable()
    {
        base.OnEnable();

        if (!allGoblins.Contains(this))
        {
            allGoblins.Add(this);
        }
    }

    // [추가] 오브젝트 비활성화/파괴 시 리스트에서 제거
    protected override void OnDisable()
    {
        base.OnDisable();

        if (allGoblins.Contains(this))
        {
            allGoblins.Remove(this);
        }
    }


    /// <summary>
    /// Raycast를 사용하여 플레이어가 적의 시야 내에 있는지 확인합니다. (벽 통과 사격 방지용)
    /// </summary>
    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 rayStart = transform.position;
        // [수정] 플레이어 가슴을 조준
        Vector3 targetPosition = player.position + Vector3.up * 1.0f;
        Vector3 direction = (targetPosition - rayStart).normalized;
        float distance = Vector3.Distance(rayStart, targetPosition);

        if (Physics.Raycast(rayStart, direction, out RaycastHit hit, distance, sightObstructionLayers))
        {
            if (hit.transform != player)
            {
                return false;
            }
        }
        return true;
    }


    // [수정] Start() 함수 - BaseEnemy의 기능을 먼저 실행하고, 중복 코드 제거
    public override void Start() // 'override' 키워드 추가
    {
        // 1. [필수] 부모(BaseEnemy)의 Start()를 먼저 호출 (HP, 헬스바, 렌더러 설정)
        base.Start();

        // 2. [Enemy 고유 설정]
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastAttackTime = -attackCooldown; // 즉시 공격 쿨타임이 돌도록 설정

        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        lastWanderTime = -wanderTimer; // 즉시 순찰 시작
        state = EnemyState.Wander;     // 상태 초기화
    }

    // [수정] FSM 로직 전체 변경
    void Update()
    {
        if (player == null || agent == null) return;

        // FSM 상태 전환
        switch (state)
        {
            case EnemyState.Wander:
                Wander(); // 순찰 및 거리 벌리기 로직 수행

                // 공격 쿨타임이 다 찼는지 확인
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    state = EnemyState.MoveToAttack;
                    if (agent != null) agent.isStopped = false;
                }
                break;

            case EnemyState.MoveToAttack:
                MoveToPlayer(); // 플레이어 추적

                float dist = Vector3.Distance(player.position, transform.position);
                bool playerVisible = CanSeePlayer();

                // 공격 범위에 들어왔고, 시야가 확보되면 공격
                if (dist < attackRange && playerVisible)
                {
                    state = EnemyState.Attack;
                    if (agent != null)
                    {
                        agent.isStopped = true; // 공격 위해 정지
                        agent.ResetPath();
                    }
                }
                break;

            case EnemyState.Attack:
                // 공격은 한 프레임에 실행되고 바로 Wander로 복귀
                AttackOnceAndRun();
                break;
        }
    }

    // [추가] 순찰 및 동료 거리 유지 로직
    void Wander()
    {
        if (agent.speed != moveSpeed)
            agent.speed = moveSpeed;

        // 순찰 타이머가 다 됐거나, 목적지에 도착했다면 새 목적지 설정
        if (Time.time > lastWanderTime + wanderTimer || (!agent.pathPending && agent.remainingDistance < 0.5f))
        {
            lastWanderTime = Time.time;

            // 1. 다른 고블린과 거리를 벌리는 방향 계산
            Vector3 separationVector = Vector3.zero;
            foreach (Enemy otherGoblin in allGoblins)
            {
                if (otherGoblin == this || otherGoblin == null) continue;

                float distToOther = Vector3.Distance(transform.position, otherGoblin.transform.position);
                if (distToOther > 0 && distToOther < separationDistance)
                {
                    separationVector += (transform.position - otherGoblin.transform.position).normalized;
                }
            }

            Vector3 finalDirection;
            if (separationVector != Vector3.zero)
            {
                // 2a. 벌어져야 할 방향이 있다면 그쪽으로 우선 이동
                finalDirection = separationVector.normalized;
            }
            else
            {
                // [수정] 2b. 플레이어 방향을 피해서 랜덤한 방향으로 순찰
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                int attempts = 0; // 무한 루프 방지용

                do
                {
                    finalDirection = Random.insideUnitSphere.normalized;
                    attempts++;
                } while (Vector3.Angle(finalDirection, directionToPlayer) < 90f && attempts < 10);
            }

            // 3. 최종 방향으로 wanderRadius만큼 떨어진 유효한 NavMesh 위치 탐색
            Vector3 destination = transform.position + finalDirection * wanderRadius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(destination, out hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    // [추가] 플레이어에게 이동하는 로직 (기존 TracePlayer와 동일)
    void MoveToPlayer()
    {
        if (agent == null) return;

        if (agent.speed != chaseSpeed)
        {
            agent.speed = chaseSpeed;
        }

        agent.SetDestination(player.position);
    }

    // [추가] 한 발 쏘고 바로 Wander 상태로 복귀하는 로직
    void AttackOnceAndRun()
    {
        if (agent == null) return;

        if (!agent.isStopped)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        transform.LookAt(player.position);
        ShootProjectile();

        lastAttackTime = Time.time;
        state = EnemyState.Wander;

        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
            if (ep != null)
            {
                // [수정] 플레이어의 발(position)이 아닌, 1m 위(가슴)를 조준
                Vector3 targetPosition = player.position + Vector3.up * 1.0f;
                Vector3 dir = (targetPosition - firePoint.position).normalized;
                ep.SetDirection(dir);
            }
        }
    }

    // [삭제] FlashOnHit(), FlashColor(), TakeDamage(), Die() 함수는
    // BaseEnemy에 있으므로 여기서는 모두 삭제합니다.
}