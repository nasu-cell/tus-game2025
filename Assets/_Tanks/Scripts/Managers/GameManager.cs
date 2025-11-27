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
        // ==============================
        // ゲーム状態管理用の列挙体
        // ==============================
        public enum GameLoopState
        {
            RoundStarting,  // ゲームの開始処理中
            RoundPlaying,   // プレイ中
            RoundEnding     // 終了処理中
        }

        // Which state the game is currently in
        public enum GameState
        {
            MainMenu,
            Game
        }

        // Data about the selected tanks passed from the menu to the GameManager
        public class PlayerData
        {
            public bool IsComputer;
            public Color TankColor;
            public GameObject UsedPrefab;
            public int ControlIndex;
        }

        public int m_NumRoundsToWin = 5;
        public float m_StartDelay = 3f;
        public float m_EndDelay = 3f;

        // CameraRigオブジェクトのTPSCameraControlクラスの参照を保持するフィールド
        public TPSCameraControl m_TPSCameraControl;

        [Header("Tanks Prefabs")]
        public GameObject m_Tank1Prefab;
        public GameObject m_Tank2Prefab;
        public GameObject m_Tank3Prefab;
        public GameObject m_Tank4Prefab;

        [FormerlySerializedAs("m_Tanks")]
        public TankManager[] m_SpawnPoints;

        private GameState m_CurrentState;
        private GameLoopState m_CurrentLoopState;   // 現在のラウンド状態を保持

        // ==============================
        // 状態変更イベント
        // ==============================
        public event Action<GameLoopState> OnGameStateChanged;

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

            switch (m_CurrentState)
            {
                case GameState.Game:
                    GameStart();
                    break;
            }
        }

        public void StartGame(PlayerData[] playerData)
        {
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
                    Instantiate(playerData.UsedPrefab, m_SpawnPoints[i].m_SpawnPoint.position, m_SpawnPoints[i].m_SpawnPoint.rotation) as GameObject;

                var mov = m_SpawnPoints[i].m_Instance.GetComponent<TankMovement>();
                mov.m_IsComputerControlled = false;

                m_SpawnPoints[i].m_PlayerNumber = i + 1;
                m_SpawnPoints[i].ControlIndex = playerData.ControlIndex;
                m_SpawnPoints[i].m_PlayerColor = playerData.TankColor;
                m_SpawnPoints[i].m_ComputerControlled = playerData.IsComputer;
            }

            foreach (var tank in m_SpawnPoints)
            {
                if (tank.m_Instance == null)
                    continue;

                tank.Setup(this);
            }
        }

        private void SetCameraTargets()
        {
            if (m_PlayerCount <= 0 || m_TPSCameraControl == null)
            {
                return;
            }

            Transform targetTransform = null;
            TankManager playerOneTank = null;

            // プレイヤー1のタンクを ControlIndex (1) で検索する
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

                // 2階層下の砲塔を検索するロジック
                Transform modelRoot = null;
                Transform turret = null;
                string turretName = string.Empty; // どの砲塔が設定されたか記録

                // 1. 中間ノードを検索
                modelRoot = playerTankInstance.Find("Tank_Alternative_Model");
                if (modelRoot == null)
                {
                    modelRoot = playerTankInstance.Find("Tank_Heavy_Model");
                }

                if (modelRoot != null)
                {
                    // 2. 中間ノードから砲塔を検索
                    turret = modelRoot.Find("TankTurret.001");
                    if (turret != null)
                    {
                        turretName = "TankTurret.001";
                    }
                    else
                    {
                        turret = modelRoot.Find("TankHeavyTurret");
                        if (turret != null)
                        {
                            turretName = "TankHeavyTurret";
                        }
                    }
                }

                // 3. 砲塔が見つかった場合の処理
                if (turret != null)
                {
                    // ターゲットを砲塔にする
                    targetTransform = turret;

                    // ローカル回転を調整量として取得
                    rotOffsetAdjustment = new Vector3(turret.localEulerAngles.x, turret.localEulerAngles.y, 0);

                    if (turretName == "TankTurret.001")
                    {
                        // 🚨 修正箇所: X軸に180度を加算 (または Y軸に 180度を加算) して反転を打ち消す
                        // TankTurret.001 のローカル X=180 の影響をここで相殺させるために、X軸に180度を設定
                        // これにより、rotOffsetAdjustment が (180, Y, 0) となる
                        rotOffsetAdjustment.x = 180f;
                        rotOffsetAdjustment.y = 360f;
                        Debug.Log($"[TPS Camera Rot Adjust] {turretName} にはX軸回転補正 180度を適用します。");
                    }

                    Debug.Log($"[TPS Camera Target] ターゲットを砲塔に設定しました。オブジェクト名: {turretName}. 最終補正値: {rotOffsetAdjustment}");
                }
                else
                {
                    // 砲塔が見つからない場合は戦車本体がターゲット
                    Debug.Log($"[TPS Camera Target] 砲塔が見つからなかったため、ターゲットを戦車本体に設定しました。オブジェクト名: {playerTankInstance.name}");
                }

                // TPSCameraControl のターゲットと回転オフセットを更新
                m_TPSCameraControl.SetTarget(targetTransform);
                m_TPSCameraControl.AdjustRotOffset(rotOffsetAdjustment);
            }
            else
            {
                Debug.LogError("[GameManager] プレイヤー1の戦車が見つかりませんでした。");
            }
        }

        // ==============================
        // ゲームループ本体
        // ==============================
        private IEnumerator GameLoop()
        {
            // RoundStarting
            SetGameState(GameLoopState.RoundStarting);
            yield return StartCoroutine(RoundStarting());

            // RoundPlaying
            SetGameState(GameLoopState.RoundPlaying);
            yield return StartCoroutine(RoundPlaying());

            // RoundEnding
            SetGameState(GameLoopState.RoundEnding);
            yield return StartCoroutine(RoundEnding());

            if (m_GameWinner != null)
            {
                SceneManager.LoadScene(0);
            }
            else
            {
                StartCoroutine(GameLoop());
            }
        }

        // ==============================
        // 状態更新メソッド
        // ==============================
        private void SetGameState(GameLoopState newState)
        {
            if (m_CurrentLoopState != newState)
            {
                m_CurrentLoopState = newState;
                OnGameStateChanged?.Invoke(newState);   // 状態変更イベント発火
                Debug.Log($"[GameManager] Game state changed to: {newState}");
            }
        }

        private IEnumerator RoundStarting()
        {
            // 俯瞰視点の初期化処理を削除
            ResetAllTanks();
            DisableTankControl();

            if (m_TPSCameraControl != null)
            {
                // TPSCameraControl の初期位置/回転設定メソッドを呼び出す
                m_TPSCameraControl.SetStartPositionAndRotation();
            }

            m_RoundNumber++;
            m_TitleText.text = "ROUND " + m_RoundNumber;

            yield return m_StartWait;
        }

        private IEnumerator RoundPlaying()
        {
            EnableTankControl();
            m_TitleText.text = string.Empty;

            while (!OneTankLeft())
            {
                yield return null;
            }
        }

        private IEnumerator RoundEnding()
        {
            DisableTankControl();

            m_RoundWinner = null;
            m_RoundWinner = GetRoundWinner();

            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;

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
                {
                    lastAlive = m_SpawnPoints[i];
                }
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

        private string EndMessage()
        {
            string message = "DRAW!";

            // ラウンド勝者の表示
            if (m_RoundWinner != null)
                message = $"PLAYER {m_RoundWinner.ControlIndex} WINS THE ROUND!";

            message += "\n\n\n\n";

            // 全プレイヤーの勝利数表示
            for (int i = 0; i < m_PlayerCount; i++)
            {
                message += $"PLAYER {m_SpawnPoints[i].ControlIndex}: {m_SpawnPoints[i].m_Wins} WINS\n";
            }

            // ゲーム勝者の表示
            if (m_GameWinner != null)
                message = $"PLAYER {m_GameWinner.ControlIndex} WINS THE GAME!";

            return message;
        }

        private void ResetAllTanks()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                m_SpawnPoints[i].Reset();
            }
        }

        private void EnableTankControl()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                m_SpawnPoints[i].EnableControl();
            }
        }

        private void DisableTankControl()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                m_SpawnPoints[i].DisableControl();
            }
        }
    }
}