using UnityEngine;

/// <summary>
/// 이 트리거 영역에 플레이어가 들어오면 플레이어의 Die() 함수를 호출합니다.
/// </summary>
[RequireComponent(typeof(Collider))] // 콜라이더가 반드시 있도록 강제
public class DeathZone : MonoBehaviour
{
    private void Awake()
    {
        // 트리거로 작동하도록 콜라이더 설정 확인 (없으면 경고)
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"DeathZone ({gameObject.name}): 콜라이더의 Is Trigger가 체크되어 있지 않습니다. 낙사 판정이 작동하지 않을 수 있습니다.", this);
            // 필요하다면 여기서 강제로 true로 설정할 수도 있습니다:
            // col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 오브젝트에서 PlayerController 컴포넌트를 찾습니다.
        PlayerController player = other.GetComponent<PlayerController>();

        // PlayerController 컴포넌트를 찾았다면 (즉, 플레이어라면)
        if (player != null)
        {
            Debug.Log($"플레이어가 DeathZone ({gameObject.name})에 진입했습니다. Die() 함수를 호출합니다.");
            // 플레이어의 Die() 함수를 호출합니다.
            player.Die();
        }
    }
}