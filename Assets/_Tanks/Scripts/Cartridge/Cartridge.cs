using UnityEngine;

public class Cartridge : MonoBehaviour
{
    [Header("寿命・点滅設定")]
    [Tooltip("点滅を開始するまでの遅延時間（秒）")]
    [SerializeField] private float m_BlinkStartDelay = 3.0f;

    [Tooltip("点滅を開始してから消滅するまでの時間（秒）")]
    [SerializeField] private float m_BlinkDuration = 3.0f;

    [Tooltip("点滅の間隔（秒）")]
    [SerializeField] private float m_BlinkInterval = 0.1f;

    // 内部タイマー
    private float m_ElapsedTime = 0.0f;
    private float m_BlinkTimer = 0.0f;

    private Renderer m_Renderer;

    void Start()
    {
        m_Renderer = GetComponent<Renderer>();
        if (m_Renderer == null)
        {
            Debug.LogWarning("Renderer component is missing on Cartridge object.");
        }
    }

    void Update()
    {
        m_ElapsedTime += Time.deltaTime;

        // 🔹 点滅開始後に一定時間経過したら消滅
        if (m_ElapsedTime >= m_BlinkStartDelay + m_BlinkDuration)
        {
            Destroy(gameObject);
            return;
        }

        // 🔹 点滅開始時間を過ぎたら点滅処理を行う
        if (m_ElapsedTime >= m_BlinkStartDelay)
        {
            m_BlinkTimer += Time.deltaTime;

            if (m_BlinkTimer >= m_BlinkInterval)
            {
                m_BlinkTimer = 0.0f;

                if (m_Renderer != null)
                {
                    m_Renderer.enabled = !m_Renderer.enabled;
                }
            }
        }
    }
}
