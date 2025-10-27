using UnityEngine;
using System.Collections;
using UnityEngine.AI;

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

    private Coroutine burnCoroutine;
    private Coroutine flashCoroutine;
    private Coroutine lightningEffectCoroutine;

    [Header("Defense & Aggro")]
    [Tooltip("적들이 우선 공격할 방어 목표물. 인스펙터에 할당하세요.")]
    public Transform defenseTarget;
    [Tooltip("플레이어가 이 거리 내로 오면 defenseTarget보다 플레이어를 우선 공격합니다.")]
    public float playerAggroRange = 7f; // 플레이어 근접 어그로 범위

    protected Transform playerTarget;
    protected NavMeshAgent agent;
    protected Transform currentTarget; // 현재 이동/공격할 최종 타겟

    protected virtual void Awake()
    {
        currentHP = maxHp;
    }

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

        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    /// <summary>
    /// 방어 타겟과 플레이어 사이의 우선순위를 결정하여 최종 타겟을 반환합니다.
    /// </summary>
    protected virtual Transform DetermineTarget()
    {
        if (playerTarget == null) return defenseTarget; // 플레이어가 없다면 방어 타겟 우선

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 1. 플레이어가 근접 어그로 범위 내에 있으면 플레이어 우선
        if (distanceToPlayer <= playerAggroRange)
        {
            return playerTarget;
        }

        // 2. 방어 타겟이 살아있고 설정되어 있으면 방어 타겟
        if (defenseTarget != null)
        {
            return defenseTarget;
        }

        // 3. 방어 타겟이 없거나 멀리 있다면 플레이어 타겟을 유지
        return playerTarget;
    }

    protected virtual void OnEnable()
    {
        // HealthBarManager가 존재하면 자신을 등록합니다.
        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.RegisterEnemy(this);
        }
    }

    protected virtual void OnDisable()
    {
        // HealthBarManager가 (씬 종료 등으로) 먼저 사라지지 않았다면
        // 자신을 등록 해제합니다.
        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.UnregisterEnemy(this);
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
        // [추가] 이미 감전(파랑) 효과가 진행 중이면, 빨간색 깜빡임을 "무시"합니다.
        if (lightningEffectCoroutine != null)
        {
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashColor());
    }

    // [확인 3] FlashColor 코루틴
    protected virtual IEnumerator FlashColor()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.SetColor("_BaseColor", Color.red);
            yield return new WaitForSeconds(flashDuration);
            enemyRenderer.material.SetColor("_BaseColor", originalColor);
        }

        // [중요!] 코루틴이 끝나면 변수를 비워줍니다.
        flashCoroutine = null;
    }

    /// <summary>
    /// [추가] 피격 깜빡임(빨간색) 없이 데미지를 적용합니다. (번개 연쇄 효과 전용)
    /// </summary>
    public virtual void TakeDamageWithoutFlash(int damage)
    {
        // FlashOnHit(); // <-- 이 줄을 "제외"하고 TakeDamage와 동일하게 만듭니다.
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

    // <summary>
    /// [추가] 적에게 '감전' 효과를 적용하여 잠시 파랗게 만듭니다.
    /// </summary>
    public void ApplyLightningEffect(float duration)
    {
        // [추가] 만약 빨간색 깜빡임(flashCoroutine)이 실행 중이었다면, 
        //       파란색 효과가 덮어쓰도록 강제로 중지시킵니다.
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        // 이미 다른 감전 효과가 실행 중이면 중지하고 새로 시작
        if (lightningEffectCoroutine != null)
        {
            StopCoroutine(lightningEffectCoroutine);
        }
        lightningEffectCoroutine = StartCoroutine(LightningEffectCoroutine(duration));
    }

    /// <summary>
    /// [추가] 파란색으로 변경했다가 원래 색으로 복구하는 코루틴
    /// </summary>
    private IEnumerator LightningEffectCoroutine(float duration)
    {
        if (enemyRenderer == null) yield break;

        // 1. 즉시 파란색으로 변경
        enemyRenderer.material.SetColor("_BaseColor", Color.blue);

        // 2. 지정된 시간(duration)만큼 파란 상태로 대기
        yield return new WaitForSeconds(duration);

        // 3. 원래 색상으로 복구
        // (만약 이 사이에 불이 붙어도, 점화 효과는 색을 바꾸지 않으므로 originalColor로 복구)
        enemyRenderer.material.SetColor("_BaseColor", originalColor);

        lightningEffectCoroutine = null; // 코루틴 완료
    }

    /// <summary>
    /// [추가] 적에게 점화(Burn) 효과를 적용합니다.
    /// </summary>
    public void ApplyBurnEffect(int ticks, float interval, int damagePerTick)
    {
        // 이미 불타고 있다면, 기존 코루틴을 중지하고 새로 시작 (효과 갱신)
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
        }
        burnCoroutine = StartCoroutine(BurnCoroutine(ticks, interval, damagePerTick));
    }

    private IEnumerator BurnCoroutine(int ticks, float interval, int damagePerTick)
    {
        // TODO: 여기에 불타는 파티클 이펙트를 자식으로 생성하고 재생하는 코드를 넣으세요.
        // 예: GameObject burnEffect = Instantiate(burnParticlePrefab, transform);

        int currentTicks = 0;
        while (currentTicks < ticks)
        {
            // [수정] 1. 데미지를 주기 전에 먼저 0.2초 대기합니다.
            yield return new WaitForSeconds(interval);

            // [수정] 2. 대기 후에 데미지를 줍니다.
            if (this != null && currentHP > 0)
            {
                // Debug.Log($"점화 데미지 {damagePerTick} 적용! (틱: {currentTicks + 1})");
                TakeDamage(damagePerTick);
                currentTicks++;
            }
            else
            {
                break; // 적이 파괴되었으면 코루틴 즉시 중지
            }
        }

        // TODO: 여기서 불타는 파티클 이펙트를 중지/제거하세요.
        // 예: Destroy(burnEffect);

        burnCoroutine = null; // 코루틴 완료
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
        Destroy(gameObject, 0.2f);
    }
}