using UnityEngine;

// [ExecuteAlways] : 에디터 모드에서도 실시간으로 실행
// [RequireComponent] : CapsuleCollider와 LineRenderer 자동 추가
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer), typeof(CapsuleCollider))]
public class GroundProjectionCircleRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private CapsuleCollider capsuleCollider;

    [Tooltip("원을 얼마나 부드럽게 그릴지 (점의 개수)")]
    [Min(12)] // 최소 12각형
    public int segmentCount = 36; // 36각형은 꽤 부드러운 원

    [Tooltip("레이캐스트가 감지할 바닥(Ground) 레이어")]
    public LayerMask groundLayer; // "Ground" 레이어를 선택해야 함

    [Tooltip("레이캐스트를 시작할 높이 (충분히 높게)")]
    public float raycastHeightOffset = 50f;

    [Tooltip("레이캐스트 최대 거리")]
    public float raycastDistance = 100f;

    [Tooltip("지형(땅)에서 살짝 띄울 높이 (Z-fighting 방지)")]
    public float groundOffset = 0.1f;

    private Vector3[] linePoints;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        SetupLineRenderer();
    }

    void Start()
    {
        UpdateProjection();
    }

    void Update()
    {
        // 1. 게임 실행 중이 아닐 때만 (에디터 모드일 때만) 작동
        if (!Application.isPlaying)
        {
            // 2. 이 오브젝트의 Transform이 마지막 프레임 이후 변경되었다면
            if (transform.hasChanged)
            {
                // 3. 원을 다시 그림
                UpdateProjection();

                // 4. "변경됨" 상태를 리셋
                transform.hasChanged = false;
            }
        }
    }

    // 인스펙터 값이 바뀔 때마다 씬에서 실시간 업데이트
    void OnValidate()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (capsuleCollider == null) capsuleCollider = GetComponent<CapsuleCollider>();
        SetupLineRenderer();
        UpdateProjection();
    }

    void SetupLineRenderer()
    {
        if (lineRenderer == null || segmentCount <= 0) return;

        lineRenderer.useWorldSpace = true;

        // (segmentCount)개의 점 + 루프를 닫기 위한 마지막 점
        lineRenderer.positionCount = segmentCount + 1;
        lineRenderer.loop = false; // SetPositions로 직접 루프를 만듦

        // 배열 크기 초기화
        linePoints = new Vector3[segmentCount + 1];
    }

    // 원을 바닥에 투사(Project)
    void UpdateProjection()
    {
        if (lineRenderer == null || capsuleCollider == null || segmentCount <= 0) return;

        // 점 개수가 바뀌었을 수 있으니 라인 렌더러/배열 크기 재설정
        if (lineRenderer.positionCount != segmentCount + 1)
        {
            SetupLineRenderer();
        }

        float radius = capsuleCollider.radius;
        Vector3 center = capsuleCollider.center;

        // 1. 원의 둘레를 따라 'segmentCount'개의 점을 계산
        for (int i = 0; i <= segmentCount; i++) // '<='를 사용하여 마지막 점까지 포함
        {
            // 0 ~ 2PI (360도) 사이의 각도 계산
            float angle = (i / (float)segmentCount) * 2f * Mathf.PI;

            // 캡슐의 반지름을 기반으로 로컬 X, Z 위치 계산
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;

            // 캡슐 콜라이더의 중심(center) 오프셋 적용
            Vector3 localCirclePoint = new Vector3(x, 0, z) + center;

            // 2. 로컬 좌표를 월드 좌표로 변환
            Vector3 worldCirclePoint = transform.TransformPoint(localCirclePoint);

            // 3. Raycast 시작 위치 (월드 XZ값, Y는 아주 높게)
            Vector3 rayStart = new Vector3(worldCirclePoint.x, transform.position.y + raycastHeightOffset, worldCirclePoint.z);

            RaycastHit hit;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, groundLayer))
            {
                // 4. 땅에 맞았다면, 그 지점을 저장
                linePoints[i] = hit.point + hit.normal * groundOffset;
            }
            else
            {
                // 5. 땅에 맞지 않았다면 (절벽 등), 그냥 캡슐의 바닥 좌표 사용
                linePoints[i] = worldCirclePoint;
            }
        }

        // 6. LineRenderer에 최종 점들 설정
        lineRenderer.SetPositions(linePoints);
    }
}