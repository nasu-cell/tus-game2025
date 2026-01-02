using Unity.Netcode;
using UnityEngine;

public class NetworkLobbyManager : NetworkBehaviour
{
    // プレイヤーごとの Ready 状態を同期する変数
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false);

    // Ready 状態を切り替える関数
    public void ToggleReady()
    {
        if (IsServer)
        {
            // Server（Host）側で直接切り替え
            IsReady.Value = !IsReady.Value;
        }
        else
        {
            // Client は Server に RPC を送る
            SubmitReadyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        IsReady.Value = !IsReady.Value;
    }
}
