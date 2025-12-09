using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.Users;
using Camera = UnityEngine.Camera; // Cameraクラスの衝突回避のため明示的に指定

namespace Tanks.Complete
{
    [Serializable]
    public class TankManager
    {
        [HideInInspector] public Color m_PlayerColor;
        public Transform m_SpawnPoint;
        [HideInInspector] public int m_PlayerNumber;
        [HideInInspector] public string m_ColoredPlayerText;
        [HideInInspector] public GameObject m_Instance;
        [HideInInspector] public int m_Wins;
        [HideInInspector] public bool m_ComputerControlled;

        public int ControlIndex { get; set; } = 1;

        private TankMovement m_Movement;
        private TankShooting m_Shooting;
        private TankHealth m_Health; 
        private GameObject m_CanvasGameObject;
        private TankAI m_AI;
        private InputUser m_InputUser;
        private GameManager m_GameManager;

        // 💡 追加: プレイヤー番号(int)と勝利数(int)を引数に持つイベント
        public event Action<int, int> OnWinCountChanged; 

        // 💡 修正点 1: HPの変化を受け取るイベントを追加 (プレイヤー番号と正規化HPを引数とする)
        public event Action<int, float> OnHealthChanged;

        // 💡 修正点: イベントの引数に ControlIndex (int) を追加
        public event Action<int, Camera> OnMinimapCameraSpawned; 

        // =========================
        // MODIFIED: 武器ストックイベントに WeaponType を追加
        // =========================
        public event Action<int, WeaponType, WeaponStockData> OnWeaponStockChangedEvent; 

        // 地雷設置イベント（そのまま）
        public event Action<TankManager> OnMinePlaced;

        // =========================
        // 初期設定
        // =========================
        public void Setup(GameManager manager)
        {
            m_GameManager = manager;

            m_Movement = m_Instance.GetComponent<TankMovement>();
            m_Shooting = m_Instance.GetComponent<TankShooting>();
            m_Health = m_Instance.GetComponent<TankHealth>(); 
            m_AI = m_Instance.GetComponent<TankAI>();
            m_CanvasGameObject = m_Instance.GetComponentInChildren<Canvas>().gameObject;

            var inputUser = m_Instance.GetComponent<TankInputUser>();
            inputUser.SetNewInputUser(m_InputUser);

            m_Movement.m_IsComputerControlled = m_ComputerControlled;
            m_Shooting.m_IsComputerControlled = m_ComputerControlled;

            m_Movement.m_PlayerNumber = m_PlayerNumber;
            m_Movement.ControlIndex = ControlIndex;

            if (m_ComputerControlled)
            {
                if (m_AI == null)
                {
                    m_AI = m_Instance.AddComponent<TankAI>();
                    m_AI.Setup(manager);
                }
            }

            m_ColoredPlayerText = $"<color=#{ColorUtility.ToHtmlStringRGB(m_PlayerColor)}>PLAYER {m_PlayerNumber}</color>";

            MeshRenderer[] renderers = m_Instance.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                for (int j = 0; j < renderer.materials.Length; ++j)
                {
                    if (renderer.materials[j].name.Contains("TankColor"))
                        renderer.materials[j].color = m_PlayerColor;
                }
            }
            
            // 💡 修正点: スポーン時に MinimapCamera を取得し、ControlIndex と共にイベントで通知する
            Camera minimapCam = m_Instance.GetComponentInChildren<Camera>(true);
            
            if (minimapCam != null)
            {
                // ControlIndex を引数に追加
                OnMinimapCameraSpawned?.Invoke(ControlIndex, minimapCam); 
            }

            // =========================
            // 💡 修正点 2: TankHealth.OnHealthChanged のイベント購読
            // =========================
            if (m_Health != null)
            {
                m_Health.OnHealthChanged += (normalizedHealth) =>
                {
                    OnHealthChanged?.Invoke(m_PlayerNumber, normalizedHealth);
                };
            }
            else
            {
                Debug.LogWarning($"TankManager {m_PlayerNumber} の戦車インスタンスに TankHealth コンポーネントが見つかりません。");
            }

            // =========================
            // 💡 追加: GameManager.OnRoundWinnerChanged の購読
            // =========================
            if (m_GameManager != null)
            {
                // GameManagerからの勝利数変更イベントを購読し、受け取った値を再発火させる
                m_GameManager.OnRoundWinnerChanged += (playerNumber, winCount) =>
                {
                    // 勝利したプレイヤー番号とこのTankManagerのプレイヤー番号が一致する場合のみ処理
                    if (playerNumber == m_PlayerNumber)
                    {
                        OnWinCountChanged?.Invoke(playerNumber, winCount);
                    }
                };
                
                // 💡 追加: 初期状態（勝利数0）を一度通知する
                OnWinCountChanged?.Invoke(m_PlayerNumber, m_Wins);
            }


            // =========================
            // TankShooting のイベント登録
            // =========================

            // 🔵 砲弾の所持数が変わったとき
            m_Shooting.OnShellStockChanged += (shellCount) =>
            {
                // MODIFIED: WeaponType を追加
                OnWeaponStockChangedEvent?.Invoke(ControlIndex, WeaponType.Shell, m_Shooting.WeaponStockData);
            }
            ;

            // 🔴 地雷の所持数が変わったとき
            m_Shooting.OnMineStockChanged += (mineCount) =>
            {
                // MODIFIED: WeaponType を追加
                OnWeaponStockChangedEvent?.Invoke(ControlIndex, WeaponType.Mine, m_Shooting.MineStockData);
            };

            // 地雷設置時のイベント
            m_Shooting.OnMinePlaced += OnMinePlacedHandler;
            Debug.Log($"{m_PlayerNumber}: ControlIndex={ControlIndex}");

        }

        // 💡 【確定】毎ラウンドのリソースリセットメソッド
        public void ResetResources()
        {
            // TankShooting にリソースのリセットを依頼する
            if (m_Shooting != null)
            {
                m_Shooting.ResetResources(); 
            }
            
            // Health のリセットもここで行う (TankHealth.cs に Reset() が必要)
            if (m_Health != null)
            {
                // TankHealth.cs に public void Reset() メソッドを追加したのでエラーは解消されます
                m_Health.Reset(); 
            }
        }

        // =========================
        // 地雷設置イベント通知
        // =========================
        private void OnMinePlacedHandler()
        {
            OnMinePlaced?.Invoke(this);

            if (m_GameManager != null)
            {
                m_GameManager.StartCoroutine(PlaceMineRoutine());
            }
        }

        // =========================
        // コントロール無効化／有効化
        // =========================
        public void DisableControl()
        {
            m_Movement.enabled = false;
            m_Shooting.enabled = false;
            if (m_ComputerControlled && m_AI != null)
                m_AI.enabled = false;

            m_CanvasGameObject.SetActive(false);
        }

        public void EnableControl()
        {
            m_Movement.enabled = true;
            m_Shooting.enabled = true;
            if (m_ComputerControlled && m_AI != null)
                m_AI.enabled = true;

            m_CanvasGameObject.SetActive(true);
        }

        // =========================
        // リセット
        // =========================
        public void Reset()
        {
            m_Instance.transform.position = m_SpawnPoint.position;
            m_Instance.transform.rotation = m_SpawnPoint.rotation;

            m_Instance.SetActive(false);
            m_Instance.SetActive(true);
        }

        // =========================
        // 地雷設置コルーチン
        // =========================
        private IEnumerator PlaceMineRoutine()
        {
            DisableControl();
            yield return new WaitForSeconds(0.5f);
            EnableControl();
        }
    }
}