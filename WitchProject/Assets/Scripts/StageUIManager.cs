using UnityEngine;
using TMPro; // TextMeshPro 사용
using System.Collections;

/// <summary>
/// 스테이지 시작/클리어 등의 알림 텍스트 UI를 관리합니다. (페이드 효과 추가)
/// </summary>
public class StageUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("화면에 표시될 알림 텍스트 (TextMeshProUGUI)")]
    [SerializeField]
    private TextMeshProUGUI announcementText;

    [Header("Timing")]
    [Tooltip("텍스트가 완전히 불투명하게 유지될 시간 (초)")]
    [SerializeField]
    private float displayDuration = 3.0f; // 페이드 시간 제외하고 보여줄 시간

    [Tooltip("페이드 인/아웃 효과 시간 (초)")]
    [SerializeField]
    private float fadeDuration = 0.5f; // 페이드 효과에 걸리는 시간

    // 현재 실행 중인 페이드/숨김 관련 코루틴
    private Coroutine displayCoroutine;

    void Awake()
    {
        // 텍스트 컴포넌트가 없으면 에러
        if (announcementText == null)
        {
            Debug.LogError("StageUIManager: 알림 텍스트 UI(announcementText)가 할당되지 않았습니다!", this);
            this.enabled = false;
            return;
        }

        // 시작 시 텍스트를 완전히 투명하게 만들고 비활성화
        SetTextAlpha(0f); // 알파 0으로 시작
        announcementText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 지정된 텍스트를 페이드 인/아웃 효과와 함께 일정 시간 동안 화면에 표시합니다.
    /// </summary>
    /// <param name="message">표시할 메시지</param>
    /// <param name="duration">완전히 불투명하게 유지될 시간 (초). 음수면 기본값 사용.</param>
    public void ShowAnnouncement(string message, float duration = -1f)
    {
        if (announcementText == null) return;

        // 1. 이전 코루틴이 실행 중이었다면 중지 (새 메시지가 덮어씀)
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        // 2. 텍스트 설정
        announcementText.text = message;

        // 3. 지속 시간 결정
        float holdDuration = (duration < 0) ? displayDuration : duration;

        // 4. 페이드 인 -> 유지 -> 페이드 아웃 코루틴 시작
        displayCoroutine = StartCoroutine(FadeInHoldFadeOut(holdDuration));
    }

    /// <summary>
    /// 텍스트를 페이드 인하고, 잠시 유지한 뒤, 페이드 아웃하는 코루틴입니다.
    /// </summary>
    private IEnumerator FadeInHoldFadeOut(float holdDuration)
    {
        // --- 페이드 인 ---
        announcementText.gameObject.SetActive(true); // 우선 활성화
        yield return StartCoroutine(FadeText(0f, 1f, fadeDuration)); // 알파 0 -> 1

        // --- 유지 ---
        yield return new WaitForSeconds(holdDuration);

        // --- 페이드 아웃 ---
        yield return StartCoroutine(FadeText(1f, 0f, fadeDuration)); // 알파 1 -> 0

        // --- 완료 후 비활성화 ---
        announcementText.gameObject.SetActive(false);
        displayCoroutine = null; // 코루틴 완료 표시
    }

    /// <summary>
    /// 지정된 시간 동안 텍스트의 알파 값을 시작 값에서 목표 값으로 변경하는 코루틴입니다.
    /// </summary>
    private IEnumerator FadeText(float startAlpha, float targetAlpha, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration); // 0과 1 사이의 진행률
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, progress); // 선형 보간
            SetTextAlpha(currentAlpha);
            yield return null; // 다음 프레임까지 대기
        }
        SetTextAlpha(targetAlpha); // 마지막에 목표 알파 값으로 확실히 설정
    }

    /// <summary>
    /// TextMeshPro 텍스트의 알파 값을 설정합니다.
    /// </summary>
    private void SetTextAlpha(float alpha)
    {
        if (announcementText != null)
        {
            Color currentColor = announcementText.color;
            currentColor.a = alpha;
            announcementText.color = currentColor;
        }
    }

    // [삭제] private IEnumerator HideTextAfterDelay(float delay) 함수는 이제 필요 없습니다.
}