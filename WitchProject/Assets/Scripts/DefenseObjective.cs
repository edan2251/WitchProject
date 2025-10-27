using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.UI; // Slider를 사용한다면

public class DefenseObjective : MonoBehaviour
{
    [Header("Objective Stats")]
    public int maxHP = 1000;
    public int currentHP;

    public UnityEvent OnObjectDestroyed;

    [Header("UI")]
    public Slider hpSlider;

    // [★추가★] 피격 효과(VFX) 관련
    [Header("Feedback")]
    [Tooltip("데미지를 입을 때 빨갛게 만들 자식 오브젝트의 Renderer")]
    public Renderer coreRenderer; // "코어 콜리전" 오브젝트를 여기에 할당

    [Tooltip("깜빡이는 시간")]
    public float flashDuration = 0.1f;

    private Color originalCoreColor; // 코어의 원래 색상 저장용
    private Coroutine flashCoroutine;  // 깜빡임 코루틴 중복 방지용

    void Start()
    {
        currentHP = maxHP;
        UpdateHPBar();

        // [★추가★] 시작할 때 코어의 원래 색상을 저장해둡니다.
        if (coreRenderer != null)
        {
            originalCoreColor = coreRenderer.material.color;
        }
        else
        {
            Debug.LogWarning("DefenseObjective: coreRenderer가 할당되지 않았습니다!", this);
        }
    }

    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;
        UpdateHPBar();

        // [★추가★] 데미지를 입으면 코어 깜빡임 코루틴을 실행합니다.
        FlashCore();

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    void Die()
    {
        // 파괴되었음을 스테이지 매니저에게 알림
        OnObjectDestroyed?.Invoke();
        gameObject.SetActive(false);
    }

    // [★추가★] 코어 깜빡임 함수
    private void FlashCore()
    {
        // 1. 코어 렌더러가 없으면 실행 중지
        if (coreRenderer == null) return;

        // 2. 이전에 실행 중이던 깜빡임 코루틴이 있다면 즉시 중지
        // (빠르게 연속으로 맞을 때 색이 꼬이는 것을 방지)
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        // 3. 새 코루틴을 시작하고 변수에 저장
        flashCoroutine = StartCoroutine(FlashCoreCoroutine());
    }

    // [★추가★] 실제 깜빡임 로직 (코루틴)
    private IEnumerator FlashCoreCoroutine()
    {
        // 1. 코어의 색을 빨간색으로 변경
        coreRenderer.material.color = Color.red;

        // 2. 0.1초 (flashDuration) 만큼 대기
        yield return new WaitForSeconds(flashDuration);

        // 3. 코어의 색을 원래 색상으로 복구
        coreRenderer.material.color = originalCoreColor;

        // 4. 코루틴이 끝났으므로 변수를 비움
        flashCoroutine = null;
    }

    
    void UpdateHPBar()
    {
        if (hpSlider != null)
        {
            hpSlider.value = (float)currentHP / maxHP;
        }
    }
    
}