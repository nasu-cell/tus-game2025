using System;
using UnityEngine;
using Tanks.Complete;
using Camera = UnityEngine.Camera; // UnityEngine.Camera を明示的に指定

public class HUDManager : MonoBehaviour
{
    // 武器在庫表示
    [SerializeField] private GameObject Player1Stock;
    [SerializeField] private GameObject Player2Stock;
    [SerializeField] private GameManager gameManager;
    
    // 💡 修正: 勝利数表示を Player 3 まで拡張
    [Header("Win Count Display")]
    [SerializeField] private GameObject player1WinCount; 
    [SerializeField] private GameObject player2WinCount;
    [SerializeField] private GameObject player3WinCount; // 🏆 追加
    
    // 💡 追加: HP表示
    [Header("HP Display")]
    [SerializeField] private GameObject player1HP; 
    [SerializeField] private GameObject player2HP; 
    
    // ミニマップ設定
    [Header("Minimap Settings")]
    [SerializeField] private Camera player1Camera; 
    [SerializeField] private Camera player2Camera; 
    [SerializeField] private GameObject minimapImageRoot; 

    private PlayerStock player1StockComponent;
    private PlayerStock player2StockComponent;
    
    // 💡 修正: PlayerWinCount コンポーネントへの参照を Player 3 まで拡張
    private PlayerWinCount player1WinCountComponent;
    private PlayerWinCount player2WinCountComponent;
    private PlayerWinCount player3WinCountComponent; // 🏆 追加
    
    // 💡 追加: PlayerHP コンポーネントへの参照
    private PlayerHP player1HPComponent;
    private PlayerHP player2HPComponent;

    private void Start()
    {
        // 💡 修正点: 全戦車プレハブの子カメラを非アクティブにする処理
        if (gameManager != null)
        {
            GameObject[] tankPrefabs = new GameObject[]
            {
                gameManager.m_Tank1Prefab,
                gameManager.m_Tank2Prefab,
                gameManager.m_Tank3Prefab,
                gameManager.m_Tank4Prefab
            };
            
            for (int i = 0; i < tankPrefabs.Length; i++)
            {
                GameObject tankPrefab = tankPrefabs[i];
                if (tankPrefab != null)
                {
                    Camera prefabCam = tankPrefab.GetComponentInChildren<Camera>(true);
                    
                    if (prefabCam != null)
                    {
                        prefabCam.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        // --- 既存の初期化ロジック ---

        // HUDの在庫表示を最初非表示
        if (Player1Stock != null) Player1Stock.SetActive(false);
        if (Player2Stock != null) Player2Stock.SetActive(false);
        
        // 🏆 修正: 勝利数表示を最初非表示
        if (player1WinCount != null) player1WinCount.SetActive(false);
        if (player2WinCount != null) player2WinCount.SetActive(false);
        if (player3WinCount != null) player3WinCount.SetActive(false); // 🏆 追加
        
        // 💡 追加: HP表示を最初非表示
        if (player1HP != null) player1HP.SetActive(false);
        if (player2HP != null) player2HP.SetActive(false);
        
        // MinimapImage のルートオブジェクトも非アクティブ化
        if (minimapImageRoot != null) minimapImageRoot.SetActive(false);

        // カメラをデフォルトで無効化
        if (player1Camera != null) player1Camera.gameObject.SetActive(false);
        if (player2Camera != null) player2Camera.gameObject.SetActive(false);


        // PlayerStockコンポーネント取得
        if (Player1Stock != null)
            player1StockComponent = Player1Stock.GetComponent<PlayerStock>();
        if (Player2Stock != null)
            player2StockComponent = Player2Stock.GetComponent<PlayerStock>();

        // 🏆 修正: PlayerWinCountコンポーネント取得
        if (player1WinCount != null)
            player1WinCountComponent = player1WinCount.GetComponent<PlayerWinCount>();
        if (player2WinCount != null)
            player2WinCountComponent = player2WinCount.GetComponent<PlayerWinCount>();
        if (player3WinCount != null) // 🏆 追加
            player3WinCountComponent = player3WinCount.GetComponent<PlayerWinCount>();

        // 💡 追加: PlayerHPコンポーネント取得
        if (player1HP != null)
            player1HPComponent = player1HP.GetComponent<PlayerHP>();
        if (player2HP != null)
            player2HPComponent = player2HP.GetComponent<PlayerHP>();
            
        // GameManager購読（イベント購読はゲームマネージャーが存在する場合のみ）
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;

            foreach (var tank in gameManager.m_SpawnPoints)
            {
                if (tank != null)
                {
                    // MODIFIED: TankManager のイベント購読 (武器在庫)
                    tank.OnWeaponStockChangedEvent += HandleWeaponStockChanged; 
                    
                    // 💡 追加: TankManager のイベント購読 (HP変更)
                    tank.OnHealthChanged += HandlePlayerHPChanged;
                    
                    // 💡 追加: TankManager のイベント購読 (勝利数変更)
                    tank.OnWinCountChanged += HandlePlayerWinCountChanged;
                    
                    // 修正: TankManager のカメラスポーンイベントを購読
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
                    
                    // 💡 追加: 購読解除 (HP変更)
                    tank.OnHealthChanged -= HandlePlayerHPChanged;
                    
                    // 💡 追加: 購読解除 (勝利数変更)
                    tank.OnWinCountChanged -= HandlePlayerWinCountChanged;
                    
                    // 修正: 購読解除
                    tank.OnMinimapCameraSpawned -= HandleMinimapCameraSpawned;
                }
            }
        }
    }
    
    // ====================================
    // 🏆 修正: 勝利数変更ハンドラメソッド (Player 3 対応)
    // ====================================
    /// <summary>
    /// プレイヤー番号とラウンド勝利数を受け取り、対応する PlayerWinCount コンポーネントを更新します。
    /// </summary>
    /// <param name="playerNumber">勝利数が変更されたプレイヤーの番号 (1, 2, 3...)</param>
    /// <param name="winCount">現在の勝利数</param>
    private void HandlePlayerWinCountChanged(int playerNumber, int winCount)
    {
        // プレイヤー番号に基づき、ターゲットの PlayerWinCount コンポーネントを選択
        if (playerNumber == 1 && player1WinCountComponent != null)
        {
            player1WinCountComponent.UpdateWinCount(winCount);
        }
        else if (playerNumber == 2 && player2WinCountComponent != null)
        {
            player2WinCountComponent.UpdateWinCount(winCount);
        }
        else if (playerNumber == 3 && player3WinCountComponent != null) // 🏆 追加
        {
            player3WinCountComponent.UpdateWinCount(winCount);
        }
        
        // Debug.Log($"[HUDManager] Player{playerNumber} Win Count updated: {winCount}");
    }

    // 💡 追加: HP変更ハンドラメソッド
    private void HandlePlayerHPChanged(int playerNumber, float normalizedHealth)
    {
        // プレイヤー番号に基づき、ターゲットの PlayerHP コンポーネントを選択
        // playerNumber は TankManager で 1, 2, 3... と設定されている
        if (playerNumber == 1 && player1HPComponent != null)
        {
            // UpdateHPSlider メソッドを呼び出し、正規化されたHP値を渡す
            player1HPComponent.UpdateHPSlider(normalizedHealth);
        }
        else if (playerNumber == 2 && player2HPComponent != null)
        {
            // UpdateHPSlider メソッドを呼び出し、正規化されたHP値を渡す
            player2HPComponent.UpdateHPSlider(normalizedHealth);
        }
        // Player 3 以降の HP 表示は現在のコードでは省略
    }


    private void HandleMinimapCameraSpawned(int controlIndex, Camera minimapCamera)
    {
        if (controlIndex == 1)
        {
            // プレイヤー1のカメラがまだ設定されていない場合のみ割り当てる
            if (player1Camera == null)
            {
                player1Camera = minimapCamera;
                
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
                
                // 取得直後は、RoundPlaying状態になるまで無効にしておく
                player2Camera.gameObject.SetActive(false);
            }
        }
        // Player 3 以降の MinimapCamera のロジックは現在のコードでは省略
    }

    private void HandleGameStateChanged(GameManager.GameLoopState newState)
    {
        // プレイ中かどうかを判定
        bool isPlaying = newState == GameManager.GameLoopState.RoundPlaying;

        // HUDの表示/非表示を切り替える
        if (Player1Stock != null) Player1Stock.SetActive(isPlaying);
        if (Player2Stock != null) Player2Stock.SetActive(isPlaying);
        
        // 🏆 修正: 勝利数表示の表示/非表示を切り替える
        if (player1WinCount != null) player1WinCount.SetActive(isPlaying);
        if (player2WinCount != null) player2WinCount.SetActive(isPlaying);
        if (player3WinCount != null) player3WinCount.SetActive(isPlaying); // 🏆 追加
        
        // 💡 追加: HP表示の表示/非表示を切り替える
        if (player1HP != null) player1HP.SetActive(isPlaying);
        if (player2HP != null) player2HP.SetActive(isPlaying);

        // MinimapImage のルートオブジェクトの表示/非表示を切り替える
        if (minimapImageRoot != null) minimapImageRoot.SetActive(isPlaying);
        
        // プレイ中のみミニマップカメラを有効化/無効化
        if (player1Camera != null)
        {
            player1Camera.gameObject.SetActive(isPlaying); 
        }
        
        if (player2Camera != null)
        {
            // プレイヤー2のカメラはここでは常に非アクティブにしておく
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
        // ⚠ 注意: ここでは ControlIndex (入力インデックス) を使用
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