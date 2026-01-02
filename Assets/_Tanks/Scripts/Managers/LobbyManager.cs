using UnityEngine;
using TMPro;
using Unity.Netcode;

public class LobbyManager : MonoBehaviour
{
    public TextMeshProUGUI readyButtonText;
    public TextMeshProUGUI myReadyText;
    public TextMeshProUGUI opponentReadyText;

    private NetworkPlayerData localPlayer;

    private void Start()
    {
        // Player が Spawn されるまで待機して LocalPlayer を設定
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        readyButtonText.text = "READY";
        myReadyText.text = "Not Ready";
        opponentReadyText.text = "Not Ready";

        TrySetLocalPlayer();
    }

    private void TrySetLocalPlayer()
    {
        // シーン内の NetworkPlayerData から自分のオーナーを探す
        foreach (var player in FindObjectsOfType<NetworkPlayerData>())
        {
            if (player.IsOwner)
            {
                SetLocalPlayer(player);
                break;
            }
        }
    }

    public void SetLocalPlayer(NetworkPlayerData player)
    {
        localPlayer = player;
        // 自分の Ready 状態を監視
        localPlayer.IsReady.OnValueChanged += OnMyReadyChanged;

        // 他プレイヤーの Ready 状態を監視
        foreach (var other in FindObjectsOfType<NetworkPlayerData>())
        {
            if (!other.IsOwner)
                other.IsReady.OnValueChanged += OnOpponentReadyChanged;
        }
    }

    public void OnReadyButtonPressed()
    {
        if (localPlayer == null)
        {
            Debug.LogWarning("LocalPlayer がまだ設定されていません");
            return;
        }
        localPlayer.ToggleReady();
    }

    private void OnMyReadyChanged(bool previousValue, bool newValue)
    {
        myReadyText.text = newValue ? "Ready" : "Not Ready";
        readyButtonText.text = newValue ? "CANCEL" : "READY";
    }

    private void OnOpponentReadyChanged(bool previousValue, bool newValue)
    {
        opponentReadyText.text = newValue ? "Ready" : "Not Ready";
    }

    private void OnClientConnected(ulong clientId)
    {
        // Client が接続されたら再度 LocalPlayer を探す
        TrySetLocalPlayer();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // 接続が切れたら必要に応じて UI をリセット
    }

    private void OnDestroy()
    {
        if (localPlayer != null)
            localPlayer.IsReady.OnValueChanged -= OnMyReadyChanged;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}
