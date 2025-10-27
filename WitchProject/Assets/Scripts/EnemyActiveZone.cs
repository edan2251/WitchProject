using UnityEngine;
using System.Collections.Generic; // List 사용 시 필요
using UnityEngine.Events;

public class EnemyActivationZone : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private StageUIManager stageUIManager;
    public string stageStartMessage = "[스테이지 1]\n충분히 강해지세요.";

    [Tooltip("이 구역과 함께 활성화/파괴될 모든 오브젝트 (적, 스포너, 테두리 등)")]
    [SerializeField]
    private List<GameObject> managedObjects = new List<GameObject>(); // 현재 구역 관리 오브젝트

    [Tooltip("플레이어 이탈 시 활성화될 다음 스테이지 오브젝트들 (다음 PuzzleManager, 횃불 3개, 다음 스포너 등)")]
    [SerializeField]
    private List<GameObject> objectsToActivateOnExit = new List<GameObject>();

    [Tooltip("플레이어 이탈 시 실행될 유니티 이벤트")]
    [SerializeField]
    private UnityEvent onPlayerExitEvent;

    [Header("UI References")] // [★추가★] 헤더
    [Tooltip("안전 지대 경고 UI를 관리하는 매니저 (선택 사항)")]
    [SerializeField]
    private SafeAreaWarningManager warningManager;

    // [선택 사항] 테두리 렌더러 (자동으로 리스트에 추가하기 위함)
    private LineRenderer boundaryRenderer;

    private bool playerInside = false; // 플레이어가 안에 있는지 추적

    void Awake()
    {
        // 테두리 렌더러가 있다면 자동으로 리스트에 추가
        boundaryRenderer = GetComponent<LineRenderer>();
        if (boundaryRenderer != null && !managedObjects.Contains(boundaryRenderer.gameObject))
        {
            // Note: LineRenderer는 GameObject가 아니라 컴포넌트이므로 gameObject를 추가
            // 하지만 테두리 오브젝트가 별도로 있다면 그 오브젝트를 넣어야 함
            // 지금은 LineRenderer가 같은 오브젝트에 있다고 가정
        }
    }


    void Start()
    {
        if (stageUIManager != null)
        {
            stageUIManager.ShowAnnouncement(stageStartMessage);
        }
        else
        {
            Debug.LogError($"{this.name}: StageUIManager가 인스펙터에 할당되지 않았습니다!", this);
        }

        // 1. 현재 관리 대상 오브젝트 비활성화 (플레이어 진입 전까지)
        foreach (GameObject obj in managedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        // 2. 다음 단계 오브젝트도 시작 시 비활성화 (안전장치. Unity 씬에서 미리 꺼두는 것이 일반적)
        foreach (GameObject obj in objectsToActivateOnExit)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        // 테두리 비활성화 (만약 LineRenderer가 관리 대상에 포함된다면)
        if (boundaryRenderer != null)
        {
            boundaryRenderer.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 들어왔고, 아직 활성화되지 않았다면
        if (!playerInside && other.CompareTag("PlayerTriggerZone"))
        {
            playerInside = true;

            // 모든 관리 대상 오브젝트 활성화
            foreach (GameObject obj in managedObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
            // 테두리 활성화
            if (boundaryRenderer != null)
            {
                boundaryRenderer.enabled = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어가 (이 큰 구역을) 나갔다면
        if (playerInside && other.CompareTag("PlayerTriggerZone"))
        {
            playerInside = false;

            // 1. 다음 단계 오브젝트 리스트 활성화 (동일)
            ActivateNextPhaseObjects();

            // 2. 유니티 이벤트 실행 (동일)
            onPlayerExitEvent?.Invoke();

            // 3.  안전 지대 경고 텍스트 숨기기
            if (warningManager != null)
            {
                warningManager.HideWarning();
            }

            // 4. 현재 구역 및 오브젝트 파괴 (동일)
            DestroyZoneAndObjects();
        }
    }

    /// <summary>
    /// 플레이어 이탈 시 다음 단계의 오브젝트들을 활성화합니다.
    /// </summary>
    private void ActivateNextPhaseObjects()
    {
        foreach (GameObject obj in objectsToActivateOnExit)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 관리 대상 오브젝트들과 이 트리거 존 자체를 파괴합니다.
    /// </summary>
    private void DestroyZoneAndObjects()
    {
        // 모든 관리 대상 오브젝트 파괴
        foreach (GameObject obj in managedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        // 이 트리거 존 오브젝트 자체도 파괴
        Destroy(gameObject);
    }

    // --- Gizmos (이전과 동일) ---
    // 경계선 시각화가 별도 스크립트(GroundProjection...)로 분리되었으므로,
    // 이 스크립트의 OnDrawGizmos는 필요 없습니다. (지워도 됨)
}