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

    [Header("Visual Effect")]
    public GameObject pullEffectPrefab;
    private GameObject activePullEffectInstance;

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

        // [★추가★] 이펙트 생성
        if (pullEffectPrefab != null)
        {
            // 이펙트를 화살 위치에 생성하고, 화살의 자식으로 만들어 따라다니게 함
            activePullEffectInstance = Instantiate(pullEffectPrefab, pullCenter, Quaternion.identity, transform);
            // TODO: 필요하다면 이펙트의 크기(scale)를 pullRadius에 맞춰 조절할 수 있습니다.
            // activePullEffectInstance.transform.localScale = Vector3.one * pullRadius * 0.2f; // 예시
        }

        // 영향을 받는 BaseEnemy 목록 (매 프레임 새로 찾음)
        List<BaseEnemy> affectedEnemies = new List<BaseEnemy>();

        while (timer < pullDuration) // 지속 시간 동안 반복
        {
            // ... (적 찾기 및 Move() 적용 로직은 동일) ...
            affectedEnemies.Clear();
            Collider[] hits = Physics.OverlapSphere(pullCenter, pullRadius, pullLayerMask);
            foreach (Collider hit in hits)
            {
                BaseEnemy enemy = hit.GetComponent<BaseEnemy>();
                if (enemy != null && enemy.agent != null && enemy.agent.enabled)
                {
                    affectedEnemies.Add(enemy);
                }
            }
            foreach (BaseEnemy enemy in affectedEnemies)
            {
                if (enemy != null && enemy.agent != null && enemy.agent.enabled)
                {
                    Vector3 directionToArrow = (pullCenter - enemy.transform.position).normalized;
                    Vector3 movement = directionToArrow * pullForce * Time.deltaTime;
                    enemy.agent.Move(movement);
                }
            }


            timer += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        Debug.Log("바람 화살 당기기 효과 종료.");

        // [★추가★] 코루틴 종료 시 이펙트 제거
        if (activePullEffectInstance != null)
        {
            // 파티클 시스템이라면 Stop()을 호출하고 잠시 후 파괴하는 것이 더 자연스러울 수 있습니다.
            // 여기서는 즉시 파괴합니다.
            Destroy(activePullEffectInstance);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 기즈모 색상 설정 (예: 하늘색)
        Gizmos.color = Color.cyan;

        // 현재 화살 위치를 중심으로 pullRadius 크기의 와이어 스피어 그리기
        Gizmos.DrawWireSphere(transform.position, pullRadius);
    }
}