using UnityEngine;
using UnityEngine.Events;

public class PlayerExperience : MonoBehaviour
{
    public static PlayerExperience Instance { get; private set; }

    [Header("Current Stats")]
    public int currentLevel = 1;
    public int currentExperience = 0;
    public int skillPoints = 0;

    [Header("Leveling Settings")]
    [Tooltip("레벨업에 필요한 기본 경험치량")]
    public int baseExpToLevelUp = 100;
    [Tooltip("레벨이 올라갈 때마다 ExpToLevelUp에 곱할 배율")]
    public float expMultiplier = 1.2f;

    // 이벤트: 경험치나 스킬 포인트 획득 시 UI 업데이트를 위해 사용
    public UnityEvent OnLevelUp = new UnityEvent();
    public UnityEvent<int> OnExpChange = new UnityEvent<int>(); // 현재 경험치/다음 레벨 경험치 등 전달 가능
    public UnityEvent<int> OnSkillPointChange = new UnityEvent<int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 현재 레벨업에 필요한 총 경험치
    public int GetExperienceToNextLevel()
    {
        // 예: Level 1 -> 100, Level 2 -> 120, Level 3 -> 144
        return Mathf.RoundToInt(baseExpToLevelUp * Mathf.Pow(expMultiplier, currentLevel - 1));
    }

    /// <summary>
    /// 플레이어에게 경험치를 추가하고 레벨업을 확인합니다.
    /// </summary>
    public void AddExperience(int amount)
    {
        currentExperience += amount;
        OnExpChange.Invoke(currentExperience); // 경험치 UI 업데이트 호출

        while (currentExperience >= GetExperienceToNextLevel())
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        int expToNext = GetExperienceToNextLevel();
        currentExperience -= expToNext;
        currentLevel++;
        skillPoints++; // 레벨업 시 스킬 포인트 1 획득

        Debug.Log($"레벨업! 현재 레벨: {currentLevel}, 스킬 포인트: {skillPoints} 획득");

        OnLevelUp.Invoke();
        OnSkillPointChange.Invoke(skillPoints);
    }

    /// <summary>
    /// 스킬 포인트를 사용하여 스킬을 찍을 때 호출
    /// </summary>
    public bool TrySpendSkillPoint(int cost = 1)
    {
        if (skillPoints >= cost)
        {
            skillPoints -= cost;
            OnSkillPointChange.Invoke(skillPoints);
            return true;
        }
        return false;
    }
}