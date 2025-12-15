using UnityEngine;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    // === Inspectorで設定するUIコンポーネント ===
    // 1. ReadyButtonの子のTextMeshPro (Ready/Cancelの表示切替用)
    public TextMeshProUGUI readyButtonText;
    
    // 2. プレイヤーの状態を表示するTextMeshPro (MyReadyの表示切替用)
    public TextMeshProUGUI myReadyText;
    
    // 3. 相手の状態を表示するTextMeshPro (今回のロジックでは変更しないが、管理用として)
    public TextMeshProUGUI opponentReadyText; 

    // === 内部の状態管理変数 ===
    private bool isPlayerReady = false; 

    void Start()
    {
        // ゲーム開始時の初期状態を設定する
        // Start()時に isPlayerReady は false です
        
        readyButtonText.text = "READY";
        myReadyText.text = "Not Ready";
        // opponentReadyText.text = "Not Ready"; // 必要に応じて
    }

    // ReadyButtonのOnClick()イベントから呼び出される関数
    public void ToggleReadyState()
    {
        if (isPlayerReady == false)
        {
            // === Not Ready -> Ready への切り替え ===
            
            // 1. 子オブジェクトの表示を "Ready" から "Cancel" に変更
            readyButtonText.text = "CANCEL";
            
            // 2. MyReadyの表示を "Not Ready" から "Ready" に変更
            myReadyText.text = "Ready";
            
            // 3. 状態を true にする
            isPlayerReady = true;
        }
        else // isPlayerReady == true の時
        {
            // === Ready -> Not Ready への切り替え ===
            
            // 1. 子オブジェクトの表示を "Cancel" から "Ready" に変更
            readyButtonText.text = "READY";
            
            // 2. MyReadyの表示を "Ready" から "Not Ready" に変更
            myReadyText.text = "Not Ready";
            
            // 3. 状態を false にする
            isPlayerReady = false;
        }
        
        Debug.Log("Player Ready State: " + isPlayerReady);
    }
}