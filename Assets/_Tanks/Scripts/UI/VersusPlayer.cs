using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VersusPlayer : MonoBehaviour
{
    // Unityエディタ上でボタンを紐づけできるようにする
    [SerializeField] private Button versusPlayer;

    private void Start()
    {
        // ボタンがクリックされたときに OnClicked メソッドを実行
        versusPlayer.onClick.AddListener(OnClicked);
    }

    // ボタンクリック時の処理
    private void OnClicked()
    {
        // SceneNames クラスを使用してゲーム画面に遷移
        SceneManager.LoadScene(SceneNames.Demo_Game_Moon);
    }
}
