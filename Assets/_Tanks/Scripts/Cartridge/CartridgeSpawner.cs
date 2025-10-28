using System.Collections;
using UnityEngine;

public class CartridgeSpawner : MonoBehaviour
{
    [Header("Cartridge Settings")]
    [Tooltip("砲弾カートリッジのプレハブ")]
    public GameObject shellCartridge;

    [Tooltip("砲弾カートリッジを生成する間隔（秒）")]
    public float spawnInterval = 5f;

    [Tooltip("砲弾カートリッジを生成する範囲")]
    public Vector3 spawnArea = new Vector3(10f, 0f, 10f); // xとzが範囲、yは固定

    private void Start()
    {
        // コルーチンを開始
        StartCoroutine(SpawnRoutine());
    }

    // 砲弾カートリッジを生成するメソッド
    private void SpawnCartridge()
    {
        if (shellCartridge == null)
        {
            Debug.LogWarning("ShellCartridge prefab is not assigned!");
            return;
        }

        // spawnArea内でランダムな位置を生成
        Vector3 randomPos = new Vector3(
            Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f),
            spawnArea.y, // y軸は固定
            Random.Range(-spawnArea.z / 2f, spawnArea.z / 2f)
        );

        Vector3 spawnPosition = transform.position + randomPos;

        // プレハブを生成
        Instantiate(shellCartridge, spawnPosition, Quaternion.identity);
    }

    // コルーチンで定期的に生成
    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnCartridge();
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
