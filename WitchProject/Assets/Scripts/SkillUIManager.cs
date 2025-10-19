using UnityEngine;

public class SkillUIManager : MonoBehaviour
{
    // 싱글톤 패턴 적용 (다른 스크립트에서 상태를 쉽게 참조할 수 있도록)
    public static SkillUIManager Instance { get; private set; }

    [Header("UI Panel")]
    [Tooltip("스킬 트리 전체를 담고 있는 UI Panel GameObject")]
    public GameObject skillTreePanel;

    // 현재 스킬 패널이 열려 있는지 확인하는 프로퍼티
    public bool IsPanelOpen { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 시작 시 스킬 패널 숨기기
        if (skillTreePanel != null)
        {
            skillTreePanel.SetActive(false);
        }

        // 게임 시작 시 마우스 커서를 잠그고 숨김
        SetCursorState(false);
    }

    void Update()
    {
        // Tab 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleSkillPanel();
        }
    }

    public void ToggleSkillPanel()
    {
        IsPanelOpen = !IsPanelOpen;

        if (skillTreePanel != null)
        {
            skillTreePanel.SetActive(IsPanelOpen);
        }

        SetCursorState(IsPanelOpen);

        // Debug.Log($"스킬 패널 토글됨: {IsPanelOpen}");
    }

    /// <summary>
    /// 마우스 커서의 가시성과 잠금 상태를 설정합니다.
    /// </summary>
    private void SetCursorState(bool isVisible)
    {
        if (isVisible)
        {
            // UI를 조작할 수 있도록 커서를 보이게 하고 잠금을 해제
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 게임 플레이를 위해 커서를 숨기고 중앙에 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
