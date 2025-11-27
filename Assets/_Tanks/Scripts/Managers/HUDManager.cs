using System;
using UnityEngine;
using Tanks.Complete;
using Camera = UnityEngine.Camera; // UnityEngine.Camera を明示的に指定

public class HUDManager : MonoBehaviour
{
    [SerializeField] private GameObject Player1Stock;
    [SerializeField] private GameObject Player2Stock;
    [SerializeField] private GameManager gameManager;

    [Header("Minimap Settings")]
    [SerializeField] private Camera player1Camera; 
    // 💡 追加: プレイヤー2用のカメラフィールド
    [SerializeField] private Camera player2Camera; 
    [SerializeField] private GameObject minimapImageRoot; 

    private PlayerStock player1StockComponent;
    private PlayerStock player2StockComponent;

    private void Start()
    {
        // 💡 修正点: 全戦車プレハブの子カメラを非アクティブにする処理
        if (gameManager != null)
        {
            // GameManager の m_Tank{i}Prefab を格納する配列
            GameObject[] tankPrefabs = new GameObject[]
            {
                gameManager.m_Tank1Prefab,
                gameManager.m_Tank2Prefab,
                gameManager.m_Tank3Prefab,
                gameManager.m_Tank4Prefab
            };
            
            // 全ての戦車プレハブについて処理
            for (int i = 0; i < tankPrefabs.Length; i++)
            {
                GameObject tankPrefab = tankPrefabs[i];
                if (tankPrefab != null)
                {
                    // 子オブジェクトにある Camera コンポーネントを取得（非アクティブなものも含む）
                    Camera prefabCam = tankPrefab.GetComponentInChildren<Camera>(true);
                    
                    if (prefabCam != null)
                    {
                        // 取得した Camera コンポーネントを持つ GameObject を非アクティブにする
                        prefabCam.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        // --- 既存の初期化ロジック ---

        // HUDの在庫表示を最初非表示
        if (Player1Stock != null) Player1Stock.SetActive(false);
        if (Player2Stock != null) Player2Stock.SetActive(false);
        
        // 💡 修正点: MinimapImage のルートオブジェクトも非アクティブ化
        if (minimapImageRoot != null) minimapImageRoot.SetActive(false);

        // カメラをデフォルトで無効化
        if (player1Camera != null) player1Camera.gameObject.SetActive(false);
        // 💡 追加: プレイヤー2のカメラもデフォルトで無効化
        if (player2Camera != null) player2Camera.gameObject.SetActive(false);


        // PlayerStockコンポーネント取得
        if (Player1Stock != null)
            player1StockComponent = Player1Stock.GetComponent<PlayerStock>();
        if (Player2Stock != null)
            player2StockComponent = Player2Stock.GetComponent<PlayerStock>();

        // GameManager購読（イベント購読はゲームマネージャーが存在する場合のみ）
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;

            foreach (var tank in gameManager.m_SpawnPoints)
            {
                if (tank != null)
                {
                    // MODIFIED: TankManager のイベント購読
                    tank.OnWeaponStockChangedEvent += HandleWeaponStockChanged; 
                    
                    // 💡 修正: TankManager のカメラスポーンイベントを購読 (引数に ControlIndex を含む)
                    tank.OnMinimapCameraSpawned += HandleMinimapCameraSpawned;
                }
            }
        }
        else
        {
            Debug.LogWarning("GameManagerがHUDManagerに設定されていません。");
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= HandleGameStateChanged;

            foreach (var tank in gameManager.m_SpawnPoints)
            {
                if (tank != null)
                {
                    // MODIFIED: 購読解除
                    tank.OnWeaponStockChangedEvent -= HandleWeaponStockChanged; 
                    
                    // 💡 修正: 購読解除
                    tank.OnMinimapCameraSpawned -= HandleMinimapCameraSpawned;
                }
            }
        }
    }

    // 💡 修正/追加: カメラ取得用のハンドラメソッド (ControlIndex で振り分け)
    private void HandleMinimapCameraSpawned(int controlIndex, Camera minimapCamera)
    {
        if (controlIndex == 1)
        {
            // プレイヤー1のカメラがまだ設定されていない場合のみ割り当てる
            if (player1Camera == null)
            {
                player1Camera = minimapCamera;
                Debug.Log("[HUDManager] 🔗 実行時に Player 1 Minimap Camera を取得しました。");
                
                // 取得直後は、RoundPlaying状態になるまで無効にしておく
                player1Camera.gameObject.SetActive(false);
            }
        }
        else if (controlIndex == 2)
        {
            // プレイヤー2のカメラがまだ設定されていない場合のみ割り当てる
            if (player2Camera == null)
            {
                player2Camera = minimapCamera;
                Debug.Log("[HUDManager] 🔗 実行時に Player 2 Minimap Camera を取得しました。");
                
                // 取得直後は、RoundPlaying状態になるまで無効にしておく
                player2Camera.gameObject.SetActive(false);
            }
        }
    }

    private void HandleGameStateChanged(GameManager.GameLoopState newState)
    {
        // プレイ中かどうかを判定
        bool isPlaying = newState == GameManager.GameLoopState.RoundPlaying;

        // HUDの表示/非表示を切り替える
        if (Player1Stock != null) Player1Stock.SetActive(isPlaying);
        if (Player2Stock != null) Player2Stock.SetActive(isPlaying);

        // 💡 修正点: MinimapImage のルートオブジェクトの表示/非表示を切り替える
        if (minimapImageRoot != null) minimapImageRoot.SetActive(isPlaying);
        
        // プレイ中のみミニマップカメラを有効化/無効化
        // 💡 修正/確認: ミニマップに使用するのは player1Camera のみ
        if (player1Camera != null)
        {
            player1Camera.gameObject.SetActive(isPlaying); 
            
            if (isPlaying)
            {
                Debug.Log($"[HUDManager] 📸 Player1 Cameraをアクティブ化しました。");
            }
            else
            {
                Debug.Log($"[HUDManager] 📸 Player1 Cameraを非アクティブ化しました。");
            }
        }
        // 💡 追加: プレイヤー2のカメラは常に非アクティブにしておく
        if (player2Camera != null)
        {
            // 常に非アクティブ、または RoundPlaying のときのみ無効化（必要に応じて）
            player2Camera.gameObject.SetActive(false); 
        }

        Debug.Log($"[HUDManager] GameLoopState changed to: {newState}, HUD visible: {isPlaying}");
    }

    //====================================
    // MODIFIED: WeaponType を利用した在庫表示更新
    //====================================
    private void HandleWeaponStockChanged(int controlIndex, WeaponType type, WeaponStockData stockData) 
    {
        // プレイヤーインデックスに基づき、ターゲットの HUD コンポーネントを選択
        PlayerStock target = controlIndex == 1 ? player1StockComponent : player2StockComponent;
        if (target == null || stockData == null) return;

        // 武器の種類に基づき、適切な在庫更新メソッドを呼び出す
        if (type == WeaponType.Shell)
        {
            target.UpdateShellStock(stockData.CurrentQuantity);
        }
        else if (type == WeaponType.Mine)
        {
            target.UpdateMineStock(stockData.CurrentQuantity);
        }

        Debug.Log($"[HUDManager] Player{controlIndex} {type} updated: {stockData.CurrentQuantity}");
    }
}