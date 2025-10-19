using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("XP UI")]
    public Slider xpSlider;
    public TextMeshProUGUI levelText;

    [Header("Skill Point UI")]
    public TextMeshProUGUI skillPointText;

    void Start()
    {
        // 씬에 PlayerExperience 인스턴스가 있는지 확인
        if (PlayerExperience.Instance != null)
        {
            // 이벤트 구독 (UI 업데이트 함수 연결)
            PlayerExperience.Instance.OnLevelUp.AddListener(UpdateLevelUI);
            PlayerExperience.Instance.OnExpChange.AddListener(UpdateXPBar);
            PlayerExperience.Instance.OnSkillPointChange.AddListener(UpdateSkillPoints);

            // 초기값 설정
            UpdateLevelUI();
            UpdateSkillPoints(PlayerExperience.Instance.skillPoints);
            UpdateXPBar(PlayerExperience.Instance.currentExperience);
        }
    }

    void UpdateLevelUI()
    {
        levelText.text = "Lv. " + PlayerExperience.Instance.currentLevel;
    }

    public void UpdateXPBar(int currentExp)
    {
        // PlayerExperience.GetExperienceToNextLevel() 함수가 public이어야 합니다.
        int expToNext = PlayerExperience.Instance.GetExperienceToNextLevel();
        int expAtCurrentLevel = currentExp;

        // 현재 레벨업에 필요한 경험치량을 전체 XP 슬라이더의 MaxValue로 설정
        xpSlider.maxValue = expToNext;
        xpSlider.value = expAtCurrentLevel;

        // (옵션) XP 수치 텍스트도 업데이트
        // ...
    }

    public void UpdateSkillPoints(int newPoints)
    {
        skillPointText.text = $"Skill Points: {newPoints}";
    }
}