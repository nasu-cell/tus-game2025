using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;

namespace Tanks.Complete
{
    public class TankShooting : MonoBehaviour
    {
        [Header("General Settings")]
        public Rigidbody m_Shell;                     // 通常弾
        public Rigidbody m_SpecialShell;              // 特殊弾
        public Transform m_FireTransform;
        public Slider m_AimSlider;
        public AudioSource m_ShootingAudio;
        public AudioClip m_ChargingClip;
        public AudioClip m_FireClip;
        public float m_MinLaunchForce = 5f;
        public float m_MaxLaunchForce = 20f;
        public float m_MaxChargeTime = 0.75f;
        public float m_ShotCooldown = 1.0f;

        [Header("Shell Properties")]
        public float m_MaxDamage = 100f;
        public float m_ExplosionForce = 50f;
        public float m_ExplosionRadius = 5f;

        [HideInInspector] public TankInputUser m_InputUser;
        public bool m_IsComputerControlled { get; set; } = false;

        // 💡 追加: TankHealth への参照
        private TankHealth m_TankHealth;

        private string m_FireButton;
        private float m_CurrentLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired;
        private InputAction fireAction;
        private float m_BaseMinLaunchForce;
        private float m_ShotCooldownTimer;

        // --- 特殊弾 ---
        private bool m_UsingSpecialShell = false;
        private float m_SpecialDamageMultiplier = 1f;
        private float m_SpecialShellDuration = 0f;
        private float m_SpecialShellTimer = 0f;

        private bool m_ChargingForward = true;

        // --- イベント ---
        public event Action<int> OnShellStockChanged;     // 砲弾イベント
        public event Action<int> OnMineStockChanged;      // 地雷イベント
        public event Action OnMinePlaced;                 // 地雷設置イベント

        // --- Weapon Data ---
        [SerializeField] private WeaponStockData m_WeaponStockData;
        public WeaponStockData WeaponStockData => m_WeaponStockData;

        [SerializeField] private WeaponStockData m_MineStockData;
        public WeaponStockData MineStockData => m_MineStockData;

        [SerializeField] private GameObject m_Mine;
        [SerializeField] private string m_SetMineButton = "SetMine";
        private InputAction setMineAction;

        private void OnEnable()
        {
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_BaseMinLaunchForce = m_MinLaunchForce;

            m_AimSlider.minValue = m_MinLaunchForce;
            m_AimSlider.maxValue = m_MaxLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;
        }

        private void Awake()
        {
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null)
                m_InputUser = gameObject.AddComponent<TankInputUser>();

            // 💡 追加: TankHealth コンポーネントの参照を取得
            m_TankHealth = GetComponent<TankHealth>();
            if (m_TankHealth == null)
                Debug.LogError($"{gameObject.name}: TankHealth コンポーネントが見つかりません。");
        }

        private void Start()
        {
            // Fire Action setup
            m_FireButton = "Fire";
            fireAction = m_InputUser.ActionAsset.FindAction(m_FireButton);
            fireAction.Enable();

            // Mine Action setup
            m_SetMineButton = "SetMine";
            setMineAction = m_InputUser.ActionAsset.FindAction(m_SetMineButton);
            if (setMineAction != null)
                setMineAction.Enable();
            else
                Debug.LogError($"{gameObject.name}: SetMine アクションが見つかりません");

            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;

            // --- WeaponStockData 初期化 ---
            if (m_WeaponStockData != null)
            {
                m_WeaponStockData.InitializeQuantity();
                OnShellStockChanged?.Invoke(m_WeaponStockData.CurrentQuantity);
            }
            // --- MineStockData 初期化 ---
            if (m_MineStockData != null)
            {
                m_MineStockData.InitializeQuantity();
                OnMineStockChanged?.Invoke(m_MineStockData.CurrentQuantity);
            }
        }

        private void Update()
        {
            // --- 特殊弾タイマー ---
            if (m_UsingSpecialShell && m_SpecialShellDuration > 0f)
            {
                m_SpecialShellTimer += Time.deltaTime;
                if (m_SpecialShellTimer >= m_SpecialShellDuration)
                {
                    m_UsingSpecialShell = false;
                    m_SpecialDamageMultiplier = 1f;
                    Debug.Log($"{gameObject.name} の特殊弾効果が終了しました。");
                }
            }

            if (!m_IsComputerControlled)
            {
                HumanUpdate();
                if (setMineAction != null && setMineAction.WasPerformedThisFrame())
                    PlaceMine();
            }
            else
            {
                ComputerUpdate();
            }
        }

        // ------------------- Human Update ---------------------
        void HumanUpdate()
        {
            // クールダウンタイマーの減少は、無敵状態に関わらず継続させる
            if (m_ShotCooldownTimer > 0.0f)
                m_ShotCooldownTimer -= Time.deltaTime;
            
            // 💡 修正: 無敵状態（ワームホール通過中）なら、砲撃/チャージの入力を無視する
            if (m_TankHealth != null && m_TankHealth.IsInvincible)
                return;

            if (m_WeaponStockData.CurrentQuantity <= 0)
                return;

            if (!m_Fired && fireAction.IsPressed())
            {
                if (m_ChargingForward)
                {
                    m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                    if (m_CurrentLaunchForce >= m_MaxLaunchForce)
                    {
                        m_CurrentLaunchForce = m_MaxLaunchForce;
                        m_ChargingForward = false;
                    }
                }
                else
                {
                    m_CurrentLaunchForce -= m_ChargeSpeed * Time.deltaTime;
                    if (m_CurrentLaunchForce <= m_MinLaunchForce)
                    {
                        m_CurrentLaunchForce = m_MinLaunchForce;
                        m_ChargingForward = true;
                    }
                }

                m_AimSlider.value = m_CurrentLaunchForce;
            }

            if (fireAction.WasPressedThisFrame() && m_ShotCooldownTimer <= 0)
            {
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_ChargingForward = true;

                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play();
            }

            if (fireAction.WasReleasedThisFrame() && !m_Fired)
                Fire();
        }

        // ------------------- AI Update ---------------------
        void ComputerUpdate()
        {
            // 💡 追加: AIも無敵状態なら砲撃を試みない
            if (m_TankHealth != null && m_TankHealth.IsInvincible)
                return;

            if (m_WeaponStockData.CurrentQuantity <= 0)
                return;

            m_AimSlider.value = m_BaseMinLaunchForce;

            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire();
            }
        }
        // =========================
        // デバッグ用: 現在の砲弾数・地雷数をログに表示
        // =========================
        public void DebugPrintWeaponStocks()
        {
            string msg = $"{gameObject.name} 現在の武器ストック: " +
                         $"Shells = {(m_WeaponStockData != null ? m_WeaponStockData.CurrentQuantity.ToString() : "null")}, " +
                         $"Mines = {(m_MineStockData != null ? m_MineStockData.CurrentQuantity.ToString() : "null")}";
            Debug.Log(msg);
        }

        // ------------------- Fire ---------------------
        private void Fire()
        {
            // 念のため、Fireメソッド内でも無敵状態をチェック
            if (m_TankHealth != null && m_TankHealth.IsInvincible)
                return;

            if (m_WeaponStockData.CurrentQuantity <= 0)
                return;

            m_WeaponStockData.Use();
            OnShellStockChanged?.Invoke(m_WeaponStockData.CurrentQuantity);

            DebugPrintWeaponStocks();

            m_Fired = true;

            Rigidbody shellPrefab = m_UsingSpecialShell && m_SpecialShell != null
                ? m_SpecialShell
                : m_Shell;

            Rigidbody shellInstance = Instantiate(shellPrefab, m_FireTransform.position, m_FireTransform.rotation);
            shellInstance.linearVelocity = m_CurrentLaunchForce * m_FireTransform.forward;

            var explosion = shellInstance.GetComponent<ShellExplosion>();
            if (explosion != null)
                explosion.m_MaxDamage *= m_SpecialDamageMultiplier;

            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();

            m_CurrentLaunchForce = m_MinLaunchForce;
            m_ShotCooldownTimer = m_ShotCooldown;
        }

        // ------------------- Collision ---------------------
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("ShellCartridge"))
            {
                m_WeaponStockData.Replenish();
                OnShellStockChanged?.Invoke(m_WeaponStockData.CurrentQuantity);
                DebugPrintWeaponStocks();
                Destroy(collision.gameObject);
                return;
            }

            if (collision.gameObject.CompareTag("MineCartridge"))
            {
                m_MineStockData.Replenish();
                OnMineStockChanged?.Invoke(m_MineStockData.CurrentQuantity);
                DebugPrintWeaponStocks();
                Destroy(collision.gameObject);
            }
        }

        // ------------------- Mine Placement ---------------------
        private void PlaceMine()
        {
            // 💡 修正: 無敵状態（ワームホール通過中）なら地雷を設置できない
            if (m_TankHealth != null && m_TankHealth.IsInvincible)
            {
                // Debug.Log($"{gameObject.name}: ワームホール通過中のため、地雷を設置できません。");
                return; 
            }
            
            if (m_MineStockData == null || m_Mine == null || m_MineStockData.CurrentQuantity <= 0)
                return;

            m_MineStockData.Use();
            OnMineStockChanged?.Invoke(m_MineStockData.CurrentQuantity);

            DebugPrintWeaponStocks();
            Instantiate(m_Mine, transform.position, Quaternion.identity);
            OnMinePlaced?.Invoke();

            Debug.Log($"{gameObject.name} が地雷を設置しました");
        }

        // --- AI 支援 ---
        public bool IsCharging => !m_Fired && fireAction.IsPressed();

        public float CurrentChargeRatio =>
            Mathf.InverseLerp(m_MinLaunchForce, m_MaxLaunchForce, m_CurrentLaunchForce);

        public Vector3 GetProjectilePosition(float chargeRatio = 1f)
        {
            float launchForce = Mathf.Lerp(m_MinLaunchForce, m_MaxLaunchForce, chargeRatio);
            return m_FireTransform.position + m_FireTransform.forward * launchForce * 0.1f;
        }

        public void StartCharging()
        {
            // 💡 追加: AI支援メソッドでも無敵状態をチェック
            if (m_TankHealth != null && m_TankHealth.IsInvincible) return;
            
            if (m_WeaponStockData.CurrentQuantity <= 0) return;

            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_ChargingForward = true;

            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }

        public void StopCharging() => Fire();

        public void EquipSpecialShell(float damageMultiplier, float duration = 0f)
        {
            m_UsingSpecialShell = true;
            m_SpecialDamageMultiplier = damageMultiplier;
            m_SpecialShellDuration = duration;
            m_SpecialShellTimer = 0f;

            Debug.Log($"{gameObject.name} が特殊弾を装備しました（倍率: {damageMultiplier}, 時間: {duration}秒）");
        }
    }
}