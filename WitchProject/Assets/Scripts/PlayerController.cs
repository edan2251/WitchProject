using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private float currentSpeed;
    private float stopSpeed = 0f;
    private float walkSpeed = 5f;
    private float runSpeed = 12f;
    private float jumpPower = 5f;
    public float gravity = -9.81f;

    public CinemachineVirtualCamera virtualCam;
    public float rotationSpeed = 10f;
    private CinemachinePOV pov;

    private CharacterController controller;
    private Vector3 velocity;
    public bool isGrounded;
    public bool isRunning;

    public CinemachineSwitcher cinemachineSwitcher;

    public int maxHP = 100;
    public int currentHP;

    public Slider hpSlider;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        pov = virtualCam.GetCinemachineComponent<CinemachinePOV>();

        currentHP = maxHP;
        hpSlider.value = 1f;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            pov.m_HorizontalAxis.Value = transform.eulerAngles.y;
            pov.m_VerticalAxis.Value = 0f;
        }

        if (cinemachineSwitcher.usingFreeLook == false)
        {
            if (Input.GetKey(KeyCode.LeftShift))                     //왼쪽 쉬프트를 눌러서 달리기로 변경
            {
                currentSpeed = runSpeed;
                isRunning = true;
            }
            else
            {
                currentSpeed = walkSpeed;
                isRunning = false;
            }
        }

        if (isRunning)
        {
            virtualCam.m_Lens.FieldOfView = Mathf.Lerp(virtualCam.m_Lens.FieldOfView, 65f, Time.deltaTime * 5f);
        }
        else
        {
            virtualCam.m_Lens.FieldOfView = Mathf.Lerp(virtualCam.m_Lens.FieldOfView, 40f, Time.deltaTime * 5f);
        }

        isGrounded = controller.isGrounded;
        if(isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // 카메라 기준 방향 계산
        Vector3 camForward = virtualCam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = virtualCam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 move = (camForward * z + camRight * x).normalized;  //이동 방향 = 카메라 forward/right기반
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (cinemachineSwitcher.usingFreeLook == true)
        {
            currentSpeed = stopSpeed;
            jumpPower = 0f;
        }
        else
        {
            currentSpeed = walkSpeed;
            jumpPower = 5f;
        }

            float cameraYaw = pov.m_HorizontalAxis.Value;   //마우스 좌우 회전값
        Quaternion targetRot = Quaternion.Euler(0f, cameraYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

        //여기부터 점프
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = jumpPower;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        hpSlider.value = (float)currentHP / maxHP;

        if(currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
