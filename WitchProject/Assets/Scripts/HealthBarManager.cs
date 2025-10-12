using UnityEngine;
using System.Collections.Generic;

public class HealthBarManager : MonoBehaviour
{
    public GameObject healthBarPrefab; 
    public Camera mainCamera; 

    // 현재 활성화된 체력 바 UI와 그 주인을 매핑하는 딕셔너리
    private Dictionary<Enemy, GameObject> activeHealthBars = new Dictionary<Enemy, GameObject>();

    public void UpdateEnemyHealth(Enemy enemy)
    {
        // 딕셔너리에서 해당 적의 체력 바 오브젝트를 찾습니다.
        if (activeHealthBars.TryGetValue(enemy, out GameObject healthBarObj))
        {
            HealthBarUI healthBarUI = healthBarObj.GetComponent<HealthBarUI>();
            if (healthBarUI != null)
            {
                // 찾은 HealthBarUI에게 현재 체력 정보로 업데이트하라고 명령합니다.
                healthBarUI.UpdateHealth(enemy.currentHP, enemy.maxHp);
            }
        }
    }

    // Enemy에서 호출: 체력 바를 생성하고 딕셔너리에 추가
    public void RegisterEnemy(Enemy enemy)
    {
        if (activeHealthBars.ContainsKey(enemy)) return; // 이미 등록됨

        // 1. 체력 바 UI 생성
        GameObject healthBarObj = Instantiate(healthBarPrefab, transform);

        // 2. HealthBarUI 스크립트 연결 및 초기 설정
        HealthBarUI healthBarUI = healthBarObj.GetComponent<HealthBarUI>();
        if (healthBarUI != null)
        {
            healthBarUI.target = enemy.transform; // 추적 대상 설정
            healthBarUI.targetEnemy = enemy;      // Enemy 스크립트 자체 할당
            healthBarUI.uiCamera = mainCamera;    // 카메라 할당
            healthBarUI.UpdateHealth(enemy.currentHP, enemy.maxHp); // 초기 체력 설정
        }

        // 3. 딕셔너리에 등록
        activeHealthBars.Add(enemy, healthBarObj);
    }

    // 외부(Enemy 사망 시)에서 호출: 체력 바 제거
    public void UnregisterEnemy(Enemy enemy)
    {
        if (activeHealthBars.TryGetValue(enemy, out GameObject healthBarObj))
        {
            // 1. UI 오브젝트 파괴
            Destroy(healthBarObj);

            // 2. 딕셔너리에서 제거
            activeHealthBars.Remove(enemy);
        }
    }
}