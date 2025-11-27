using UnityEngine;
using TMPro;
using Tanks.Complete; // GameManagerがここにある前提

public class FPSDisplay : MonoBehaviour
{
    [Header("TextMeshPro Texts")]
    public TMP_Text fpsText;
    public TMP_Text lowestFpsText;

    [Header("GameManager参照")]
    public GameManager gameManager;

    private float deltaTime = 0.0f;
    private float lowestFps = Mathf.Infinity;
    private bool isActive = false;

    private void Start()
    {
        // GameManagerイベント購読
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;
        }
        else
        {
            Debug.LogWarning("FPSDisplay: GameManagerが設定されていません。");
        }

        // 最初は非表示
        SetTextVisible(false);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void Update()
    {
        if (!isActive)
            return; // RoundPlaying中以外は動作停止

        // FPS計測
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        if (fps < lowestFps)
            lowestFps = fps;

        // Text更新
        if (fpsText != null)
            fpsText.text = $"FPS: {fps:F1}";
        if (lowestFpsText != null)
            lowestFpsText.text = $"Lowest FPS: {lowestFps:F1}";
    }

    // ===============================
    // GameManagerから状態変化を受け取る
    // ===============================
    private void HandleGameStateChanged(GameManager.GameLoopState newState)
    {
        isActive = (newState == GameManager.GameLoopState.RoundPlaying);
        SetTextVisible(isActive);

        if (isActive)
        {
            lowestFps = Mathf.Infinity; // Round開始時にリセット
        }

        Debug.Log($"[FPSDisplay] 状態変化: {newState}, 表示中: {isActive}");
    }

    // ===============================
    // TMP_Textの表示切り替え
    // ===============================
    private void SetTextVisible(bool visible)
    {
        if (fpsText != null)
            fpsText.gameObject.SetActive(visible);
        if (lowestFpsText != null)
            lowestFpsText.gameObject.SetActive(visible);
    }
}
