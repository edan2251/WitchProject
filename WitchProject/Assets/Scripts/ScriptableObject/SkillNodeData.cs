using UnityEngine;

// 스킬의 등급 (Tier)
public enum SkillTier { Tier1, Tier2, Tier3, Tier4, Tier5 }

// 스킬 타입 (주요 활, 패시브)
public enum SkillType { Arrow, Attack, Health, Ability }

// 프로젝트 뷰에서 마우스 우클릭 -> Create -> Skill -> Skill Node Data로 생성 가능
[CreateAssetMenu(fileName = "NewSkillNodeData", menuName = "Skill/Skill Node Data", order = 1)]
public class SkillNodeData : ScriptableObject
{
    [Header("Node Info")]
    public string skillName = "일반 화살";
    public SkillTier tier = SkillTier.Tier1;
    public SkillType type = SkillType.Arrow;
    public int skillPointCost = 1;

    [Header("Stats/Effects (Passive)")]
    public int attackIncrease = 0; // 공격력 증가 (+1, +2, +3)
    public int healthIncrease = 0;   // 체력 증가 (+5)

    [Header("Prerequisites (Skill Tree)")]
    // 상위 스킬 잠금 해제에 필요한 이 노드의 하위 노드 개수 (이미지 요구사항)
    public int requiredNodesForNextTier = 0;

    // 이 스킬을 찍기 위해 선행되어야 하는 스킬 노드들 (예: 폭탄 화살은 일반 활, 불 화살 필요)
    public SkillNodeData[] parentNodes;
}