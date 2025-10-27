using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq; // LINQ 사용

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [Header("스킬 데이터")]
    public SkillNodeData[] allSkillNodes; // 모든 스킬 노드 데이터 목록 (에디터에서 할당)
    private HashSet<SkillNodeData> unlockedSkills = new HashSet<SkillNodeData>(); // 해금된 스킬 목록
    private Dictionary<SkillTier, int> unlockedNodesPerTier = new Dictionary<SkillTier, int>(); // 티어별 해금 개수

    public int totalBonusAttack { get; private set; } = 0; // 총 추가 공격력
    public int totalBonusHealth { get; private set; } = 0; // 총 추가 체력

    [Header("이벤트")]
    public UnityEvent OnSkillUnlock = new UnityEvent(); // 스킬 해금 시
    [System.Serializable] public class ActiveSkillChangedEvent : UnityEvent<SkillNodeData> { }
    public ActiveSkillChangedEvent OnActiveArrowChanged = new ActiveSkillChangedEvent(); // 사용 중인 화살 변경 시

    // --- 능력 스킬 추적 ---
    [Header("능력 상태 (내부용)")]
    // 각 스킬의 남은 재사용 대기시간 추적 (이름 기준)
    public Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();
    // 연속 화살 등 시간제 스킬의 남은 지속 시간 추적 (이름 기준)
    private Dictionary<string, float> skillDurations = new Dictionary<string, float>();
    // 용/바람 등 토글 스킬의 켜짐/꺼짐 상태 추적 (이름 기준)
    private Dictionary<string, bool> skillToggles = new Dictionary<string, bool>();

    // --- 능력 스킬 UI 이벤트 (UI용) ---
    [System.Serializable] public class SkillFloatEvent : UnityEvent<string, float, float> { } // 스킬 이름, 현재 값, 최대 값
    [System.Serializable] public class SkillBoolEvent : UnityEvent<string, bool> { } // 스킬 이름, 켜짐 여부

    [Header("능력 UI 이벤트")]
    public SkillFloatEvent OnCooldownUpdate = new SkillFloatEvent(); // 쿨다운 갱신 시
    public SkillFloatEvent OnDurationUpdate = new SkillFloatEvent(); // 지속 시간 갱신 시
    public SkillBoolEvent OnToggleUpdate = new SkillBoolEvent(); // 토글 상태 변경 시
    // ----------------------------

    public SkillNodeData activeArrowSkill { get; private set; } = null; // 현재 사용 중인 화살 스킬
    private List<SkillNodeData> unlockedArrowSkills = new List<SkillNodeData>(); // 해금된 화살 스킬 목록 (정렬됨)

    // --- 스킬 이름 정의 (SkillNodeData의 이름과 정확히 일치해야 함!) ---
    public const string DRAGON_SKILL = "용 스킬";
    public const string MULTISHOT_SKILL = "연속화살 스킬";
    public const string WIND_SKILL = "바람 스킬";


    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeUnlockedNodesPerTier(); // 티어별 해금 개수 초기화
        // "일반 활" 찾아서 시작 시 강제 해금
        SkillNodeData defaultArrow = allSkillNodes.FirstOrDefault(n => n.skillName == "일반 활");
        if (defaultArrow != null) StartUnlockSkillNode(defaultArrow);

        // 해금된 능력 스킬 상태 초기화
        InitializeAbilityStates();

        OnSkillUnlock.Invoke(); // 초기 UI 업데이트 트리거
    }

    private void Update()
    {
        // 매 프레임 재사용 대기시간 및 지속 시간 갱신
        UpdateSkillTimers(Time.deltaTime);
    }

    private void InitializeUnlockedNodesPerTier()
    {
        // Dictionary 초기화
        foreach (SkillTier tier in System.Enum.GetValues(typeof(SkillTier)))
        {
            unlockedNodesPerTier[tier] = 0;
        }
    }

    /// <summary>
    /// 시스템 시작 시 비용 없이 스킬을 강제로 해금하고 스탯을 적용합니다. (일반 활 전용)
    /// </summary>
    private void StartUnlockSkillNode(SkillNodeData skillNode)
    {
        if (unlockedSkills.Contains(skillNode)) return;

        unlockedSkills.Add(skillNode);
        ApplySkillEffects(skillNode);

        // 해금된 노드 개수 업데이트
        unlockedNodesPerTier[skillNode.tier] = unlockedNodesPerTier.ContainsKey(skillNode.tier) ?
                                                unlockedNodesPerTier[skillNode.tier] + 1 : 1;

    }


    /// <summary>
    /// 특정 스킬 노드를 잠금 해제합니다.
    /// </summary>
    public bool TryUnlockSkill(SkillNodeData skillNode)
    {
        if (unlockedSkills.Contains(skillNode)) return false; // 이미 해금됨

        // PlayerExperience.Instance가 null일 경우 방지
        if (PlayerExperience.Instance == null)
        {
            return false;
        }

        if (PlayerExperience.Instance.skillPoints < skillNode.skillPointCost) return false; // 스킬 포인트 부족

        // 선행 노드 충족 검사
        if (!CheckPrerequisites(skillNode)) return false;

        // 상위 티어 잠금 해제 요구사항 충족 검사
        if (!CheckTierUnlockRequirement(skillNode)) return false;

        // 스킬 포인트 사용
        if (PlayerExperience.Instance.TrySpendSkillPoint(skillNode.skillPointCost))
        {
            unlockedSkills.Add(skillNode);
            ApplySkillEffects(skillNode);

            if (skillNode.type == SkillType.Arrow && skillNode.tier != SkillTier.Tier5)
            {
                UpdateUnlockedArrowList();
            }

            // 해금된 노드 개수 업데이트
            unlockedNodesPerTier[skillNode.tier] = unlockedNodesPerTier.ContainsKey(skillNode.tier) ?
                                                     unlockedNodesPerTier[skillNode.tier] + 1 : 1;

            OnSkillUnlock.Invoke();
            return true;
        }

        return false;
    }

    public bool IsSkillUnlocked(SkillNodeData skillNode)
    {
        return unlockedSkills.Contains(skillNode);
    }

    public bool CanUnlockSkill(SkillNodeData skillNode)
    {
        if (IsSkillUnlocked(skillNode)) return false; // 이미 해금됨
        if (PlayerExperience.Instance == null || PlayerExperience.Instance.skillPoints < skillNode.skillPointCost) return false; // 포인트 부족

        // 선행 노드와 티어 요구사항이 모두 충족되었는지 확인
        return CheckPrerequisites(skillNode) && CheckTierUnlockRequirement(skillNode);
    }

    /// <summary>
    /// 해당 노드의 선행 노드가 모두 해금되었는지 확인 (기존 로직 유지)
    /// </summary>
    private bool CheckPrerequisites(SkillNodeData skillNode)
    {
        if (skillNode.parentNodes != null)
        {
            foreach (var parentNode in skillNode.parentNodes)
            {
                if (!unlockedSkills.Contains(parentNode))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// 상위 티어 스킬을 찍기 위한 하위 티어 스킬 개수가 충족되었는지 확인
    /// </summary>
    private bool CheckTierUnlockRequirement(SkillNodeData skillNode)
    {
        // Tier 1은 항상 해금 가능
        if (skillNode.tier == SkillTier.Tier1) return true;

        // 1. 필요한 총 누적 개수 설정
        int requiredCumulativeCount = 0;

        // Tier enum은 기본적으로 0부터 시작: Tier1=0, Tier2=1, Tier3=2, Tier4=3, Tier5=4
        switch (skillNode.tier)
        {
            case SkillTier.Tier2:
                requiredCumulativeCount = 2; // T1에서 총 2개 해금 필요
                break;
            case SkillTier.Tier3:
                requiredCumulativeCount = 5; // T1+T2에서 총 5개 해금 필요
                break;
            case SkillTier.Tier4:
                requiredCumulativeCount = 7; // T1+T2+T3에서 총 7개 해금 필요
                break;
            case SkillTier.Tier5:
                requiredCumulativeCount = 9; // T1+T2+T3+T4에서 총 9개 해금 필요
                break;
            default:
                return true;
        }

        // 2. 누적된 해금 개수 계산 (현재 티어보다 낮은 모든 티어 합산)
        int cumulativeUnlockedCount = 0;
        int currentTierIndex = (int)skillNode.tier;

        // 현재 티어 인덱스(예: T4=3)보다 작은 인덱스(T1=0, T2=1, T3=2)까지 반복
        for (int i = 0; i < currentTierIndex; i++)
        {
            SkillTier precedingTier = (SkillTier)i;
            // 해당 티어의 해금된 노드 개수를 누적합니다.
            cumulativeUnlockedCount += unlockedNodesPerTier.ContainsKey(precedingTier) ? unlockedNodesPerTier[precedingTier] : 0;
        }


        if (cumulativeUnlockedCount < requiredCumulativeCount)
        {
            return false;
        }

        return true;
    }


    /// <summary>
    /// 스킬 효과를 플레이어에게 적용합니다. (패시브 효과)
    /// </summary>
    private void ApplySkillEffects(SkillNodeData skillNode)
    {
        // 1. 패시브 스탯 적용
        totalBonusAttack += skillNode.attackIncrease;
        totalBonusHealth += skillNode.healthIncrease;
        UpdatePlayerStats(); // 헬퍼 함수로 플레이어 스탯 갱신

        // 2. 화살 스킬 처리
        if (skillNode.type == SkillType.Arrow && skillNode.tier != SkillTier.Tier5)
        {
            // StartUnlock으로 이미 추가되지 않았다면 목록 갱신
            if (!unlockedArrowSkills.Contains(skillNode))
            {
                UpdateUnlockedArrowList(); // 목록 갱신 및 정렬
            }
            // 새로 해금한 화살을 자동으로 활성화
            SelectActiveArrowSkill(skillNode);
        }

        // 3. [★신규★] 능력 스킬 초기화
        if (skillNode.type == SkillType.Ability)
        {
            // 딕셔너리에 아직 없으면 기본값으로 추가
            if (!skillCooldowns.ContainsKey(skillNode.skillName))
            {
                skillCooldowns[skillNode.skillName] = 0f;
                skillDurations[skillNode.skillName] = 0f;
                skillToggles[skillNode.skillName] = false;

                // 새로 해금된 능력의 초기 상태를 UI에 알림
                float maxCooldown = GetSkillCooldown(skillNode.skillName);
                float maxDuration = GetSkillDuration(skillNode.skillName);
                OnCooldownUpdate.Invoke(skillNode.skillName, 0f, maxCooldown);
                OnDurationUpdate.Invoke(skillNode.skillName, 0f, maxDuration);
                OnToggleUpdate.Invoke(skillNode.skillName, false);
            }
        }
    }

    private void UpdatePlayerStats()
    {
        // FindObjectOfType은 성능에 좋지 않으므로, 캐싱하거나 다른 방식으로 참조하는 것을 고려하세요.
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
            playerController.UpdateBonusHealth(totalBonusHealth);

        PlayerShooting playerShooting = FindObjectOfType<PlayerShooting>();
        if (playerShooting != null)
            playerShooting.UpdateArrowDamage(totalBonusAttack);
    }

    /// <summary>
    /// 플레이어가 현재 사용할 화살 스킬을 선택합니다. (Tier 2-4 활 스킬)
    /// </summary>
    public void SelectActiveArrowSkill(SkillNodeData arrowSkill)
    {
        if (unlockedSkills.Contains(arrowSkill) && arrowSkill.type == SkillType.Arrow && arrowSkill.tier != SkillTier.Tier5)
        {
            activeArrowSkill = arrowSkill;
            // PlayerShooting의 발사 로직에서 이 정보를 사용합니다.
            OnActiveArrowChanged.Invoke(activeArrowSkill);
        }
    }

    /// <summary>
    /// [추가] 해금된 화살 목록을 갱신합니다. (Tier 1 -> 4 순으로 정렬)
    /// </summary>
    private void UpdateUnlockedArrowList()
    {
        unlockedArrowSkills = unlockedSkills
            .Where(n => n.type == SkillType.Arrow && n.tier != SkillTier.Tier5)
            .OrderBy(n => n.tier) // Tier 1 (일반 활)이 항상 처음
            .ThenBy(n => n.skillName) // 같은 티어는 이름순
            .ToList();
    }

    /// <summary>
    /// [추가] 다음 활성 화살 스킬로 교체합니다. (PlayerShooting이 호출)
    /// </summary>
    public void SelectNextArrowSkill()
    {
        if (unlockedArrowSkills.Count <= 1) return; // "일반 활" 하나뿐이면 교체 안 함

        // 현재 활성 스킬의 인덱스를 찾습니다.
        int currentIndex = unlockedArrowSkills.IndexOf(activeArrowSkill);

        // 다음 인덱스를 계산합니다. (목록 끝이면 처음으로 돌아감)
        int nextIndex = (currentIndex + 1) % unlockedArrowSkills.Count;

        // 다음 스킬을 활성화합니다.
        SelectActiveArrowSkill(unlockedArrowSkills[nextIndex]);
    }


    /////////////////////////////////Abillity 구현///////////////////////////////////
    public void InitializeAbilityStates()
    {
        skillCooldowns.Clear();
        skillDurations.Clear();
        skillToggles.Clear();

        foreach (SkillNodeData node in unlockedSkills)
        {
            if (node.type == SkillType.Ability)
            {
                skillCooldowns[node.skillName] = 0f; // 사용 가능 상태로 시작
                skillDurations[node.skillName] = 0f; // 비활성 상태로 시작
                skillToggles[node.skillName] = false; // 꺼진 상태로 시작

                // UI에 초기 상태 알림
                // 참고: SkillNodeData에 cooldown 필드가 필요. 우선 30초로 가정.
                float maxCooldown = GetSkillCooldown(node.skillName); // 헬퍼 함수 필요
                float maxDuration = GetSkillDuration(node.skillName); // 헬퍼 함수 필요
                OnCooldownUpdate.Invoke(node.skillName, 0f, maxCooldown);
                OnDurationUpdate.Invoke(node.skillName, 0f, maxDuration);
                OnToggleUpdate.Invoke(node.skillName, false);
            }
        }
        // 다른 화살이 없을 경우 기본 화살("일반 활")이 활성화되도록 보장
        if (activeArrowSkill == null && unlockedArrowSkills.Count > 0)
        {
            SelectActiveArrowSkill(unlockedArrowSkills[0]); // "일반 활" 선택
        }
    }

    /// <summary>
    /// 능력 스킬이 해금되었고 재사용 대기 중이 아닌지 확인합니다.
    /// </summary>
    public bool IsSkillReady(string skillName)
    {
        SkillNodeData node = GetSkillNodeData(skillName); // 헬퍼 함수 사용
        if (node == null || !unlockedSkills.Contains(node) || node.type != SkillType.Ability)
        {
            return false;
        }
        // 스킬이 목록에 있고 쿨다운이 0 이하면 사용 가능
        return skillCooldowns.ContainsKey(skillName) && skillCooldowns[skillName] <= 0f;
    }

    /// <summary>
    /// 연속 화살처럼 시간제 능력 스킬이 현재 활성 상태인지 확인합니다.
    /// </summary>
    public bool IsSkillActive(string skillName)
    {
        SkillNodeData node = GetSkillNodeData(skillName);
        if (node == null || !unlockedSkills.Contains(node) || node.type != SkillType.Ability)
        {
            return false;
        }
        // 스킬이 목록에 있고 남은 지속 시간이 0보다 크면 활성 상태
        return skillDurations.ContainsKey(skillName) && skillDurations[skillName] > 0f;
    }

    /// <summary>
    /// 용/바람처럼 토글형 능력 스킬이 현재 켜져 있는지 확인합니다.
    /// </summary>
    public bool IsSkillToggled(string skillName)
    {
        SkillNodeData node = GetSkillNodeData(skillName);
        if (node == null || !unlockedSkills.Contains(node) || node.type != SkillType.Ability)
        {
            return false;
        }
        // 스킬이 목록에 있고 토글 값이 true면 켜진 상태
        return skillToggles.ContainsKey(skillName) && skillToggles[skillName];
    }

    /// <summary>
    /// 연속 화살 같은 스킬을 활성화 시도합니다. 재사용 대기시간이 시작됩니다.
    /// </summary>
    /// <returns>활성화 성공 여부.</returns>
    public bool TryActivateSkill(string skillName)
    {
        if (!IsSkillReady(skillName)) return false; // 준비 안됐으면 실패

        // 연속 화살 지속 시간 10초 가정
        float duration = GetSkillDuration(skillName); // 헬퍼 함수 필요
        float cooldown = GetSkillCooldown(skillName); // 헬퍼 함수 필요

        skillDurations[skillName] = duration; // 지속 시간 설정
        skillCooldowns[skillName] = cooldown; // 재사용 대기시간 시작

        OnDurationUpdate.Invoke(skillName, duration, duration); // UI에 지속 시간 시작 알림
        OnCooldownUpdate.Invoke(skillName, cooldown, cooldown); // UI에 쿨다운 시작 알림
        Debug.Log($"{skillName} 활성화! 지속시간: {duration}초, 재사용 대기시간: {cooldown}초");
        return true;
    }

    /// <summary>
    /// 용/바람 같은 스킬을 켜거나 끕니다 (준비된 상태일 때만 켤 수 있음). 아직 재사용 대기시간은 시작하지 않습니다.
    /// </summary>
    /// <returns>새로운 토글 상태 (켜졌으면 true).</returns>
    public bool ToggleSkill(string skillName)
    {
        // 켜려는 경우 준비 상태여야 함, 끄는 건 언제나 가능
        if (!IsSkillReady(skillName) && !IsSkillToggled(skillName))
        {
            Debug.Log($"{skillName} 준비 안됨 (재사용 대기 중).");
            return false; // 현재 토글 상태 반환
        }

        bool currentState = IsSkillToggled(skillName);
        bool newState = !currentState; // 상태 반전
        skillToggles[skillName] = newState;

        // 하나를 켜면 다른 토글 스킬은 끄기 (동시 사용 방지)
        if (newState)
        { // 방금 켰다면
            if (skillName == DRAGON_SKILL && IsSkillToggled(WIND_SKILL))
            {
                ToggleSkill(WIND_SKILL); // 바람 끄기
            }
            else if (skillName == WIND_SKILL && IsSkillToggled(DRAGON_SKILL))
            {
                ToggleSkill(DRAGON_SKILL); // 용 끄기
            }
        }

        OnToggleUpdate.Invoke(skillName, newState); // UI에 상태 변경 알림
        Debug.Log($"{skillName} 토글 {(newState ? "켜짐" : "꺼짐")}");
        return newState;
    }

    /// <summary>
    /// 토글된 스킬(용/바람)이 *사용된 후* PlayerShooting에서 호출됩니다. 토글을 끄고 재사용 대기시간을 시작합니다.
    /// </summary>
    public void UseToggledSkill(string skillName)
    {
        if (!IsSkillToggled(skillName)) return; // 켜져 있지 않으면 무시 (정상적인 경우 발생 안 함)

        float cooldown = GetSkillCooldown(skillName); // 헬퍼 함수 필요

        skillToggles[skillName] = false; // 토글 끄기
        skillCooldowns[skillName] = cooldown; // 재사용 대기시간 시작

        OnToggleUpdate.Invoke(skillName, false); // UI에 토글 꺼짐 알림
        OnCooldownUpdate.Invoke(skillName, cooldown, cooldown); // UI에 쿨다운 시작 알림
        Debug.Log($"{skillName} 사용됨! 재사용 대기시간 시작: {cooldown}초");
    }

    /// <summary>
    /// 모든 활성 재사용 대기시간 및 지속 시간을 갱신합니다. Update()에서 호출됩니다.
    /// </summary>
    private void UpdateSkillTimers(float deltaTime)
    {
        // 반복 중 딕셔너리 수정을 피하기 위해 임시 리스트 사용
        List<string> cooldownKeys = new List<string>(skillCooldowns.Keys);
        List<string> durationKeys = new List<string>(skillDurations.Keys);

        // 재사용 대기시간 갱신
        foreach (string skillName in cooldownKeys)
        {
            if (skillCooldowns[skillName] > 0f)
            {
                skillCooldowns[skillName] -= deltaTime;
                if (skillCooldowns[skillName] < 0f) skillCooldowns[skillName] = 0f; // 0 이하로 내려가지 않게
                // UI에 갱신 알림
                OnCooldownUpdate.Invoke(skillName, skillCooldowns[skillName], GetSkillCooldown(skillName));
            }
        }

        // 지속 시간 갱신
        foreach (string skillName in durationKeys)
        {
            if (skillDurations[skillName] > 0f)
            {
                skillDurations[skillName] -= deltaTime;
                if (skillDurations[skillName] < 0f) skillDurations[skillName] = 0f; // 0 이하 방지
                // UI에 갱신 알림
                OnDurationUpdate.Invoke(skillName, skillDurations[skillName], GetSkillDuration(skillName));
                // 지속 시간이 끝나면 로그 출력 (선택 사항)
                if (skillDurations[skillName] <= 0f)
                {
                    Debug.Log($"{skillName} 지속 시간 종료.");
                }
            }
        }
    }

    // --- 헬퍼 메서드 ---
    public SkillNodeData GetSkillNodeData(string skillName)
    { // 외부에서도 접근 가능하도록 public 변경
        return allSkillNodes.FirstOrDefault(n => n.skillName == skillName);
    }

    // TODO: 이 헬퍼 함수들을 구현해야 합니다 - SkillNodeData에서 값을 가져오거나 기본값을 정의하세요.
    public float GetSkillCooldown(string skillName)
    { // 외부 접근 위해 public 변경
      // 방법 1: SkillNodeData에서 가져오기 (cooldown 필드를 추가했다면)
      // SkillNodeData node = GetSkillNodeData(skillName);
      // return (node != null && node.cooldown > 0) ? node.cooldown : 30f; // 설정 안됐으면 기본 30초

        // 방법 2: 우선 하드코딩
        return 30f; // 모든 스킬 기본 30초
    }

    public float GetSkillDuration(string skillName)
    { // 외부 접근 위해 public 변경
        // 방법 1: SkillNodeData에서 가져오기 (duration 필드를 추가했다면)
        // SkillNodeData node = GetSkillNodeData(skillName);
        // return (node != null && node.duration > 0) ? node.duration : 0f;

        // 방법 2: 우선 하드코딩
        if (skillName == MULTISHOT_SKILL) return 10f; // 연속 화살만 10초
        return 0f; // 용과 바람은 즉시 시전형
    }
}