using UnityEngine;
using System.Collections; // コルーチン (IEnumerator) に必要
using System.Collections.Generic; // Dictionary に必要
using Tanks.Complete; // TankHealth クラスにアクセスするために必要

public class WormHole : MonoBehaviour
{
    // フィールド
    [SerializeField] private WormHole m_Destination;             // 移動先のワームホール
    [SerializeField] private float m_WarpDuration = 1.0f;       // ワープまでの時間（無敵時間とクールタイムを兼ねる）

    // static フィールド（全ワームホールでクールタイム情報を共有）
    // キー: TankHealth インスタンス (どの戦車か)
    // 値: クールタイムが終了する正確な時刻 (Time.time + クールタイム)
    private static Dictionary<TankHealth, float> m_TankCoolDowns;

    private void Awake()
    {
        // 辞書がまだ初期化されていない場合にのみ初期化を行う
        if (m_TankCoolDowns == null)
        {
            m_TankCoolDowns = new Dictionary<TankHealth, float>();
        }
    }

    void Start()
    {
        // ワームホールをトリガーとして機能させるために、自身の Collider が IsTrigger に設定されている必要があります。
    }

    void Update()
    {
        
    }

    // =================================================================
    // トリガーに何かが侵入した際の処理
    // =================================================================
    private void OnTriggerEnter(Collider other)
    {
        // 接触したオブジェクトから TankHealth コンポーネントを取得
        TankHealth tankHealth = other.GetComponent<TankHealth>();

        // TankHealth が見つかった場合（戦車が接触した場合）のみ処理を実行
        if (tankHealth != null)
        {
            // WarpTank コルーチンを開始
            StartCoroutine(WarpTank(other, tankHealth));
        }
    }

    // =================================================================
    // 戦車を一定時間待機させてワープさせるコルーチン
    // =================================================================
    public IEnumerator WarpTank(Collider tankCollider, TankHealth tankHealth)
    {
        // 1. m_TankCoolDownsを参照し、戦車がクールタイム中であれば処理を中断
        if (m_TankCoolDowns.ContainsKey(tankHealth) && m_TankCoolDowns[tankHealth] > Time.time)
        {
            Debug.Log($"ワープホール: {tankHealth.gameObject.name} はクールタイム中です。");
            yield break; // コルーチンの実行を終了
        }

        // 目的地が設定されているか確認
        if (m_Destination == null)
        {
            Debug.LogError("ワープホール: 移動先 (m_Destination) が設定されていません！");
            yield break;
        }

        // 2. ワープまで一定時間無敵状態で待機
        
        // ワープ処理中は、戦車を無敵状態にし、移動・砲撃・被弾を制限する
        tankHealth.ActivateInvincibility(m_WarpDuration);
        
        // m_WarpDuration 秒間、処理を停止して待機（ワープ演出時間）
        yield return new WaitForSeconds(m_WarpDuration);
        
        // 3. ワームホールの目的地に戦車を移動
        Vector3 destinationPos = m_Destination.transform.position;
        
        // 地面にめり込まないよう、戦車の Collider の高さ（中心）分だけ持ち上げる
        destinationPos.y += tankCollider.bounds.extents.y; 
        
        // 戦車の位置を移動
        tankCollider.transform.position = destinationPos;
        
        // 4. m_TankCoolDownsにクールタイムが終了する時刻を記録
        float cooldownEndTime = Time.time + m_WarpDuration; // m_WarpDurationをクールタイムとして流用
        if (m_TankCoolDowns.ContainsKey(tankHealth))
        {
            m_TankCoolDowns[tankHealth] = cooldownEndTime;
        }
        else
        {
            m_TankCoolDowns.Add(tankHealth, cooldownEndTime);
        }

        Debug.Log($"{tankHealth.gameObject.name} が {gameObject.name} から {m_Destination.gameObject.name} へワープしました。");
    }
}