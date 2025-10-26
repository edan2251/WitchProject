using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스 추가

public class PlayerStatUI : MonoBehaviour
{
    [Header("UI Text Elements")]
    public TextMeshProUGUI healthText; // 1단계에서 만든 체력 텍스트
    public TextMeshProUGUI attackText; // 1단계에서 만든 공격력 텍스트

    [Header("Player References")]
    public PlayerController playerController; // 플레이어 오브젝트
    public PlayerShooting playerShooting;     // 플레이어 오브젝트

    // Update는 매 프레임마다 호출됩니다.
    void Update()
    {
        // 1. 체력 텍스트 업데이트
        if (playerController != null && healthText != null)
        {
            // 예: "체력: 105 / 105"
            healthText.text = $"HP: {playerController.currentHP} / {playerController.maxHP}";
        }

        // 2. 공격력 텍스트 업데이트
        if (playerShooting != null && attackText != null)
        {
            // 예: "공격력: 5"
            // playerShooting의 currentArrowDamage가 최종 공격력입니다.
            attackText.text = $"DMG: {playerShooting.currentArrowDamage}";
        }
    }
}