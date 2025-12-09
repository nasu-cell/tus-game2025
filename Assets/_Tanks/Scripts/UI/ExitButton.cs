using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExitButton : MonoBehaviour
{
    // Unityエディタ上でボタンを紐づけできるようにする
    [SerializeField] private Button exitbutton;

    private void Start()
    {
        // ボタンがクリックされたときに OnClicked メソッドを実行
        exitbutton.onClick.AddListener(OnClicked);
    }

    // ボタンクリック時の処理
    private void OnClicked()
    {
        // SceneNames クラスを使用してゲーム画面に遷移
        SceneManager.LoadScene(SceneNames.HomeScene);
    }
}

