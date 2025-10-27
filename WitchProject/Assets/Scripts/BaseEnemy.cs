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

    [HideInInspector] 
    public NavMeshAgent agent;

    [Header("Targeting")]
    [SerializeField] private float playerAggroRange = 8f; // 플레이어 어그로 범위

    protected Transform playerTransform; // 플레이어의 Transform (고정)
    protected Transform defenseObjectTransform; // 방어 오브젝트의 Transform
    protected Transform currentTarget; // 적이 실제 추적/공격할 대상

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // 헬스바 매니저 인스턴스 캐싱
        healthBarManager = HealthBarManager.Instance;

        // 렌더러 설정 (자식 Start에서 하던 것을 Awake로 이동)
        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
    }

    // 'virtual' : 자식 스크립트가 이 함수를 덮어쓰거나 확장할 수 있게 함
    public virtual void Start()
    {
        

    }

    protected virtual void OnEnable()
    {
        // 1. 체력 설정 (가장 중요!)
        // (Start에서 이 로직을 가져옴)
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

        // 2. 렌더러(색상 변경용) 설정 (Awake에서 못 찾았을 경우 대비)
        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponentInChildren<Renderer>();
            if (enemyRenderer != null)
            {
                originalColor = enemyRenderer.material.color;
            }
        }

        // 3. 헬스바 매니저 등록
        // (HealthBarManager가 먼저 로드되었는지 확인)
        if (healthBarManager == null)
        {
            healthBarManager = HealthBarManager.Instance;
        }

        if (healthBarManager != null)
        {
            healthBarManager.RegisterEnemy(this); // 'this' (BaseEnemy)를 매니저에 등록
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

        // [추가] 비활성화될 때 실행 중인 코루틴 정리
        StopAllCoroutines();
        burnCoroutine = null;
        flashCoroutine = null;
        lightningEffectCoroutine = null;
    }

    protected virtual void UpdateTarget()
    {
        if (playerTransform == null)
        {
            playerTransform = EnemyTargetManager.PlayerTarget;

            // 아직도 플레이어 등록이 안됐으면, 어떤 타겟팅도 불가능하므로 종료
            if (playerTransform == null)
            {
                currentTarget = null; // 확실하게 null로 설정
                return;
            }
        }


        // 2. 디펜스 스테이지가 아닌 경우 (평상시)
        if (!EnemyTargetManager.IsDefenseStageActive)
        {
            currentTarget = playerTransform;
            return;
        }

        // 3. 디펜스 스테이지인 경우

        // 3a. 방어 오브젝트 정보가 없으면 가져오기
        if (defenseObjectTransform == null)
        {
            defenseObjectTransform = EnemyTargetManager.DefenseTarget;
            // 아직도 없으면(시작 중 딜레이 등) 일단 플레이어를 타겟
            if (defenseObjectTransform == null)
            {
                currentTarget = playerTransform;
                return;
            }
        }

        // 3b. [핵심 로직] 플레이어와의 거리 계산
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distToPlayer <= playerAggroRange)
        {
            // 3c. 플레이어가 5f 안으로 들어오면 플레이어를 타겟
            currentTarget = playerTransform;
        }
        else
        {
            // 3d. 플레이어가 5f 밖이면 방어 오브젝트를 타겟
            currentTarget = defenseObjectTransform;
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