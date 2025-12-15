using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Tanks.Complete
{
    public class GameManager : MonoBehaviour
    {
        public enum GameLoopState
        {
            RoundStarting,
            RoundPlaying,
            RoundEnding
        }

        public enum GameState
        {
            MainMenu,
            Game
        }

        public class PlayerData
        {
            public bool IsComputer;
            public Color TankColor;
            public GameObject UsedPrefab;
            public int ControlIndex;
        }

        public int m_NumRoundsToWin;
        public float m_StartDelay;
        public float m_EndDelay;

        public TPSCameraControl m_TPSCameraControl;

        [Header("Tanks Prefabs")]
        public GameObject m_Tank1Prefab;
        public GameObject m_Tank2Prefab;
        public GameObject m_Tank3Prefab;
        public GameObject m_Tank4Prefab;

        public TankManager[] m_SpawnPoints;

        private GameState m_CurrentState;
        private GameLoopState m_CurrentLoopState;

        public event Action<GameLoopState> OnGameStateChanged;
        public event Action<int, int> OnRoundWinnerChanged;

        private int m_RoundNumber;
        private WaitForSeconds m_StartWait;
        private WaitForSeconds m_EndWait;

        private TankManager m_RoundWinner;
        private TankManager m_GameWinner;

        private PlayerData[] m_TankData;
        private int m_PlayerCount = 0;
        private TextMeshProUGUI m_TitleText;


        private void Start()
        {
            m_CurrentState = GameState.MainMenu;

            var textRef = FindAnyObjectByType<MessageTextReference>(FindObjectsInactive.Include);
            if (textRef == null)
            {
                Debug.LogError("You need to add the Menus prefab in the scene to use the GameManager!");
                return;
            }

            m_TitleText = textRef.Text;
            m_TitleText.text = "";

            if (m_Tank1Prefab == null || m_Tank2Prefab == null || m_Tank3Prefab == null || m_Tank4Prefab == null)
            {
                Debug.LogError("You need to assign 4 tank prefab in the GameManager!");
            }
        }

        void GameStart()
        {
            m_StartWait = new WaitForSeconds(m_StartDelay);
            m_EndWait = new WaitForSeconds(m_EndDelay);

            SpawnAllTanks();
            SetCameraTargets();

            StartCoroutine(GameLoop());
        }

        void ChangeGameState(GameState newState)
        {
            m_CurrentState = newState;

            if (newState == GameState.Game)
            {
                GameStart();
            }
        }

        public void StartGame(PlayerData[] playerData)
        {
            Array.Sort(playerData, (a, b) => a.ControlIndex.CompareTo(b.ControlIndex));
            m_TankData = playerData;
            m_PlayerCount = m_TankData.Length;
            ChangeGameState(GameState.Game);
        }

        private void SpawnAllTanks()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                var playerData = m_TankData[i];

                m_SpawnPoints[i].m_Instance =
                    Instantiate(playerData.UsedPrefab, m_SpawnPoints[i].m_SpawnPoint.position,
                    m_SpawnPoints[i].m_SpawnPoint.rotation) as GameObject;

                var mov = m_SpawnPoints[i].m_Instance.GetComponent<TankMovement>();
                mov.m_IsComputerControlled = false;

                m_SpawnPoints[i].m_PlayerNumber = i + 1;
                m_SpawnPoints[i].ControlIndex = playerData.ControlIndex;
                m_SpawnPoints[i].m_PlayerColor = playerData.TankColor;
                m_SpawnPoints[i].m_ComputerControlled = playerData.IsComputer;
            }

            foreach (var tank in m_SpawnPoints)
            {
                if (tank.m_Instance != null)
                    tank.Setup(this);
            }
        }

        private void SetCameraTargets()
        {
            if (m_PlayerCount <= 0 || m_TPSCameraControl == null)
                return;

            Transform targetTransform = null;
            TankManager playerOneTank = null;

            for (int i = 0; i < m_PlayerCount; i++)
            {
                if (m_SpawnPoints[i].ControlIndex == 1)
                {
                    playerOneTank = m_SpawnPoints[i];
                    break;
                }
            }

            if (playerOneTank != null && playerOneTank.m_Instance != null)
            {
                Transform playerTankInstance = playerOneTank.m_Instance.transform;
                targetTransform = playerTankInstance;

                Vector3 rotOffsetAdjustment = Vector3.zero;

                Transform modelRoot = null;
                Transform turret = null;

                modelRoot = playerTankInstance.Find("Tank_Alternative_Model");
                if (modelRoot == null)
                    modelRoot = playerTankInstance.Find("Tank_Heavy_Model");

                if (modelRoot != null)
                {
                    turret = modelRoot.Find("TankTurret.001");
                    if (turret == null)
                        turret = modelRoot.Find("TankHeavyTurret");
                }

                if (turret != null)
                {
                    targetTransform = turret;

                    rotOffsetAdjustment = new Vector3(turret.localEulerAngles.x, turret.localEulerAngles.y, 0);

                    if (turret.name == "TankTurret.001")
                    {
                        rotOffsetAdjustment.x = 180f;
                        rotOffsetAdjustment.y = 360f;
                    }

                    m_TPSCameraControl.SetTarget(targetTransform);
                    m_TPSCameraControl.AdjustRotOffset(rotOffsetAdjustment);
                }
                else
                {
                    m_TPSCameraControl.SetTarget(targetTransform);
                    m_TPSCameraControl.AdjustRotOffset(rotOffsetAdjustment);
                }
            }
        }


        // ============================================
        //                GAME LOOP
        // ============================================
        private IEnumerator GameLoop()
        {
            SetGameState(GameLoopState.RoundStarting);
            yield return StartCoroutine(RoundStarting());

            SetGameState(GameLoopState.RoundPlaying);
            yield return StartCoroutine(RoundPlaying());

            SetGameState(GameLoopState.RoundEnding);
            yield return StartCoroutine(RoundEnding());

            // 🎯【修正ポイント】ゲーム勝者が出たらロビーへ移動
            if (m_GameWinner != null)
            {
                // RoundEnding の m_EndWait では短いので、さらに 3 秒表示
                yield return new WaitForSeconds(0.5f);

                SceneManager.LoadScene(SceneNames.LobbyScene);
                yield break;
            }

            // ゲーム続行
            StartCoroutine(GameLoop());
        }


        private void SetGameState(GameLoopState newState)
        {
            if (m_CurrentLoopState != newState)
            {
                m_CurrentLoopState = newState;
                OnGameStateChanged?.Invoke(newState);
            }
        }

        private IEnumerator RoundStarting()
        {
            ResetAllTanks();
            DisableTankControl();
            ResetAllTankResources();

            if (m_TPSCameraControl != null)
                m_TPSCameraControl.SetStartPositionAndRotation();

            m_RoundNumber++;
            m_TitleText.text = "ROUND " + m_RoundNumber;

            yield return m_StartWait;
        }

        private IEnumerator RoundPlaying()
        {
            EnableTankControl();
            m_TitleText.text = "";

            while (!OneTankLeft())
                yield return null;
        }

        private IEnumerator RoundEnding()
        {
            DisableTankControl();

            m_RoundWinner = GetRoundWinner();

            if (m_RoundWinner != null)
            {
                m_RoundWinner.m_Wins++;
                OnRoundWinnerChanged?.Invoke(m_RoundWinner.m_PlayerNumber, m_RoundWinner.m_Wins);
            }

            m_GameWinner = GetGameWinner();

            string message = EndMessage();
            m_TitleText.text = message;

            yield return m_EndWait;
        }

        private bool OneTankLeft()
        {
            int numTanksLeft = 0;
            for (int i = 0; i < m_PlayerCount; i++)
            {
                if (m_SpawnPoints[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }
            return numTanksLeft <= 1;
        }

        private TankManager GetRoundWinner()
        {
            TankManager lastAlive = null;

            for (int i = 0; i < m_PlayerCount; i++)
            {
                if (m_SpawnPoints[i].m_Instance.activeSelf)
                    lastAlive = m_SpawnPoints[i];
            }

            return lastAlive;
        }

        private TankManager GetGameWinner()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                if (m_SpawnPoints[i].m_Wins == m_NumRoundsToWin)
                    return m_SpawnPoints[i];
            }
            return null;
        }


        // ============================================
        //          ★ 修正版 EndMessage()
        // ============================================
        private string EndMessage()
        {
            string message = "DRAW!";

            if (m_RoundWinner != null)
                message = $"PLAYER {m_RoundWinner.m_PlayerNumber} WINS THE ROUND!";

            message += "\n\n\n\n";

            for (int i = 0; i < m_PlayerCount; i++)
            {
                message += $"PLAYER {m_SpawnPoints[i].m_PlayerNumber}: {m_SpawnPoints[i].m_Wins} WINS\n";
            }

            if (m_GameWinner != null)
            {
                message = $"PLAYER {m_GameWinner.m_PlayerNumber} WINS THE GAME!";
            }

            return message;
        }


        private void ResetAllTanks()
        {
            for (int i = 0; i < m_PlayerCount; i++)
                m_SpawnPoints[i].Reset();
        }

        private void ResetAllTankResources()
        {
            for (int i = 0; i < m_PlayerCount; i++)
                m_SpawnPoints[i].ResetResources();
        }

        private void EnableTankControl()
        {
            for (int i = 0; i < m_PlayerCount; i++)
                m_SpawnPoints[i].EnableControl();
        }

        private void DisableTankControl()
        {
            for (int i = 0; i < m_PlayerCount; i++)
                m_SpawnPoints[i].DisableControl();
        }
    }
}
