using UnityEngine;
using Cinemachine;

public class CinemachineSwitcher : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCam;
    public CinemachineFreeLook freeLookCam;
    public bool usingFreeLook = false;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        virtualCam.Priority = 10;
        freeLookCam.Priority = 0;

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { ToggleCursor(); }

        if (Cursor.lockState != CursorLockMode.Locked) return;

        if (Input.GetMouseButtonDown(1))
        {
            usingFreeLook = !usingFreeLook;
            if(usingFreeLook)
            {
                freeLookCam.Priority = 20;
                virtualCam.Priority = 0;
            }
            else
            {
                virtualCam.Priority = 20;
                freeLookCam.Priority = 0;
            }
        }
    }

    void ToggleCursor()                                 //커서 표시 변경 함수
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
