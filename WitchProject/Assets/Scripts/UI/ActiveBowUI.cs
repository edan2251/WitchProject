using UnityEngine;
using UnityEngine.UI; // UI 요소를 사용하기 위해 추가

public class ActiveBowUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("현재 활성화된 활의 아이콘을 표시할 UI Image 컴포넌트")]
    public Image activeBowIconImage;

    void Start()
    {
        // SkillManager가 존재하는지 확인
        if (SkillManager.Instance == null)
        {
            if (activeBowIconImage != null) activeBowIconImage.enabled = false; // 아이콘 숨기기
            return;
        }

        // 1. SkillManager의 OnActiveArrowChanged 이벤트에 UpdateIcon 함수를 구독(연결)
        SkillManager.Instance.OnActiveArrowChanged.AddListener(UpdateIcon);

        // 2. 게임 시작 시 SkillManager에 이미 설정된 활성 스킬로 아이콘 초기화
        //    (SkillManager의 Start()에서 "일반 활"을 활성화하므로, "일반 활" 아이콘이 표시됨)
        if (SkillManager.Instance.activeArrowSkill != null)
        {
            UpdateIcon(SkillManager.Instance.activeArrowSkill);
        }
        else
        {
            // 활성 스킬이 없으면 아이콘 숨기기
            if (activeBowIconImage != null) activeBowIconImage.enabled = false;
        }
    }

    void OnDestroy()
    {
        // 씬이 종료되거나 이 오브젝트가 파괴될 때, 메모리 누수를 방지하기 위해 이벤트를 구독 해제
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnActiveArrowChanged.RemoveListener(UpdateIcon);
        }
    }

    /// <summary>
    /// SkillManager의 이벤트에 의해 호출될 함수
    /// </summary>
    /// <param name="newSkillData">새롭게 활성화된 스킬의 SkillNodeData</param>
    private void UpdateIcon(SkillNodeData newSkillData)
    {
        if (activeBowIconImage == null) return;
        if (newSkillData == null)
        {
            activeBowIconImage.enabled = false;
            return;
        }

        // 1단계에서 SkillNodeData에 추가한 'skillIcon' 변수를 사용
        Sprite iconToShow = newSkillData.skillIcon;

        if (iconToShow != null)
        {
            // 아이콘 이미지가 있으면 UI Image에 설정하고 활성화
            activeBowIconImage.sprite = iconToShow;
            activeBowIconImage.enabled = true;
        }
        else
        {
            activeBowIconImage.enabled = false;
        }
    }
}