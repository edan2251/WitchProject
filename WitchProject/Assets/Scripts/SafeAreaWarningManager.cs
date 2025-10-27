using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 추가
using System.Collections; // 코루틴 사용을 위해 추가

/// <summary>
/// 안전 지대 이탈 경고 UI 텍스트를 관리합니다. (펄싱 효과 추가)
/// </summary>
public class SafeAreaWarningManager : MonoBehaviour
{
    [Tooltip("화면에 표시할 경고 텍스트 (TextMeshProUGUI)")]
    [SerializeField]
    private TextMeshProUGUI warningTextElement;

    [Header("Pulsing Effect")] // [★추가★] 펄싱 효과 관련 설정
    [Tooltip("펄싱 효과 속도")]
    [SerializeField]
    private float pulseSpeed = 1.0f;

    [Tooltip("펄싱 시 최소 알파 값 (투명도)")]
    [Range(0f, 1f)] // 0과 1 사이 값으로 제한
    [SerializeField]
    private float pulseMinIntensity = 0.5f;

    [Tooltip("펄싱 시 최대 알파 값 (투명도)")]
    [Range(0f, 1f)]
    [SerializeField]
    private float pulseMaxIntensity = 1.0f;

    // 현재 실행 중인 펄싱 코루틴 저장용
    private Coroutine pulseCoroutine;

    void Awake()
    {
        // 경고 텍스트가 할당되지 않았으면 에러 메시지 출력
        if (warningTextElement == null)
        {
            Debug.LogError("SafeAreaWarningManager: 경고 텍스트 UI 요소(warningTextElement)가 할당되지 않았습니다!", this);
            this.enabled = false; // 스크립트 비활성화
            return;
        }

        // 시작 시 경고 텍스트를 숨깁니다.
        SetTextAlpha(0f); // 알파 0으로 설정
        warningTextElement.gameObject.SetActive(false);
    }

    /// <summary>
    /// 경고 텍스트를 화면에 표시하고 펄싱 효과를 시작합니다.
    /// </summary>
    public void ShowWarning()
    {
        if (warningTextElement != null)
        {
            // 1. 텍스트 오브젝트 활성화
            warningTextElement.gameObject.SetActive(true);

            // 2. 이미 펄싱 코루틴이 실행 중이면 중지 (중복 방지)
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }

            // 3. 펄싱 효과 코루틴 시작
            pulseCoroutine = StartCoroutine(PulseWarningCoroutine());
        }
    }

    /// <summary>
    /// 경고 텍스트를 화면에서 숨기고 펄싱 효과를 중지합니다.
    /// </summary>
    public void HideWarning()
    {
        if (warningTextElement != null)
        {
            // 1. 실행 중인 펄싱 코루틴 중지
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }

            // 2. 텍스트 오브젝트 비활성화 (즉시 숨김)
            warningTextElement.gameObject.SetActive(false);

            // 3. (선택사항) 알파 값을 원래대로 돌려놓거나 0으로 설정할 수 있습니다.
            // 여기서는 비활성화하므로 굳이 필요 없습니다.
            // SetTextAlpha(0f); 
        }
    }

    /// <summary>
    /// 텍스트 알파 값을 부드럽게 깜빡이는 코루틴입니다.
    /// </summary>
    private IEnumerator PulseWarningCoroutine()
    {
        // 시작 시 최대 알파 값으로 설정
        SetTextAlpha(pulseMaxIntensity);

        // 무한 반복 (HideWarning에서 중지될 때까지)
        while (true)
        {
            // Mathf.PingPong 함수를 사용하여 min ~ max 사이를 부드럽게 왕복하는 값을 만듭니다.
            // Time.time * pulseSpeed 를 넣어 시간에 따라 값이 변하게 합니다.
            float targetAlpha = Mathf.PingPong(Time.time * pulseSpeed, pulseMaxIntensity - pulseMinIntensity) + pulseMinIntensity;

            // 현재 알파 값에서 목표 알파 값으로 부드럽게 변경 (Lerp 사용)
            float currentAlpha = warningTextElement.color.a;
            float newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * pulseSpeed * 5f); // Lerp 속도 조절

            SetTextAlpha(newAlpha);

            yield return null; // 다음 프레임까지 대기
        }
    }

    /// <summary>
    /// TextMeshPro 텍스트의 알파 값을 설정합니다. (헬퍼 함수)
    /// </summary>
    private void SetTextAlpha(float alpha)
    {
        if (warningTextElement != null)
        {
            Color currentColor = warningTextElement.color;
            currentColor.a = alpha;
            warningTextElement.color = currentColor;
        }
    }
}