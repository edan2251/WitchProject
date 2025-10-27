using UnityEngine;

/// <summary>
/// 모든 적의 현재 타겟을 관리하는 정적 클래스입니다.
/// 디펜스 스테이지 여부와 타겟 정보를 저장합니다.
/// </summary>
public static class EnemyTargetManager
{
    public static Transform PlayerTarget { get; private set; }
    public static Transform DefenseTarget { get; private set; }
    public static bool IsDefenseStageActive { get; private set; } = false;

    /// <summary>
    /// 플레이어가 게임 시작 시 자신을 등록합니다.
    /// </summary>
    public static void RegisterPlayer(Transform player)
    {
        PlayerTarget = player;
    }

    /// <summary>
    /// 디펜스 스테이지가 시작될 때 호출됩니다.
    /// </summary>
    public static void StartDefenseStage(Transform defenseObject)
    {
        DefenseTarget = defenseObject;
        IsDefenseStageActive = true;
    }

    /// <summary>
    /// 디펜스 스테이지가 종료될 때 호출됩니다.
    /// </summary>
    public static void EndDefenseStage()
    {
        IsDefenseStageActive = false;
        DefenseTarget = null; // 타겟 정보 초기화
    }
}