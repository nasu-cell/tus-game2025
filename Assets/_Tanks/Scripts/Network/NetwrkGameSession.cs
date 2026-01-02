using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 通信プレイヤーを管理するセッションクラス
/// </summary>
public class NetworkGameSession : NetworkBehaviour
{
    // 接続中のプレイヤー一覧 (ClientId → PlayerData)
    public Dictionary<ulong, NetworkPlayerData> Players = new Dictionary<ulong, NetworkPlayerData>();

    /// <summary>
    /// 接続したプレイヤーを登録
    /// </summary>
    /// <param name="player">登録する NetworkPlayerData</param>
    public void RegisterPlayer(NetworkPlayerData player)
    {
        if (!IsServer) return; // サーバー側だけ管理

        if (!Players.ContainsKey(player.ClientId))
        {
            Players.Add(player.ClientId, player);
            Debug.Log($"[NetworkGameSession] Player registered: {player.ClientId}");
        }
    }

    /// <summary>
    /// ローカルプレイヤーを Spawn して登録
    /// </summary>
    /// <param name="playerPrefab">プレイヤープレハブ</param>
    public void SpawnLocalPlayer(GameObject playerPrefab)
    {
        if (!IsServer) return; // Host しか Spawn できない場合

        var playerObj = Instantiate(playerPrefab);
        var playerData = playerObj.GetComponent<NetworkPlayerData>();
        playerObj.GetComponent<NetworkObject>().SpawnWithOwnership(NetworkManager.Singleton.LocalClientId);

        RegisterPlayer(playerData);

        // LobbyManager に通知して自分のPlayerを登録
        var lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.SetLocalPlayer(playerData);
        }
    }
}
