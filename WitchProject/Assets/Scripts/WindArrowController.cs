using UnityEngine;
using System.Collections;
using System.Collections.Generic; // List 사용

// 기본 화살 로직을 상속받고, 박힌 후 바람 당기기 효과 추가
public class WindArrowController : ArrowController // 중요: ArrowController 상속
{
    [Header("바람 효과")]
    public float pullRadius = 8f;   // 당기는 범위
    public float pullForce = 5f;    // 당기는 힘
    public float pullDuration = 3f; // 당기기 지속 시간
    public LayerMask pullLayerMask; // 인스펙터에서 Enemy 레이어로 설정

    private bool pullEffectActivated = false; // 효과 중복 실행 방지

    // ApplyStickLogic을 오버라이드하여 박힌 *후에* 당기기 시작
    // ArrowController.cs의 ApplyStickLogic을 'protected virtual'로 변경해야 함
    /*
    // ArrowController.cs 내부:
    protected virtual void ApplyStickLogic(GameObject target) { // private을 protected virtual로 변경
        // ... 기존 박히는 로직 ...
    }
    */

    protected override void ApplyStickLogic(GameObject target)
    {
        // 먼저 기본 박히는 로직 실행
        base.ApplyStickLogic(target);

        // 아직 활성화되지 않았다면 당기기 효과 시작
        if (!pullEffectActivated)
        {
            pullEffectActivated = true;
            Debug.Log("바람 화살 박힘! 당기기 효과 시작.");
            StartCoroutine(PullEnemiesCoroutine());
        }
    }

    // 적들을 당기는 코루틴
    private IEnumerator PullEnemiesCoroutine()
    {
        float timer = 0f;
        Vector3 pullCenter = transform.position; // 화살이 박힌 위치

        // 영향을 받는 Rigidbody 목록 (매 프레임 찾거나 캐싱)
        List<Rigidbody> affectedRbs = new List<Rigidbody>();
        // 시작 시 한 번 찾기 (선택적 최적화)
        FindEnemiesInRange(pullCenter, affectedRbs);

        while (timer < pullDuration) // 지속 시간 동안 반복
        {
            // 방법 1: 매 프레임 적 찾기 (더 동적, 성능 약간 저하)
            // affectedRbs.Clear();
            // FindEnemiesInRange(pullCenter, affectedRbs);

            // 찾은 적들에게 힘 적용
            foreach (Rigidbody enemyRb in affectedRbs)
            {
                if (enemyRb != null) // 당기는 도중 적이 파괴될 수 있으므로 확인
                {
                    // 화살 방향으로의 벡터 계산
                    Vector3 directionToArrow = (pullCenter - enemyRb.position).normalized;
                    // 화살 방향으로 힘 가하기
                    enemyRb.AddForce(directionToArrow * pullForce, ForceMode.Force);

                    // 선택 사항: 너무 빠르게 끌려오거나 지나치는 것 방지 (항력 추가 또는 속도 제한)
                    // BaseEnemy enemy = enemyRb.GetComponent<BaseEnemy>();
                    // if(enemy != null && enemy.agent != null) {
                    //     // NavMeshAgent 속도 직접 제어는 위험할 수 있음. AddForce가 더 안전.
                    //     enemy.agent.velocity = directionToArrow * pullForce * 0.1f;
                    // }
                }
            }

            timer += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        Debug.Log("바람 화살 당기기 효과 종료.");
        // 코루틴 종료. 화살 자체는 나중에 부모 클래스 로직에 의해 파괴됨.
    }

    // 범위 내 적 Rigidbody 찾는 헬퍼 함수
    private void FindEnemiesInRange(Vector3 center, List<Rigidbody> rigidbodies)
    {
        rigidbodies.Clear(); // 새 목록 만들기 전 초기화
        Collider[] hits = Physics.OverlapSphere(center, pullRadius, pullLayerMask);
        foreach (Collider hit in hits)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>(); // 적이 Rigidbody를 가지고 있다고 가정
                                                          // 유효한 적인지 확인 (BaseEnemy 스크립트 존재 여부)
            BaseEnemy enemy = hit.GetComponent<BaseEnemy>();
            // Rigidbody가 있고, 적이며, 목록에 아직 없으면 추가
            if (rb != null && enemy != null && !rigidbodies.Contains(rb))
            {
                rigidbodies.Add(rb);
            }
        }
    }
}