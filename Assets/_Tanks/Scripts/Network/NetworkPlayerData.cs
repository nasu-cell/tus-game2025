using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 接続したプレイヤーごとの情報を管理する NetworkBehaviour
/// </summary>
public class NetworkPlayerData : NetworkBehaviour
{
    // Ready状態を同期するNetworkVariable
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false);

    // 自分のClientId
    public ulong ClientId { get; private set; }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ClientId = OwnerClientId;
        Debug.Log($"[NetworkPlayerData] Spawned | ClientId={ClientId}");
    }

    /// <summary>
    /// Ready状態を切り替える
    /// </summary>
    public void ToggleReady()
    {
        if (IsServer)
        {
            IsReady.Value = !IsReady.Value; // Server側で直接切り替え
        }
        else
        {
            ToggleReadyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        IsReady.Value = !IsReady.Value;
    }
}
