using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq; // LINQ 사용

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    // 모든 스킬 노드 데이터 목록 (에디터에서 수동 할당)
    [Header("Skill Data")]
    public SkillNodeData[] allSkillNodes;

    // 플레이어가 이미 해금한 스킬 목록
    private HashSet<SkillNodeData> unlockedSkills = new HashSet<SkillNodeData>();

    // 티어별 해금한 스킬 노드 개수
    private Dictionary<SkillTier, int> unlockedNodesPerTier = new Dictionary<SkillTier, int>();

    // 플레이어의 총 추가 공격력/체력
    public int totalBonusAttack { get; private set; } = 0;
    public int totalBonusHealth { get; private set; } = 0;


    public UnityEvent OnSkillUnlock = new UnityEvent();

    // 현재 활성화된(사용 중인) 특수 화살 스킬
    public SkillNodeData activeArrowSkill { get; private set; } = null;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeUnlockedNodesPerTier();

        // [수정]: Tier 1 스킬 중 '일반 활'을 찾아서 강제 해금합니다.
        SkillNodeData defaultArrow = allSkillNodes.FirstOrDefault(n => n.skillName == "일반 활");
        if (defaultArrow != null)
        {
            StartUnlockSkillNode(defaultArrow);
        }
        else
        {
            Debug.LogError("Error: '일반 활' SkillNodeData를 찾을 수 없습니다. 인스펙터에 모든 노드가 할당되었는지 확인하세요.");
        }

        // 초기 상태를 반영하여 UI 업데이트를 한 번 호출합니다. (SkillTreeUI.cs가 이 이벤트를 구독해야 합니다.)
        OnSkillUnlock.Invoke();
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

        Debug.Log($"[초기화] {skillNode.skillName} 스킬 강제 해금 완료!");
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
            Debug.LogError("PlayerExperience Instance is missing!");
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

            // 해금된 노드 개수 업데이트
            unlockedNodesPerTier[skillNode.tier] = unlockedNodesPerTier.ContainsKey(skillNode.tier) ?
                                                     unlockedNodesPerTier[skillNode.tier] + 1 : 1;

            OnSkillUnlock.Invoke();
            Debug.Log($"{skillNode.skillName} 스킬 해금 완료!");
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
                    Debug.LogWarning($"{skillNode.skillName} 스킬은 {parentNode.skillName} 스킬이 선행되어야 합니다.");
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
            Debug.LogWarning($"티어 {skillNode.tier} 스킬 해금 실패: 누적 해금 노드 총 {requiredCumulativeCount}개가 필요합니다. (현재 {cumulativeUnlockedCount}개)");
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

        // PlayerShooting 스크립트에 스탯을 반영하는 로직이 필요합니다.
        PlayerShooting playerShooting = FindObjectOfType<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.UpdateArrowDamage(totalBonusAttack);
        }

        // 2. 특수 화살 스킬인 경우 (Tier 2~4)
        if (skillNode.type == SkillType.Arrow && skillNode.tier != SkillTier.Tier5)
        {
            // 새로운 특수 활을 찍으면 자동으로 활성화되도록 설정 (옵션)
            SelectActiveArrowSkill(skillNode);
        }

        // 3. 궁극기 스킬인 경우 (Tier 5)
        if (skillNode.tier == SkillTier.Tier5)
        {
            // TODO: 궁극기 활성화/키 바인딩 로직 구현
        }

        // HealthManager에 totalBonusHealth 반영 로직 필요
    }

    /// <summary>
    /// 플레이어가 현재 사용할 화살 스킬을 선택합니다. (Tier 2-4 활 스킬)
    /// </summary>
    public void SelectActiveArrowSkill(SkillNodeData arrowSkill)
    {
        if (unlockedSkills.Contains(arrowSkill) && arrowSkill.type == SkillType.Arrow && arrowSkill.tier != SkillTier.Tier5)
        {
            activeArrowSkill = arrowSkill;
            Debug.Log($"{arrowSkill.skillName} 활성화!");
            // PlayerShooting의 발사 로직에서 이 정보를 사용합니다.
        }
    }
}