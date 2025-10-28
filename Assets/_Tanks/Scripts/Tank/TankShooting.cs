using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class TankShooting : MonoBehaviour
    {
        public Rigidbody m_Shell;
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

        [Header("Shell Stock Settings")]
        [Tooltip("ゲーム開始時の砲弾数")]
        public int m_StartingShells = 10;

        [Tooltip("砲弾の最大所持数")]
        public int m_MaxShells = 50;

        [Tooltip("カートリッジを拾ったときの補充量")]
        public int m_ShellsPerCartridge = 10;

        [HideInInspector]
        public int m_CurrentShells;  // 現在の砲弾所持数

        [HideInInspector]
        public TankInputUser m_InputUser;

        public float CurrentChargeRatio =>
            (m_CurrentLaunchForce - m_MinLaunchForce) / (m_MaxLaunchForce - m_MinLaunchForce);
        public bool IsCharging => m_IsCharging;
        public bool m_IsComputerControlled { get; set; } = false;

        private string m_FireButton;
        private float m_CurrentLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired;
        private bool m_HasSpecialShell;
        private float m_SpecialShellMultiplier;
        private InputAction fireAction;
        private bool m_IsCharging = false;
        private float m_BaseMinLaunchForce;
        private float m_ShotCooldownTimer;

        private void OnEnable()
        {
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_BaseMinLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_BaseMinLaunchForce;
            m_HasSpecialShell = false;
            m_SpecialShellMultiplier = 1.0f;

            m_AimSlider.minValue = m_MinLaunchForce;
            m_AimSlider.maxValue = m_MaxLaunchForce;
        }

        private void Awake()
        {
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null)
                m_InputUser = gameObject.AddComponent<TankInputUser>();
        }

        private void Start()
        {
            m_FireButton = "Fire";
            fireAction = m_InputUser.ActionAsset.FindAction(m_FireButton);
            fireAction.Enable();

            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;

            // 🔹 初期砲弾数を設定
            m_CurrentShells = m_StartingShells;
        }

        private void Update()
        {
            if (!m_IsComputerControlled)
                HumanUpdate();
            else
                ComputerUpdate();
        }

        public void StartCharging()
        {
            if (m_CurrentShells <= 0)
                return; // 🔹 砲弾がない場合は発射できない

            m_IsCharging = true;
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }

        public void StopCharging()
        {
            if (m_IsCharging)
            {
                Fire();
                m_IsCharging = false;
            }
        }

        void ComputerUpdate()
        {
            // 砲弾が無い場合は発射できない
            if (m_CurrentShells <= 0)
                return;

            m_AimSlider.value = m_BaseMinLaunchForce;

            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire();
            }
            else if (m_IsCharging && !m_Fired)
            {
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                m_AimSlider.value = m_CurrentLaunchForce;
            }
            else if (fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                Fire();
                m_IsCharging = false;
            }
        }

        void HumanUpdate()
        {
            if (m_ShotCooldownTimer > 0.0f)
                m_ShotCooldownTimer -= Time.deltaTime;

            // 砲弾が無い場合は何もしない
            if (m_CurrentShells <= 0)
                return;

            m_AimSlider.value = m_BaseMinLaunchForce;

            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire();
            }
            else if (m_ShotCooldownTimer <= 0 && fireAction.WasPressedThisFrame())
            {
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play();
            }
            else if (fireAction.IsPressed() && !m_Fired)
            {
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                m_AimSlider.value = m_CurrentLaunchForce;
            }
            else if (fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                Fire();
            }
        }

        private void Fire()
        {
            if (m_CurrentShells <= 0)
            {
                Debug.Log("No shells left!");
                return;
            }

            // 🔹 砲弾を1つ消費
            m_CurrentShells--;
            Debug.Log($"Shell fired! Remaining shells: {m_CurrentShells}");

            m_Fired = true;

            Rigidbody shellInstance =
                Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

            shellInstance.linearVelocity = m_CurrentLaunchForce * m_FireTransform.forward;

            ShellExplosion explosionData = shellInstance.GetComponent<ShellExplosion>();
            explosionData.m_ExplosionForce = m_ExplosionForce;
            explosionData.m_ExplosionRadius = m_ExplosionRadius;
            explosionData.m_MaxDamage = m_MaxDamage;

            if (m_HasSpecialShell)
            {
                explosionData.m_MaxDamage *= m_SpecialShellMultiplier;
                m_HasSpecialShell = false;
                m_SpecialShellMultiplier = 1f;
                PowerUpDetector powerUpDetector = GetComponent<PowerUpDetector>();
                if (powerUpDetector != null)
                    powerUpDetector.m_HasActivePowerUp = false;

                PowerUpHUD powerUpHUD = GetComponentInChildren<PowerUpHUD>();
                if (powerUpHUD != null)
                    powerUpHUD.DisableActiveHUD();
            }

            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();

            m_CurrentLaunchForce = m_MinLaunchForce;
            m_ShotCooldownTimer = m_ShotCooldown;
        }

        // 🔹 砲弾を補充する
        public void AddShells(int amount)
        {
            m_CurrentShells = Mathf.Min(m_CurrentShells + amount, m_MaxShells);
            Debug.Log($"Shells added! Current: {m_CurrentShells}");
        }

        public void EquipSpecialShell(float damageMultiplier)
        {
            m_HasSpecialShell = true;
            m_SpecialShellMultiplier = damageMultiplier;
        }

        public Vector3 GetProjectilePosition(float chargingLevel)
        {
            float chargeLevel = Mathf.Lerp(m_MinLaunchForce, m_MaxLaunchForce, chargingLevel);
            Vector3 velocity = chargeLevel * m_FireTransform.forward;

            float a = 0.5f * Physics.gravity.y;
            float b = velocity.y;
            float c = m_FireTransform.position.y;

            float sqrtContent = b * b - 4 * a * c;
            if (sqrtContent <= 0)
                return m_FireTransform.position;

            float answer1 = (-b + Mathf.Sqrt(sqrtContent)) / (2 * a);
            float answer2 = (-b - Mathf.Sqrt(sqrtContent)) / (2 * a);
            float answer = answer1 > 0 ? answer1 : answer2;

            Vector3 position = m_FireTransform.position +
                               new Vector3(velocity.x, 0, velocity.z) * answer;
            position.y = 0;

            return position;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 衝突したオブジェクトのタグが "ShellCartridge" の場合
            if (collision.gameObject.CompareTag("ShellCartridge"))
            {
                // 砲弾を補充
                AddShells(m_ShellsPerCartridge);

                // カートリッジオブジェクトを削除
                Destroy(collision.gameObject);
            }
        }
    }
}
