using UnityEngine;
using UnityEngine.Events; // UnityEvent를 사용하기 위해 추가

/// <summary>
/// 보스 스테이지를 관리합니다.
/// 매 프레임 'targetCoreObject'가 파괴되었는지 "독립적으로" 감시하고,
/// 파괴가 확인되면 스테이지 클리어 이벤트를 실행합니다.
/// </summary>
public class BossStageManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private StageUIManager stageUIManager;
    public string stageStartMessage = "[최종 스테이지]\n힘을 잃은 마녀가 화났습니다.\n지금이 기회입니다!\n마녀를 해치우고 세상을 구하세요!";

    [Header("스테이지 목표")]
    [Tooltip("이 스테이지에서 파괴해야 하는 목표물 게임 오브젝트입니다.")]
    public GameObject targetCoreObject; // 파괴할 오브젝트의 "게임 오브젝트"를 이 슬롯에 연결

    [Header("스테이지 클리어 이벤트")]
    [Tooltip("targetCore가 파괴되었을 때 실행할 이벤트입니다.")]
    public UnityEvent OnStageCleared;

    // [★추가★] 이미 클리어했는지 확인하는 변수 (이벤트 중복 실행 방지)
    private bool isStageCleared = false;

    // 스크립트가 활성화될 때 (스테이지 시작 시)
    void OnEnable()
    {
        if (MinionManager.Instance != null)
        {
            MinionManager.Instance.ClearAllMinions();
        }
        else
        {
            Debug.LogWarning($"{this.name}: MinionManager 인스턴스를 찾을 수 없어 미니언을 제거할 수 없습니다.", this);
        }

        if (stageUIManager != null)
        {
            stageUIManager.ShowAnnouncement(stageStartMessage);
        }
        else
        {
            Debug.LogError($"{this.name}: StageUIManager가 인스펙터에 할당되지 않았습니다!", this);
        }


        // 1. 스테이지가 시작될 때마다 클리어 상태를 리셋
        isStageCleared = false;

        // 2. 게임 오브젝트가 할당되었는지 확인
        if (targetCoreObject == null)
        {
            Debug.LogError("BossStageManager: 'targetCoreObject'가 할당되지 않았습니다!", this);
            this.enabled = false; // 스크립트 비활성화
            return;
        }
    }

    void Update()
    {
        // 1. 이미 클리어했다면, Update 함수를 즉시 종료
        if (isStageCleared)
        {
            return;
        }

        // 2. [핵심 로직]
        // DestructibleCore에서 'Destroy(gameObject)'가 호출되면,
        // 이곳에 저장된 'targetCoreObject' 참조는 'null'이 됩니다.
        if (targetCoreObject == null)
        {
            // 3. 클리어 처리
            ClearStage();
        }
    }

    /// <summary>
    /// 목표물이 파괴된 것을 감지했을 때 호출됩니다.
    /// </summary>
    private void ClearStage()
    {
        // 1. 중복 실행 방지
        isStageCleared = true;

        Debug.Log("보스 스테이지 클리어! 'OnStageCleared' 이벤트를 실행합니다.");

        // 2. BossStageManager에 등록된 'OnStageCleared' 이벤트를 실행
        OnStageCleared?.Invoke();

        // 3. 스테이지 매니저 자신은 할 일을 다 했으므로 비활성화
        this.enabled = false;
    }
}