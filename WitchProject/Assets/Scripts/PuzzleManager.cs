using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 상호작용 가능한 모든 오브젝트의 상태를 관리하고,
/// 퍼즐이 완료되었을 때 특정 이벤트를 실행합니다.
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    //  싱글톤 패턴: 어디서든 쉽게 접근할 수 있도록 Instance를 설정합니다.
    public static PuzzleManager Instance { get; private set; }

    [Header("퍼즐 오브젝트 설정")]
    [Tooltip("플레이어가 활성화해야 하는 모든 횃불/오브젝트를 할당하세요.")]
    public List<InteractableObject> interactableObjects = new List<InteractableObject>();

    [Header("성공 시 이벤트 설정")]
    //  이 부분이 List로 변경되었습니다.
    [Tooltip("모든 오브젝트가 활성화되면 파괴될 오브젝트들입니다. (예: 웨이브 차단벽)")]
    public List<GameObject> objectsToDestroy = new List<GameObject>();

    [Tooltip("모든 오브젝트가 활성화되면 활성화될 오브젝트들입니다. (예: 다음 웨이브 스포너, 보물 상자)")]
    public List<GameObject> objectsToActivate = new List<GameObject>();

    [Header("상태")]
    public bool isPuzzleCompleted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // 안전을 위해 초기 상태를 확인합니다.
        CheckPuzzleCompletion();
    }

    /// <summary>
    /// InteractableObject에 의해 호출되며, 모든 퍼즐이 완료되었는지 확인합니다.
    /// </summary>
    public void CheckPuzzleCompletion()
    {
        if (isPuzzleCompleted) return;

        // LINQ 사용: 리스트의 모든 요소(o)가 o.isActive == true인지 확인
        bool allActive = interactableObjects.All(o => o != null && o.isActive);

        if (allActive)
        {
            CompletePuzzle();
        }
    }

    /// <summary>
    /// 퍼즐 완료 시 실행되는 최종 이벤트 로직입니다.
    /// </summary>
    private void CompletePuzzle()
    {
        isPuzzleCompleted = true;
        Debug.Log(" 퍼즐 완료! 다음 단계로 넘어갑니다.");

        // 1. 파괴할 오브젝트 리스트를 순회하며 모두 파괴
        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj != null)
            {
                Destroy(obj);
                Debug.Log(obj.name + " 파괴됨.");
            }
        }

        // 2. 활성화할 오브젝트 리스트를 순회하며 모두 활성화
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log(obj.name + " 활성화됨.");
            }
        }

        // 이 후 스크립트 비활성화 (더 이상 실행할 필요가 없으므로)
        enabled = false;
    }
}