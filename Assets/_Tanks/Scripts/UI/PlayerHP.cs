using UnityEngine;
using UnityEngine.UI; // Slider クラスを使用するために必要

public class PlayerHP : MonoBehaviour
{
    // 1. HPSlider: Slider型の変数 (SerializeField属性を持つ)
    [SerializeField]
    private Slider HPSlider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 💡 補足: スライダーが設定されているか確認
        if (HPSlider == null)
        {
            Debug.LogError("HPSliderが設定されていません！インスペクターでSliderコンポーネントを割り当ててください。");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 2. UpdateHPSlider: HPの値を引数にHPSlider.valueを更新するpublicメソッド
    /// <summary>
    /// HPゲージのスライダー値を更新します。
    /// </summary>
    /// <param name="hpValue">現在のHP値</param>
    public void UpdateHPSlider(float hpValue)
    {
        if (HPSlider != null)
        {
            // スライダーの値を引数として渡されたHP値で更新
            HPSlider.value = hpValue;
        }
    }
}