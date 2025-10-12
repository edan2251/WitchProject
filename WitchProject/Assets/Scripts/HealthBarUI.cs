using UnityEngine;
using UnityEngine.UI; 

public class HealthBarUI : MonoBehaviour
{
    [HideInInspector] public Transform target;
    [HideInInspector] public Enemy targetEnemy; 
    [HideInInspector] public Camera uiCamera;

    public Slider healthSlider; 

    public Vector3 offset = new Vector3(0, 2.5f, 0);

    public LayerMask obstructionMask; // 장애물 레이어 마스크 (Inspector에서 설정)
    public float raycastOffset = 0.5f; // 레이캐스트 시작점을 머리 위로 살짝 띄울 높이

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        // 적이 안보일때 UI 숨기기
        if (target == null || uiCamera == null) return;

        Vector3 targetWorldPosition = target.position + offset;

        Vector3 screenPos = uiCamera.WorldToScreenPoint(targetWorldPosition);

        bool isObscured = CheckObstruction(targetWorldPosition);

        if (screenPos.z < 0 || isObscured)
        {
            canvasGroup.alpha = 0;
        }
        else
        {
            canvasGroup.alpha = 1;
            transform.position = screenPos;
        }

        //UI 크기조절
        float distance = Vector3.Distance(uiCamera.transform.position, target.position);

        const float referenceDistance = 50f;

        float scaleFactor = referenceDistance / distance;

        transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
    }

    // 매니저가 호출하여 체력을 업데이트합니다.
    public void UpdateHealth(int currentHp, int maxHp)
    {
        float ratio = (float)currentHp / maxHp;

        if (healthSlider != null)
        {
            // uGUI Slider 업데이트
            healthSlider.maxValue = maxHp;
            healthSlider.value = currentHp;
        }
    }

    bool CheckObstruction(Vector3 targetPos)
    {
        Vector3 rayStart = uiCamera.transform.position;

        Vector3 rayEnd = targetPos;

        Vector3 direction = rayEnd - rayStart;

        float distance = direction.magnitude;

        if (Physics.Raycast(rayStart, direction, out RaycastHit hit, distance, obstructionMask))
        {
            if (hit.transform != target)
            {
                return true;
            }
        }

        return false;
    }
}