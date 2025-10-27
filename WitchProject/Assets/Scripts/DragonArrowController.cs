using UnityEngine;

// 충돌 시 적에게 데미지를 주고 관통하는 기본 발사체
public class DragonArrowController : MonoBehaviour
{
    public float speed = 25f;       // 용 속도
    public int damage = 10;         // 높은 데미지?
    public float lifetime = 5f;     // 생존 시간
    // 필요시 LayerMask 추가

    private Rigidbody rb;
    //private bool hasHitSomething = false; // 밸런스를 위해 다중 타격 방지 필요시 사용

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 콜라이더의 IsTrigger가 반드시 TRUE여야 함
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("DragonArrowController: Collider는 IsTrigger=true여야 합니다.", this);
        }
    }

    void Start()
    {
        Destroy(gameObject, lifetime); // 생존 시간 후 자동 파괴
                                       // Rigidbody AddForce를 사용하지 않는 경우 여기서 속도 설정:
                                       // if (rb != null && rb.isKinematic) rb.velocity = transform.forward * speed;
    }

    void FixedUpdate()
    {
        // Kinematic Rigidbody 또는 Rigidbody 없을 때
        if (rb == null || rb.isKinematic)
        {
            // 직접 이동
            transform.Translate(Vector3.forward * speed * Time.fixedDeltaTime);
        }
        // 선택 사항: Non-kinematic RB 사용 시 속도 방향으로 회전
        else if (rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity);
        }
    }

    // 트리거 충돌 시
    void OnTriggerEnter(Collider other)
    {
        // 선택 사항: 너무 빠르게 여러 적 타격 방지
        // if (hasHitSomething) return;

        // 적과 충돌했는지 확인
        if (other.TryGetComponent<BaseEnemy>(out BaseEnemy enemy))
        {
            Debug.Log($"용 화살이 {enemy.name} 명중");
            enemy.TakeDamage(damage); // 데미지 적용
            // hasHitSomething = true; // 용이 한 대상만 공격하거나 관통 제한이 있다면 주석 해제
        }
        // 선택 사항: 파괴 가능한 코어와 충돌했는지 확인
        else if (other.TryGetComponent<DestructibleCore>(out DestructibleCore core))
        {
            Debug.Log($"용 화살이 파괴 가능한 코어 명중");
            core.TakeDamage(damage);
            // hasHitSomething = true;
        }

        // 용은 충돌 시 파괴되거나 멈추지 않음 (생존 시간이 다할 때까지 관통)
    }
}