using System;
using System.Collections;
using UnityEngine;
using Tanks.Complete;  // GameManagerクラスを使うために必要

public class CartridgeSpawner : MonoBehaviour
{
    [Header("Cartridge Data Settings")]
    [SerializeField] private CartridgeData shellCartridgeData;  // 砲弾データ
    [SerializeField] private CartridgeData mineCartridgeData;   // 地雷データ

    [Header("Spawn Area Settings")]
    [Tooltip("カートリッジを生成する範囲")]
    [SerializeField] private Vector3 spawnArea = new Vector3(10f, 0f, 10f);

    [Header("Game Manager Reference")]
    [SerializeField] private GameManager gameManager; // GameManagerへの参照

    private Coroutine spawnCoroutine; // コルーチン制御用

    private void Start()
    {
        // GameManagerが指定されていなければ自動で探す
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>(); // Unity 2023以降推奨
        }

        // GameManagerの状態変更イベントを購読
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;
        }
        else
        {
            Debug.LogError("GameManager not found in the scene!");
        }
    }

    private void OnDestroy()
    {
        // イベント購読を解除
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    // 🔹 ゲーム状態が変化した時に呼ばれる
   private void HandleGameStateChanged(GameManager.GameLoopState newState)
   {
     // プレイ中（RoundPlaying）ならスポーンを開始
     if (newState == GameManager.GameLoopState.RoundPlaying)
     {
         if (spawnCoroutine == null)
         {
             // 砲弾と地雷の両方の生成を開始
             spawnCoroutine = StartCoroutine(SpawnRoutine(shellCartridgeData));
             StartCoroutine(SpawnRoutine(mineCartridgeData));
         }
     }
     else
     {
         // それ以外の状態では停止
         if (spawnCoroutine != null)
         {
             StopCoroutine(spawnCoroutine);
              spawnCoroutine = null;
        }
     }
   }


    // 🔹 カートリッジ生成コルーチン
    private IEnumerator SpawnRoutine(CartridgeData cartridgeData)
    {
        while (true)
        {
            SpawnCartridge(cartridgeData);
            yield return new WaitForSeconds(cartridgeData.spawnInterval);
        }
    }

    // 🔹 指定されたCartridgeDataを元にカートリッジを生成
    private void SpawnCartridge(CartridgeData cartridgeData)
    {
        if (cartridgeData == null || cartridgeData.prefab == null)
        {
            Debug.LogWarning("CartridgeDataまたはPrefabが設定されていません。");
            return;
        }

        Vector3 randomPos = new Vector3(
            UnityEngine.Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f),
            spawnArea.y,
            UnityEngine.Random.Range(-spawnArea.z / 2f, spawnArea.z / 2f)
        );

        Vector3 spawnPosition = transform.position + randomPos;

        Instantiate(cartridgeData.prefab, spawnPosition, Quaternion.identity);
    }
}
