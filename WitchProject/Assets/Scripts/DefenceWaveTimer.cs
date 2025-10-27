using UnityEngine;
using System.Collections;
using TMPro; // UI Text를 사용한다면 추가

public class DefenseWaveTimer : MonoBehaviour
{
    [Header("웨이브 타이머 설정")]
    public float waveDuration = 60.0f; // 60초 버티기
    public TextMeshProUGUI timerText; // (선택 사항) 남은 시간 표시 UI

    [Header("다음 스테이지 연결")]
    [Tooltip("타이머 완료 시 활성화할 다음 EnemyActivationZone 혹은 PuzzleManager")]
    public GameObject nextStageObject;

    private float timeRemaining;
    private bool timerIsRunning = false;

    void OnEnable()
    {
        // 오브젝트가 활성화될 때 타이머를 시작합니다.
        StartWaveTimer();
    }

    void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                // UI 업데이트 (선택 사항)
                if (timerText != null)
                {
                    timerText.text = Mathf.Ceil(timeRemaining).ToString("0");
                }
            }
            else
            {
                // 타이머 종료!
                timeRemaining = 0;
                timerIsRunning = false;

                Debug.Log("★ 방어 성공! 다음 스테이지 활성화 ★");
                EndWaveSuccess();
            }
        }
    }

    public void StartWaveTimer()
    {
        timeRemaining = waveDuration;
        timerIsRunning = true;
    }

    private void EndWaveSuccess()
    {
        if (nextStageObject != null)
        {
            nextStageObject.SetActive(true);
        }

        // 이 타이머 컴포넌트를 비활성화하여 더 이상 Update가 호출되지 않도록 합니다.
        enabled = false;
    }
}