using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum EnemyState { Idle, Trace, Attack, RunAway }
    public EnemyState state = EnemyState.Idle;

    public float moveSpeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 6f;
    public float attackCooldown = 1.5f;


    private HealthBarManager healthBarManager;

    public GameObject projectilePrefab;
    public Transform firePoint;

    public Transform player;

    private float lastAttackTime;
    public int maxHp = 5;
    public int currentHP;

    public float flashDuration = 0.1f; // 빨갛게 유지되는 시간
    private Renderer enemyRenderer;
    private Color originalColor;


    //---------------AI 제작------------------
    private UnityEngine.AI.NavMeshAgent agent;
    //----------------------------------------

    // Start is called before the first frame update
    public void Start()
    {
        currentHP = maxHp;

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

        //FSM 상태 전환
        switch (state)
        {
            case EnemyState.Idle:
                if (dist < traceRange)
                    state = EnemyState.Trace;
                else if (currentHP <= maxHp / 5 * 2)
                    state = EnemyState.RunAway;
                break;

            case EnemyState.Trace:
                if (dist < attackRange)
                    state = EnemyState.Attack;
                else if (currentHP <= maxHp / 5 * 2)
                    state = EnemyState.RunAway;
                else if (dist > traceRange)
                    state = EnemyState.Idle;
                else
                    TracePlayer();
                break;

            case EnemyState.Attack:
                if (dist > attackRange)
                    state = EnemyState.Trace;
                else if (currentHP <= maxHp / 5 * 2)
                    state = EnemyState.RunAway;
                else
                    AttackPlayer();
                break;

            case EnemyState.RunAway:
                if (dist > traceRange)
                    state = EnemyState.Idle;
                else
                    RunAway();
                break;
        }
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
        if (healthBarManager != null)
        {
            healthBarManager.UnregisterEnemy(this);
        }
        Destroy(gameObject);
    }
}
