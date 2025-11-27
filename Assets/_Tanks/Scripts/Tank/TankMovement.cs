using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Tanks.Complete
{
    [DefaultExecutionOrder(-10)]
    public class TankMovement : MonoBehaviour
    {
        [Tooltip("The player number. Without a tank selection menu, Player 1 is left keyboard control, Player 2 is right keyboard")]
        public int m_PlayerNumber = 1;
        [Tooltip("The speed in unity unit/second the tank move at")]
        public float m_Speed = 12f;
        [Tooltip("The speed in deg/s that tank will rotate at")]
        public float m_TurnSpeed = 180f;
        [Tooltip("If set to true, the tank auto orient and move toward the pressed direction instead of rotating on left/right and move forward on up")]
        public bool m_IsDirectControl;
        public AudioSource m_MovementAudio;
        public AudioClip m_EngineIdling;
        public AudioClip m_EngineDriving;
        public float m_PitchRange = 0.2f;
        public bool m_IsComputerControlled = false;
        [HideInInspector]
        public TankInputUser m_InputUser;

        // 💡 追加: TankHealth への参照
        private TankHealth m_TankHealth;

        public Rigidbody Rigidbody => m_Rigidbody;
        public int ControlIndex { get; set; } = -1;

        private string m_MovementAxisName;
        private string m_TurnAxisName;
        private Rigidbody m_Rigidbody;
        private float m_MovementInputValue;
        private float m_TurnInputValue;
        private Vector3 m_ExplosionForceValue;
        private float m_OriginalPitch;
        private ParticleSystem[] m_particleSystems;
        private InputAction m_MoveAction;
        private InputAction m_TurnAction;
        private Vector3 m_RequestedDirection;

        // ===================== 🧩 追加部分（砲塔関連） =====================
        [Header("Turret Control")]
        [Tooltip("砲塔のTransformを指定")]
        public Transform m_TurretTransform;          // 砲塔オブジェクト
        [Tooltip("砲塔の回転速度（deg/s）")]
        public float m_TurretTurnSpeedValue = 90f;   // 砲塔の回転速度
        private float m_TurretTurnInputValue;        // 入力値（-1〜1）
        private string m_TurretTurnActionName = "TurretTurn"; // InputAction名
        private InputAction m_TurretTurnAction;      // InputActionの参照

        [Tooltip("砲塔のHUD（照準UIなど）のTransformを指定")]
        public Transform m_TurretHUDTransform;       // 🆕 HUD追従用
        // ===============================================================

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null)
                m_InputUser = gameObject.AddComponent<TankInputUser>();

            // 💡 追加: TankHealth コンポーネントの参照を取得
            m_TankHealth = GetComponent<TankHealth>();
            if (m_TankHealth == null)
                Debug.LogWarning($"{name}: TankHealth コンポーネントが見つかりません。");

            // 🔹 Nullチェック
            if (m_TurretTransform == null)
                Debug.LogWarning($"{name}: Turret Transform is not assigned!");
            if (m_TurretHUDTransform == null)
                Debug.LogWarning($"{name}: Turret HUD Transform is not assigned!");
        }

        private void OnEnable()
        {
            m_Rigidbody.isKinematic = false;
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;
            m_TurretTurnInputValue = 0f;  // 🔹砲塔入力値リセット
            m_ExplosionForceValue = Vector3.zero;

            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in m_particleSystems) ps.Play();
        }

        private void OnDisable()
        {
            m_Rigidbody.isKinematic = true;
            foreach (var ps in m_particleSystems) ps.Stop();
        }

        private void Start()
        {
            if (m_IsComputerControlled)
            {
                var ai = GetComponent<TankAI>();
                if (ai == null) gameObject.AddComponent<TankAI>();
            }

            var mobileControl = FindAnyObjectByType<MobileUIControl>();
            if (mobileControl != null && ControlIndex == 1)
            {
                m_InputUser.SetNewInputUser(InputUser.PerformPairingWithDevice(mobileControl.Device));
                m_InputUser.ActivateScheme("Gamepad");
            }
            else
            {
                m_InputUser.ActivateScheme(ControlIndex == 1 ? "KeyboardLeft" : "KeyboardRight");
            }

            m_MovementAxisName = "Vertical";
            m_TurnAxisName = "Horizontal";
            m_MoveAction = m_InputUser.ActionAsset.FindAction(m_MovementAxisName);
            m_TurnAction = m_InputUser.ActionAsset.FindAction(m_TurnAxisName);

            // 🔹 砲塔の回転アクション設定
            m_TurretTurnAction = m_InputUser.ActionAsset.FindAction(m_TurretTurnActionName);
            if (m_TurretTurnAction != null)
            {
                m_TurretTurnAction.Enable();
            }
            else
            {
                Debug.LogWarning("TurretTurn action not found in Input Action Asset!");
            }

            m_MoveAction.Enable();
            m_TurnAction.Enable();

            if (m_MovementAudio)
                m_OriginalPitch = m_MovementAudio.pitch;
        }

        private void Update()
        {
            // 💡 修正点: 無敵状態でも入力値は取得し続ける（解除後の動作のため）
            if (!m_IsComputerControlled)
            {
                m_MovementInputValue = m_MoveAction.ReadValue<float>();
                m_TurnInputValue = m_TurnAction.ReadValue<float>();
                if (m_TurretTurnAction != null)
                    m_TurretTurnInputValue = m_TurretTurnAction.ReadValue<float>(); // 🔹砲塔入力取得
            }

            if (m_MovementAudio)
                EngineAudio();
        }

        private void FixedUpdate()
        {
            // 💡 修正点: 無敵状態（テレポート中）なら、移動・回転の物理処理を無視
            if (m_TankHealth != null && m_TankHealth.IsInvincible)
            {
                // Rigidbodyの速度をゼロにして、テレポート中の慣性を強制的に停止させる
                m_Rigidbody.linearVelocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            if (m_InputUser.InputUser.controlScheme.Value.name == "Gamepad" || m_IsDirectControl)
            {
                var camForward = Camera.main.transform.forward;
                camForward.y = 0;
                if (camForward.sqrMagnitude < 0.0001f)
                    camForward = Camera.main.transform.up;

                camForward.Normalize();
                var camRight = Vector3.Cross(Vector3.up, camForward);
                m_RequestedDirection = (camForward * m_MovementInputValue + camRight * m_TurnInputValue);
                m_RequestedDirection.Normalize();
            }

            Move();
            Turn();
            TurretTurn(); // 🔹砲塔回転呼び出し
        }

        private void Move()
        {
            float speedInput = m_IsDirectControl
                ? m_RequestedDirection.magnitude * (1.0f - Mathf.Clamp01((Vector3.Angle(m_RequestedDirection, transform.forward) - 90) / 90.0f))
                : m_MovementInputValue;

            Vector3 movement = transform.forward * speedInput * m_Speed;
            m_Rigidbody.linearVelocity = movement + m_ExplosionForceValue;
            m_ExplosionForceValue = Vector3.Lerp(m_ExplosionForceValue, Vector3.zero, Time.deltaTime * 3f);
        }

        private void Turn()
        {
            float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }

        // ===================== 🧠 新規メソッド：砲塔＋HUDの回転 =====================
        private void TurretTurn()
        {
            // FixedUpdateの冒頭で無敵チェックを行うため、ここでは不要だが、
            // 念のため、砲塔がない場合はスキップするチェックを残しておく
            if (m_TurretTransform == null) return; 

            float turn = m_TurretTurnInputValue * m_TurretTurnSpeedValue * Time.deltaTime;

            // 🔸ローカル回転をY軸で回す（Quaternionの掛け算順序に注意）
            m_TurretTransform.Rotate(0f, turn, 0f, Space.World);
            Quaternion rotation = Quaternion.Euler(0f, turn, 0f);
            // 🆕 HUD（照準UI）も同じ角度で回転
            if (m_TurretHUDTransform != null)
            {
                m_TurretHUDTransform.localRotation *= rotation;
            }
        }
        // =====================================================================

        private void EngineAudio()
        {
            if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
            {
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }

        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier = 0f)
        {
            Vector3 explosionDir = transform.position - explosionPosition;

            if (upwardsModifier != 0)
            {
                explosionDir.y += upwardsModifier;
                explosionDir.Normalize();
            }
            else
            {
                explosionDir = explosionDir.normalized;
            }

            float attenuation = 1f - Mathf.Clamp01(explosionDir.magnitude / explosionRadius);

            // ノックバックほぼゼロにしたい場合は、TankShootingで explosionForce を小さく設定する
            Vector3 velocityChange = explosionDir * (explosionForce * attenuation);

            // Rigidbody に適用
            m_ExplosionForceValue = velocityChange;

            // Rigidbody がある場合のみ適用
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
    }
}