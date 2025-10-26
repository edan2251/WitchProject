using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using DG.Tweening; // DOTween 추가

public class PlayerShooting : MonoBehaviour
{
    PlayerController playerController;

    // [1] 무기 및 Cinemachine 설정
    // ---------------------------------------------------------------------
    [Header("Cinemachine/Aim")]
    [SerializeField] private CinemachineVirtualCamera aimCam;
    private bool isAiming = false;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("DOTween Settings")] // DOTween 관련 설정 추가
    [SerializeField] private float aimFOV = 40f;          // 조준 시 FOV
    [SerializeField] private float defaultFOV = 60f;      // 기본 FOV
    [SerializeField] private float fovDuration = 0.3f;    // FOV 변경 애니메이션 시간

    [Header("Bow Charge & UI")] // 조준점 변수 추가
    public GameObject bowCrosshairUI; // BowCrosshair Image의 GameObject를 여기에 할당합니다.
    public float launchForceMax = 60f;
    public float chargeRate = 40f;
    private float currentChargeTime = 0f;
    public float launchForce = 30f; // 화살 발사 속도
    private bool isCharging = false;

    // 무기 프리팹 및 모델
    [Header("Weapon Prefabs & Models")]
    public GameObject arrowPrefab;      // 화살
    public GameObject handedBow;        // 활 모델
    public Transform firePoint;
    Camera cam;

    // ---------------------------------------------------------------------
    // [2] 근접/광역 공격 설정 (기존 로직)
    // ---------------------------------------------------------------------
    [Header("Cone Attack Settings")]
    public float damageRange = 5f;      // 원뿔의 깊이 (최대 거리)
    public LayerMask enemyLayer;        // 적 오브젝트의 Layer Mask
    private const int instaKillDamage = 10; // 부여할 데미지

    public float coneAngle = 60f; // 원뿔의 각도
    public Transform effectSpawnPoint;

    [Header("Visual Effects")]
    public GameObject areaAttackParticlePrefab;

    [Header("Skill Tree Integration")]
    public int baseArrowDamage = 3; // 일반 활의 기본 데미지
    public int currentArrowDamage; // 최종 화살 데미지


    //----------------------------------------------------------------------------------
    // Start & Update
    //----------------------------------------------------------------------------------
    void Start()
    {
        playerController = GetComponent<PlayerController>();

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
            if (isCharging)
            {
                isCharging = false;
                currentChargeTime = 0f;
            }
            return; // 이후의 모든 입력/공격 로직을 건너뜀
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            SkillManager.Instance.SelectNextArrowSkill();
        }

        if (isCharging)
        {
            currentChargeTime += Time.deltaTime;
        }

        HandleBowInput();

        // 조준 중이 아닐 때(isCharging=false) 좌클릭 시 근접 공격 시도
        if (!isCharging && Input.GetMouseButtonDown(0))
        {
            if (TryConeDamage())
            {
                return;
            }
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
    }

    //----------------------------------------------------------------------------------
    // 무기 및 조준 관리
    //----------------------------------------------------------------------------------

    void UpdateWeaponVisibility()
    {
        if (handedBow != null) handedBow.SetActive(true);
        if (isAiming) StopAiming();
        if (bowCrosshairUI != null)
        {
            bowCrosshairUI.SetActive(isAiming);
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

        if (bowCrosshairUI != null)
        {
            bowCrosshairUI.SetActive(false);
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
            currentChargeTime = 0f;
            return;
        }

        Vector3 aimDirection = Camera.main.transform.forward;

        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.LookRotation(aimDirection));

        ArrowController ac = arrow.GetComponent<ArrowController>();
        if (ac != null)
        {
            ac.InitializeArrow(currentArrowDamage, SkillManager.Instance.activeArrowSkill, enemyLayer);
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
                // [수정] BaseEnemy로 한 번에 검사
                if (col.TryGetComponent<BaseEnemy>(out BaseEnemy baseEnemyScript))
                {
                    baseEnemyScript.TakeDamage(instaKillDamage);
                    damageCount++;
                }
                // [참고] 만약 SummonerEnemy가 BaseEnemy를 상속받지 않는
                // 특별한 경우라면, else if로 남겨둬야 합니다.
                // (하지만 이전 단계에서 상속시켰으므로 이젠 필요 없습니다.)
            }
        }

        if (damageCount > 0)
        {
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