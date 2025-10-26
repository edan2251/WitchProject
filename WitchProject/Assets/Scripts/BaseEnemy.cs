using UnityEngine;
using System.Collections;

// abstract: 이 스크립트 자체는 인스펙터에 붙일 수 없고, 상속용으로만 쓰겠다는 의미
public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Enemy Data")]
    public EnemyData enemyData;

    [Header("Health & Effects")]
    public int maxHp;
    public int currentHP;
    public float flashDuration = 0.1f;

    protected HealthBarManager healthBarManager;
    protected Renderer enemyRenderer;
    protected Color originalColor;

    // 'virtual' : 자식 스크립트가 이 함수를 덮어쓰거나 확장할 수 있게 함
    public virtual void Start()
    {
        // 1. 체력 설정
        if (enemyData != null)
        {
            currentHP = enemyData.maxHealth;
            maxHp = enemyData.maxHealth;
        }
        else
        {
            currentHP = 10; // 기본값
            maxHp = 10;
            Debug.LogError(gameObject.name + ": EnemyData가 할당되지 않았습니다!");
        }

        // 2. 헬스바 매니저 등록
        healthBarManager = FindObjectOfType<HealthBarManager>();
        if (healthBarManager != null)
        {
            healthBarManager.RegisterEnemy(this); // 'this' (BaseEnemy)를 매니저에 등록
        }

        // 3. 렌더러(색상 변경용) 설정
        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
    }

    // 몬스터가 데미지를 받는 공통 함수
    public virtual void TakeDamage(int damage)
    {
        FlashOnHit();
        currentHP -= damage;

        if (healthBarManager != null)
        {
            healthBarManager.UpdateEnemyHealth(this);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 피격 시 깜빡임 공통 함수
    public virtual void FlashOnHit()
    {
        StopAllCoroutines();
        StartCoroutine(FlashColor());
    }

    protected virtual IEnumerator FlashColor()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.SetColor("_BaseColor", Color.red);
            yield return new WaitForSeconds(flashDuration);
            enemyRenderer.material.SetColor("_BaseColor", originalColor);
        }
    }

    // 사망 시 공통 처리 함수
    protected virtual void Die()
    {
        // 1. 경험치 처리
        if (enemyData != null)
        {
            int expAmount = enemyData.experienceGained;
            if (PlayerExperience.Instance != null)
            {
                PlayerExperience.Instance.AddExperience(expAmount);
            }
        }

        // 2. 헬스바 매니저에서 제거
        if (healthBarManager != null)
        {
            healthBarManager.UnregisterEnemy(this);
        }

        // 3. 오브젝트 파괴
        Destroy(gameObject);
    }
}