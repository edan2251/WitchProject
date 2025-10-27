using UnityEngine;

public class DefenseTargetHealth : MonoBehaviour
{
    public int currentHealth = 100;
    public int maxHealth = 100;

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log(gameObject.name + "이(가) " + damage + " 피해를 입었습니다. 남은 체력: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " 파괴됨!");
        // TODO: 게임 오버 로직 또는 웨이브 실패 로직 실행
        Destroy(gameObject);
    }
}