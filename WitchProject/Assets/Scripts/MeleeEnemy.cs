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
    private NavMeshAgent agent;
    private Transform player;
    private float lastAttackTime;

    // [수정] Start() 함수 - BaseEnemy의 기능을 먼저 실행하고, 중복 코드 제거
    public override void Start()
    {
        base.Start();

        player = GameObject.FindGameObjectWithTag("Player").transform;

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            // [수정] 멈추는 거리(stoppingDistance)를 attackRange와 일치시킵니다.
            agent.stoppingDistance = attackRange;
        }

        lastAttackTime = -attackCooldown;
    }

    void Update()
    {
        if (player == null || agent == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        // FSM (상태 머신)
        switch (state)
        {
            case EnemyState.Idle:
                // 플레이어를 찾으면 즉시 추적 (무한 추적)
                if (player != null)
                {
                    state = EnemyState.Trace;
                }
                break;

            case EnemyState.Trace:
                TracePlayer();

                // [수정] agent.stoppingDistance 대신 attackRange를 기준으로 검사합니다.
                // agent.pathPending: 에이전트가 경로 계산 중인지 확인 (오류 방지)
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
                // [수정] 여기도 attackRange 기준으로 검사
                if (dist > attackRange)
                {
                    state = EnemyState.Trace;
                    if (agent != null)
                    {
                        agent.isStopped = false;
                    }
                }
                else
                {
                    AttackPlayer();
                }
                break;
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