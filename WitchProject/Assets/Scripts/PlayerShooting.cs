using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using DG.Tweening; // DOTween 추가

public class PlayerShooting : MonoBehaviour
{
    // 무기 모드 Enum 정의
    public enum WeaponMode { Gun, Bomb, Bow }

    // ---------------------------------------------------------------------
    // [1] 무기 및 Cinemachine 설정
    // ---------------------------------------------------------------------
    [Header("Weapon Mode")]
    public WeaponMode currentWeapon = WeaponMode.Bow;

    [Header("Cinemachine/Aim")]
    [SerializeField] private CinemachineVirtualCamera aimCam;
    private bool isAiming = false;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("DOTween Settings")] // DOTween 관련 설정 추가
    [SerializeField] private float aimFOV = 40f;        // 조준 시 FOV
    [SerializeField] private float defaultFOV = 60f;    // 기본 FOV
    [SerializeField] private float fovDuration = 0.3f;  // FOV 변경 애니메이션 시간

    [Header("Bow Charge & UI")] // 조준점 변수 추가
    public GameObject bowCrosshairUI; // BowCrosshair Image의 GameObject를 여기에 할당합니다.
    public float launchForceMax = 60f;
    public float chargeRate = 40f;
    private float currentChargeTime = 0f;
    public float launchForce = 30f; // 화살 발사 속도
    private bool isCharging = false;

    // 무기 프리팹 및 모델
    [Header("Weapon Prefabs & Models")]
    public GameObject projectilePrefab; // 총알
    public GameObject bombPrefab;       // 폭탄
    public GameObject arrowPrefab;      // 화살

    public GameObject handedBomb;
    public GameObject handedGun;
    public GameObject handedBow;        // 활 모델

    public Transform firePoint;
    Camera cam;

    // ---------------------------------------------------------------------
    // [2] 근접/광역 공격 설정 (기존 로직)
    // ---------------------------------------------------------------------
    [Header("Cone Attack Settings")]
    public float damageRange = 5f;        // 원뿔의 깊이 (최대 거리)
    public LayerMask enemyLayer;            // 적 오브젝트의 Layer Mask
    private const int instaKillDamage = 10; // 부여할 데미지

    public float coneAngle = 60f; // 원뿔의 각도
    public Transform effectSpawnPoint;

    [Header("Visual Effects")]
    public GameObject areaAttackParticlePrefab;

    [Header("Skill Tree Integration")]
    [SerializeField] private int baseArrowDamage = 3; // 일반 활의 기본 데미지
    private int currentArrowDamage; // 최종 화살 데미지


    //----------------------------------------------------------------------------------
    // Start & Update
    //----------------------------------------------------------------------------------
    void Start()
    {
        cam = Camera.main;
        currentArrowDamage = baseArrowDamage;

        UpdateWeaponVisibility();

        if (aimCam != null)
        {
            aimCam.Priority = 0;
            aimCam.m_Lens.FieldOfView = defaultFOV;
        }

        // 시작 시 조준점 숨기기
        if (bowCrosshairUI != null)
        {
            bowCrosshairUI.SetActive(false);
        }
    }

    void Update()
    {
        if (SkillUIManager.Instance != null && SkillUIManager.Instance.IsPanelOpen)
        {
            // 패널이 열려 있을 때 마우스 입력(공격, 조준)을 막음
            // 활 충전 중이라면 충전 상태를 강제 종료 (UI를 열면서 발사가 나가는 것을 방지)
            if (isCharging)
            {
                isCharging = false;
                currentChargeTime = 0f;
            }
            return; // 이후의 모든 입력/공격 로직을 건너뜀
        }

        // 1. 무기 전환 (Z 키): 현재 모드에서 다음 모드로 순환
        if (Input.GetKeyDown(KeyCode.Z))
        {
            currentWeapon = (WeaponMode)(((int)currentWeapon + 1) % System.Enum.GetValues(typeof(WeaponMode)).Length);
            Debug.Log($"무기 전환: {currentWeapon}");
            UpdateWeaponVisibility();
        }

        // 2. 활 모드 조준/발사 로직
        if (currentWeapon == WeaponMode.Bow)
        {
            if (isCharging)
            {
                currentChargeTime += Time.deltaTime;
            }

            HandleBowInput();

            if (!isCharging && Input.GetMouseButtonDown(0))
            {
                if (TryConeDamage())
                {
                    return;
                }
            }

            return; // 활 모드일 때 아래 총/폭탄 발사 로직은 건너뜁니다.
        }

        // 3. 총/폭탄 모드 발사 (원뿔 범위 공격 체크)
        if (Input.GetMouseButtonDown(0))
        {
            if (TryConeDamage())
            {
                return;
            }

            if (currentWeapon == WeaponMode.Bomb)
                ThrowBomb();
            else if (currentWeapon == WeaponMode.Gun)
                ShootFront();
        }
    }

    void LateUpdate()
    {
        if (SkillUIManager.Instance != null && SkillUIManager.Instance.IsPanelOpen)
        {
            return;
        }

        // 조준 중일 때 캐릭터가 카메라 방향을 부드럽게 바라보도록 처리
        if (isAiming)
        {
            LookAtCameraDirection();
        }
    }

    public void UpdateArrowDamage(int bonusAttack)
    {
        currentArrowDamage = baseArrowDamage + bonusAttack;
        Debug.Log($"화살 최종 데미지 업데이트: {currentArrowDamage}");
    }

    //----------------------------------------------------------------------------------
    // 무기 및 조준 관리
    //----------------------------------------------------------------------------------

    void UpdateWeaponVisibility()
    {
        if (handedGun != null) handedGun.SetActive(currentWeapon == WeaponMode.Gun);
        if (handedBomb != null) handedBomb.SetActive(currentWeapon == WeaponMode.Bomb);
        if (handedBow != null) handedBow.SetActive(currentWeapon == WeaponMode.Bow);

        if (isAiming) StopAiming();

        // 무기 전환 시 활이 아니면 조준점 숨기기
        if (bowCrosshairUI != null)
        {
            bowCrosshairUI.SetActive(currentWeapon == WeaponMode.Bow && isAiming);
        }
    }

    void HandleBowInput()
    {
        // 우클릭: 조준 시작
        if (Input.GetMouseButtonDown(1))
        {
            StartAiming();
        }

        // 우클릭 해제: 조준 종료
        if (Input.GetMouseButtonUp(1))
        {
            StopAiming();
        }

        // 좌클릭: 시위 당기기 시작
        if (isAiming && Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            currentChargeTime = 0f;
        }

        // 좌클릭 해제: 발사
        if (isAiming && Input.GetMouseButtonUp(0))
        {
            ShootArrow();
            isCharging = false;
        }

        // 조준 취소 시 충전 리셋
        if (!isAiming && isCharging)
        {
            isCharging = false;
            currentChargeTime = 0f;
        }
    }

    void StartAiming()
    {
        if (aimCam == null) return;
        isAiming = true;
        aimCam.Priority = 10;

        // [조준점 활성화]
        if (bowCrosshairUI != null)
        {
            bowCrosshairUI.SetActive(true);
        }

        DOTween.To(() => aimCam.m_Lens.FieldOfView,
                   x => aimCam.m_Lens.FieldOfView = x,
                   aimFOV,
                   fovDuration)
               .SetEase(Ease.OutQuad);
    }

    void StopAiming()
    {
        if (aimCam == null) return;
        isAiming = false;
        aimCam.Priority = 0;

        // [조준점 비활성화]
        if (bowCrosshairUI != null)
        {
            // 충전 상태가 아니거나, 무기가 활이 아닐 때만 완전히 숨김
            if (currentWeapon == WeaponMode.Bow)
            {
                bowCrosshairUI.SetActive(false);
            }
        }

        DOTween.To(() => aimCam.m_Lens.FieldOfView,
                   x => aimCam.m_Lens.FieldOfView = x,
                   defaultFOV,
                   fovDuration)
               .SetEase(Ease.OutQuad);
    }

    void LookAtCameraDirection()
    {
        if (cam == null) return;

        Vector3 lookDir = cam.transform.forward;
        lookDir.y = 0;

        if (lookDir.sqrMagnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    //----------------------------------------------------------------------------------
    // 공격 함수
    //----------------------------------------------------------------------------------

    void ShootArrow()
    {
        if (arrowPrefab == null || firePoint == null) return;

        float finalLaunchForce = Mathf.Clamp(currentChargeTime * chargeRate, 0f, launchForceMax);

        if (finalLaunchForce < 5f)
        {
            Debug.Log("활 시위를 충분히 당기지 않았습니다!");
            currentChargeTime = 0f;
            return;
        }

        Vector3 aimDirection = Camera.main.transform.forward;

        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.LookRotation(aimDirection));

        ArrowController ac = arrow.GetComponent<ArrowController>();
        if (ac != null)
        {
            // 기본 데미지에 스킬 보너스를 더한 최종 데미지 전달
            ac.InitializeArrow(currentArrowDamage, SkillManager.Instance.activeArrowSkill);
        }

        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb == null) { rb = arrow.AddComponent<Rigidbody>(); }

        rb.AddForce(aimDirection * finalLaunchForce, ForceMode.VelocityChange);

        currentChargeTime = 0f;
    }


    private bool TryConeDamage()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, damageRange, enemyLayer);
        int damageCount = 0;

        foreach (Collider col in colliders)
        {
            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
            Vector3 forward = transform.forward;

            float angleToTarget = Vector3.Angle(forward, directionToTarget);

            if (angleToTarget < coneAngle / 2)
            {
                if (col.TryGetComponent<Enemy>(out Enemy enemyScript))
                {
                    enemyScript.TakeDamage(instaKillDamage);
                    damageCount++;
                }
                if (col.TryGetComponent<SummonerEnemy>(out SummonerEnemy SummonerenemyScript))
                {
                    SummonerenemyScript.TakeDamage(instaKillDamage);
                    damageCount++;
                }
            }
        }

        if (damageCount > 0)
        {
            Debug.Log($"원뿔 범위 내 {damageCount}명의 적에게 {instaKillDamage} 데미지 부여!");

            if (areaAttackParticlePrefab != null)
            {
                Transform spawnPoint = effectSpawnPoint != null ? effectSpawnPoint : transform;
                GameObject particleInstance = Instantiate(areaAttackParticlePrefab, spawnPoint.position, spawnPoint.rotation);
                Destroy(particleInstance, 2f);
            }
            return true;
        }
        return false;
    }

    void ShootFront()
    {
        Vector3 direction = firePoint.forward;
        Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
    }

    void ThrowBomb()
    {
        GameObject bomb = Instantiate(bombPrefab, firePoint.position, Quaternion.identity);
        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 throwForce = firePoint.forward * 10f + firePoint.up * 5f;
            rb.AddForce(throwForce, ForceMode.Impulse);
        }
    }

    //----------------------------------------------------------------------------------
    // Gizmos (디버그 시각화)
    //----------------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 coneCenter = transform.position + transform.forward * damageRange;
        float radius = damageRange * Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad);

        Gizmos.DrawWireSphere(coneCenter, radius);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, coneAngle / 2, 0) * transform.forward * damageRange);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, -coneAngle / 2, 0) * transform.forward * damageRange);
    }
}