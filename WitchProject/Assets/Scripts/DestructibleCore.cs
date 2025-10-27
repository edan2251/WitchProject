using UnityEngine;
using UnityEngine.Events; // 이벤트를 사용하기 위해 추가
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// 플레이어의 공격을 받아 파괴되는 오브젝트 스크립트입니다.
/// </summary>
public class DestructibleCore : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private StageUIManager stageUIManager;
    public string stageStartMessage = "[스테이지 4]\n마녀의 힘을 담은 코어가 모습을 드러냈습니다!\n전력을 다해 이것을 파괴하세요!";

    [Header("Core Stats")]
    public int maxHP = 500;
    public int currentHP;

    [Header("Feedback")]
    [Tooltip("데미지를 입을 때 빨갛게 만들 코어의 Renderer")]
    public Renderer coreRenderer;
    public float flashDuration = 0.1f;

    [Header("Events")]
    [Tooltip("코어가 파괴되었을 때 실행할 이벤트 (예: 다음 스테이지 문 열기)")]
    public UnityEvent OnCoreDestroyed;

    [Header("UI")] 
    public Slider hpSlider;

    private Color originalCoreColor;
    private Coroutine flashCoroutine;

    void OnEnable()
    {
        if (MinionManager.Instance != null)
        {
            MinionManager.Instance.ClearAllMinions();
        }
        else
        {
            Debug.LogWarning($"{this.name}: MinionManager 인스턴스를 찾을 수 없어 미니언을 제거할 수 없습니다.", this);
        }

        if (stageUIManager != null)
        {
            stageUIManager.ShowAnnouncement(stageStartMessage);
        }
        else
        {
            Debug.LogError($"{this.name}: StageUIManager가 인스펙터에 할당되지 않았습니다!", this);
        }
    }

    void Start()
    {
        currentHP = maxHP;
        UpdateHPBar();
        if (coreRenderer != null)
        {
            // 재질(Material)의 인스턴스를 생성하여 원본 재질을 공유하지 않도록 합니다.
            // (이걸 안하면 같은 재질을 쓰는 모든 오브젝트가 빨갛게 변함)
            coreRenderer.material = new Material(coreRenderer.material);
            originalCoreColor = coreRenderer.material.color;
        }
        else
        {
            Debug.LogWarning("DestructibleCore: coreRenderer가 할당되지 않았습니다!", this);
        }
    }

    /// <summary>
    /// 플레이어의 공격이 이 함수를 호출합니다.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;
        UpdateHPBar();
        FlashCore();


        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    private void Die()
    {
        // "코어 파괴!" 이벤트에 연결된 모든 함수를 실행
        OnCoreDestroyed?.Invoke();

        // TODO: 파괴 이펙트(VFX, SFX) 재생

        // 코어 오브젝트 파괴
        Destroy(gameObject);
    }

    // --- 피격 피드백 (DefenseObjective와 동일) ---

    private void FlashCore()
    {
        if (coreRenderer == null) return;
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashCoreCoroutine());
    }

    private IEnumerator FlashCoreCoroutine()
    {
        coreRenderer.material.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        coreRenderer.material.color = originalCoreColor;
        flashCoroutine = null;
    }

    void UpdateHPBar()
    {
        if (hpSlider != null)
        {
            // Slider의 value는 0~1 사이 값이므로 비율로 계산
            hpSlider.value = (float)currentHP / maxHP;
        }
    }
}