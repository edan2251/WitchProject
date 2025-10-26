using UnityEngine;
using UnityEngine.AI;

public class ArrowController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float gravityScale = 1.0f; // 중력 배율

    private int arrowDamage = 3;
    private SkillNodeData arrowSkillData;
    private bool hasHitEnemy = false;

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

        // 1. [핵심 수정] BaseEnemy를 상속받는 모든 적을 한 번에 검사
        if (other.TryGetComponent<BaseEnemy>(out BaseEnemy hitEnemy))
        {
            // 적을 맞혔으므로 데미지 적용 및 화살 박기
            ApplyDamageAndStick(hitEnemy.gameObject);
            return; // 함수 종료
        }

        // 2. 적이 아니고, 플레이어도 아니고, 트리거도 아닌 물체(벽, 바닥 등)에 박히는 로직
        if (!other.isTrigger && !other.CompareTag("Player") && !other.CompareTag("Enemy"))
        {
            ApplyStickLogic(other.gameObject);
        }
    }

    public void InitializeArrow(int damage, SkillNodeData skillData)
    {
        this.arrowDamage = damage;
        this.arrowSkillData = skillData;

        if (skillData != null)
        {
            Debug.Log($"화살 초기화: 데미지 {damage}, 스킬: {skillData.skillName}");
        }
    }

    // [수정] 데미지 적용 및 박히는 처리 통합 함수
    private void ApplyDamageAndStick(GameObject target)
    {
        // 1. 특수 효과 적용 (있다면)
        if (arrowSkillData != null)
        {
            ApplySpecialArrowEffect(target);
        }

        // 2. [핵심 수정] BaseEnemy 컴포넌트로 데미지 적용
        // (OnTriggerEnter에서 이미 검사했지만, 안전을 위해 한 번 더)
        if (target.TryGetComponent<BaseEnemy>(out BaseEnemy baseEnemy))
        {
            baseEnemy.TakeDamage(arrowDamage);
        }

        // 3. 화살 박히는 로직 실행
        ApplyStickLogic(target);
    }

    private void ApplySpecialArrowEffect(GameObject target)
    {
        switch (arrowSkillData.skillName)
        {
            case "불 화살":
                Debug.Log("불 화살: 화상 효과 적용!");
                break;
            case "번개 화살":
                Debug.Log("번개 화살: 연쇄 번개 효과 적용!");
                break;
            case "폭탄 화살":
                Debug.Log("폭탄 화살: 폭발 광역 데미지 적용!");
                break;
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