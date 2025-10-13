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

    // public CinemachineSwitcher cinemachineSwitcher; // REMOVED

    public int maxHP = 100;
    public int currentHP;

    public Slider hpSlider;

    // --- 마우스 락 상태 변수 추가 ---
    private bool isCursorLocked = true;


    void Start()
    {
        controller = GetComponent<CharacterController>();

        // 주의: virtualCam에 CinemachinePOV가 붙어있지 않으면 이 줄은 에러를 일으킬 수 있습니다.
        // 일반 3인칭 숄더뷰에서는 POV 대신 Transposer/Composer를 사용하는 경우가 많습니다.
        pov = virtualCam.GetCinemachineComponent<CinemachinePOV>();

        currentHP = maxHP;
        hpSlider.value = 1f;
        currentSpeed = walkSpeed; // Initialize currentSpeed

        // --- 마우스 커서 락 초기 설정 ---
        SetCursorLock(true);
    }

    void Update()
    {
        // --- 마우스 락/해제 로직 추가 ---
        HandleCursorLock();

        // 커서가 해제된 상태에서는 플레이어 움직임 및 회전 로직을 건너뜁니다.
        if (!isCursorLocked) return;
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

        // Ground check and velocity reset
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
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
        controller.Move(move * currentSpeed * Time.deltaTime);

        // --- Rotation Logic (Always active) ---
        if (pov != null)
        {
            float cameraYaw = pov.m_HorizontalAxis.Value;    //마우스 좌우 회전값
            Quaternion targetRot = Quaternion.Euler(0f, cameraYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
        // -------------------------------------

        // Jump logic
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = jumpPower;
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
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
        hpSlider.value = (float)currentHP / maxHP;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}