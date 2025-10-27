using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using DG.Tweening; // DOTween 추가
using System.Linq;

public class PlayerShooting : MonoBehaviour
{
    [System.Serializable]
    public class BowModelEntry
    {
        public string skillName; // SkillNodeData의 "skillName" (예: "일반 활")
        public GameObject bowModelPrefab;
    }

    PlayerController playerController;

    // [1] 무기 및 Cinemachine 설정
    // ---------------------------------------------------------------------
    [Header("Cinemachine/Aim")]
    [SerializeField] private CinemachineVirtualCamera aimCam;
    private bool isAiming = false;
    [SerializeField] private float rotationSpeed = 15f;

    [Tooltip("활 시위를 당길 때 적용될 마우스 감도 배율 (1.0 = 변화 없음, 0.5 = 절반)")]
    [SerializeField] private float aimingSensitivityMultiplier = 0.5f;
    private CinemachinePOV aimPovComponent; // 조준 카메라의 POV 컴포넌트
    private float originalSensitivityX;   // 원래 X축 감도
    private float originalSensitivityY;   // 원래 Y축 감도

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
    public Transform firePoint;

    [Header("능력 프리팹")]
    public GameObject dragonArrowPrefab; // 용 화살 프리팹
    public GameObject windArrowPrefab;   // 바람 화살 프리팹

    public List<BowModelEntry> bowModels; // 인스펙터에서 설정할 활 모델 리스트
    public Transform bowSocket;           // 활이 생성될 위치 (예: 플레이어의 손)
    private GameObject currentBowInstance;  // 현재 활성화된 활 인스턴스

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
        UpdateActiveBowModel();

        if (aimCam != null)
        {
            aimCam.Priority = 0;
            aimCam.m_Lens.FieldOfView = defaultFOV;

            // [★추가★] 조준 카메라에서 CinemachinePOV 컴포넌트 찾기 및 원본 감도 저장
            aimPovComponent = aimCam.GetCinemachineComponent<CinemachinePOV>();
            if (aimPovComponent != null)
            {
                originalSensitivityX = aimPovComponent.m_HorizontalAxis.m_MaxSpeed;
                originalSensitivityY = aimPovComponent.m_VerticalAxis.m_MaxSpeed;
            }
            else
            {
                Debug.LogWarning("PlayerShooting: 조준 카메라(aimCam)에 CinemachinePOV 컴포넌트가 없습니다. 감도 조절이 작동하지 않습니다.", this);
            }
            // --------------------------------------------------------------------
        }

        if (bowCrosshairUI != null)
        {
            bowCrosshairUI.SetActive(false);
        }
    }

    void Update()
    {
        bool uiOpen = SkillUIManager.Instance != null && SkillUIManager.Instance.IsPanelOpen;

        if (!uiOpen) // UI가 닫혀있을 때만 게임 입력 처리
        {
            // 화살 교체 (기존)
            if (Input.GetKeyDown(KeyCode.X))
            {
                SkillManager.Instance?.SelectNextArrowSkill();
                UpdateActiveBowModel(); // 외형 업데이트
            }

            // 조준 및 충전 (기존)
            HandleBowInput();

            // 원뿔 공격 (기존)
            if (!isCharging && Input.GetMouseButtonDown(0)) // 충전 중 아닐 때 좌클릭
            {
                if (TryConeDamage()) return; // 근접 공격 성공 시 이후 로직 건너뛰기
            }

            // --- [★신규★] 능력 입력 ---
            // 1. 용 스킬 (R - 토글)
            if (Input.GetKeyDown(KeyCode.R))
            {
                SkillManager.Instance?.ToggleSkill(SkillManager.DRAGON_SKILL);
            }

            // 2. 연속 화살 스킬 (E - 활성화)
            if (Input.GetKeyDown(KeyCode.E))
            {
                SkillManager.Instance?.TryActivateSkill(SkillManager.MULTISHOT_SKILL);
            }

            // 3. 바람 스킬 (Q - 토글)
            if (Input.GetKeyDown(KeyCode.Q))
            {
                SkillManager.Instance?.ToggleSkill(SkillManager.WIND_SKILL);
            }
            // -----------------------------
        }
        else // UI 열리면 충전 상태 리셋
        {
            if (isCharging)
            {
                isCharging = false;
                currentChargeTime = 0f;
            }
        }

        // --- 충전 로직 (기존 - 필요시 약간 수정 가능) ---
        if (isCharging) // UI 상태와 관계없이 충전은 진행될 수 있음
        {
            currentChargeTime += Time.deltaTime;
            // 여기서 충전 UI 게이지 업데이트 가능
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

    void UpdateActiveBowModel()
    {
        if (bowSocket == null)
        {
            return;
        }

        // 1. SkillManager에서 현재 활성화된 SkillNodeData를 가져옵니다.
        SkillNodeData currentSkillData = SkillManager.Instance.activeArrowSkill;

        // 2. (방어 코드) 스킬 데이터가 null이면 (예: 게임 시작 직후) 아무것도 하지 않거나 기본 활을 표시합니다.
        //    SkillManager의 Start()에서 "일반 활"을 바로 활성화하므로 이 경우는 거의 없어야 합니다.
        if (currentSkillData == null)
        {
            if (currentBowInstance != null) Destroy(currentBowInstance); // 기존 활 숨기기
            return;
        }

        // 3. 현재 활성 스킬의 이름(string)을 가져옵니다.
        string currentSkillName = currentSkillData.skillName;

        // 4. bowModels 리스트에서 일치하는 skillName을 가진 항목을 찾습니다.
        BowModelEntry entry = bowModels.FirstOrDefault(m => m.skillName == currentSkillName);

        // 5. 일치하는 프리팹이 리스트에 없으면 경고를 출력합니다.
        if (entry == null || entry.bowModelPrefab == null)
        {
            if (currentBowInstance != null) Destroy(currentBowInstance); // 기존 활 숨기기
            return;
        }

        // 6. 기존에 생성된 활이 있다면 파괴합니다.
        if (currentBowInstance != null)
        {
            Destroy(currentBowInstance);
        }

        // 7. 새 활 프리팹을 bowSocket의 자식으로 생성합니다.
        currentBowInstance = Instantiate(entry.bowModelPrefab, bowSocket.position, bowSocket.rotation, bowSocket);

        // 8. (권장) 생성된 인스턴스의 로컬 위치/회전을 리셋하여 소켓에 정확히 맞춥니다.
        currentBowInstance.transform.localPosition = Vector3.zero;
        currentBowInstance.transform.localRotation = Quaternion.identity;
    }


    void UpdateWeaponVisibility()
    {
        // if (handedBow != null) handedBow.SetActive(true); // [삭제]

        if (isAiming) StopAiming();
        if (bowCrosshairUI != null)
        {
            bowCrosshairUI.SetActive(isAiming); // isAiming은 Start()에서 false이므로 숨겨집니다.
        }
    }

    void HandleBowInput()
    {
        // 우클릭: 조준 시작 (감도 변경 여기서!)
        if (Input.GetMouseButtonDown(1))
        {
            StartAiming(); // StartAiming 내부에서 감도 낮춤
        }

        // 우클릭 해제: 조준 종료 (감도 변경 여기서!)
        if (Input.GetMouseButtonUp(1))
        {
            StopAiming(); // StopAiming 내부에서 감도 복구
        }

        // --- [★수정★] 감도 변경 로직 삭제 ---
        // 좌클릭: 시위 당기기 시작 (감도 변경 없음)
        if (isAiming && Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            currentChargeTime = 0f;
            // ApplyAimingSensitivity(); // <-- 삭제!
        }

        // 좌클릭 해제: 발사 (감도 변경 없음)
        if (isAiming && Input.GetMouseButtonUp(0) && isCharging)
        {
            // RestoreSensitivity(); // <-- 삭제!
            ShootArrow();
            // isCharging = false; // ShootArrow 내부에서 false로 설정됨
        }

        // 조준 취소 시 충전 리셋 (감도 변경 없음)
        if (!isAiming && isCharging)
        {
            isCharging = false;
            currentChargeTime = 0f;
            // RestoreSensitivity(); // <-- 삭제!
        }
        // ------------------------------------
    }

    void StartAiming()
    {
        if (aimCam == null) return;
        isAiming = true;
        aimCam.Priority = 10;

        if (bowCrosshairUI != null) { bowCrosshairUI.SetActive(true); }

        // FOV 변경 (DOTween)
        DOTween.To(() => aimCam.m_Lens.FieldOfView, x => aimCam.m_Lens.FieldOfView = x, aimFOV, fovDuration).SetEase(Ease.OutQuad);

        // [★추가★] 조준 시작 시 감도 낮추기
        ApplyAimingSensitivity();
    }

    void StopAiming()
    {
        if (aimCam == null) return;
        isAiming = false;
        aimCam.Priority = 0;

        if (bowCrosshairUI != null) { bowCrosshairUI.SetActive(false); }

        // FOV 변경 (DOTween)
        DOTween.To(() => aimCam.m_Lens.FieldOfView, x => aimCam.m_Lens.FieldOfView = x, defaultFOV, fovDuration).SetEase(Ease.OutQuad);

        // [★추가★] 조준 종료 시 감도 복구
        RestoreSensitivity();
    }

    private void ApplyAimingSensitivity()
    {
        if (aimPovComponent != null)
        {
            aimPovComponent.m_HorizontalAxis.m_MaxSpeed = originalSensitivityX * aimingSensitivityMultiplier;
            aimPovComponent.m_VerticalAxis.m_MaxSpeed = originalSensitivityY * aimingSensitivityMultiplier;
        }
    }

    /// <summary>
    /// 마우스 감도를 원래대로 복구합니다.
    /// </summary>
    private void RestoreSensitivity()
    {
        if (aimPovComponent != null)
        {
            aimPovComponent.m_HorizontalAxis.m_MaxSpeed = originalSensitivityX;
            aimPovComponent.m_VerticalAxis.m_MaxSpeed = originalSensitivityY;
        }
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
        // --- 사전 계산 ---
        float finalLaunchForce = Mathf.Clamp(currentChargeTime * chargeRate, 0f, launchForceMax);
        if (finalLaunchForce < 5f) // 최소 충전 확인
        {
            currentChargeTime = 0f;
            isCharging = false; // 충전 중지 확실히
            return;
        }
        Vector3 aimDirection = Camera.main.transform.forward; // 또는 화면 중앙 레이캐스트
        // 현재 선택된 기본 화살 종류 가져오기
        SkillNodeData currentArrowSkill = SkillManager.Instance?.activeArrowSkill;
        // 현재 기본 데미지 가져오기
        int damage = currentArrowDamage;


        // --- [★신규★] 능력 스킬 확인 ---

        // 1. 용 스킬 토글 확인
        if (SkillManager.Instance != null && SkillManager.Instance.IsSkillToggled(SkillManager.DRAGON_SKILL))
        {
            if (dragonArrowPrefab != null && firePoint != null)
            {
                Debug.Log("용 화살 발사!");
                // 용 화살 프리팹 생성
                GameObject dragonArrow = Instantiate(dragonArrowPrefab, firePoint.position, Quaternion.LookRotation(aimDirection));
                Rigidbody dragonRb = dragonArrow.GetComponent<Rigidbody>();
                // 용에게 힘 가하기 (충전된 힘? 고정된 힘?)
                if (dragonRb != null) dragonRb.AddForce(aimDirection * finalLaunchForce, ForceMode.VelocityChange);
                // TODO: DragonArrowController 초기화 필요시 (데미지 등 설정)

                // 스킬 사용 처리: 토글 끄고 쿨다운 시작
                SkillManager.Instance.UseToggledSkill(SkillManager.DRAGON_SKILL);
            }
            else Debug.LogError("용 화살 프리팹이 설정되지 않았습니다!");
        }
        // 2. 바람 스킬 토글 확인
        else if (SkillManager.Instance != null && SkillManager.Instance.IsSkillToggled(SkillManager.WIND_SKILL))
        {
            if (windArrowPrefab != null && firePoint != null)
            {
                Debug.Log("바람 화살 발사!");
                // 바람 화살 프리팹 생성
                GameObject windArrow = Instantiate(windArrowPrefab, firePoint.position, Quaternion.LookRotation(aimDirection));
                // 이 프리팹에는 일반 ArrowController 또는 전용 WindArrowController가 붙어있을 수 있음
                ArrowController ac = windArrow.GetComponent<ArrowController>(); // 또는 WindArrowController
                                                                                // 일반 화살 초기화 사용?
                if (ac != null) ac.InitializeArrow(damage, currentArrowSkill, enemyLayer);

                Rigidbody windRb = windArrow.GetComponent<Rigidbody>();
                // 힘 가하기
                if (windRb != null) windRb.AddForce(aimDirection * finalLaunchForce, ForceMode.VelocityChange);

                // 스킬 사용 처리: 토글 끄고 쿨다운 시작
                SkillManager.Instance.UseToggledSkill(SkillManager.WIND_SKILL);
            }
            else Debug.LogError("바람 화살 프리팹이 설정되지 않았습니다!");
        }
        // 3. 연속 화살 활성 상태 확인
        else if (SkillManager.Instance != null && SkillManager.Instance.IsSkillActive(SkillManager.MULTISHOT_SKILL))
        {
            Debug.Log("연속 화살 발사!");
            // 연속 발사 코루틴 시작
            StartCoroutine(MultiShotCoroutine(aimDirection, finalLaunchForce, damage, currentArrowSkill));
        }
        // 4. 기본 화살 발사
        else
        {
            if (arrowPrefab != null && firePoint != null)
            {
                Debug.Log("일반/특수 화살 발사!");
                // 기본 화살 프리팹 생성
                GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.LookRotation(aimDirection));
                ArrowController ac = arrow.GetComponent<ArrowController>();
                // 화살 초기화 (데미지, 스킬 종류 등 전달)
                if (ac != null) ac.InitializeArrow(damage, currentArrowSkill, enemyLayer);

                Rigidbody rb = arrow.GetComponent<Rigidbody>();
                // 힘 가하기
                if (rb != null) rb.AddForce(aimDirection * finalLaunchForce, ForceMode.VelocityChange);
            }
            else Debug.LogError("화살 프리팹이 설정되지 않았습니다!");
        }
        // -----------------------------

        // 어떤 화살을 쐈든 충전 상태 리셋
        currentChargeTime = 0f;
        isCharging = false;
    }

    // --- [★신규★] 연속 화살 코루틴 ---
    private IEnumerator MultiShotCoroutine(Vector3 direction, float force, int damage, SkillNodeData skillData)
    {
        int shotsFired = 0;
        while (shotsFired < 3) // 3발 발사
        {
            if (arrowPrefab != null && firePoint != null)
            {
                // 선택 사항: 발사마다 약간의 부정확도 추가
                // Quaternion randomRot = Quaternion.Euler(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
                // Vector3 shotDirection = randomRot * direction;
                Vector3 shotDirection = direction; // 우선 정확하게 발사

                // 화살 생성 및 초기화
                GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.LookRotation(shotDirection));
                ArrowController ac = arrow.GetComponent<ArrowController>();
                if (ac != null) ac.InitializeArrow(damage, skillData, enemyLayer);

                // 힘 가하기
                Rigidbody rb = arrow.GetComponent<Rigidbody>();
                if (rb != null) rb.AddForce(shotDirection * force, ForceMode.VelocityChange);

                shotsFired++;
            }
            else
            {
                Debug.LogError("연속 화살 발사를 위한 화살 프리팹이 없습니다!");
                yield break; // 프리팹 없으면 코루틴 중지
            }

            // 마지막 발사 후에는 기다리지 않음
            if (shotsFired < 3)
                yield return new WaitForSeconds(0.15f); // 0.15초 간격
        }
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