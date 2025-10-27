using UnityEngine;

/// <summary>
/// 소환된 미니언들의 부모 오브젝트를 관리하고, 모든 미니언을 제거하는 기능을 제공합니다. (싱글톤)
/// </summary>
public class MinionManager : MonoBehaviour
{
    // --- 싱글톤 설정 ---
    public static MinionManager Instance { get; private set; }

    // --- 미니언 부모 ---
    [Header("Minion Parent Settings")]
    [Tooltip("소환된 미니언들의 부모가 될 오브젝트의 이름입니다.")]
    [SerializeField]
    private string parentObjectName = "Minions"; // 부모 오브젝트 이름

    public Transform MinionsParent { get; private set; } // 외부에서 부모 Transform에 접근 가능

    void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 지정된 이름의 부모 오브젝트를 찾거나 새로 생성합니다.
        FindOrCreateParent();
    }

    /// <summary>
    /// 지정된 이름의 게임 오브젝트를 찾아 MinionsParent로 설정하거나, 없으면 새로 만듭니다.
    /// </summary>
    private void FindOrCreateParent()
    {
        GameObject parentGO = GameObject.Find(parentObjectName);
        if (parentGO == null)
        {
            parentGO = new GameObject(parentObjectName);
            Debug.Log($"MinionManager: '{parentObjectName}' 오브젝트를 찾을 수 없어 새로 생성했습니다.", this);
        }
        MinionsParent = parentGO.transform;
    }

    /// <summary>
    /// MinionsParent 오브젝트 아래의 모든 자식 오브젝트(미니언)를 파괴합니다.
    /// </summary>
    public void ClearAllMinions()
    {
        if (MinionsParent == null)
        {
            Debug.LogWarning("MinionManager: MinionsParent가 설정되지 않아 미니언을 제거할 수 없습니다.", this);
            // 만약을 대비해 다시 찾아봅니다.
            FindOrCreateParent();
            if (MinionsParent == null) return; // 그래도 없으면 종료
        }

        Debug.Log($"MinionManager: '{MinionsParent.name}' 아래의 모든 자식 오브젝트를 제거합니다.");

        // 자식들을 거꾸로 순회하며 파괴 (정방향 순회 시 인덱스 문제 발생 가능)
        for (int i = MinionsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(MinionsParent.GetChild(i).gameObject);
        }
    }
}