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

    // public LineRenderer[] connectionLines;

    void Start()
    {
        // 초기 UI 상태 설정
        if (skillNodeData != null)
        {
            costText.text = skillNodeData.skillPointCost.ToString();
            // skillIcon.sprite = skillNodeData.skillIcon; // SkillNodeData에 아이콘 필드를 추가했다면

            unlockButton.onClick.AddListener(TryUnlockSkill);
        }

        // --- 이벤트 구독 ---
        // 1. SkillManager가 초기화된 후 스킬 해금 시 상태 업데이트
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillUnlock.AddListener(UpdateState);
        }

        // 2. [★추가★] PlayerExperience가 초기화된 후 스킬 포인트 변경 시 상태 업데이트
        if (PlayerExperience.Instance != null)
        {
            // OnSkillPointChange는 int 매개변수를 보내지만, UpdateState는 받지 않으므로
            // 람다식(Lambda expression)을 사용해 UpdateState()를 호출합니다.
            PlayerExperience.Instance.OnSkillPointChange.AddListener((points) => UpdateState());
            // 또는 별도의 함수를 만들어서 연결해도 됩니다.
            // PlayerExperience.Instance.OnSkillPointChange.AddListener(HandleSkillPointChange);
        }
        else
        {
            Debug.LogError($"SkillTreeUI ({skillNodeData?.skillName}): PlayerExperience 인스턴스를 찾을 수 없어 스킬 포인트 변경 구독 실패!");
        }


        // 3. 초기 상태 업데이트 호출 (모든 구독 설정 후)
        UpdateState(); // 시작 시점의 상태를 반영
    }

    // [★추가★] 오브젝트 파괴 시 이벤트 구독 해제 (메모리 누수 방지)
    private void OnDestroy()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillUnlock.RemoveListener(UpdateState);
        }
        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.OnSkillPointChange.RemoveListener((points) => UpdateState());
            // PlayerExperience.Instance.OnSkillPointChange.RemoveListener(HandleSkillPointChange); // 별도 함수 사용 시
        }
    }

    /* // 람다식 대신 별도 함수 사용 예시
    private void HandleSkillPointChange(int newSkillPoints)
    {
        UpdateState(); // 받은 포인트 값을 사용하지 않더라도 함수 호출
    }
    */


    // UI 상태 업데이트: 잠금 여부, 비용, 활성화 여부 등
    public void UpdateState() // 이 함수는 변경할 필요 없음
    {
        if (skillNodeData == null) return; // 데이터 없으면 실행 중지

        // SkillManager가 아직 초기화되지 않았을 수 있으므로 null 체크 추가
        if (SkillManager.Instance == null) return;


        if (SkillManager.Instance.IsSkillUnlocked(skillNodeData)) // 해금됨
        {
            unlockButton.interactable = false;
            lockIcon.gameObject.SetActive(false);
            skillIcon.color = Color.white; // 해금된 색상
        }
        else if (SkillManager.Instance.CanUnlockSkill(skillNodeData)) // 해금 가능
        {
            unlockButton.interactable = true;
            lockIcon.gameObject.SetActive(false);
            skillIcon.color = Color.yellow; // 해금 가능한 색상 ★★★
        }
        else // 잠금됨
        {
            unlockButton.interactable = false;
            lockIcon.gameObject.SetActive(true);
            skillIcon.color = Color.gray; // 잠금된 색상
        }
    }

    void TryUnlockSkill() // 이 함수는 변경할 필요 없음
    {
        if (SkillManager.Instance != null && skillNodeData != null)
        {
            if (SkillManager.Instance.TryUnlockSkill(skillNodeData))
            {
                // 해금 후 상태 업데이트는 OnSkillUnlock 이벤트 구독에 의해 자동으로 호출됨
                if (skillNodeData.type == SkillType.Arrow && skillNodeData.tier < SkillTier.Tier5)
                {
                    SkillManager.Instance.SelectActiveArrowSkill(skillNodeData);
                }
            }
        }
    }
}