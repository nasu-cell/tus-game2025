using UnityEngine;
using System;

[Serializable] // Inspectorで中身を表示できるようにする
public class CartridgeData : MonoBehaviour
{
    [Header("Cartridge Settings")]
    public GameObject prefab;       // 生成するカートリッジのPrefab
    public float spawnInterval = 5f; // 生成間隔（秒）

    void Start()
    {
        // 初期化処理などをここに書ける
    }

    void Update()
    {
        // 一定間隔でカートリッジを生成したい場合などに利用可能
    }
}
