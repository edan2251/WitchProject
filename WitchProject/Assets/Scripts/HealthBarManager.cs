using UnityEngine;
using System.Collections.Generic;

public class HealthBarManager : MonoBehaviour
{

    public static HealthBarManager Instance { get; private set; }

    public GameObject healthBarPrefab;
    public Camera mainCamera;

    // [수정] Enemy -> BaseEnemy
    private Dictionary<BaseEnemy, GameObject> activeHealthBars = new Dictionary<BaseEnemy, GameObject>();

    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

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