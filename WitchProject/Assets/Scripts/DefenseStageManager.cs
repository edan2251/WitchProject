using UnityEngine;
using UnityEngine.UI; // UI (Text) 사용 시
using System.Collections.Generic; // List를 사용하기 위해 추가

public class DefenseStageManager : MonoBehaviour
{
    [Header("Stage Settings")]
    public float stageDuration = 60f; // 디펜스 시간 (초)
    private float currentTimer;
    private bool isStageActive = false;

    [Header("References")]
    public DefenseObjective defenseObject; // 인스펙터에서 방어할 오브젝트 할당

    // public Text timerText; // 타이머 UI

    [Header("Stage End Actions")]
    [Tooltip("스테이지 성공 시 활성화될 오브젝트들")]
    public List<GameObject> winObjectsToActivate; // [이름 변경] 성공 시 활성화 리스트

    [Tooltip("스테이지 성공 시 파괴될 오브젝트들")]
    public List<GameObject> winObjectsToDestroy;  // [★추가★] 성공 시 파괴 리스트

    [Tooltip("스테이지 실패 시 파괴될 오브젝트들")]
    public List<GameObject> failObjectsToDestroy; // [★수정★] 실패 시 파괴 리스트

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

        // 3. [★수정★] 성공 시 활성화될 오브젝트들만 미리 비활성화
        foreach (GameObject obj in winObjectsToActivate)
        {
            obj?.SetActive(false);
        }

        // (실패 시 활성화 리스트가 없으므로 해당 foreach문 삭제)

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

        // [★수정★] 1. 성공 오브젝트 리스트 순회하며 "활성화"
        foreach (GameObject obj in winObjectsToActivate)
        {
            obj?.SetActive(true);
        }

        // [★추가★] 2. 성공 시 "파괴"할 오브젝트 리스트 순회
        foreach (GameObject obj in winObjectsToDestroy)
        {
            if (obj != null) // null 체크 후 파괴
            {
                Destroy(obj);
            }
        }

        this.enabled = false; // 이 매니저 비활성화
    }

    void HandleStageFail()
    {
        isStageActive = false;
        Debug.Log("디펜스 실패!");
        EnemyTargetManager.EndDefenseStage(); // 타겟 매니저에게 스테이지 종료 알림

        // [★수정★] 실패 시 "파괴"할 오브젝트 리스트 순회
        foreach (GameObject obj in failObjectsToDestroy)
        {
            if (obj != null) // null 체크 후 파괴
            {
                Destroy(obj);
            }
        }

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