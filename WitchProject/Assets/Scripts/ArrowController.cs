using UnityEngine;
using UnityEngine.AI; // NavMeshAgent 사용 시 필요 (미니언 AI 코드에서 가져옴)

public class ArrowController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float gravityScale = 1.0f; // 중력 배율

    // [추가]: 화살의 데미지 값을 설정합니다. (PlayerShooting에서 설정하거나, 여기서 고정)
    private int arrowDamage = 3;

    // [추가]: 적용할 특수 화살 스킬 데이터
    private SkillNodeData arrowSkillData;

    private bool hasHitEnemy = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 화살이 발사 직후 회전하도록 설정 (선택 사항)
        transform.rotation = Quaternion.LookRotation(rb.velocity);
    }

    void FixedUpdate()
    {
        // 이미 박혔다면 움직임/회전 로직 건너뜀
        if (rb.isKinematic) return;

        // 화살에 중력 적용 (FixedUpdate에서 물리 연산 처리)
        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);

        // 화살이 항상 이동 방향을 바라보게 회전
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity);
        }
    }

    // [수정]: OnTriggerEnter로 변경하여 다른 투사체와 동일한 방식으로 처리
    private void OnTriggerEnter(Collider other)
    {
        // 이미 적을 맞혔다면 중복 처리 방지
        if (hasHitEnemy) return;

        // Enemy 태그가 아닐 경우, 벽이나 바닥에 부딪혔는지 확인하는 로직이 필요할 수 있습니다.
        // 현재는 Enemy 충돌만 처리합니다.

        if (other.CompareTag("Enemy"))
        {
            // 1. 데미지 로직: Enemy 타입 검사
            if (other.TryGetComponent<Enemy>(out Enemy hitEnemy))
            {
                ApplyDamageAndStick(hitEnemy.gameObject);
                return;
            }

            // 2. 데미지 로직: SummonerEnemy 타입 검사
            if (other.TryGetComponent<SummonerEnemy>(out SummonerEnemy hitSummonerEnemy))
            {
                ApplyDamageAndStick(hitSummonerEnemy.gameObject);
                return;
            }
        }

        //Enemy 태그 없이 다른 물체(벽, 바닥)에 닿았을 때 박히는 처리가 필요하다면 여기에 추가:
         if (!other.isTrigger && !other.CompareTag("Player"))
        {
            ApplyStickLogic(other.gameObject);
        }
    }

    public void InitializeArrow(int damage, SkillNodeData skillData)
    {
        this.arrowDamage = damage;
        this.arrowSkillData = skillData;

        // 특수 화살에 따라 속도나 궤적 등 추가 설정 가능
        if (skillData != null)
        {
            Debug.Log($"화살 초기화: 데미지 {damage}, 스킬: {skillData.skillName}");
        }
    }

    // 데미지 적용 및 박히는 처리 통합 함수
    private void ApplyDamageAndStick(GameObject target)
    {
        if (arrowSkillData != null)
        {
            ApplySpecialArrowEffect(target);
        }

        // 데미지 적용 (TryGetComponent를 통해 한 번에 처리)
        if (target.TryGetComponent<Enemy>(out Enemy enemy))
        {
            enemy.TakeDamage(arrowDamage);
        }
        else if (target.TryGetComponent<SummonerEnemy>(out SummonerEnemy summonerEnemy))
        {
            summonerEnemy.TakeDamage(arrowDamage);
        }

        // 박히는 처리
        ApplyStickLogic(target);
    }

    private void ApplySpecialArrowEffect(GameObject target)
    {
        switch (arrowSkillData.skillName)
        {
            case "불 화살":
                // target에 화상(Burn) 효과 컴포넌트 추가 및 적용
                Debug.Log("불 화살: 화상 효과 적용!");
                break;
            case "번개 화살":
                // 주변 적에게 체인 라이트닝 효과 적용
                Debug.Log("번개 화살: 연쇄 번개 효과 적용!");
                break;
            case "폭탄 화살":
                // 충돌 지점에 폭발 효과 및 광역 데미지 적용
                Debug.Log("폭탄 화살: 폭발 광역 데미지 적용!");
                break;
                // ... 다른 특수 화살 스킬 ...
        }
    }

    // 화살을 오브젝트에 박히게 하는 로직
    private void ApplyStickLogic(GameObject target)
    {
        hasHitEnemy = true;

        // Rigidbody를 비활성화 (물리 운동 중단)
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // 화살이 박힌 오브젝트를 부모로 설정하여 같이 움직이게 함
        transform.SetParent(target.transform);

        // Collider를 비활성화하여 추가적인 트리거/충돌을 방지 (선택 사항)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 일정 시간 후 화살 제거
        Destroy(gameObject, 5f);
    }
}