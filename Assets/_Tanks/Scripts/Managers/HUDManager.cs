using System;
using UnityEngine;
using Tanks.Complete;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private GameObject Player1Stock;
    [SerializeField] private GameObject Player2Stock;
    [SerializeField] private GameManager gameManager;

    private PlayerStock player1StockComponent;
    private PlayerStock player2StockComponent;

    private void Start()
    {
        // 最初は非表示
        if (Player1Stock != null) Player1Stock.SetActive(false);
        if (Player2Stock != null) Player2Stock.SetActive(false);

        // PlayerStockコンポーネント取得
        if (Player1Stock != null)
            player1StockComponent = Player1Stock.GetComponent<PlayerStock>();
        if (Player2Stock != null)
            player2StockComponent = Player2Stock.GetComponent<PlayerStock>();

        // GameManager購読
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;

            foreach (var tank in gameManager.m_SpawnPoints)
            {
                if (tank != null)
                {
                    // MODIFIED: TankManager のイベント購読を WeaponType 付きに変更
                    tank.OnWeaponStockChangedEvent += HandleWeaponStockChanged; // MODIFIED
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
                    // MODIFIED: 購読解除も WeaponType 付きに変更
                    tank.OnWeaponStockChangedEvent -= HandleWeaponStockChanged; // MODIFIED
                }
            }
        }
    }

    private void HandleGameStateChanged(GameManager.GameLoopState newState)
    {
        bool isPlaying = newState == GameManager.GameLoopState.RoundPlaying;

        if (Player1Stock != null) Player1Stock.SetActive(isPlaying);
        if (Player2Stock != null) Player2Stock.SetActive(isPlaying);

        Debug.Log($"[HUDManager] GameLoopState changed to: {newState}, HUD visible: {isPlaying}");
    }

    //====================================
    // MODIFIED: WeaponType を追加して正しく分岐
    //====================================
    private void HandleWeaponStockChanged(int controlIndex, WeaponType type, WeaponStockData stockData) // MODIFIED
    {
        PlayerStock target = controlIndex == 1 ? player1StockComponent : player2StockComponent;
        if (target == null || stockData == null) return;

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
