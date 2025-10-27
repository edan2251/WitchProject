using UnityEngine;

/// <summary>
/// 이 스크립트가 붙은 오브젝트를 제자리에서 위아래로 둥실거리게 만듭니다.
/// </summary>
public class CoreHover : MonoBehaviour
{
    [Header("Hover Settings")]
    [Tooltip("둥실거리는 속도")]
    public float hoverSpeed = 1.5f;

    [Tooltip("둥실거리는 높낮이(진폭)")]
    public float hoverAmplitude = 0.25f;

    // 오브젝트의 원래 로컬 위치(부모 기준)
    private Vector3 originalLocalPos;

    void Start()
    {
        // 시작할 때 자신의 원래 위치를 기억합니다.
        originalLocalPos = transform.localPosition;
    }

    void Update()
    {
        // Mathf.Sin() 함수를 이용해 -1 ~ +1 사이를 부드럽게 왕복하는 값을 만듭니다.
        // Time.time * hoverSpeed를 넣어 시간에 따라 값이 변하게 합니다.
        float hoverY = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;

        // 원래 위치의 Y값에만 둥실거리는 값을 더해줍니다.
        transform.localPosition = new Vector3(
            originalLocalPos.x,
            originalLocalPos.y + hoverY, // Y축에만 적용
            originalLocalPos.z
        );
    }
}