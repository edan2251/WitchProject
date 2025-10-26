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
        // 1. 태그 대신 'PlayerController' 스크립트를 먼저 찾습니다.
        //    (부딪힌 오브젝트가 자식이든 부모든 스크립트를 찾아냅니다)
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        //Debug.Log("자찾기시작한다이잉ㅇ");
        //Debug.Log("부딪힌 대상 이름: " + other.name + ", 태그: " + other.tag);
        // 2. 'pc'를 찾았다면 (즉, 플레이어의 일부와 부딪혔다면)
        if (pc != null)
        {
            //Debug.Log("진짜찾음");
            // 2a. 데미지를 줍니다.
            pc.ApplyPoisonDamage(initialHitDamage, poisonDamagePerTick, poisonInterval, poisonTicks, poisonChance);

            // 2b. 투사체를 파괴합니다.
            Destroy(gameObject);
        }
        // 3. 'pc'를 못 찾았다면 (플레이어가 아니라면)
        else if (!other.CompareTag("Enemy")) // 그리고 그게 적도 아니라면 (벽, 바닥 등)
        {
            //Debug.Log("이거뭐야진짜로");
            // 3a. 투사체를 파괴합니다. (이건 다시 추가하는 게 좋습니다)
            Destroy(gameObject);
        }
    }
}