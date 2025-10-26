using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening; // DOTween 사용을 위해 추가

public class GhostEnemy : BaseEnemy
{
    public enum GhostState { Idle, Chase, Explode }
    public GhostState state = GhostState.Idle;

    [Header("Ghost AI")]
    [SerializeField] private float detectionRange = 15f; // 플레이어 감지 범위

    [Header("Idle State")]
    [SerializeField] private float idleDashInterval = 2f;
    [SerializeField] private float idleDashSpeed = 10f;
    [SerializeField] private float idleDashRange = 8f;
    private float lastIdleDashTime;

    [Header("Chase & Explode")]
    [SerializeField] private float chaseSpeed = 18f;
    [SerializeField] private float explodeTriggerRange = 2f;
    [SerializeField] private float explodeDelay = 0.3f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private int explosionDamage = 25;
    [SerializeField] private Color explodeColor = Color.blue;
    [SerializeField] private GameObject explosionVFX;

    [Header("Detection Indicator")]
    [SerializeField] private GameObject detectionIndicatorPrefab; // 머리 위에 띄울 느낌표 UI 프리팹
    [SerializeField] private Vector3 indicatorOffset = new Vector3(0, 3f, 0); // 머리 위 Y축 위치
    private GameObject activeIndicator; // 현재 활성화된 느낌표 오브젝트

    // [추가] 둥실거리는 효과 변수
    [Header("Hover Effect")]
    [SerializeField] private float hoverSpeed = 2f; // 둥실거리는 속도
    [SerializeField] private float hoverAmplitude = 0.3f; // 둥실거리는 높낮이
    private Vector3 modelOriginalLocalPos; // 모델의 원래 로컬 Y위치

    // --- 비공개 변수 ---
    private NavMeshAgent agent;
    private Transform player;
    private bool isExploding = false;

    // [수정] Start() 함수
    public override void Start()
    {
        base.Start(); // 1. 부모 Start() 호출 (HP, 헬스바, 'enemyRenderer' 찾기)

        // 2. [GhostEnemy 고유 설정]
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = idleDashSpeed;
            agent.isStopped = true;
        }
        lastIdleDashTime = -idleDashInterval;

        // 3. [추가] 둥실거림 효과를 위해 모델의 원래 로컬 위치 저장
        // 'enemyRenderer'는 BaseEnemy의 Start()에서 'GetComponentInChildren'으로 찾아옵니다.
        if (enemyRenderer != null)
        {
            // 'enemyRenderer.transform'이 바로 모델(시각적 외형)의 트랜스폼입니다.
            modelOriginalLocalPos = enemyRenderer.transform.localPosition;
        }
    }

    void Update()
    {
        // --- 1. 상태 머신(FSM) 로직 ---
        if (player == null || agent == null || isExploding) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case GhostState.Idle:
                HandleIdleState(distToPlayer);
                break;
            case GhostState.Chase:
                HandleChaseState(distToPlayer);
                break;
            case GhostState.Explode:
                // 코루틴에서 처리
                break;
        }

        // --- 2. 둥실거림(Hover) 효과 로직 (상태와 관계없이 항상 실행) ---
        HandleHoverEffect();
    }

    /// <summary>
    /// [추가] 모델(Renderer)을 위아래로 둥실거리게 합니다.
    /// </summary>
    private void HandleHoverEffect()
    {
        if (enemyRenderer != null)
        {
            // Sin 함수를 이용해 -1 ~ +1 사이를 부드럽게 왕복하는 값을 만듭니다.
            float hoverY = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;

            // 모델의 원래 로컬 Y위치에 둥실거림 값을 더합니다.
            enemyRenderer.transform.localPosition = new Vector3(
                modelOriginalLocalPos.x,
                modelOriginalLocalPos.y + hoverY, // Y축에만 적용
                modelOriginalLocalPos.z
            );
        }
    }

    // ... (HandleIdleState, HandleChaseState, ChangeState, IdleDash, ExplodeSequence, OnDrawGizmosSelected 함수는 이전과 동일하게 유지) ...
    // ... (아래는 생략된 기존 함수들입니다. 수정할 필요 없습니다) ...

    private void HandleIdleState(float distToPlayer)
    {
        if (distToPlayer <= detectionRange)
        {
            ChangeState(GhostState.Chase);
        }
        else if (Time.time >= lastIdleDashTime + idleDashInterval)
        {
            lastIdleDashTime = Time.time;
            StartCoroutine(IdleDash());
        }
    }

    private void HandleChaseState(float distToPlayer)
    {
        if (distToPlayer <= explodeTriggerRange)
        {
            ChangeState(GhostState.Explode);
        }
        else if (distToPlayer > detectionRange * 1.2f)
        {
            ChangeState(GhostState.Idle);
        }
        else
        {
            agent.SetDestination(player.position);
        }
    }

    private void ChangeState(GhostState newState)
    {
        if (state == newState || isExploding) return;

        state = newState;

        switch (newState)
        {
            case GhostState.Idle:
                if (agent != null)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                // [추가] 느낌표가 켜져있다면 끔
                if (activeIndicator != null)
                {
                    activeIndicator.SetActive(false);
                }
                break;

            case GhostState.Chase:
                if (agent != null)
                {
                    agent.speed = chaseSpeed;
                    agent.isStopped = false;
                }

                // [추가] 느낌표가 없다면 생성하고, 있다면 활성화
                if (activeIndicator == null && detectionIndicatorPrefab != null)
                {
                    // 유령의 자식으로 생성해서 같이 따라다니게 함
                    activeIndicator = Instantiate(detectionIndicatorPrefab, transform);
                    // 오프셋 설정
                    activeIndicator.transform.localPosition = indicatorOffset;
                }
                else if (activeIndicator != null)
                {
                    activeIndicator.SetActive(true);
                }
                break;

            case GhostState.Explode:
                isExploding = true;
                if (agent != null)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                }

                // [추가] 폭발 시작 시 느낌표 즉시 파괴
                if (activeIndicator != null)
                {
                    Destroy(activeIndicator);
                }

                StartCoroutine(ExplodeSequence());
                break;
        }
    }

    IEnumerator IdleDash()
    {
        Vector3 randomDir = Random.insideUnitSphere * idleDashRange;
        randomDir.y = 0;
        Vector3 targetPos = transform.position + randomDir;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, idleDashRange, NavMesh.AllAreas))
        {
            targetPos = hit.position;
        }
        else
        {
            yield break;
        }

        if (agent != null)
        {
            agent.speed = idleDashSpeed;
            agent.isStopped = false;
            agent.SetDestination(targetPos);
        }

        yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance < 0.5f);

        if (agent != null && state == GhostState.Idle)
        {
            agent.isStopped = true;
        }
    }

    IEnumerator ExplodeSequence()
    {
        // 1. 초기 크기 저장 (나중에 복원할 필요는 없지만, 혹시 모를 경우를 대비해)
        Vector3 originalScale = transform.localScale;

        // 'enemyRenderer'는 BaseEnemy로부터 상속받아 사용 가능
        if (enemyRenderer != null)
        {
            // DOTween을 사용해 0.3초간 _BaseColor를 explodeColor로 변경
            enemyRenderer.material.DOColor(explodeColor, "_BaseColor", explodeDelay)
                .SetEase(Ease.InQuad); // 점점 빠르게 변함
        }

        // [추가] DOTween을 사용해 explodeDelay 시간 동안 크기를 2배로 키움 (원하는 배율로 조절)
        transform.DOScale(originalScale * 2f, explodeDelay) // 현재 크기의 2배로 커짐
                 .SetEase(Ease.OutSine); // 부드럽게 커지도록 설정

        // 0.3초 대기
        yield return new WaitForSeconds(explodeDelay);

        // --- 폭발 ---

        // 1. 시각 효과(VFX) 생성
        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }

        // 2. 플레이어 데미지 처리
        if (player != null && Vector3.Distance(transform.position, player.position) <= explosionRadius)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(explosionDamage);
            }
        }

        // 3. 자폭 (중요: 헬스바를 수동으로 제거)
        if (healthBarManager != null)
        {
            healthBarManager.UnregisterEnemy(this);
        }
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeTriggerRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}