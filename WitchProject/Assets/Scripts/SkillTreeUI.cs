using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 이 스크립트를 개별 스킬 노드 UI 버튼에 추가합니다.
public class SkillTreeUI : MonoBehaviour
{
    [Header("Data")]
    public SkillNodeData skillNodeData; // 이 버튼이 나타내는 ScriptableObject

    [Header("UI Elements")]
    public Button unlockButton;
    public Image skillIcon;
    public TextMeshProUGUI costText;
    public Image lockIcon; // 잠금 표시 이미지

    // 스킬 트리 UI에 노드 간의 연결 선을 시각화하는 Image/LineRenderer 등을 추가해야 합니다.
    // public LineRenderer[] connectionLines; 

    void Start()
    {
        // 초기 UI 상태 설정
        if (skillNodeData != null)
        {
            costText.text = skillNodeData.skillPointCost.ToString();
            // skillIcon.sprite = skillNodeData.icon; // SkillNodeData에 아이콘 필드를 추가했다면

            unlockButton.onClick.AddListener(TryUnlockSkill);
        }

        // SkillManager가 초기화된 후 상태 업데이트를 위해 LateStart() 호출
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillUnlock.AddListener(UpdateState);
            UpdateState();
        }
    }

    // UI 상태 업데이트: 잠금 여부, 비용, 활성화 여부 등
    public void UpdateState()
    {
        if (SkillManager.Instance.IsSkillUnlocked(skillNodeData)) // 해금됨
        {
            unlockButton.interactable = false;
            lockIcon.gameObject.SetActive(false);
            skillIcon.color = Color.white; // 해금된 색상
            // 스킬 활성화 버튼을 표시 (특수 화살인 경우)
            // ...
        }
        else if (SkillManager.Instance.CanUnlockSkill(skillNodeData)) // 해금 가능
        {
            unlockButton.interactable = true;
            lockIcon.gameObject.SetActive(false);
            skillIcon.color = Color.yellow; // 해금 가능한 색상
        }
        else // 잠금됨
        {
            unlockButton.interactable = false;
            lockIcon.gameObject.SetActive(true);
            skillIcon.color = Color.gray; // 잠금된 색상
        }
    }

    void TryUnlockSkill()
    {
        if (SkillManager.Instance != null)
        {
            if (SkillManager.Instance.TryUnlockSkill(skillNodeData))
            {
                Debug.Log($"{skillNodeData.skillName} 해금됨!");
                // 해금 후 상태 업데이트는 SkillManager 이벤트에 의해 자동으로 호출됩니다.

                // (선택 사항) 특수 활 스킬인 경우 자동으로 활성화
                if (skillNodeData.type == SkillType.Arrow && skillNodeData.tier < SkillTier.Tier5)
                {
                    SkillManager.Instance.SelectActiveArrowSkill(skillNodeData);
                }
            }
        }
    }
}