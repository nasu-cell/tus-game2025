using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    // Unityエディタ上でボタンを紐づけできるようにする
    [SerializeField] private Button startButton;

    private void Start()
    {
        // ボタンがクリックされたときに OnClicked メソッドを実行
        startButton.onClick.AddListener(OnClicked);
    }

    // ボタンクリック時の処理
    private void OnClicked()
    {
        // SceneNames クラスを使用してホーム画面に遷移
        SceneManager.LoadScene(SceneNames.HomeScene);
    }
}
