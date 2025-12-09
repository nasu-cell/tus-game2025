using UnityEngine;
using TMPro; 
using System.Collections; 

public class StampController : MonoBehaviour
{
    // === Inspectorで設定 ===
    public GameObject StampSelectionPanel;  // [設定1] 選択肢パネル
    public GameObject StampDisplayPrefab;   // [設定2] 表示用Prefab
    public Transform DisplayParent;         // [設定3] 生成物の親オブジェクト

    // パネルの表示/非表示を切り替える
    public void ToggleSelectionPanel()
    {
        StampSelectionPanel.SetActive(!StampSelectionPanel.activeSelf);
    }

    // 選択肢ボタンから呼び出され、スタンプを表示する
    public void OnStampSelected(string stampText)
    {
        // 選択肢パネルを非表示にする
        StampSelectionPanel.SetActive(false);

        // スタンプを表示する
        DisplayStamp(stampText);
    }

    // スタンプを表示し、5秒後にフェードアウトさせる
    private void DisplayStamp(string text)
    {
        GameObject stampInstance = Instantiate(StampDisplayPrefab, DisplayParent);
        TextMeshProUGUI tmp = stampInstance.GetComponent<TextMeshProUGUI>();
        
        tmp.text = text;
        
        // 5秒間のフェードアウト処理を開始
        StartCoroutine(FadeOutAndDestroy(stampInstance, 5.0f));
    }

    // 5秒間のフェードアウトと破棄のコルーチン（省略）
    private IEnumerator FadeOutAndDestroy(GameObject targetObject, float duration)
    {
        // 最初の4秒間は表示を維持
        yield return new WaitForSeconds(duration - 1.0f); 

        // 最後の1秒間でフェードアウトの処理を記述...
        float fadeDuration = 1.0f;
        float timer = 0f;
        TextMeshProUGUI tmp = targetObject.GetComponent<TextMeshProUGUI>();
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1.0f - (timer / fadeDuration); 
            Color color = tmp.color;
            color.a = alpha;
            tmp.color = color;
            yield return null;
        }
        
        Destroy(targetObject);
    }
}