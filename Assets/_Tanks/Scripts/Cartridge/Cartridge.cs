using UnityEngine;

public class Cartridge : MonoBehaviour
{
    // 砲弾カートリッジが点滅してから消滅するまでの時間（秒）
    [SerializeField] private float m_LifeTime = 5.0f;

    // 点滅の間隔（秒）
    [SerializeField] private float m_BlinkInterval = 0.5f;

    // 点滅のためのタイマー
    private float m_BlinkTimer = 0.0f;

    // Rendererコンポーネントの参照
    private Renderer m_Renderer;

    // 生成からの経過時間
    private float m_ElapsedTime = 0.0f;

    void Start()
    {
        // Rendererコンポーネントを取得
        m_Renderer = GetComponent<Renderer>();
        if (m_Renderer == null)
        {
            Debug.LogWarning("Renderer component is missing on Cartridge object.");
        }
    }

    void Update()
    {
        // 経過時間を更新
        m_ElapsedTime += Time.deltaTime;

        // 砲弾カートリッジの寿命を過ぎたら消滅
        if (m_ElapsedTime >= m_LifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // 点滅処理
        m_BlinkTimer += Time.deltaTime;
        if (m_BlinkTimer >= m_BlinkInterval)
        {
            m_BlinkTimer = 0.0f;

            if (m_Renderer != null)
            {
                m_Renderer.enabled = !m_Renderer.enabled; // ON/OFFを切り替え
            }
        }
    }
}
