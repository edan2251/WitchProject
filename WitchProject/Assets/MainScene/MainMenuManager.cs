using UnityEngine;
// SceneManager를 직접 쓰지 않으므로 using 구문이 필요 없어집니다.

public class MainMenuManager : MonoBehaviour
{
    [Tooltip("불러올 게임 씬의 이름")]
    public string sceneToLoad = "InGame";

    // 씬 전환이 시작되었는지 확인하는 플래그
    private bool sceneLoadingStarted = false;

    void Update()
    {
        // 어떤 입력이라도 감지되고, '아직 씬 전환이 시작되지 않았다면'
        if (Input.GetMouseButtonDown(0) && !sceneLoadingStarted)
        {
            // 씬 전환 시작!
            sceneLoadingStarted = true;

            // SceneFader의 인스턴스에 접근하여 씬 전환 함수 호출
            SceneFader.Instance.FadeToScene(sceneToLoad);
        }
    }
}