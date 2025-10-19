using UnityEngine;

// 프로젝트 뷰에서 마우스 우클릭 -> Create -> Enemy -> Enemy Data로 생성 가능
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data", order = 1)]
public class EnemyData : ScriptableObject
{
    [Header("Base Stats")]
    public int maxHealth = 10;
    public int attackDamage = 1; // 일반 적의 공격력 또는 소환사의 경우 미니언 공격력

    [Header("Progression")]
    public int experienceGained = 10; // 처치 시 플레이어가 얻는 경험치
}