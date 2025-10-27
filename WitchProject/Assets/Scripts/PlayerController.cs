using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private float currentSpeed;
    private float walkSpeed = 5f;
    private float runSpeed = 12f;
    private float jumpPower = 5f;
    public float gravity = -9.81f;

    public CinemachineVirtualCamera virtualCam;
    public CinemachineVirtualCamera aimCam;
    public float rotationSpeed = 10f;
    private CinemachinePOV pov;

    public bool isAiming = false;

    private CharacterController controller;
    private Vector3 velocity;
    public bool isGrounded;
    public bool isRunning;

    public int bonusAttack { get; private set; } = 0;

    // [수정] maxHP -> baseMaxHP (기본 체력)
    public int baseMaxHP = 100;
    // [추가] 스킬로 인한 추가 체력
    public int bonusMaxHP { get; private set; } = 0;
    // [수정] 최종 최대 체력 (기본 + 보너스). 읽기 전용 프로퍼티로 변경
    public int maxHP => baseMaxHP + bonusMaxHP;

    public int currentHP;

    public Slider hpSlider;

    // [추가] 플레이어 외형 렌더러 (자식 오브젝트에 있다면 인스펙터에서 할당)
    public Renderer playerRenderer;
    private Color originalColor; // 원래 색상 저장용
    public Color poisonColor = new Color(0.5f, 1f, 0.5f, 1f); // 독에 걸렸을 때의 초록빛
    public float flashDuration = 0.1f; // 피격 시 빨갛게 깜빡이는 시간

    // [추가] 중독 상태 추적용 변수
    private bool isPoisoned = false;
    // [추가] 색상 변경 코루틴 중복 실행 방지용
    private Coroutine poisonEffectCoroutine;

    // --- 마우스 락 상태 변수 추가 ---
    private bool isCursorLocked = true;


    void Start()
    {
        controller = GetComponent<CharacterController>();

        // 주의: virtualCam에 CinemachinePOV가 붙어있지 않으면 이 줄은 에러를 일으킬 수 있습니다.
        // 일반 3인칭 숄더뷰에서는 POV 대신 Transposer/Composer를 사용하는 경우가 많습니다.
        pov = virtualCam.GetCinemachineComponent<CinemachinePOV>();

        currentHP = maxHP; // maxHP 프로퍼티(baseMaxHP + bonusMaxHP)를 사용
        // hpSlider.value = 1f; // [수정] UpdateHPSlider() 호출로 대체
        UpdateHPSlider();

        currentSpeed = walkSpeed; // Initialize currentSpeed

        // [추가] 렌더러 및 원래 색상 초기화
        if (playerRenderer == null)
        {
            // 인스펙터에서 할당 안했으면 자식에서 찾아보기
            playerRenderer = GetComponentInChildren<Renderer>();
        }
        if (playerRenderer != null)
        {
            originalColor = playerRenderer.material.color;
        }

        // --- 마우스 커서 락 초기 설정 ---
        SetCursorLock(true);

        EnemyTargetManager.RegisterPlayer(this.transform);
    }

    void Update()
    {
        // --- 마우스 락/해제 로직 추가 ---
        HandleCursorLock();

        // --- 1. 중력 및 땅 감지 (항상 실행) ---
        // 이 로직은 커서가 잠겨있든 아니든 항상 실행되어야
        // 캐릭터가 공중에 떠다니거나 바닥을 뚫는 것을 방지합니다.
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 중력 적용
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);


        // --- 2. 플레이어 입력 처리 (커서가 잠겨있을 때만 실행) ---
        // 커서가 해제된 상태(예: ESC 누름)에서는 플레이어의 움직임, 점프, 회전 로직을 건너뜁니다.
        if (!isCursorLocked) return;

        // ----------------------------------
        // ( ↓↓↓ 이제부터는 isCursorLocked가 true일 때만 실행되는 코드 ↓↓↓ )
        // ----------------------------------


        // Resets the POV camera's horizontal and vertical values
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // pov가 null이 아닐 경우에만 실행
            if (pov != null)
            {
                pov.m_HorizontalAxis.Value = transform.eulerAngles.y;
                pov.m_VerticalAxis.Value = 0f;
            }
        }

        // --- Movement Speed Logic ---
        if (Input.GetKey(KeyCode.LeftShift)) // 왼쪽 쉬프트를 눌러서 달리기로 변경
        {
            currentSpeed = runSpeed;
            isRunning = true;
        }
        else
        {
            currentSpeed = walkSpeed;
            isRunning = false;
        }
        // ------------------------------------------------

        // Field of View (FOV) adjustment for running
        if (isRunning)
        {
            virtualCam.m_Lens.FieldOfView = Mathf.Lerp(virtualCam.m_Lens.FieldOfView, 65f, Time.deltaTime * 5f);
        }
        else
        {
            virtualCam.m_Lens.FieldOfView = Mathf.Lerp(virtualCam.m_Lens.FieldOfView, 40f, Time.deltaTime * 5f);
        }


        // Get movement input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // 카메라 기준 방향 계산
        Vector3 camForward = virtualCam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = virtualCam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 move = (camForward * z + camRight * x).normalized;
        controller.Move(move * currentSpeed * Time.deltaTime); // <-- 플레이어 입력에 의한 이동

        // --- Rotation Logic ---
        if (pov != null)
        {
            float cameraYaw = pov.m_HorizontalAxis.Value;   //마우스 좌우 회전값
            Quaternion targetRot = Quaternion.Euler(0f, cameraYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
        // -------------------------------------

        // Jump logic
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = jumpPower;
        }
    }

    // --- 마우스 락/해제 처리 함수 ---
    private void HandleCursorLock()
    {
        // 1. ESC 키를 누르면 커서 해제
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLock(false);
        }
        // 2. 커서가 해제된 상태에서 마우스 왼쪽 버튼(GetMouseButtonDown(0) 대신 마우스 버튼 일반 사용)을 클릭하면 다시 잠금
        else if (!isCursorLocked && Input.GetMouseButtonDown(0))
        {
            SetCursorLock(true);
        }
    }

    private void SetCursorLock(bool lockState)
    {
        isCursorLocked = lockState;

        if (lockState)
        {
            // 커서 잠금 및 숨기기 (게임 플레이 모드)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // 커서 해제 및 보이기 (UI 메뉴 또는 일시 정지 모드)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        // hpSlider.value = (float)currentHP / maxHP; // [수정] UpdateHPSlider() 호출로 대체
        UpdateHPSlider(); // [추가] 슬라이더 업데이트 함수 호출

        FlashOnHit();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void ApplyPoisonDamage(int initialDamage, int dotDamage, float dotInterval, int dotTicks, float poisonChance)
    {
        // 1. 첫 타격 데미지를 즉시 적용
        TakeDamage(initialDamage);

        // 2. 70% 확률 (poisonChance) 체크
        if (Random.value <= poisonChance) // Random.value는 0.0 ~ 1.0 사이의 난수 반환
        {
            // 3. 중독 코루틴 시작
            // [수정] 중복 실행을 막기 위해 기존 코루틴이 있다면 중지
            if (poisonEffectCoroutine != null)
            {
                StopCoroutine(poisonEffectCoroutine);
            }
            poisonEffectCoroutine = StartCoroutine(PoisonCoroutine(dotDamage, dotInterval, dotTicks));
        }
    }

    // [추가] 일정 시간마다 독 데미지를 입히는 코루틴
    private IEnumerator PoisonCoroutine(int damagePerTick, float interval, int ticks)
    {
        int ticksRemaining = ticks;
        isPoisoned = true;

        // [추가] 1. 독 상태 시작 (초록색으로 변경)
        if (playerRenderer != null)
        {
            playerRenderer.material.color = poisonColor;
        }

        while (ticksRemaining > 0)
        {
            yield return new WaitForSeconds(interval);

            if (currentHP > 0)
            {
                // [수정] 데미지를 입히면 TakeDamage (내부에서 FlashOnHit 호출)
                TakeDamage(damagePerTick);

                // [추가] 빨간색 플래시가 끝난 후, 다시 독 색상(초록)으로 복귀
                // (FlashOnHit가 0.1초간 빨갛게 만드므로 그보다 조금 더 기다림)
                yield return new WaitForSeconds(flashDuration + 0.05f);
                if (playerRenderer != null && isPoisoned) // 아직 독 상태면
                {
                    playerRenderer.material.color = poisonColor;
                }
            }

            ticksRemaining--;
        }

        // [추가] 2. 독 상태 종료 (원래 색상으로 복구)
        isPoisoned = false;
        if (playerRenderer != null)
        {
            playerRenderer.material.color = originalColor;
        }
        poisonEffectCoroutine = null; // 코루틴 종료됨을 표시
    }


    public void FlashOnHit()
    {
        // 이미 다른 플래시 코루틴이 실행 중이면 중복 방지
        StopCoroutine("FlashColorCoroutine");
        StartCoroutine(FlashColorCoroutine());
    }

    // [추가] 플래시 효과 코루틴
    IEnumerator FlashColorCoroutine()
    {
        if (playerRenderer != null)
        {
            // 1. 빨간색으로 변경
            playerRenderer.material.color = Color.red;

            // 2. 짧은 시간 대기
            yield return new WaitForSeconds(flashDuration);

            // 3. 상태에 따라 색상 복구
            if (isPoisoned)
            {
                // 중독 상태였다면 초록색으로 복구
                playerRenderer.material.color = poisonColor;
            }
            else
            {
                // 아니라면 원래 색상으로 복구
                playerRenderer.material.color = originalColor;
            }
        }
    }

    private void UpdateHPSlider()
    {
        if (hpSlider != null)
        {
            // maxHP가 0이 되는 경우(오류)를 방지
            if (maxHP > 0)
            {
                hpSlider.value = (float)currentHP / maxHP;
            }
            else
            {
                hpSlider.value = 0;
            }
        }
    }

    public void UpdateBonusHealth(int bonus)
    {
        int oldMaxHP = maxHP; // 이전 최대 체력 (프로퍼티)
        bonusMaxHP = bonus;
        int newMaxHP = maxHP; // 새 최대 체력 (프로퍼티)

        int healthIncrease = newMaxHP - oldMaxHP;

        // 체력이 증가했다면, 현재 체력도 그만큼 올려줍니다.
        if (healthIncrease > 0)
        {
            currentHP += healthIncrease;
        }

        // (스킬 초기화 등으로) 체력이 감소할 경우를 대비해
        // 현재 체력이 최대 체력을 넘지 않도록 Clamp
        currentHP = Mathf.Clamp(currentHP, 0, newMaxHP);

        UpdateHPSlider();
    }

    void Die()
    {
        Destroy(gameObject);
    }
}