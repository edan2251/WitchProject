using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    // [수정] 요청대로 속도를 8f에서 20f로 상향
    public float speed = 20f;
    public float lifeTime = 3f;

    // 독 데미지 값 정의
    public int initialHitDamage = 3;
    public int poisonDamagePerTick = 1;
    public float poisonInterval = 0.2f;
    public int poisonTicks = 4;
    [Range(0, 1)] // [추가] 인스펙터에서 0~1 사이 슬라이더로 조절
    public float poisonChance = 0.7f; // [추가] 독 확률 70%

    private Vector3 moveDir;

    public void SetDirection(Vector3 dir)
    {
        moveDir = dir.normalized;

        // [추가] 독침이 날아가는 방향을 바라보도록 회전
        if (moveDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDir);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        // [수정] transform.Translate 대신 SetDirection에서 받은 'moveDir'을 사용
        // 이것이 고블린 -> 플레이어의 정확한 방향입니다.
        transform.position += moveDir * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 플레이어를 먼저 찾습니다.
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            // 1a. 플레이어라면 독 데미지를 적용합니다.
            pc.ApplyPoisonDamage(initialHitDamage, poisonDamagePerTick, poisonInterval, poisonTicks, poisonChance);
            Destroy(gameObject);
            return; // 충돌 처리 완료
        }

        // 2. [추가] 플레이어가 아니라면, 방어 오브젝트인지 확인합니다.
        DefenseObjective obj = other.GetComponentInParent<DefenseObjective>();
        if (obj != null)
        {
            // 2a. 방어 오브젝트라면 일반 데미지를 줍니다. (독 효과 없음)
            obj.TakeDamage(initialHitDamage);
            Destroy(gameObject);
            return; // 충돌 처리 완료
        }

        // 3. 플레이어도, 방어 오브젝트도 아니라면 (그리고 적도 아니라면)
        if (!other.CompareTag("Enemy") && !other.CompareTag("Projectile")) // [수정] "Projectile" 태그도 무시
        {
            Destroy(gameObject);
        }
    }
}