using UnityEngine;

// 이 스크립트는 Sprite가 항상 카메라를 바라보게 만듭니다.
public class Billboard : MonoBehaviour
{
    private Transform camTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        // 카메라를 바라보도록 회전
        if (camTransform != null)
        {
            transform.LookAt(transform.position + camTransform.rotation * Vector3.forward,
                camTransform.rotation * Vector3.up);
        }
    }
}