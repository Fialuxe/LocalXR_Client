using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

/// <summary>
/// 【裏方 / System - Gaze送信】
/// Remote Expert 側で、カメラの視線データをリアルタイムにPhoton経由で送信する。
/// Local Worker 側に配置する必要はない（受信は PhotonGazeReceiver が担当）。
///
/// 使用シーン: Remote Expert Client のカメラ（または視線追跡センサー）にアタッチ。
///
/// Photon Event Code: 102
/// </summary>
public class PhotonGazeSender : MonoBehaviour
{
    public const byte GazeEventCode = 102;

    [Header("データ送信元")]
    [Tooltip("視線の発生源カメラ（未指定なら Camera.main）")]
    [SerializeField] private Camera gazeCamera;

    [Header("Raycast設定")]
    [Tooltip("視線のヒット判定に使うレイヤーマスク（0=全レイヤー）")]
    [SerializeField] private LayerMask hitLayerMask;

    [Tooltip("Raycastの最大距離 [m]")]
    [SerializeField] private float maxRayDistance = 20f;

    [Header("送信設定")]
    [Tooltip("送信間隔 [s]（小さいほどリアルタイムだが帯域増加）")]
    [SerializeField] private float sendInterval = 0.05f; // 20fps

    [Tooltip("Photonに接続していない場合でも GazeEventHub に書き込む（ローカルデバッグ用）")]
    [SerializeField] private GazeEventHubSO localDebugHub;

    private float _lastSendTime;

    // ─── ライフサイクル ─────────────────────────────────

    private void Start()
    {
        if (gazeCamera == null) gazeCamera = Camera.main;
        if (gazeCamera == null)
        {
            Debug.LogError("[PhotonGazeSender] 視線カメラが見つかりません！ Inspector で指定してください。");
            this.enabled = false; return;
        }
        Debug.Log($"[PhotonGazeSender] 視線カメラ: {gazeCamera.name}。送信間隔: {sendInterval}s");
    }

    private void Update()
    {
        if (Time.time - _lastSendTime < sendInterval) return;
        _lastSendTime = Time.time;

        SampleAndSend();
    }

    // ─── 視線サンプリング＆送信 ───────────────────────────

    private void SampleAndSend()
    {
        Vector3 origin    = gazeCamera.transform.position;
        Vector3 direction = gazeCamera.transform.forward;

        // ヒット計算
        Vector3 hitPoint  = Vector3.zero;
        Vector3 hitNormal = Vector3.up;
        bool    hasHit    = false;

        Ray ray = new Ray(origin, direction);
        int mask = hitLayerMask == 0 ? ~0 : (int)hitLayerMask;
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, mask))
        {
            hitPoint  = hit.point;
            hitNormal = hit.normal;
            hasHit    = true;
        }

        // Photon送信
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            object[] data = {
                origin.x,    origin.y,    origin.z,
                direction.x, direction.y, direction.z,
                hitPoint.x,  hitPoint.y,  hitPoint.z,
                hasHit
            };
            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(GazeEventCode, data, opts, SendOptions.SendUnreliable);
        }
        else if (localDebugHub != null)
        {
            // オフラインデバッグ: ローカルハブへ直接書き込み
            localDebugHub.UpdateGaze(origin, direction, hitPoint, hitNormal, hasHit);
        }
    }
}
