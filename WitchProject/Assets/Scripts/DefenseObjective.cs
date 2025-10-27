using UnityEngine;
using UnityEngine.Events; // 이벤트 사용을 위해 추가

public class DefenseObjective : MonoBehaviour
{
    [Header("Objective Stats")]
    public int maxHP = 1000;
    public int currentHP;

    // [추가] 오브젝트가 파괴되었을 때 스테이지 매니저에게 알리기 위한 이벤트
    public UnityEvent OnObjectDestroyed;

    // TODO: UI 슬라이더를 연결해서 체력바를 표시할 수 있습니다.
    // public Slider hpSlider; 

    void Start()
    {
        currentHP = maxHP;
        // UpdateHPBar();
    }

    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;
        // UpdateHPBar();

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

        // TODO: 파괴 이펙트(VFX, SFX) 재생

        // 오브젝트 비활성화 (Destroy 대신)
        gameObject.SetActive(false);
    }

    /*
    void UpdateHPBar()
    {
        if (hpSlider != null)
        {
            hpSlider.value = (float)currentHP / maxHP;
        }
    }
    */
}