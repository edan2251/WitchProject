using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 이 스크립트는 씬이 바뀌어도 파괴되지 않으며,
// 모든 씬 전환을 관리하는 '싱글톤' 역할을 합니다.
public class SceneFader : MonoBehaviour
{
    // 1. 싱글톤 인스턴스
    public static SceneFader Instance { get; private set; }

    // 2. 인스펙터에서 연결할 변수
    [Tooltip("페이드 효과에 사용할 검은색 이미지의 Canvas Group")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("페이드 인/아웃에 걸리는 시간 (초)")]
    public float fadeDuration = 1.0f;

    // 3. 내부 변수
    private bool isFading = false; // 현재 페이드 진행 중인지 확인

    private void Awake()
    {
        // 4. 싱글톤 패턴 설정
        if (Instance == null)
        {
            Instance = this;
            // 씬이 전환되어도 이 오브젝트는 파괴하지 않음
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 이미 인스턴스가 존재하면 새로 생긴 것은 파괴
            Destroy(gameObject);
            return;
        }

        // 5. 씬이 로드될 때마다 FadeIn 코루틴을 실행하도록 이벤트에 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // 6. 첫 씬(MainMenu)이 시작될 때 밝아지는 효과 실행
        StartCoroutine(Fade(0f)); // 0f = 알파값 0 (투명)
    }

    // 씬이 로드되었을 때 호출될 함수
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 7. 새 씬이 로드되면 다시 밝아지는 효과 실행
        StartCoroutine(Fade(0f));
    }

    /// <summary>
    /// 지정한 씬으로 페이드 아웃했다가 로드하는 메인 함수
    /// </summary>
    /// <param name="sceneName">불러올 씬의 이름</param>
    public void FadeToScene(string sceneName)
    {
        // 이미 씬 전환 중이면 중복 실행 방지
        if (isFading)
        {
            return;
        }

        // 8. 페이드 아웃 -> 씬 로드 코루틴 실행
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    /// <summary>
    /// 실제로 알파값을 조절하는 코루틴
    /// </summary>
    /// <param name="targetAlpha">목표 알파값 (1 = 불투명, 0 = 투명)</param>
    private IEnumerator Fade(float targetAlpha)
    {
        isFading = true;

        // 클릭을 막거나 허용
        fadeCanvasGroup.blocksRaycasts = (targetAlpha == 1f);

        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float progress = time / fadeDuration;

            // Lerp(시작값, 끝값, 진행도)
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            yield return null; // 1프레임 대기
        }

        // 루프가 끝나면 목표 알파값으로 정확히 설정
        fadeCanvasGroup.alpha = targetAlpha;
        isFading = false;
    }

    /// <summary>
    /// 페이드 아웃(어두워지기)을 먼저 실행하고 씬을 로드하는 코루틴
    /// </summary>
    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        // 9. Fade(1f) 코루틴(어두워지기)을 먼저 실행하고, 끝날 때까지 대기
        yield return StartCoroutine(Fade(1f));

        // 10. 어두워진 것이 완료되면 씬 로드
        SceneManager.LoadScene(sceneName);
    }

    // 오브젝트 파괴 시 이벤트 구독 해제 (메모리 누수 방지)
    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}