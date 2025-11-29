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
    // 💡 追加: PlayerHP コンポーネントへの参照
    private PlayerHP player1HPComponent;
    private PlayerHP player2HPComponent;

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
                    
                    // 修正: 購読解除
                    tank.OnMinimapCameraSpawned -= HandleMinimapCameraSpawned;
                }
            }
        }
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
        // 3人以上のプレイヤーが想定される場合、ここにelse ifを追加
        
        // Debug.Log($"[HUDManager] Player{playerNumber} HP updated: {normalizedHealth}");
    }


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