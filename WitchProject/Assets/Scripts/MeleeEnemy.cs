using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// [수정] MonoBehaviour 대신 BaseEnemy를 상속
public class MeleeEnemy : BaseEnemy
{
    public enum EnemyState { Idle, Trace, Attack }
    public EnemyState state = EnemyState.Idle;

    // [수정] EnemyData, HP, FlashDuration 등 공통 변수는 BaseEnemy에 있으므로 여기선 삭제

    [Header("Melee AI")]
    public int damage = 5;      // 근접 공격 데미지
    public float moveSpeed = 4f;
    public float attackRange = 2f;    // 근접 공격 사거리 (매우 짧음)
    public float attackCooldown = 1.5f;

    // --- 비공개 변수 ---
    private Transform player;
    private float lastAttackTime;

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

    // [수정] Start() 함수 - BaseEnemy의 기능을 먼저 실행하고, 중복 코드 제거
    public override void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();

        player = GameObject.FindGameObjectWithTag("Player").transform;

        agent = GetComponent<NavMeshAgent>();
        player = playerTarget;

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange;
        }

        lastAttackTime = -attackCooldown;
    }

    void Update()
    {
        currentTarget = DetermineTarget();

        if (player == null || agent == null) return;


        // FSM (상태 머신)
        switch (state)
        {
            case EnemyState.Idle:
                // 타겟이 정해지면 Trace 상태로 전환
                if (currentTarget != null)
                {
                    state = EnemyState.Trace;
                }
                break;

            case EnemyState.Trace:
                TraceTarget(); // ★ TraceTarget으로 변경

                float dist = Vector3.Distance(currentTarget.position, transform.position);

                if (!agent.pathPending && dist <= attackRange)
                {
                    state = EnemyState.Attack;
                    if (agent != null)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                    }
                }
                break;

            case EnemyState.Attack:
                float distToTarget = Vector3.Distance(currentTarget.position, transform.position);

                if (distToTarget > attackRange)
                {
                    state = EnemyState.Trace;
                    if (agent != null) agent.isStopped = false;
                }
                else
                {
                    AttackTarget(); // ★ AttackTarget으로 변경
                }
                break;
        }
    }

    void TraceTarget()
    {
        if (agent == null || currentTarget == null) return;
        agent.SetDestination(currentTarget.position);
    }

    // [추가] 현재 타겟을 공격
    void AttackTarget()
    {
        if (currentTarget == null) return;

        Vector3 lookDirection = (currentTarget.position - transform.position).normalized;
        lookDirection.y = 0;
        transform.rotation = Quaternion.LookRotation(lookDirection);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            // 타겟의 체력 컴포넌트를 찾아 데미지 적용
            if (currentTarget.CompareTag("Player"))
            {
                PlayerController pc = currentTarget.GetComponent<PlayerController>();
                if (pc != null) pc.TakeDamage(damage);
            }
            else
            {
                // 방어 목표물 데미지 (DefenseTargetHealth 사용)
                DefenseTargetHealth dth = currentTarget.GetComponent<DefenseTargetHealth>();
                if (dth != null) dth.TakeDamage(damage);
            }
        }
    }

    /// <summary>
    /// 플레이어를 향해 이동합니다.
    /// </summary>
    void TracePlayer()
    {
        if (agent == null) return;
        agent.SetDestination(player.position);
    }

    /// <summary>
    /// 플레이어를 근접 공격합니다. (몸통박치기)
    /// </summary>
    void AttackPlayer()
    {
        // 공격 시 플레이어를 바라봄 (선택적)
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0; // Y축 회전은 고정
        transform.rotation = Quaternion.LookRotation(lookDirection);

        // 공격 쿨타임이 다 찼다면
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            // 플레이어에게 직접 데미지를 줍니다.
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(damage);
            }
        }
    }

    // [삭제] FlashOnHit(), FlashColor(), TakeDamage(), Die() 함수는
    // BaseEnemy에 있으므로 여기서는 모두 삭제합니다.
}