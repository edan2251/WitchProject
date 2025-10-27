using UnityEngine;
using UnityEngine.UI; // UI (Text) 사용 시

public class DefenseStageManager : MonoBehaviour
{
    [Header("Stage Settings")]
    public float stageDuration = 60f; // 디펜스 시간 (초)
    private float currentTimer;
    private bool isStageActive = false;

    [Header("References")]
    public DefenseObjective defenseObject; // 인스펙터에서 방어할 오브젝트 할당

    // public Text timerText; // 타이머 UI

    [Header("Stage End")]
    public GameObject winObjects; // 성공 시 활성화할 오브젝트
    public GameObject failObjects; // 실패 시 활성화할 오브젝트

    // [수정] 이 스크립트가 활성화될 때(즉, 스테이지가 시작될 때) 호출됩니다.
    void OnEnable()
    {
        if (defenseObject == null)
        {
            Debug.LogError("방어 오브젝트(DefenseObjective)가 할당되지 않았습니다!");
            this.enabled = false;
            return;
        }

        currentTimer = stageDuration;
        isStageActive = true;

        // 1. 타겟 매니저에게 디펜스 시작을 알림
        EnemyTargetManager.StartDefenseStage(defenseObject.transform);

        // 2. 방어 오브젝트가 파괴되면 HandleStageFail 함수를 호출하도록 연결
        defenseObject.OnObjectDestroyed.AddListener(HandleStageFail);

        // 3. UI 초기화
        winObjects?.SetActive(false);
        failObjects?.SetActive(false);
        // UpdateTimerUI();
    }

    void Update()
    {
        if (!isStageActive) return;

        currentTimer -= Time.deltaTime;
        // UpdateTimerUI();

        if (currentTimer <= 0)
        {
            HandleStageWin();
        }
    }

    void HandleStageWin()
    {
        isStageActive = false;
        Debug.Log("디펜스 성공!");
        EnemyTargetManager.EndDefenseStage(); // 타겟 매니저에게 스테이지 종료 알림

        winObjects?.SetActive(true);
        this.enabled = false; // 이 매니저 비활성화
    }

    void HandleStageFail()
    {
        isStageActive = false;
        Debug.Log("디펜스 실패!");
        EnemyTargetManager.EndDefenseStage(); // 타겟 매니저에게 스테이지 종료 알림

        failObjects?.SetActive(true);

        // 이미 리스너가 호출되었으므로 제거
        defenseObject.OnObjectDestroyed.RemoveListener(HandleStageFail);
        this.enabled = false; // 이 매니저 비활성화
    }

    /*
    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            // 00:00 형식으로 표시
            int minutes = Mathf.FloorToInt(currentTimer / 60);
            int seconds = Mathf.FloorToInt(currentTimer % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    */
}