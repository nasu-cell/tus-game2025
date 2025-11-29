using UnityEngine;
using Tanks.Complete;
using System.Linq;

namespace Tanks.Complete
{
    public class TPSCameraControl : MonoBehaviour
    {
        // GameManagerクラスのインスタンスを保持する変数 (publicに変更)
        // 🚨 インスペクターで手動割り当てが必要です
        public GameManager gameManager;

        // ラウンド中かどうかを示すbool型の変数
        private bool isRoundPlaying;

        // 追従対象のタンクのTransformコンポーネントを保持する変数
        private Transform target;

        // カメラとタンクの相対的な位置を指定するVector3型の変数
        [SerializeField]
        private Vector3 posOffset = new Vector3(0f, 5f, -7f);

        // カメラとタンクの相対的な回転を指定するVector3型の変数
        [SerializeField]
        private Vector3 rotOffset = new Vector3(30f, 0f, 0f);

        // 🚨 Awake() メソッドは削除されました

        private void Start()
        {
            if (gameManager != null)
            {
                // gameManager.OnGameStateChanged イベントに、作成した OnGameStateChanged メソッドを登録
                gameManager.OnGameStateChanged += OnGameStateChanged;

                // GameManagerに自身のインスタンスを登録させる
                gameManager.m_TPSCameraControl = this;
            }
            else
            {
                Debug.LogError("TPS Camera: GameManagerフィールドにインスタンスが割り当てられていません。インスペクターを確認してください。");
            }
        }

        private void FixedUpdate()
        {
            // ターゲットとラウンドプレイ中であるかを確認
            if (target == null || !isRoundPlaying)
            {
                return;
            }

            // 1. カメラの位置の更新
            // posOffset (ローカル座標) をターゲットの Transform に基づいてグローバル座標に変換
            Vector3 desiredPosition = target.TransformPoint(posOffset);
            transform.position = desiredPosition;


            // 2. カメラの回転の更新
            // rotOffset のオイラー角をクォータニオンに変換
            Quaternion desiredRotation = Quaternion.Euler(rotOffset);

            // ターゲットの回転にオフセット回転を適用し、ワールド回転を計算
            Quaternion finalRotation = target.rotation * desiredRotation;
            transform.rotation = finalRotation;
        }

        // ==========================
        // 外部公開メソッド
        // ==========================

        // GameManagerから呼ばれる、ターゲット設定用メソッド
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }


        // GameManagerから回転オフセットの調整値を受け取るメソッド
        public void AdjustRotOffset(Vector3 adjustment)
        {
            // ターゲットのローカル回転との差分を吸収するため、rotOffsetから調整値を引く
            rotOffset -= adjustment;
        }
        // GameManagerから呼ばれる、初期位置設定用メソッド (RoundStartingから呼ばれる想定)
        public void SetStartPositionAndRotation()
        {
            if (target == null)
            {
                Debug.LogWarning("TPS Camera: ターゲットが設定されていません。初期位置を設定できませんでした。");
                return;
            }

            // 位置を瞬時に設定
            transform.position = target.TransformPoint(posOffset);

            // 回転を瞬時に設定
            Quaternion desiredRotation = Quaternion.Euler(rotOffset);
            transform.rotation = target.rotation * desiredRotation;
        }

        // ==========================
        // イベントハンドラ
        // ==========================

        public void OnGameStateChanged(GameManager.GameLoopState newState)
        {
            if (newState == GameManager.GameLoopState.RoundPlaying)
            {
                isRoundPlaying = true;
            }
            else
            {
                isRoundPlaying = false;
            }
        }
    }
}