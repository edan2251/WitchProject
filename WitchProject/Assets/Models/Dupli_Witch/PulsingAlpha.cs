using UnityEngine;

public class PulsingAlpha : MonoBehaviour
{
    [Tooltip("알파값이 깜빡이는 속도")]
    public float pulseSpeed = 1.0f;

    [Tooltip("최소 알파값 (0~255)")]
    [Range(0, 255)]
    public float minAlpha = 130f;

    [Tooltip("최대 알파값 (0~255)")]
    [Range(0, 255)]
    public float maxAlpha = 170f;

    private Renderer objectRenderer;
    private Material materialInstance; // 원본 머티리얼이 아닌, 이 오브젝트만의 머티리얼 인스턴스
    private Color originalColor;
    private string colorPropertyName = "_BaseColor"; // URP/HDRP 기본값

    void Start()
    {
        // 자식 오브젝트의 Renderer를 찾아옵니다 (유령 모델 등)
        objectRenderer = GetComponentInChildren<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError("Renderer를 찾을 수 없습니다!", this);
            enabled = false; // 스크립트 비활성화
            return;
        }

        // 머티리얼 인스턴스 생성 (프로젝트의 원본 머티리얼 에셋을 변경하지 않기 위함)
        materialInstance = objectRenderer.material;

        // 머티리얼 프로퍼티 이름 찾기 (Standard 렌더러는 "_Color"를 사용)
        if (!materialInstance.HasProperty(colorPropertyName))
        {
            if (materialInstance.HasProperty("_Color"))
            {
                colorPropertyName = "_Color";
            }
            else
            {
                Debug.LogWarning("'_BaseColor' 또는 '_Color' 프로퍼티를 찾지 못했습니다.", this);
                enabled = false;
                return;
            }
        }

        // 알파값을 조절하기 전의 원래 색상 저장
        originalColor = materialInstance.GetColor(colorPropertyName);

        // [중요!] 이 스크립트가 작동하려면
        // 이 머티리얼의 Surface Type(표면 유형)이 "Transparent"(투명)로 
        // 미리 설정되어 있어야 합니다!
    }

    void Update()
    {
        // 1. 알파값을 0-1 스케일로 변환
        float minAlpha01 = minAlpha / 255f;
        float maxAlpha01 = maxAlpha / 255f;

        // 2. 중간값(150)과 진폭(20) 계산
        float midpoint = (minAlpha01 + maxAlpha01) / 2f;
        float amplitude = (maxAlpha01 - minAlpha01) / 2f;

        // 3. Mathf.Sin을 사용하여 -1 ~ +1 사이를 부드럽게 왕복하는 값 생성
        float sinWave = Mathf.Sin(Time.time * pulseSpeed); // -1에서 1 사이

        // 4. 최종 알파값 계산 (예: 0.59 + (1 * 0.08) = 0.67, 0.59 + (-1 * 0.08) = 0.51)
        float newAlpha = midpoint + (sinWave * amplitude);

        // 5. 새 색상 설정 (원래 색상의 RGB값은 유지, Alpha값만 변경)
        Color newColor = originalColor;
        newColor.a = newAlpha;

        // 6. 머티리얼 인스턴스에 최종 색상 적용
        materialInstance.SetColor(colorPropertyName, newColor);
    }
}