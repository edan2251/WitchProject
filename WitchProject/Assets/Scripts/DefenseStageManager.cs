using UnityEngine;
using UnityEngine.UI; // UI (Text) 사용 시
using System.Collections.Generic; // List를 사용하기 위해 추가
using TMPro;
using System.Collections;

public class DefenseStageManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private StageUIManager stageUIManager;
    public string stageStartMessage;

    [Header("Stage Settings")]
    public float stageDuration = 60f; // 디펜스 시간 (초)
    private float currentTimer;
    private bool isStageActive = false;

    [Header("References")]
    public DefenseObjective defenseObject; // 인스펙터에서 방어할 오브젝트 할당

    [Header("Timer UI (World Space)")]
    public TextMeshProUGUI timerText;
    public Color warningColor = Color.red; // 경고 색상 (빨간색)
    private Color originalTimerColor;

    public float blinkInterval = 0.3f; // 0.5초마다 깜빡임
    private bool isBlinking = false; // 현재 깜빡이는 중인지 상태 플래그
    private Coroutine blinkCoroutine;

    [Header("Stage End Actions")]
    [Tooltip("스테이지 성공 시 활성화될 오브젝트들")]
    public List<GameObject> winObjectsToActivate; // [이름 변경] 성공 시 활성화 리스트

    [Tooltip("스테이지 성공 시 파괴될 오브젝트들")]
    public List<GameObject> winObjectsToDestroy;  // [★추가★] 성공 시 파괴 리스트

    [Tooltip("스테이지 실패 시 파괴될 오브젝트들")]
    public List<GameObject> failObjectsToDestroy; // [★수정★] 실패 시 파괴 리스트

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

        currentTimer = stageDuration;

        if (stageUIManager != null)
        {
            stageStartMessage = $"[스테이지 3]\n마녀의 부하들이 핵심 코어를 공격합니다!\n{currentTimer}초 동안 지키세요!";

            stageUIManager.ShowAnnouncement(stageStartMessage);
        }
        else
        {
            Debug.LogError($"{this.name}: StageUIManager가 인스펙터에 할당되지 않았습니다!", this);
        }

        if (defenseObject == null)
        {
            Debug.LogError("방어 오브젝트(DefenseObjective)가 할당되지 않았습니다!");
            this.enabled = false;
            return;
        }

        
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

        if (timerText != null)
        {
            originalTimerColor = timerText.color; // 원래 색상 저장
            UpdateTimerUI(); // 타이머 업데이트 (색상 포함)
            timerText.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (!isStageActive) return;

        currentTimer -= Time.deltaTime;
        UpdateTimerUI();

        if (currentTimer <= 0)
        {
            // 타이머가 0이 되면 음수로 표시되지 않도록 0으로 고정
            currentTimer = 0;
            UpdateTimerUI(); // 00:00으로 마지막 업데이트
            HandleStageWin();
        }
    }

    void HandleStageWin()
    {
        isStageActive = false;
        StopBlinking();
        EnemyTargetManager.EndDefenseStage();
        foreach (GameObject obj in winObjectsToActivate) { obj?.SetActive(true); }
        foreach (GameObject obj in winObjectsToDestroy) { if (obj != null) Destroy(obj); }

        //  타이머 텍스트 비활성화
        if (timerText != null) timerText.gameObject.SetActive(false);

        this.enabled = false;
    }

    void HandleStageFail()
    {
        isStageActive = false;
        StopBlinking();
        Debug.Log("디펜스 실패!");
        EnemyTargetManager.EndDefenseStage();
        foreach (GameObject obj in failObjectsToDestroy) { if (obj != null) Destroy(obj); }

        // 타이머 텍스트 비활성화
        if (timerText != null) timerText.gameObject.SetActive(false);

        defenseObject.OnObjectDestroyed.RemoveListener(HandleStageFail);
        this.enabled = false;
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            float timeToShow = Mathf.Max(0, currentTimer);
            int minutes = Mathf.FloorToInt(timeToShow / 60);
            int seconds = Mathf.FloorToInt(timeToShow % 60);

            timerText.text = $"{minutes:00}:{seconds:00}";

            // 1. 3초 이하일 때 처리
            if (currentTimer <= 5f)
            {
                timerText.color = warningColor; // 색상은 계속 빨간색 유지
                // 아직 깜빡이지 않고 있다면 깜빡임 시작
                if (!isBlinking)
                {
                    StartBlinking();
                }
            }
            // 2. 10초 이하일 때 처리 (3초 초과)
            else if (currentTimer <= 10f)
            {
                StopBlinking(); // 깜빡임 중지 (혹시 실행 중이었다면)
                timerText.color = warningColor; // 빨간색으로 변경
            }
            // 3. 10초 초과일 때 처리
            else
            {
                StopBlinking(); // 깜빡임 중지 (혹시 실행 중이었다면)
                timerText.color = originalTimerColor; // 원래 색상으로 복구
            }
        }
    }

    //  깜빡임 시작 함수
    void StartBlinking()
    {
        isBlinking = true;
        // 기존 코루틴이 있다면 중지 (안전 장치)
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        // 새 코루틴 시작
        blinkCoroutine = StartCoroutine(BlinkTimerCoroutine());
    }

    //  깜빡임 중지 함수
    void StopBlinking()
    {
        if (isBlinking)
        {
            isBlinking = false;
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            // 깜빡임 멈출 때 텍스트가 보이도록 알파 값을 1로 설정
            SetTextAlpha(1f);
        }
    }

    // 텍스트 알파 값을 깜빡이는 코루틴
    private IEnumerator BlinkTimerCoroutine()
    {
        // isBlinking이 true인 동안 무한 반복
        while (isBlinking)
        {
            // --- 페이드 아웃 (1 -> 0) ---
            float timer = 0f;
            float startAlpha = 1f; // 시작 알파 (불투명)
            float targetAlpha = 0f; // 목표 알파 (투명)
            float duration = blinkInterval / 2f; // 페이드 아웃에 걸리는 시간

            while (timer < duration)
            {
                // isBlinking 상태가 바뀌었는지 프레임마다 확인
                if (!isBlinking) yield break; // 바뀌었으면 코루틴 즉시 종료

                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / duration);
                SetTextAlpha(Mathf.Lerp(startAlpha, targetAlpha, progress));
                yield return null; // 다음 프레임까지 대기
            }
            SetTextAlpha(targetAlpha); // 확실하게 0으로 설정

            // isBlinking 상태가 바뀌었는지 다시 확인
            if (!isBlinking) yield break;

            // --- 페이드 인 (0 -> 1) ---
            timer = 0f; // 타이머 리셋
            startAlpha = 0f; // 시작 알파 (투명)
            targetAlpha = 1f; // 목표 알파 (불투명)
            // duration은 동일 (blinkInterval / 2f)

            while (timer < duration)
            {
                // isBlinking 상태가 바뀌었는지 프레임마다 확인
                if (!isBlinking) yield break;

                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / duration);
                SetTextAlpha(Mathf.Lerp(startAlpha, targetAlpha, progress));
                yield return null; // 다음 프레임까지 대기
            }
            SetTextAlpha(targetAlpha); // 확실하게 1로 설정
        }
    }

    // 텍스트 알파 설정 헬퍼 함수
    private void SetTextAlpha(float alpha)
    {
        if (timerText != null)
        {
            Color currentColor = timerText.color;
            currentColor.a = alpha;
            timerText.color = currentColor;
        }
    }
}