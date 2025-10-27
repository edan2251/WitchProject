using UnityEngine;

/// <summary>
/// 플레이어가 일정 거리 안에 있을 때 E키로 활성화시킬 수 있는 오브젝트입니다.
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("상호작용 설정")]
    public float interactionDistance = 3f; // 플레이어가 상호작용 가능한 거리
    public KeyCode interactKey = KeyCode.E; // 상호작용 키

    [Header("상태")]
    public bool isActive = false; // 현재 활성화(불이 켜진) 상태

    // 외형을 바꾸기 위한 컴포넌트 (선택 사항)
    [SerializeField] private MeshRenderer objectRenderer;
    [SerializeField] private Color activeColor = Color.yellow;
    private Color originalColor;

    // ▼▼▼ [추가] 이펙트 설정 변수 ▼▼▼
    [Header("이펙트 설정")]
    [Tooltip("활성화 시 출력할 모닥불/불꽃 이펙트 프리팹")]
    public GameObject activeEffectPrefab;
    private GameObject currentActiveEffect; // 현재 씬에 생성된 이펙트 인스턴스
    // ▲▲▲ [추가] 이펙트 설정 변수 ▲▲▲

    private void Start()
    {
        // 렌더러가 있다면 원래 색상을 저장하고 초기 상태를 설정합니다.
        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
            UpdateAppearance();
        }
    }

    private void Update()
    {
        // 이미 활성화된 상태라면 더 이상 상호작용하지 않습니다.
        if (isActive) return;

        // 1. 플레이어 찾기 (태그가 "Player"인 오브젝트를 가정)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // 2. 플레이어와의 거리 확인
        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= interactionDistance)
        {
            // 3. E 키 입력 확인
            if (Input.GetKeyDown(interactKey))
            {
                Activate();
            }
        }
    }

    /// <summary>
    /// 오브젝트를 활성화(불을 켜는) 처리하고 이펙트를 생성합니다.
    /// </summary>
    public void Activate()
    {
        if (isActive) return;

        isActive = true;
        UpdateAppearance();

        if (activeEffectPrefab != null)
        {
            // 오브젝트의 위치에 이펙트를 생성하고, 이 오브젝트를 부모(자식)로 설정합니다.
            // Quaternion.identity는 이펙트 프리팹의 기본 회전을 사용합니다.
            currentActiveEffect = Instantiate(activeEffectPrefab, transform.position, Quaternion.identity, transform);

             currentActiveEffect.transform.localPosition = Vector3.up * 0.55f;
        }

        // PuzzleManager에게 알립니다.
        PuzzleManager.Instance?.CheckPuzzleCompletion();

    }

    /// <summary>
    /// 오브젝트의 외형(색상)을 상태에 따라 업데이트합니다.
    /// </summary>
    private void UpdateAppearance()
    {
        if (objectRenderer == null) return;

        if (isActive)
        {
            objectRenderer.material.color = activeColor;
        }
        else
        {
            objectRenderer.material.color = originalColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터에서 상호작용 가능 범위를 시각적으로 표시합니다.
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}