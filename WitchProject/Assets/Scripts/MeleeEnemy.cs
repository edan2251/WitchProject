using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MeleeEnemy : BaseEnemy
{
    public enum EnemyState { Idle, Trace, Attack }
    public EnemyState state = EnemyState.Idle;

    [Header("Melee AI")]
    public int damage = 5;
    public float moveSpeed = 4f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    // [★추가★] 공격 시작 딜레이
    [Tooltip("공격 범위 진입 후 첫 공격까지의 딜레이 (초)")]
    public float attackDelay = 1.0f;
    private float timeEnteredAttackRange = -1f; // 공격 범위 진입 시간 (-1은 범위 밖)

    // --- 비공개 변수 ---
    private float lastAttackTime;

    // ... (Awake, OnEnable, OnDisable은 동일) ...
    protected override void Awake() { base.Awake(); }
    protected override void OnEnable() { base.OnEnable(); }
    protected override void OnDisable() { base.OnDisable(); }

    public override void Start()
    {
        base.Start();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange; // 멈추는 거리는 유지
        }
        lastAttackTime = -attackCooldown; // 즉시 공격 가능하도록 초기화
        timeEnteredAttackRange = -1f; // 시작 시 범위 밖으로 초기화
    }

    void Update()
    {
        UpdateTarget();
        if (currentTarget == null || agent == null) return;

        float dist = Vector3.Distance(currentTarget.position, transform.position);

        // FSM (상태 머신)
        switch (state)
        {
            case EnemyState.Idle:
                if (currentTarget != null)
                {
                    state = EnemyState.Trace;
                    // [★추가★] Trace 상태 시작 시 이동 재개 (혹시 멈춰있었다면)
                    if (agent != null && agent.isStopped)
                    {
                        agent.isStopped = false;
                    }
                }
                break;

            case EnemyState.Trace:
                // 1. 타겟 추적
                TraceTarget();

                // 2. 공격 범위 안에 들어왔는지 확인
                if (dist <= attackRange)
                {
                    // 2a. 처음 들어왔다면, 진입 시간 기록
                    if (timeEnteredAttackRange < 0f)
                    {
                        timeEnteredAttackRange = Time.time;
                        // [★추가★] 범위에 들어왔으니 일단 멈춤 (딜레이 동안)
                        if (agent != null) agent.isStopped = true;
                    }

                    // 2b. 들어온 지 attackDelay(1초)가 지났다면 공격 상태로 전환
                    if (Time.time >= timeEnteredAttackRange + attackDelay)
                    {
                        state = EnemyState.Attack;
                        // 공격 상태에서는 이미 isStopped = true 상태여야 함
                        if (agent != null) agent.ResetPath(); // 경로 초기화는 여기서
                    }
                }
                // 3. 공격 범위 밖에 있다면, 진입 시간 리셋
                else
                {
                    timeEnteredAttackRange = -1f;
                    // [★추가★] 범위 밖이므로 다시 추적 시작 (혹시 멈췄었다면)
                    if (agent != null && agent.isStopped)
                    {
                        agent.isStopped = false;
                    }
                }
                break;

            case EnemyState.Attack:
                // 1. 공격 범위를 벗어났는지 확인
                if (dist > attackRange)
                {
                    state = EnemyState.Trace;
                    timeEnteredAttackRange = -1f; // 진입 시간 리셋
                    // 추적 상태로 돌아가므로 이동 재개
                    if (agent != null)
                    {
                        agent.isStopped = false;
                    }
                }
                // 2. 범위 안에 있다면 공격 시도
                else
                {
                    AttackTarget(); // AttackTarget 내부에서 쿨다운 체크
                }
                break;
        }
    }

    /// <summary>
    /// 타겟을 향해 이동합니다. (멈춰있으면 이동 안 함)
    /// </summary>
    void TraceTarget()
    {
        if (agent == null || agent.isStopped) return; // 멈춰있으면 목적지 설정 안 함
        agent.SetDestination(currentTarget.position);
    }

    /// <summary>
    /// 타겟을 근접 공격합니다. (쿨다운 확인)
    /// </summary>
    void AttackTarget()
    {
        // 공격 시 타겟 바라보기
        Vector3 lookDirection = (currentTarget.position - transform.position).normalized;
        lookDirection.y = 0;
        transform.rotation = Quaternion.LookRotation(lookDirection);

        // 공격 쿨타임이 다 찼다면 공격 실행
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time; // 마지막 공격 시간 갱신

            // --- 실제 공격 로직 ---
            Debug.Log($"{gameObject.name} attacks!"); // 공격 로그 (확인용)
            if (currentTarget == playerTransform)
            {
                PlayerController pc = currentTarget.GetComponent<PlayerController>();
                if (pc != null) pc.TakeDamage(damage);
            }
            else if (currentTarget == defenseObjectTransform)
            {
                DefenseObjective obj = currentTarget.GetComponent<DefenseObjective>();
                if (obj != null) obj.TakeDamage(damage);
            }
            // --- 여기까지 ---
        }
    }
}