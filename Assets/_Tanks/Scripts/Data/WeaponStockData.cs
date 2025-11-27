using UnityEngine;

[System.Serializable]
public enum WeaponType
{
    Shell,
    Mine
}
public class WeaponStockData : MonoBehaviour
{
    [Header("Weapon Type")]
    public WeaponType Type;  
    // =========================
    // 武器カートリッジの初期設定
    // =========================
    [SerializeField] private int m_InitialQuantity;    // 初期所持数
    [SerializeField] private int m_MaxCapacity;       // 最大所持数
    [SerializeField] private int m_ReplenishQuantity;  // 補充量

    // =========================
    // 現在の所持数
    // =========================
    private int m_CurrentQuantity;

    // getter
    public int CurrentQuantity => m_CurrentQuantity;

    // =========================
    // 所持数を初期化
    // =========================
    public void InitializeQuantity()
    {
        m_CurrentQuantity = m_InitialQuantity;
    }

    // =========================
    // 補充
    // =========================
    public void Replenish()
    {
        m_CurrentQuantity += m_ReplenishQuantity;
        if (m_CurrentQuantity > m_MaxCapacity)
            m_CurrentQuantity = m_MaxCapacity;
    }

    // =========================
    // 使用
    // =========================
    public void Use()
    {
        m_CurrentQuantity--;
        if (m_CurrentQuantity < 0)
            m_CurrentQuantity = 0;
    }
}
