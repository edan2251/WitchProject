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
        if (hasHitEnemy) return; // 중복 충돌 방지

        // 1. 적(BaseEnemy)인지 확인
        if (other.TryGetComponent<BaseEnemy>(out BaseEnemy hitEnemy))
        {
            hasHitEnemy = true;

            Vector3 hitPoint = other.bounds.center;

            // 2. 특수 효과 적용 (폭탄, 번개 등)
            ApplySpecialArrowEffect(hitEnemy, hitPoint);

            // 3. 기본 데미지 적용 (특수 효과로 안 죽었을 시)
            if (hitEnemy != null)
            {
                hitEnemy.TakeDamage(arrowDamage);
            }

            // 4. 화살 박기
            ApplyStickLogic(other.gameObject);
            return; // 처리 완료
        }

        // 2.  파괴 가능한 코어(DestructibleCore)인지 확인
        if (other.TryGetComponent<DestructibleCore>(out DestructibleCore core))
        {
            hasHitEnemy = true;

            // (참고: 폭탄 화살이 코어에 맞으면 폭발하지 않습니다.
            //       만약 코어에 맞아도 폭발하게 하려면 ApplySpecialArrowEffect(null, ...) 호출 필요)

            // 코어에 기본 데미지만 적용
            core.TakeDamage(arrowDamage);

            // 화살 박기
            ApplyStickLogic(other.gameObject);
            return; // 처리 완료
        }

        // 3. 적도, 코어도 아닌 '환경' (벽, 바닥 등)인지 확인
        // (플레이어 태그 무시, 트리거 무시)
        if (!other.isTrigger && !other.CompareTag("Player"))
        {
            hasHitEnemy = true;

            // [추가] 폭탄 화살은 벽에 맞아도 터져야 함
            ApplySpecialArrowEffect(null, transform.position); // 첫 번째 인자로 null 전달

            // 화살 박기
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


    private void ApplySpecialArrowEffect(BaseEnemy targetEnemy, Vector3 hitCenter)
    {
        if (arrowSkillData == null || arrowSkillData.skillName == "일반 활")
        {
            return;
        }

        switch (arrowSkillData.skillName)
        {
            case "불 화살":
                if (targetEnemy != null)
                {
                    targetEnemy.ApplyBurnEffect(3, 0.2f, 1);
                }
                break;

            case "번개 화살":
                if (targetEnemy != null)
                {
                    // ... (번개 화살 로직은 DestructibleCore와 상관 없으므로 동일) ...
                    HashSet<BaseEnemy> hitEnemies = new HashSet<BaseEnemy>();
                    hitEnemies.Add(targetEnemy);
                    targetEnemy.TakeDamage(arrowDamage);
                    if (targetEnemy != null && targetEnemy.currentHP > 0)
                    {
                        targetEnemy.ApplyLightningEffect(0.5f);
                    }
                    if (lightningLinePrefab != null)
                    {
                        GameObject lineGO = Instantiate(lightningLinePrefab, Vector3.zero, Quaternion.identity);
                        LineRenderer lineRenderer = lineGO.GetComponent<LineRenderer>();
                        if (lineRenderer != null)
                        {
                            lineRenderer.positionCount = 2;
                            lineRenderer.SetPosition(0, transform.position);
                            lineRenderer.SetPosition(1, hitCenter);
                            Destroy(lineGO, 0.5f);
                        }
                    }
                    ChainLightning(hitCenter, lightningChainJumps, lightningChainRange, hitEnemies);
                }
                break;

            case "폭탄 화살":
                // [★수정★] 이제 targetEnemy가 null이어도 (즉, 벽이나 코어에 맞아도) 폭발

                // TODO: 폭발 이펙트 생성
                // Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

                // [★수정★] 폭발 데미지 (적 + 파괴 가능한 코어)
                Collider[] hits = Physics.OverlapSphere(transform.position, bombRadius, enemyLayer | LayerMask.GetMask("Destructible")); // [★수정★] Destructible 레이어 추가

                foreach (Collider hit in hits)
                {
                    // 1. 적인지 확인
                    if (hit.TryGetComponent<BaseEnemy>(out BaseEnemy enemy))
                    {
                        enemy.TakeDamage(bombDamage);
                        if (enemy != null && enemy.currentHP > 0)
                        {
                            enemy.ApplyBurnEffect(3, 0.2f, 1);
                        }
                    }
                    // 2. [★추가★] 파괴 가능한 코어인지 확인
                    else if (hit.TryGetComponent<DestructibleCore>(out DestructibleCore core))
                    {
                        core.TakeDamage(bombDamage); // 폭발 데미지를 줌
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