using UnityEngine;
using TMPro; // TextMeshPro 사용

// SkillManager가 있는 오브젝트나 UI 캔버스에 이 스크립트를 추가합니다.
public class ActiveSkillUI : MonoBehaviour
{
    public TextMeshProUGUI activeSkillText; // UI 텍스트 컴포넌트

    private void Start()
    {
        if (SkillManager.Instance == null)
        {
            Debug.LogError("SkillManager 인스턴스가 없습니다!");
            return;
        }

        // SkillManager의 이벤트에 구독 신청
        SkillManager.Instance.OnActiveArrowChanged.AddListener(UpdateActiveSkillText);

        // 게임 시작 시 현재 활성화된 스킬로 텍스트 초기화
        if (SkillManager.Instance.activeArrowSkill != null)
        {
            UpdateActiveSkillText(SkillManager.Instance.activeArrowSkill);
        }
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴 시 이벤트 구독 해제 (메모리 누수 방지)
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnActiveArrowChanged.RemoveListener(UpdateActiveSkillText);
        }
    }

    /// <summary>
    /// 스킬이 변경될 때마다 호출될 함수
    /// </summary>
    private void UpdateActiveSkillText(SkillNodeData newSkill)
    {
        if (activeSkillText != null && newSkill != null)
        {
            activeSkillText.text = $"Skill: {newSkill.skillName}";
        }
    }
}