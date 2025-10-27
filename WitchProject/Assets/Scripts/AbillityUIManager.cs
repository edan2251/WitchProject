using UnityEngine;
using UnityEngine.UI; // Image / Slider 사용
using TMPro; // TextMeshPro 숫자 사용 시
using System.Collections.Generic; // Dictionary 사용

public class AbillityUIManager : MonoBehaviour
{
    // 각 스킬 UI 요소들을 묶는 클래스
    [System.Serializable]
    public class SkillUIElements
    {
        public string skillName; // SkillManager의 이름과 "정확히" 일치해야 함 (DRAGON_SKILL 등)
        public Image iconImage;       // 기본 스킬 아이콘
        public Image cooldownOverlay; // 재사용 대기시간 표시용 원형 채우기 이미지
        public Image durationOverlay; // 지속 시간 표시용 원형 채우기 이미지 (예: 연속 화살)
        public Image toggleOverlay;   // 토글 켜짐 상태 표시용 이미지 (용/바람)
        // 선택 사항: 재사용 대기시간 숫자 표시용 텍스트
        // public TextMeshProUGUI cooldownText;
    }

    [Header("UI 요소 매핑")]
    public List<SkillUIElements> skillUIs; // 인스펙터에서 설정할 UI 요소 목록

    // 빠른 참조를 위한 딕셔너리
    private Dictionary<string, SkillUIElements> uiLookup = new Dictionary<string, SkillUIElements>();

    void Start()
    {
        // 참조 딕셔너리 생성
        foreach (var ui in skillUIs)
        {
            if (!string.IsNullOrEmpty(ui.skillName) && ui.iconImage != null)
            {
                uiLookup[ui.skillName] = ui;
                // UI 초기 상태 설정 (오버레이 숨기기)
                if (ui.cooldownOverlay) ui.cooldownOverlay.fillAmount = 0;
                if (ui.durationOverlay) ui.durationOverlay.fillAmount = 0;
                if (ui.toggleOverlay) ui.toggleOverlay.gameObject.SetActive(false);
                // 선택 사항: 아직 해금 안 된 스킬 아이콘 숨기기
                // SkillManager 참조 필요
                bool isUnlocked = SkillManager.Instance != null && SkillManager.Instance.IsSkillUnlocked(SkillManager.Instance.GetSkillNodeData(ui.skillName));
                ui.iconImage.gameObject.SetActive(isUnlocked); // 해금된 경우에만 표시
            }
        }

        // SkillManager 이벤트 구독
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnCooldownUpdate.AddListener(HandleCooldownUpdate);
            SkillManager.Instance.OnDurationUpdate.AddListener(HandleDurationUpdate);
            SkillManager.Instance.OnToggleUpdate.AddListener(HandleToggleUpdate);
            SkillManager.Instance.OnSkillUnlock.AddListener(HandleSkillUnlock); // 스킬 해금 시 아이콘 표시용
        }
        else
        {
            Debug.LogError("SkillUIManager: SkillManager.Instance를 찾을 수 없습니다!");
        }
    }

    // 오브젝트 파괴 시 구독 해제 (메모리 누수 방지)
    void OnDestroy()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnCooldownUpdate.RemoveListener(HandleCooldownUpdate);
            SkillManager.Instance.OnDurationUpdate.RemoveListener(HandleDurationUpdate);
            SkillManager.Instance.OnToggleUpdate.RemoveListener(HandleToggleUpdate);
            SkillManager.Instance.OnSkillUnlock.RemoveListener(HandleSkillUnlock);
        }
    }

    // 재사용 대기시간 UI 업데이트 처리
    void HandleCooldownUpdate(string skillName, float currentCooldown, float maxCooldown)
    {
        if (uiLookup.TryGetValue(skillName, out SkillUIElements ui)) // 해당 스킬 UI 찾기
        {
            if (ui.cooldownOverlay != null) // 쿨다운 오버레이 이미지가 있다면
            {
                if (maxCooldown > 0 && currentCooldown > 0) // 쿨다운 진행 중이면
                {
                    // fillAmount (채우기 양) 조절 (0~1 값)
                    ui.cooldownOverlay.fillAmount = currentCooldown / maxCooldown;
                    ui.cooldownOverlay.gameObject.SetActive(true); // 오버레이 표시
                }
                else // 쿨다운 완료
                {
                    ui.cooldownOverlay.fillAmount = 0; // 채우기 초기화
                    ui.cooldownOverlay.gameObject.SetActive(false); // 오버레이 숨기기
                }
            }
            // 선택 사항: 숫자 텍스트 업데이트
            // if (ui.cooldownText != null) ui.cooldownText.text = currentCooldown > 0 ? Mathf.CeilToInt(currentCooldown).ToString() : "";
        }
    }

    // 지속 시간 UI 업데이트 처리
    void HandleDurationUpdate(string skillName, float currentDuration, float maxDuration)
    {
        if (uiLookup.TryGetValue(skillName, out SkillUIElements ui))
        {
            if (ui.durationOverlay != null) // 지속 시간 오버레이 이미지가 있다면
            {
                if (maxDuration > 0 && currentDuration > 0) // 지속 시간 진행 중이면
                {
                    // fillAmount 조절
                    ui.durationOverlay.fillAmount = currentDuration / maxDuration;
                    ui.durationOverlay.gameObject.SetActive(true); // 오버레이 표시
                }
                else // 지속 시간 종료
                {
                    ui.durationOverlay.fillAmount = 0; // 채우기 초기화
                    ui.durationOverlay.gameObject.SetActive(false); // 오버레이 숨기기
                }
            }
        }
    }

    // 토글 상태 UI 업데이트 처리
    void HandleToggleUpdate(string skillName, bool isToggled)
    {
        if (uiLookup.TryGetValue(skillName, out SkillUIElements ui))
        {
            if (ui.toggleOverlay != null) // 토글 오버레이 이미지가 있다면
            {
                // 켜짐/꺼짐 상태에 따라 활성화/비활성화
                ui.toggleOverlay.gameObject.SetActive(isToggled);
            }
        }
    }

    // 스킬 해금 시 아이콘 표시 처리
    void HandleSkillUnlock()
    {
        // 스킬 해금 시 모든 UI 아이콘의 표시 여부 재확인
        foreach (var kvp in uiLookup) // 딕셔너리 순회
        {
            // SkillManager에서 해당 스킬이 해금되었는지 확인
            bool unlocked = SkillManager.Instance.IsSkillUnlocked(SkillManager.Instance.GetSkillNodeData(kvp.Key));
            // 아이콘 게임 오브젝트 활성화/비활성화
            kvp.Value.iconImage.gameObject.SetActive(unlocked);

            // 선택 사항: 해금 시 해당 스킬의 초기 UI 상태 강제 업데이트
            if (unlocked)
            {
                float maxCD = SkillManager.Instance.GetSkillCooldown(kvp.Key);
                // 현재 쿨다운 상태 가져오기 (GetValueOrDefault로 안전하게)
                float currentCD = SkillManager.Instance.skillCooldowns.GetValueOrDefault(kvp.Key, 0f);
                HandleCooldownUpdate(kvp.Key, currentCD, maxCD); // 쿨다운 UI 업데이트
                                                                 // 필요시 지속 시간 및 토글 상태도 유사하게 업데이트
            }
        }
    }
}