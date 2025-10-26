using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ArrowController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float gravityScale = 1.0f; // 중력 배율

    private int arrowDamage = 3;
    private SkillNodeData arrowSkillData;
    private bool hasHitEnemy = false;
    private LayerMask enemyLayer; // [추가] 적 레이어

    [Header("Special Effect Config")]
    [SerializeField] private float bombRadius = 5f;
    [SerializeField] private int bombDamage = 10;

    [SerializeField] private float lightningChainRange = 10f;
    [SerializeField] private int lightningChainJumps = 10;
    [SerializeField] private int lightningDamage = 2;

    [Header("Visual Effects")]
    [SerializeField] private GameObject lightningLinePrefab;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 발사 시 속도(velocity)가 아직 0일 수 있으므로 Awake에서 회전 설정은 제거
    }

    void FixedUpdate()
    {
        if (rb.isKinematic) return;

        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);

        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity);
        }
    }

    // [수정] OnTriggerEnter 로직
    private void OnTriggerEnter(Collider other)
    {
        if (hasHitEnemy) return; // 이미 무언가에 맞았다면 중복 실행 방지

        BaseEnemy hitEnemy = null;
        bool isEnemy = other.TryGetComponent<BaseEnemy>(out hitEnemy);

        // 적인, 플레이어가 아니고, 트리거가 아닌 물체(벽, 바닥 등)
        bool isEnvironment = !other.isTrigger && !other.CompareTag("Player") && !isEnemy;

        // 적 또는 환경(벽, 바닥 등)에 맞았을 때만 아래 로직 실행
        if (isEnemy || isEnvironment)
        {
            // 1. 중복 실행 방지 플래그를 가장 먼저 설정
            hasHitEnemy = true;

            Vector3 hitPoint = isEnemy ? other.bounds.center : other.transform.position;
            ApplySpecialArrowEffect(hitEnemy, hitPoint);

            // 3. 기본 데미지 적용 (오직 '적'에게만)
            if (isEnemy)
            {
                // (특수 효과로 이미 죽었을 수 있으므로 null 체크)
                if (hitEnemy != null)
                {
                    hitEnemy.TakeDamage(arrowDamage);
                }
            }

            // 4. 화살 박기 (적 또는 환경)
            ApplyStickLogic(other.gameObject);
        }
    }

    public void InitializeArrow(int damage, SkillNodeData skillData, LayerMask enemyLayer)
    {
        this.arrowDamage = damage;
        this.arrowSkillData = skillData;
        this.enemyLayer = enemyLayer; // [추가]

        if (skillData != null)
        {
        }
    }


    private void ApplySpecialArrowEffect(BaseEnemy targetEnemy, Vector3 hitCenter) // targetEnemy는 null일 수 있음 (벽에 맞은 경우)
    {
        if (arrowSkillData == null || arrowSkillData.skillName == "일반 활")
        {
            return;
        }

        switch (arrowSkillData.skillName)
        {
            case "불 화살":
                // 오직 '적'에게 맞았을 때만 점화
                if (targetEnemy != null)
                {
                    targetEnemy.ApplyBurnEffect(3, 0.2f, 1);
                }
                break;

            case "번개 화살":
                if (targetEnemy != null)
                {
                    HashSet<BaseEnemy> hitEnemies = new HashSet<BaseEnemy>();
                    hitEnemies.Add(targetEnemy);


                    // 1. 첫 번째 적에게 데미지 (화살 기본 데미지)
                    targetEnemy.TakeDamage(arrowDamage); // 첫 타격은 기본 데미지와 빨간색 깜빡임

                    // 2. 첫 번째 적에게 파란색 효과 적용 (빨간색 깜빡임 위에 덮어씀)
                    if (targetEnemy != null && targetEnemy.currentHP > 0)
                    {
                        targetEnemy.ApplyLightningEffect(0.5f);
                    }

                    // 3. 화살 위치에서 첫 타겟까지 Line Renderer 생성
                    if (lightningLinePrefab != null)
                    {
                        GameObject lineGO = Instantiate(lightningLinePrefab, Vector3.zero, Quaternion.identity);
                        LineRenderer lineRenderer = lineGO.GetComponent<LineRenderer>();
                        if (lineRenderer != null)
                        {
                            lineRenderer.positionCount = 2;
                            lineRenderer.SetPosition(0, transform.position); // 화살의 위치

                            // [수정] targetEnemy.transform.position 대신 전달받은 'hitCenter' 사용
                            lineRenderer.SetPosition(1, hitCenter);

                            Destroy(lineGO, 0.5f);
                        }
                    }

                    // [수정] 4. 연쇄 번개 시작 위치를 'hitCenter'로 전달
                    ChainLightning(hitCenter, lightningChainJumps, lightningChainRange, hitEnemies);
                }
                break;

            case "폭탄 화살":
                // '어디에 맞든' (적, 벽) 화살의 현재 위치에서 폭발

                // TODO: 여기에 폭발 이펙트(파티클) 생성 코드를 넣으세요.
                // 예: Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

                Collider[] hits = Physics.OverlapSphere(transform.position, bombRadius, enemyLayer);

                foreach (Collider hit in hits)
                {
                    if (hit.TryGetComponent<BaseEnemy>(out BaseEnemy enemy))
                    {
                        // 1. 폭발 데미지 적용
                        enemy.TakeDamage(bombDamage);

                        // 2. [수정] 생존한 적에게 화상 효과 적용
                        if (enemy != null && enemy.currentHP > 0)
                        {
                            enemy.ApplyBurnEffect(3, 0.2f, 1);
                        }
                    }
                }
                break;
        }
    }

    /// <summary>
    /// [추가] 연쇄 번개 재귀 함수
    /// </summary>
    private void ChainLightning(Vector3 center, int maxTotalHits, float currentRange, HashSet<BaseEnemy> alreadyHit)
    {
        // ... (maxTotalHits 체크 로직은 그대로) ...
        if (alreadyHit.Count >= maxTotalHits)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(center, currentRange, enemyLayer);

        BaseEnemy closestEnemyForNextJump = null;
        Vector3 nextJumpCenter = Vector3.zero; // 중심점을 저장할 변수 추가
        float minDistance = float.MaxValue;

        foreach (Collider hit in hits) // 'hit' 변수가 바로 우리가 찾는 Collider입니다!
        {
            if (hit.TryGetComponent<BaseEnemy>(out BaseEnemy enemy) && !alreadyHit.Contains(enemy))
            {
                if (alreadyHit.Count >= maxTotalHits)
                {
                    break;
                }

                // --- [수정] 적의 중심점을 'hit.bounds.center'로 계산 ---
                Vector3 enemyCenter = hit.bounds.center;

                // --- [핵심] 즉시 데미지와 효과 적용 ---
                enemy.TakeDamageWithoutFlash(lightningDamage);

                if (enemy != null && enemy.currentHP > 0)
                {
                    enemy.ApplyLightningEffect(0.5f);
                }

                // --- [추가] Line Renderer 생성 및 연결 ---
                if (lightningLinePrefab != null)
                {
                    GameObject lineGO = Instantiate(lightningLinePrefab, Vector3.zero, Quaternion.identity);
                    LineRenderer lineRenderer = lineGO.GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                    {
                        lineRenderer.positionCount = 2;
                        lineRenderer.SetPosition(0, center); // 이전 점프의 중심

                        // [수정] enemy.transform.position 대신 'enemyCenter' 사용
                        lineRenderer.SetPosition(1, enemyCenter);

                        Destroy(lineGO, 0.5f);
                    }
                }
                // ------------------------------------

                alreadyHit.Add(enemy);

                float distance = Vector3.Distance(center, enemyCenter); // [수G] 거리 계산도 중심점 기준
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemyForNextJump = enemy;

                    // [수정] 다음 점프를 위해 '중심점'을 저장
                    nextJumpCenter = enemyCenter;
                }
            }
        }

        // ... (다음 점프 실행 로직은 그대로) ...
        if (closestEnemyForNextJump != null && alreadyHit.Count < maxTotalHits)
        {
            ChainLightning(nextJumpCenter, maxTotalHits, currentRange * 0.8f, alreadyHit);
        }
    }

    // 화살을 오브젝트에 박히게 하는 로직
    private void ApplyStickLogic(GameObject target)
    {
        hasHitEnemy = true; // 중복 충돌 방지 플래그 설정

        // 물리 운동 중단
        if (rb != null)
        {
            // [수정] 순서 변경: 속도를 먼저 0으로 만들고 Kinematic으로 전환
            rb.velocity = Vector3.zero; // 1. 속도를 0으로 정지
            rb.isKinematic = true;        // 2. 물리 효과 끔
        }

        // 화살이 박힌 오브젝트를 부모로 설정
        transform.SetParent(target.transform);

        // 추가 충돌 방지를 위해 콜라이더 비활성화
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 일정 시간 후 화살 제거
        Destroy(gameObject, 5f);
    }
}