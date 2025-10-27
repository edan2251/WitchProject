using UnityEngine;

/// <summary>
/// 플레이어가 이 트리거 존을 벗어나거나 들어올 때 경고 UI를 제어합니다.
/// </summary>
public class SafeAreaTrigger : MonoBehaviour
{
    [Tooltip("경고 UI를 관리하는 SafeAreaWarningManager 스크립트")]
    [SerializeField]
    private SafeAreaWarningManager warningManager;

    void Start()
    {
        // 경고 매니저가 할당되지 않았으면 에러 메시지 출력
        if (warningManager == null)
        {
            Debug.LogError("SafeAreaTrigger: 경고 매니저(warningManager)가 할당되지 않았습니다!", this);
            this.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 "다시" 안전 지대로 들어왔다면
        if (other.CompareTag("PlayerTriggerZone") && warningManager != null)
        {
            // 경고 텍스트 숨기기
            warningManager.HideWarning();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어가 안전 지대를 "벗어났다면"
        if (other.CompareTag("PlayerTriggerZone") && warningManager != null)
        {
            // 경고 텍스트 표시하기
            warningManager.ShowWarning();
        }
    }
}