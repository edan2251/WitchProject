using UnityEngine;
using System.Collections.Generic;

public class HealthBarManager : MonoBehaviour
{
    public GameObject healthBarPrefab;
    public Camera mainCamera;

    // [수정] Enemy -> BaseEnemy
    private Dictionary<BaseEnemy, GameObject> activeHealthBars = new Dictionary<BaseEnemy, GameObject>();

    // [수정] Enemy -> BaseEnemy
    public void UpdateEnemyHealth(BaseEnemy enemy)
    {
        if (activeHealthBars.TryGetValue(enemy, out GameObject healthBarObj))
        {
            HealthBarUI healthBarUI = healthBarObj.GetComponent<HealthBarUI>();
            if (healthBarUI != null)
            {
                healthBarUI.UpdateHealth(enemy.currentHP, enemy.maxHp);
            }
        }
    }

    // [수정] Enemy -> BaseEnemy
    public void RegisterEnemy(BaseEnemy enemy)
    {
        if (activeHealthBars.ContainsKey(enemy)) return;

        GameObject healthBarObj = Instantiate(healthBarPrefab, transform);
        HealthBarUI healthBarUI = healthBarObj.GetComponent<HealthBarUI>();
        if (healthBarUI != null)
        {
            healthBarUI.target = enemy.transform;

            // [중요] 사용자의 'HealthBarUI.cs' 스크립트도 수정이 필요할 수 있습니다.
            // 'targetEnemy' 필드의 타입을 'Enemy'에서 'BaseEnemy'로 바꿔야 합니다.
            // healthBarUI.targetEnemy = enemy; // 이 부분이 BaseEnemy를 받도록 수정 필요

            healthBarUI.uiCamera = mainCamera;
            healthBarUI.UpdateHealth(enemy.currentHP, enemy.maxHp);
        }
        activeHealthBars.Add(enemy, healthBarObj);
    }

    // [수정] Enemy -> BaseEnemy
    public void UnregisterEnemy(BaseEnemy enemy)
    {
        if (activeHealthBars.TryGetValue(enemy, out GameObject healthBarObj))
        {
            Destroy(healthBarObj);
            activeHealthBars.Remove(enemy);
        }
    }
}