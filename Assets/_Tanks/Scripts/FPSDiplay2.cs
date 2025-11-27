using UnityEngine;
using TMPro;  // ← TextMeshProを使う場合はこれ！

public class FPSDisplay2 : MonoBehaviour
{
    public TMP_Text fpsText;          // TextMeshPro用
    public TMP_Text lowestFpsText;    // TextMeshPro用

    private float deltaTime = 0.0f;
    private float lowestFps = Mathf.Infinity;

    void Update()
    {
        // 平滑化したフレーム時間を計算
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        // FPS計算
        float fps = 1.0f / deltaTime;

        // 最低FPS更新
        if (fps < lowestFps)
        {
            lowestFps = fps;
        }

        // TextMeshProに表示
        if (fpsText != null)
            fpsText.text = $"FPS: {fps:F1}";

        if (lowestFpsText != null)
            lowestFpsText.text = $"Lowest FPS: {lowestFps:F1}";
    }
}
