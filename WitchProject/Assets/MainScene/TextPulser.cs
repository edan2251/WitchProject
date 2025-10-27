using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스 추가

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextPulser : MonoBehaviour
{
    public float minAlpha = 0.3f;
    public float maxAlpha = 1.0f;
    public float pulseSpeed = 1.0f;

    private TextMeshProUGUI tmpText;
    private Color startColor;

    void Start()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        startColor = tmpText.color;
    }

    void Update()
    {
        // Sin 함수를 사용하여 0과 1 사이를 부드럽게 왕복하는 값을 만듭니다.
        // (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) / 2.0f
        // -1 ~ 1 범위를 0 ~ 2 범위로, 2로 나누어 0 ~ 1 범위로 만듭니다.
        float normalizedTime = (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) / 2.0f;

        // Mathf.Lerp를 사용하여 minAlpha와 maxAlpha 사이를 왕복하도록 매핑합니다.
        float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, normalizedTime);

        // 텍스트의 실제 알파 값을 변경합니다.
        tmpText.color = new Color(startColor.r, startColor.g, startColor.b, currentAlpha);
    }
}