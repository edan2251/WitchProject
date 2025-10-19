using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum EnemyState { Idle, Trace, Attack, RunAway }
    public EnemyState state = EnemyState.Idle;

    [Header("Enemy Data")]
    public EnemyData enemyData;

    public float moveSpeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 6f;
    public float attackCooldown = 1.5f;


    private HealthBarManager healthBarManager;

    public GameObject projectilePrefab;
    public Transform firePoint;

    public Transform player;

    private float lastAttackTime;
    public int maxHp;
    public int currentHP;

    public float flashDuration = 0.1f; // 빨갛게 유지되는 시간
    private Renderer enemyRenderer;
    private Color originalColor;


    //---------------AI 제작------------------
    private UnityEngine.AI.NavMeshAgent agent;

    [Header("Sight Check")]
    private Vector3 lastKnownPlayerPosition; // 마지막으로 플레이어를 본 위치
    [SerializeField] private LayerMask sightObstructionLayers; // 시야를 가리는 벽/장애물 레이어
                                                               //----------------------------------------


    /// <summary>
    /// Raycast를 사용하여 플레이어가 적의 시야 내에 있는지 확인합니다.
    /// </summary>
    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 rayStart = transform.position;
        Vector3 direction = (player.position - rayStart).normalized;
        float distance = Vector3.Distance(rayStart, player.position);

        // Raycast를 쏘아 플레이어와 적 사이에 시야를 가리는 장애물이 있는지 확인합니다.
        if (Physics.Raycast(rayStart, direction, out RaycastHit hit, distance, sightObstructionLayers))
        {
            // Raycast가 플레이어에 도달하지 못하고 장애물에 먼저 부딪혔다면 시야 차단
            // hit.transform이 플레이어가 아니라면 시야가 막힌 것입니다.
            if (hit.transform != player)
            {
                return false;
            }
        }
        // Raycast가 플레이어에게 도달했거나 아무것도 막지 않았다면 시야 확보
        return true;
    }


    // Start is called before the first frame update
    public void Start()
    {
        if (enemyData != null)
        {
            currentHP = enemyData.maxHealth;
            maxHp = enemyData.maxHealth;

        }
        else
        {
            currentHP = 5;
            Debug.LogError(gameObject.name + ": EnemyData가 할당되지 않았습니다!");
        }

        healthBarManager = FindObjectOfType<HealthBarManager>();
        if (healthBarManager != null)
        {
            healthBarManager.RegisterEnemy(this); // 매니저에 자신(Enemy)을 등록
        }

        player = GameObject.FindGameObjectWithTag("Player").transform;

        lastAttackTime = -attackCooldown;


        //---------------AI 제작------------------
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed; // 이동 속성을 스크립트 변수와 연결
        }
        //----------------------------------------


        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        // [수정]: 매 프레임 플레이어의 시야 확보 여부를 확인합니다.
        bool playerVisible = CanSeePlayer();

        // 시야가 확보되었다면 마지막 위치를 업데이트합니다.
        if (playerVisible)
        {
            lastKnownPlayerPosition = player.position;
        }

        // FSM 상태 전환
        switch (state)
        {
            case EnemyState.Idle:
                if (dist < traceRange && playerVisible) // 시야 확보 시에만 추적 시작
                    state = EnemyState.Trace;
                else if (currentHP <= enemyData.maxHealth / 5 * 2)
                    state = EnemyState.RunAway;
                break;

            case EnemyState.Trace:
                if (dist < attackRange && playerVisible)
                    state = EnemyState.Attack;
                else if (currentHP <= enemyData.maxHealth / 5 * 2)
                    state = EnemyState.RunAway;
                else if (dist > traceRange)
                    state = EnemyState.Idle;
                else if (!playerVisible) // [추가]: 시야를 놓쳤다면 마지막 위치로 이동
                {
                    TraceLastKnownPosition();
                }
                else
                    TracePlayer(); // 시야가 확보되면 계속 쫓아감
                break;

            case EnemyState.Attack:
                if (dist > attackRange || !playerVisible) // [수정]: 시야를 잃으면 Trace로 복귀
                    state = EnemyState.Trace;
                else if (currentHP <= enemyData.maxHealth / 5 * 2)
                    state = EnemyState.RunAway;
                else
                    AttackPlayer();
                break;

            case EnemyState.RunAway:
                // ... (RunAway 로직 유지) ...
                if (dist > traceRange)
                    state = EnemyState.Idle;
                else
                    RunAway();
                break;
        }
    }


    // Enemy.cs (새 함수 추가)
    /// <summary>
    /// 플레이어를 볼 수 없을 때 마지막으로 플레이어를 본 위치로 이동합니다.
    /// </summary>
    void TraceLastKnownPosition()
    {
        if (agent == null) return;

        agent.speed = moveSpeed;
        agent.SetDestination(lastKnownPlayerPosition);

        // NavMeshAgent가 마지막 위치에 거의 도달했고 (거리가 짧고), 여전히 플레이어를 볼 수 없다면 대기 상태로 전환합니다.
        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 1.0f && !CanSeePlayer())
        {
            state = EnemyState.Idle;
        }
        // 도중에 플레이어가 다시 시야에 들어오면 Update()에서 TracePlayer()로 복귀합니다.
    }

    void RunAway()
    {
        //----------------기본코드-----------------------
        //Vector3 dir = (player.position - transform.position).normalized;
        //transform.position += dir * -moveSpeed * 2 * Time.deltaTime;

        //Vector3 oppositeDirection = -dir;

        //transform.rotation = Quaternion.LookRotation(oppositeDirection);
        //-----------------------------------------------

        //-------------------------------AI제작---------------------------------------------
        if (agent == null) return;

        if (agent.speed != moveSpeed * 2f)
        {
            agent.speed = moveSpeed * 2f;
        }

        Vector3 runDirection = transform.position - player.position;
        Vector3 destination = transform.position + runDirection.normalized * traceRange;
        agent.SetDestination(destination);
        //--------------------------------------------------------------------------------
    }

    void TracePlayer()
    {
        //-----------------------기본코드-------------------------------
        //Vector3 dir = (player.position - transform.position).normalized;
        //transform.position += dir * moveSpeed * Time.deltaTime;
        //transform.LookAt(player.position);
        //--------------------------------------------------------------

        //-------------------------AI-------------------------------------
        if (agent == null) return;

        if (agent.speed != moveSpeed)
        {
            agent.speed = moveSpeed;
        }

        agent.SetDestination(player.position);
        //--------------------------------------------------------------
    }

    void AttackPlayer()
    {
        //일정 쿨다운마다 발사
        if (Time.time >= lastAttackTime + attackCooldown)
        {

            if (agent == null) return;

            if (agent.speed != moveSpeed)
            {
                agent.speed = moveSpeed;
            }

            lastAttackTime = Time.time;
            ShootProjectile();
        }
    }

    void ShootProjectile()
    {
        if(projectilePrefab != null && firePoint != null)
        {
            transform.LookAt(player.position);
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
            if(ep != null)
            {
                Vector3 dir = (player.position - firePoint.position).normalized;
                ep.SetDirection(dir);
            }
        }
    }

    public void FlashOnHit()
    {
        // 중복 실행 방지를 위해 이미 코루틴이 실행 중이면 정지 후 다시 시작
        StopAllCoroutines();
        StartCoroutine(FlashColor());
    }

    // 피격 시 빨갛게 깜빡이는 코루틴
    IEnumerator FlashColor()
    {
        if (enemyRenderer != null)
        {
            // 피격 시 색상을 빨간색으로 변경
            enemyRenderer.material.SetColor("_BaseColor", Color.red);

            yield return new WaitForSeconds(flashDuration);

            // 원래 색상으로 복구
            enemyRenderer.material.SetColor("_BaseColor", originalColor);
        }
    }


    public void TakeDamage(int damage)
    {
        FlashOnHit();
        currentHP -= damage;

        if (healthBarManager != null)
        {
            healthBarManager.UpdateEnemyHealth(this);
        }

        if (currentHP <= 0)
        {
            Die();
        }
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

        if (healthBarManager != null)
        {
            healthBarManager.UnregisterEnemy(this);
        }
        Destroy(gameObject);
    }
}
