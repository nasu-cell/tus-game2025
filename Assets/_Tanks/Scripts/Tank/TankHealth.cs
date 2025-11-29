using System; // Actionデリゲートを使用するために必要
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class TankHealth : MonoBehaviour
    {
        // ==============================
        // 💡 修正点 1: HPの変化を受け取るイベントを追加 (正規化されたHPを引数とする)
        // ==============================
        public event Action<float> OnHealthChanged;
        
        public float m_StartingHealth = 100f;               // The amount of health each tank starts with.
        public Slider m_Slider;                             // The slider to represent how much health the tank currently has.
        public Image m_FillImage;                           // The image component of the slider.
        public Color m_FullHealthColor = Color.green;    // The color the health bar will be when on full health.
        public Color m_ZeroHealthColor = Color.red;      // The color the health bar will be when on no health.
        public GameObject m_ExplosionPrefab;                // A prefab that will be instantiated in Awake, then used whenever the tank dies.
        [HideInInspector] public bool m_HasShield;          // Has the tank picked up a shield power up?

        // ワームホール用: 無敵状態タイマー
        private float m_InvincibilityTimer;
        // ワームホール用: 戦車のレンダラー配列
        private Renderer[] m_Renderers;

        private AudioSource m_ExplosionAudio;               // The audio source to play when the tank explodes.
        private ParticleSystem m_ExplosionParticles;        // The particle system the will play when the tank is destroyed.
        private float m_CurrentHealth;                      // How much health the tank currently has.
        private bool m_Dead;                                // Has the tank been reduced beyond zero health yet?
        private float m_ShieldValue;                        // Percentage of reduced damage when the tank has a shield.
        
        // 無敵状態かどうかの判定を行う bool 型プロパティ
        public bool IsInvincible
        {
            get { return m_InvincibilityTimer > 0f; }
        }

        private void Awake ()
        {
            // Instantiate the explosion prefab and get a reference to the particle system on it.
            m_ExplosionParticles = Instantiate (m_ExplosionPrefab).GetComponent<ParticleSystem> ();

            // Get a reference to the audio source on the instantiated prefab.
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource> ();

            // Disable the prefab so it can be activated when it's required.
            m_ExplosionParticles.gameObject.SetActive (false);
            
            // Set the slider max value to the max health the tank can have
            m_Slider.maxValue = m_StartingHealth;

            // Renderer 配列の初期化 (自身の子オブジェクトを含む全ての Renderer を取得)
            m_Renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnDestroy()
        {
            if(m_ExplosionParticles != null)
                Destroy(m_ExplosionParticles.gameObject);
        }

        private void OnEnable()
        {
            // When the tank is enabled, reset the tank's health and whether or not it's dead.
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            m_HasShield = false;
            m_ShieldValue = 0;
            m_InvincibilityTimer = 0f; // タイマーをリセット

            // すべての Renderer を有効にする（無敵状態でないことを保証）
            SetRenderersEnabled(true);

            // Update the health slider's value and color.
            // 💡 修正: HPリセット時にUI更新とイベント発火
            SetHealthUI();
        }

        // m_InvincibilityTimer の減少と点滅処理
        private void Update()
        {
            if (m_InvincibilityTimer > 0f)
            {
                // タイマーを減少させる
                m_InvincibilityTimer -= Time.deltaTime;

                // 無敵状態の最中は、戦車を点滅させる処理
                // 0.1秒ごとに enabled を切り替える
                bool isVisible = Mathf.FloorToInt(Time.time * 10f) % 2 == 0;
                SetRenderersEnabled(isVisible);

                // タイマーが0になったら点滅を停止し、Renderer を有効に戻す
                if (m_InvincibilityTimer <= 0f)
                {
                    SetRenderersEnabled(true);
                }
            }
        }

        public void TakeDamage (float amount)
        {
            // 無敵状態かどうかを IsInvincible プロパティで判定
            if (!IsInvincible)
            {
                // Reduce current health by the amount of damage done.
                m_CurrentHealth -= amount * (1 - m_ShieldValue);

                // Change the UI elements appropriately.
                // 💡 修正: HP変更が発生したのでUI更新とイベント発火
                SetHealthUI ();

                // If the current health is at or below zero and it has not yet been registered, call OnDeath.
                if (m_CurrentHealth <= 0f && !m_Dead)
                {
                    OnDeath ();
                }
            }
        }

        // 無敵状態を発動させる ActivateInvincibility メソッド
        public void ActivateInvincibility(float duration)
        {
            // 新しい無敵時間がタイマーより長い場合にのみ更新（より長い無敵時間を優先）
            if (duration > m_InvincibilityTimer)
            {
                m_InvincibilityTimer = duration;
            }
            // タイマーがリセットされた場合、Rendererを有効にする（Updateで点滅が開始されるため）
            if (m_Renderers.Length > 0 && !m_Renderers[0].enabled)
            {
                SetRenderersEnabled(true);
            }
        }


        public void IncreaseHealth(float amount)
        {
            float previousHealth = m_CurrentHealth; // HPが実際に増加したか判定するため

            // Check if adding the amount would keep the health within the maximum limit
            if (m_CurrentHealth + amount <= m_StartingHealth)
            {
                // If the new health value is within the limit, add the amount
                m_CurrentHealth += amount;
            }
            else
            {
                // If the new health exceeds the starting health, set it at the maximum
                m_CurrentHealth = m_StartingHealth;
            }
            
            // HPが実際に変化した場合のみUIとイベントを更新
            if (m_CurrentHealth != previousHealth)
            {
                // Change the UI elements appropriately.
                // 💡 修正: HP変更が発生したのでUI更新とイベント発火
                SetHealthUI();
            }
        }


        public void ToggleShield (float shieldAmount)
        {
            // Inverts the value of has shield.
            m_HasShield = !m_HasShield;

            // Stablish the amount of damage that will be reduced by the shield
            if (m_HasShield)
            {
                m_ShieldValue = shieldAmount;
            }
            else
            {
                m_ShieldValue = 0;
            }
        }
        
        // Renderer の有効/無効を一括で切り替えるヘルパーメソッド
        private void SetRenderersEnabled(bool enabled)
        {
            foreach (var renderer in m_Renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }

        private void SetHealthUI ()
        {
            // Set the slider's value appropriately.
            m_Slider.value = m_CurrentHealth;

            // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health.
            float healthRatio = m_CurrentHealth / m_StartingHealth;
            m_FillImage.color = Color.Lerp (m_ZeroHealthColor, m_FullHealthColor, healthRatio);

            // ==============================
            // 💡 修正点 2: HPの変化イベントを発火させる
            // ==============================
            OnHealthChanged?.Invoke(healthRatio);
        }


        private void OnDeath ()
        {
            // Set the flag so that this function is only called once.
            m_Dead = true;

            // Move the instantiated explosion prefab to the tank's position and turn it on.
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive (true);

            // Play the particle system of the tank exploding.
            m_ExplosionParticles.Play ();

            // Play the tank explosion sound effect.
            m_ExplosionAudio.Play();

            // Turn the tank off.
            gameObject.SetActive (false);
        }
    }
}