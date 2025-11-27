using UnityEngine;
using System.Collections;

namespace Tanks.Complete
{
    public class ShellExplosion : MonoBehaviour
    {
        public LayerMask m_TankMask;
        public ParticleSystem m_ExplosionParticles;
        public AudioSource m_ExplosionAudio;

        [HideInInspector] public float m_MaxLifeTime = 2f;   // Shell は自動消滅
        [HideInInspector] public float m_MaxDamage = 100f;
        [HideInInspector] public float m_ExplosionForce = 1f;
        [HideInInspector] public float m_ExplosionRadius = 1f;

        private bool m_CanExplode = true;  // Mine用
        private float m_ArmDelay = 2f;     // Mine設置後の待機時間

        private void Start()
        {
            if (tag != "Mine")
            {
                Destroy(gameObject, m_MaxLifeTime);
            }
            else
            {
                m_CanExplode = false;
                StartCoroutine(EnableMineAfterDelay());
            }
        }

        private IEnumerator EnableMineAfterDelay()
        {
            yield return new WaitForSeconds(m_ArmDelay);
            m_CanExplode = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (tag == "Mine" && !m_CanExplode)
                return; // 設置直後は爆発しない

            // Shell / Mine で威力を分ける
            float explosionForce = m_ExplosionForce;
            float maxDamage = m_MaxDamage;

            if (tag == "Mine")
            {
                explosionForce *= 0.05f;  // ノックバックほぼゼロ
                maxDamage *= 0.8f;        // ダメージ少し控えめ
            }
            else if (tag == "Shell")
            {
                explosionForce *= 0.05f;
                maxDamage *= 1f;
            }

            // 対象のTankに爆風を与える
            Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);
            for (int i = 0; i < colliders.Length; i++)
            {
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();
                if (!targetRigidbody) continue;

                TankMovement movement = targetRigidbody.GetComponent<TankMovement>();
                if (movement != null)
                    movement.AddExplosionForce(explosionForce, transform.position, m_ExplosionRadius);

                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
                if (targetHealth != null)
                    targetHealth.TakeDamage(CalculateDamage(targetRigidbody.position, maxDamage));
            }

            // エフェクト再生
            m_ExplosionParticles.transform.parent = null;
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();

            ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
            Destroy(m_ExplosionParticles.gameObject, mainModule.duration);

            Destroy(gameObject);
        }

        private float CalculateDamage(Vector3 targetPosition, float maxDamage)
        {
            Vector3 explosionToTarget = targetPosition - transform.position;
            float explosionDistance = explosionToTarget.magnitude;
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;
            float damage = relativeDistance * maxDamage;
            return Mathf.Max(0f, damage);
        }
    }
}
