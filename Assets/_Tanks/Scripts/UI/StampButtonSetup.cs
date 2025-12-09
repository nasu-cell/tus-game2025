using UnityEngine;
using UnityEngine.UI;

public class StampButtonSetup : MonoBehaviour
{
    // === Inspectorで設定 ===
    public string StampText = ""; // [設定1] 送信するスタンプの文字列

    void Start()
    {
        // シーン内のStampControllerを探す
        StampController controller = FindObjectOfType<StampController>();
        if (controller == null) return;

        Button button = GetComponent<Button>();
        
        // ボタンがクリックされたら、Controllerの関数を呼び出し、StampTextを渡す
        if (button != null)
        {
            button.onClick.AddListener(() => controller.OnStampSelected(StampText));
        }
    }
}