using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoMain : MonoBehaviour
{
    public void GoToMainMenuWithFade()
    {
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene("MainMenu");
        }
        else
        {
            Debug.LogWarning("SceneFader 인스턴스를 찾을 수 없습니다! 직접 씬을 로드합니다.");
            SceneManager.LoadScene("MainMenu");
        }
    }
}
