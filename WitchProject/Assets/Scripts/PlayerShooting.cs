using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public GameObject projectilePrefab;
    public GameObject bombPrefab;

    public GameObject handedBomb;
    public GameObject handedGun;

    public Transform firePoint;
    Camera cam;

    private bool useBomb = false;         // 무기 전환 상태 (false = 총알, true = 폭탄)

    // 기존 변수 유지
    public float damageRange = 5f;        // 원뿔의 깊이 (최대 거리)
    public LayerMask enemyLayer;           // 적 오브젝트의 Layer Mask
    private const int instaKillDamage = 99999; // 부여할 데미지

    // 원뿔형 범위 공격을 위한 새 변수 추가
    public float coneAngle = 60f; // 원뿔의 각도 (전체 각도, 예: 60도는 정면에서 좌우 30도씩)
    public Transform effectSpawnPoint;

    public GameObject areaAttackParticlePrefab;



    void Start()
    {
        cam = Camera.main;

        if (handedBomb != null)
        {
            handedBomb.SetActive(useBomb); // useBomb이 false라면 비활성화된 상태로 시작
        }
        if (handedGun != null)
        {
            handedGun.SetActive(useBomb == false); 
        }
    }

    void Update()
    {
        // 무기 전환 (Z 키)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            useBomb = !useBomb;
            Debug.Log(useBomb ? "폭탄 모드" : "총알 모드");

            // 무기 모델 활성화/비활성화
            if (handedBomb != null)
            {
                handedBomb.SetActive(useBomb);
            }
            if (handedGun != null)
            {
                handedGun.SetActive(useBomb == false);
            }
        }

        // 발사
        if (Input.GetMouseButtonDown(0))
        {
            // 1. 원뿔 범위 내 적 검출 및 데미지 부여 시도
            if (TryConeDamage()) // 함수 이름 변경
            {
                // 적이 범위 내에 있었으므로 발사 동작을 수행하지 않고 끝냄
                return;
            }

            // 2. 범위 내 적이 없었을 경우에만 기존 발사 로직 실행
            if (useBomb)
                ThrowBomb();
            else
                ShootFront();
        }
    }

    // 원뿔 범위 내 적 찾기 및 데미지 부여 함수
    private bool TryConeDamage()
    {
        // 1. OverlapSphere를 사용하여 1차적으로 '구' 내의 모든 잠재적인 적을 검출합니다.
        // 이는 Raycast보다 훨씬 효율적입니다.
        Collider[] colliders = Physics.OverlapSphere(transform.position, damageRange, enemyLayer);

        int damageCount = 0;

        foreach (Collider col in colliders)
        {
            // 2. 검출된 오브젝트가 원뿔 범위 내에 있는지 '각도'를 계산하여 확인합니다.

            // 플레이어에서 적을 향하는 방향 벡터
            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;

            // 플레이어의 정면 벡터 (원뿔의 축)
            Vector3 forward = transform.forward;

            // 정면 벡터와 적 방향 벡터 사이의 각도 계산
            float angleToTarget = Vector3.Angle(forward, directionToTarget);

            // 각도가 설정된 원뿔 각도(coneAngle)의 절반보다 작으면 범위 안에 있는 것입니다.
            if (angleToTarget < coneAngle / 2)
            {
                // **옵션: 벽 관통을 막으려면 레이캐스트 추가 검사**
                // 적이 원뿔 범위 내에 있고, 플레이어와 적 사이에 장애물이 없는지 확인합니다.
                // Physics.Linecast(플레이어 위치, 적 위치, ~장애물 LayerMask)를 사용하여 추가 검사가 가능하나,
                // 간단한 구현을 위해 여기서는 생략하고 각도만으로 판정합니다.

                // 3. 적 컴포넌트를 가져와 데미지를 부여합니다.
                Enemy enemyScript = col.GetComponent<Enemy>();

                if (enemyScript != null)
                {
                    enemyScript.TakeDamage(instaKillDamage);
                    damageCount++;
                }
            }
        }

        if (damageCount > 0)
        {
            Debug.Log($"원뿔 범위 내 {damageCount}명의 적에게 {instaKillDamage} 데미지 부여!");

            if (areaAttackParticlePrefab != null)
            {
                // effectSpawnPoint가 연결되어 있으면 해당 Transform의 위치와 회전을 사용
                Transform spawnPoint = effectSpawnPoint != null ? effectSpawnPoint : transform;

                GameObject particleInstance = Instantiate(areaAttackParticlePrefab, spawnPoint.position, spawnPoint.rotation);

                Destroy(particleInstance, 2f);
            }

            return true;
        }

        return false; // 범위 내 적이 없었으므로 false 반환
    }

    void ShootFront()
    {
        Vector3 direction = firePoint.forward;
        Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
    }

    void ThrowBomb()
    {
        // 폭탄 생성
        GameObject bomb = Instantiate(bombPrefab, firePoint.position, Quaternion.identity);

        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 throwForce = firePoint.forward * 10f + firePoint.up * 5f;
            rb.AddForce(throwForce, ForceMode.Impulse);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        // 1. 원뿔의 끝 지점을 구합니다. (원의 중심)
        Vector3 coneCenter = transform.position + transform.forward * damageRange;

        // 2. 원뿔의 밑면(원)의 반지름을 각도를 이용해 계산합니다.
        float radius = damageRange * Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad);

        // 3. 구 형태로 원뿔의 끝 부분을 시각화 (정확한 원뿔 시각화는 복잡하므로 간단히 표현)
        Gizmos.DrawWireSphere(coneCenter, radius);

        // 4. 원뿔의 방향선을 그립니다.
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, coneAngle / 2, 0) * transform.forward * damageRange);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, -coneAngle / 2, 0) * transform.forward * damageRange);
    }
}