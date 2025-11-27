using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.Users;

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
        private GameObject m_CanvasGameObject;
        private TankAI m_AI;
        private InputUser m_InputUser;
        private GameManager m_GameManager;

        // =========================
        // MODIFIED: 武器ストックイベントに WeaponType を追加
        // =========================
        public event Action<int, WeaponType, WeaponStockData> OnWeaponStockChangedEvent;  // MODIFIED

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

            // =========================
            // TankShooting のイベント登録
            // =========================

            // 🔵 砲弾の所持数が変わったとき
            m_Shooting.OnShellStockChanged += (shellCount) =>
            {
                // MODIFIED: WeaponType を追加
                OnWeaponStockChangedEvent?.Invoke(ControlIndex, WeaponType.Shell, m_Shooting.WeaponStockData); // MODIFIED
            };

            // 🔴 地雷の所持数が変わったとき
            m_Shooting.OnMineStockChanged += (mineCount) =>
            {
                // MODIFIED: WeaponType を追加
                OnWeaponStockChangedEvent?.Invoke(ControlIndex, WeaponType.Mine, m_Shooting.MineStockData); // MODIFIED
            };

            // 地雷設置時のイベント
            m_Shooting.OnMinePlaced += OnMinePlacedHandler;
            Debug.Log($"{m_PlayerNumber}: ControlIndex={ControlIndex}");

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
