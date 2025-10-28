using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Tanks.Complete
{
    /// <summary>
    /// Handle the tank control when the tank is set to Computer controlled
    /// </summary>
    public class TankAI : MonoBehaviour
    {
        // Possible state of the Computer controlled tank : either seeking itsd target or fleeing from it
        enum State
        {
            Seek,
            Flee
        }
    
        private TankMovement m_Movement;                // Reference to the movement script
        private TankShooting m_Shooting;                // Reference to the shooting script
        
        private float m_PathfindTime = 0.5f;            // Only trigger a pathfind after this time, to not degrade performance
        private float m_PathfindTimer = 0.0f;           // The time until the next pathfind call

        private Transform m_CurrentTarget = null;       // Which Transform the tank is following
        private float m_MaxShootingDistance = 0.0f;     // Store the max shooting distance based on TankShooting settings

        private float m_TimeBetweenShot = 2.0f;         // The AI Tank have a cooldown on shot to avoid spamming shot
        private float m_ShotCooldown = 0.0f;            // The remaining time until the next shot

        private Vector3 m_LastTargetPosition;           // The position of the target last frame
        private float m_TimeSinceLastTargetMove;        // Timer counting how long the target hasn't moved. This is used to trigger the flee state
        private float m_TimeCloseToTarget;

        private Vector3 m_FleeingLastPosition;          // Used to check how far we moved as we flee. If this doesn't change for a while, need to pick another point
        private float m_SinceLastFleeingMove = 0.0f;
        
        private NavMeshPath m_CurrentPath = null;       // The current path followed by the tank.
        private int m_CurrentCorner = 0;                // Which corner of the path the tank is currently going forward to 
        private bool m_IsMoving = false;                // Is the tank currently moving or not (the tank stop to shoot)

        private GameObject[] m_AllTanks;                // List of all the tanks in the scene.

        private State m_CurrentState = State.Seek;      // The current AI state the Tank is in.

        private void Awake()
        {
            //Awake is still called on disabled component. So that the user can test disabling AI on a single tank
            //we ensure that the component wasn't disabled before initializing everything
            if(!isActiveAndEnabled)
                return;
            
            m_Movement = GetComponent<TankMovement>();
            m_Shooting = GetComponent<TankShooting>();

            // ensure that both movement and shooting script are set in "computer controlled" mode
            m_Movement.m_IsComputerControlled = true;
            m_Shooting.m_IsComputerControlled = true;
            
            // to avoid all computer controlled tank pathfinding together (and taxing the CPU), AI tank have a random
            // pathfinding time that will stagger them across multiple frame
            m_PathfindTime = Random.Range(0.3f, 0.6f);
            
            // Compute and store what is the maximum distance a shot from this tank can reach. This will be used when deciding when
            // to start charging and when to release a shot
            m_MaxShootingDistance = Vector3.Distance(m_Shooting.GetProjectilePosition(1.0f), transform.position);
            
            // We use FindObjectByType to get all Tanks, to not depend on GameManager so user can try adding AI in an
            // empty scene where no GameManager was added yet.
            m_AllTanks = FindObjectsByType<TankMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(e => e.gameObject).ToArray();
        }

        // If a GameManager exist, it will call this function after creating a computer controlled tank. This just replace
        // the list of tanks with the one from the GameManager
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
            // If there is a cooldown active, we decrement it by the time elapsed since last frame
            if(m_ShotCooldown > 0)
                m_ShotCooldown -= Time.deltaTime;
            
            // increment the time since last pathfind
            m_PathfindTimer += Time.deltaTime;

            switch (m_CurrentState)
            {
                case State.Seek:
                    SeekUpdate();
                    break;
                case State.Flee:
                    FleeUpdate();
                    break;
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
                    var tank = m_AllTanks[i].gameObject;
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
                    if (target != m_CurrentTarget)
                    {
                        m_CurrentTarget = target;
                        m_LastTargetPosition = m_CurrentTarget.position;
                    }

                    m_CurrentTarget = target;
                    m_CurrentPath = paths[usedPath];
                    m_CurrentCorner = 1;
                    m_IsMoving = true;
                }
            }

            if (m_CurrentTarget != null)
            {
                float targetMovement = Vector3.Distance(m_CurrentTarget.position, m_LastTargetPosition);
                if (targetMovement < 0.001f)
                {
                    m_TimeSinceLastTargetMove += Time.deltaTime;
                }
                else
                {
                    m_TimeSinceLastTargetMove = 0;
                }

                m_LastTargetPosition = m_CurrentTarget.position;
                
                Vector3 toTarget = m_CurrentTarget.position - transform.position;
                toTarget.y = 0;
                
                float targetDistance = toTarget.magnitude;
                toTarget.Normalize();

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

                float dotToTarget = Vector3.Dot(toTarget, transform.forward);

                if (m_Shooting.IsCharging)
                {
                    Vector3 currentShotTarget = m_Shooting.GetProjectilePosition(m_Shooting.CurrentChargeRatio);
                    float currentShotDistance = Vector3.Distance(currentShotTarget, transform.position);

                    if (currentShotDistance >= targetDistance - 2 && dotToTarget > 0.99f)
                    {
                        m_IsMoving = false;
                        m_Shooting.StopCharging();
                        m_ShotCooldown = m_TimeBetweenShot;

                        if (m_TimeSinceLastTargetMove > 2.0f)
                        {
                            StartFleeing();
                        }
                    }
                }
                else
                {
                    if (targetDistance < m_MaxShootingDistance)
                    {
                        if (!NavMesh.Raycast(transform.position, m_CurrentTarget.position, out var hit, ~0))
                        {
                            m_IsMoving = false;

                            // 砲弾が残っている場合のみ発射を開始
                            if (m_ShotCooldown <= 0.0f && m_Shooting.m_CurrentShells > 0)
                            {
                                m_Shooting.StartCharging();
                            }
                        }
                    }
                }
            }
        }

        private void FleeUpdate()
        {
            if(m_CurrentCorner >= m_CurrentPath.corners.Length)
                m_CurrentState = State.Seek;
            
            var distance = (transform.position - m_FleeingLastPosition).magnitude;
            m_FleeingLastPosition = transform.position;

            if (distance < 0.001f)
            {
                m_SinceLastFleeingMove += Time.deltaTime;
            }
            else
            {
                m_SinceLastFleeingMove = 0;
            }

            if (m_SinceLastFleeingMove > 2.0f)
            {
                StartFleeing();
            }
        }

        private void StartFleeing()
        {
            m_FleeingLastPosition = transform.position;
            m_SinceLastFleeingMove = 0.0f;
            
            var toTarget = (m_CurrentTarget.position - transform.position).normalized;
            toTarget = Quaternion.AngleAxis(Random.Range(90.0f, 180.0f) * Mathf.Sign(Random.Range(-1.0f, 1.0f)),
                Vector3.up) * toTarget;
            toTarget *= Random.Range(5.0f, 20.0f);

            if (NavMesh.CalculatePath(transform.position, transform.position + toTarget, NavMesh.AllAreas,
                    m_CurrentPath))
            {
                m_CurrentState = State.Flee;
                m_CurrentCorner = 1;
                m_IsMoving = true;
            }
        }

        private void FixedUpdate()
        {
            if(m_CurrentPath == null || m_CurrentPath.corners.Length == 0)
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
            {
                rb.MovePosition(rb.position + forward * moveAmount);
            }

            rotatingAngle = Mathf.Sign(rotatingAngle) * Mathf.Min(Mathf.Abs(rotatingAngle), m_Movement.m_TurnSpeed * Time.deltaTime);
            if(Mathf.Abs(rotatingAngle) > 0.000001f)
                rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(-rotatingAngle, Vector3.up));

            if (Vector3.Distance(rb.position, orientTarget) < 0.5f)
            {
                m_CurrentCorner += 1;
            }
        }

        float GetPathLength(NavMeshPath path)
        {
            float dist = 0;
            for (var i = 1; i < path.corners.Length; ++i)
            {
                dist += Vector3.Distance(path.corners[i-1], path.corners[i]);
            }
            return dist;
        }
    }
}
