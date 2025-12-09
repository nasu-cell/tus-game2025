using UnityEngine;
using UnityEngine.UI; // Imageコンポーネントを使うために必要

public class PlayerWinCount : MonoBehaviour
{
    // 💡 WinImages: Win1とWin2のImageを参照する配列 (サイズを2に変更)
    [SerializeField]
    private Image[] WinImages = new Image[3]; // 配列サイズを 2 に変更

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 初期状態をリセットする場合、ここで UpdateWinCount(0) などを呼び出せます。
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 🏆 UpdateWinCountメソッド: 勝利数だけ Win1 から Win2 の Image オブジェクトを点灯（有効化）する
    /// <summary>
    /// 指定された勝利数に基づいて、Win1 と Win2 の Image オブジェクトを有効化します。
    /// </summary>
    /// <param name="currentWinCount">現在の勝利数（0, 1, または 2）</param>
    public void UpdateWinCount(int currentWinCount)
    {
        // 勝利数が 0 から 配列の長さ (2) の間に収まるように制限
        int effectiveCount = Mathf.Clamp(currentWinCount, 0, WinImages.Length);

        for (int i = 0; i < WinImages.Length; i++)
        {
            Image img = WinImages[i];
            
            if (img != null)
            {
                // インデックス i が有効な勝利数 (effectiveCount) 未満であれば有効化 (true)
                bool shouldBeActive = (i < effectiveCount);
                
                // ImageコンポーネントがアタッチされているGameObjectの有効/無効を切り替える
                img.gameObject.SetActive(shouldBeActive);
            }
        }
        
        Debug.Log("勝利数を " + effectiveCount + " に更新しました。Win1とWin2を制御しています。");
    }
}