using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Tanks.Complete
{
    public class TankAI : MonoBehaviour
    {
        enum State
        {
            Seek,
            Flee
        }

        private TankMovement m_Movement;
        private TankShooting m_Shooting;

        private float m_PathfindTime = 0.5f;
        private float m_PathfindTimer = 0.0f;

        private Transform m_CurrentTarget = null;
        private float m_MaxShootingDistance = 0.0f;

        private Vector3 m_LastTargetPosition;
        private float m_TimeSinceLastTargetMove;
        private float m_TimeCloseToTarget;

        private Vector3 m_FleeingLastPosition;
        private float m_SinceLastFleeingMove = 0.0f;

        private NavMeshPath m_CurrentPath = null;
        private int m_CurrentCorner = 0;
        private bool m_IsMoving = false;

        private GameObject[] m_AllTanks;

        private State m_CurrentState = State.Seek;

        private void Awake()
        {
            if (!isActiveAndEnabled)
                return;

            m_Movement = GetComponent<TankMovement>();
            m_Shooting = GetComponent<TankShooting>();

            m_Movement.m_IsComputerControlled = true;
            m_Shooting.m_IsComputerControlled = true;

            m_PathfindTime = Random.Range(0.3f, 0.6f);
            m_MaxShootingDistance = Vector3.Distance(m_Shooting.GetProjectilePosition(1.0f), transform.position);

            m_AllTanks = FindObjectsByType<TankMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Select(e => e.gameObject).ToArray();
        }

        public void Setup(GameManager manager)
        {
            m_AllTanks = manager.m_SpawnPoints.Select(e => e.m_Instance).ToArray();
        }

        public void TurnOff()
        {
            enabled = false;
        }

        void Update()
        {
            m_PathfindTimer += Time.deltaTime;

            // 🔹 AI も弾切れなら射撃しない
            if (m_Shooting.WeaponStockData.CurrentQuantity <= 0)
            {
                // ただ Seek 行動だけ続ける
                SeekUpdate();
                return;
            }

            switch (m_CurrentState)
            {
                case State.Seek:
                    SeekUpdate();
                    break;
                case State.Flee:
                    FleeUpdate();
                    break;
            }

            // 🔹 射撃可能なら発射するロジック
            TryShootTarget();
        }

        /// <summary>
        /// 🔹 AI の射撃判定を WeaponStockData に対応
        /// </summary>
        private void TryShootTarget()
        {
            if (m_CurrentTarget == null)
                return;

            float distance = Vector3.Distance(transform.position, m_CurrentTarget.position);

            // 射程内で
            if (distance < m_MaxShootingDistance)
            {
                // 🔹 TankShooting の通常 AI 射撃処理を呼ぶ
                if (!m_Shooting.m_IsComputerControlled)
                    return;

                // Charge → Fire
                m_Shooting.StartCharging();

                // AI は即射撃でOK
                if (distance < m_MaxShootingDistance * 0.9f)
                {
                    m_Shooting.StopCharging();
                }
            }
        }

        void SeekUpdate()
        {
            if (m_PathfindTimer > m_PathfindTime)
            {
                m_PathfindTimer = 0;
                NavMeshPath[] paths = new NavMeshPath[m_AllTanks.Length];
                float shortestPath = float.MaxValue;
                int usedPath = -1;
                Transform target = null;

                for (var i = 0; i < m_AllTanks.Length; i++)
                {
                    var tank = m_AllTanks[i];
                    if (tank == gameObject || tank == null || !tank.activeInHierarchy)
                        continue;

                    paths[i] = new NavMeshPath();
                    if (NavMesh.CalculatePath(transform.position, tank.transform.position, ~0, paths[i]))
                    {
                        float length = GetPathLength(paths[i]);
                        if (shortestPath > length)
                        {
                            usedPath = i;
                            shortestPath = length;
                            target = tank.transform;
                        }
                    }
                }

                if (usedPath != -1)
                {
                    m_CurrentTarget = target;
                    m_CurrentPath = paths[usedPath];
                    m_CurrentCorner = 1;
                    m_IsMoving = true;
                    m_LastTargetPosition = m_CurrentTarget.position;
                }
            }

            if (m_CurrentTarget != null)
            {
                float targetMovement = Vector3.Distance(m_CurrentTarget.position, m_LastTargetPosition);
                if (targetMovement < 0.001f)
                    m_TimeSinceLastTargetMove += Time.deltaTime;
                else
                    m_TimeSinceLastTargetMove = 0;

                m_LastTargetPosition = m_CurrentTarget.position;

                Vector3 toTarget = m_CurrentTarget.position - transform.position;
                toTarget.y = 0;
                float targetDistance = toTarget.magnitude;

                if (targetDistance < 3.0f)
                {
                    m_TimeCloseToTarget += Time.deltaTime;
                    if (m_TimeCloseToTarget > 2.0f)
                    {
                        StartFleeing();
                        return;
                    }
                }
                else
                {
                    m_TimeCloseToTarget = 0.0f;
                }
            }
        }

        private void FleeUpdate()
        {
            if (m_CurrentCorner >= m_CurrentPath.corners.Length)
                m_CurrentState = State.Seek;

            var distance = (transform.position - m_FleeingLastPosition).magnitude;
            m_FleeingLastPosition = transform.position;

            if (distance < 0.001f)
                m_SinceLastFleeingMove += Time.deltaTime;
            else
                m_SinceLastFleeingMove = 0;

            if (m_SinceLastFleeingMove > 2.0f)
                StartFleeing();
        }

        private void StartFleeing()
        {
            m_FleeingLastPosition = transform.position;
            m_SinceLastFleeingMove = 0.0f;

            var toTarget = (m_CurrentTarget.position - transform.position).normalized;
            toTarget = Quaternion.AngleAxis(Random.Range(90.0f, 180.0f) * Mathf.Sign(Random.Range(-1.0f, 1.0f)),
                Vector3.up) * toTarget;
            toTarget *= Random.Range(5.0f, 20.0f);

            if (NavMesh.CalculatePath(transform.position, transform.position + toTarget, NavMesh.AllAreas, m_CurrentPath))
            {
                m_CurrentState = State.Flee;
                m_CurrentCorner = 1;
                m_IsMoving = true;
            }
        }

        private void FixedUpdate()
        {
            if (m_CurrentPath == null || m_CurrentPath.corners.Length == 0)
                return;

            var rb = m_Movement.Rigidbody;
            Vector3 orientTarget = m_CurrentPath.corners[Mathf.Min(m_CurrentCorner, m_CurrentPath.corners.Length - 1)];
            if (!m_IsMoving)
                orientTarget = m_CurrentTarget.position;

            Vector3 toOrientTarget = orientTarget - transform.position;
            toOrientTarget.y = 0;
            toOrientTarget.Normalize();

            Vector3 forward = rb.rotation * Vector3.forward;
            float orientDot = Vector3.Dot(forward, toOrientTarget);
            float rotatingAngle = Vector3.SignedAngle(toOrientTarget, forward, Vector3.up);

            float moveAmount = Mathf.Clamp01(orientDot) * m_Movement.m_Speed * Time.deltaTime;
            if (m_IsMoving && moveAmount > 0.000001f)
                rb.MovePosition(rb.position + forward * moveAmount);

            rotatingAngle = Mathf.Sign(rotatingAngle) * Mathf.Min(Mathf.Abs(rotatingAngle), m_Movement.m_TurnSpeed * Time.deltaTime);
            if (Mathf.Abs(rotatingAngle) > 0.000001f)
                rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(-rotatingAngle, Vector3.up));

            if (Vector3.Distance(rb.position, orientTarget) < 0.5f)
                m_CurrentCorner += 1;
        }

        float GetPathLength(NavMeshPath path)
        {
            float dist = 0;
            for (var i = 1; i < path.corners.Length; ++i)
                dist += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return dist;
        }
    }
}
