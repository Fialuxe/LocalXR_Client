using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

/// <summary>
/// 【翻訳者 / Provider - NetworkData (Gaze受信)】
/// Photonネットワークから届いた視線データを受信し、
/// 掲示板（GazeEventHubSO）に書き込む。
///
/// 役割の分離:
/// - 受信したデータをWorldspace座標のRayに変換して掲示板に投稿するだけ。
/// - 実際の描画は FrustumViewController / RayGazeViewController / SurfaceCircleViewController が担当。
///
/// Photon Event Code: 102（GazeデータはAlignmentの101と別コードで管理）
///
/// アタッチ場所: Hierarchy内の [InputProviders] または [NetworkData] という空のGameObject
/// </summary>
public class PhotonGazeReceiver : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public const byte GazeEventCode = 102;

    [Header("Event Channel (必須)")]
    [Tooltip("受信データを書き込む掲示板。GazeEventHub.assetをここへドラッグ。")]
    [SerializeField] private GazeEventHubSO eventHub;

    [Header("視線ヒット計算")]
    [Tooltip("Raycastを試みるレイヤーマスク（0=全レイヤー）")]
    [SerializeField] private LayerMask hitLayerMask;

    [Tooltip("Raycastの最大距離 [m]")]
    [SerializeField] private float maxRayDistance = 20f;

    [Tooltip("Raycastを使わず、送信側のhitPointをそのまま使う場合はON")]
    [SerializeField] private bool useRemoteHitPoint = true;

    [Header("タイムアウト設定")]
    [Tooltip("この秒数データが来なければ LostGaze を発火 [s]")]
    [SerializeField] private float gazeTimeoutSeconds = 2f;

    private float _lastReceiveTime;
    private bool _isActive;

    // ─── ライフサイクル ─────────────────────────────────

    private void Awake()
    {
        if (eventHub == null)
        {
            Debug.LogError("[PhotonGazeReceiver] GazeEventHubSO がアサインされていません！");
            this.enabled = false; return;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
        Debug.Log("[PhotonGazeReceiver] Gazeデータの受信監視を開始しました。");
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
        if (_isActive && eventHub != null) eventHub.LostGaze();
        Debug.Log("[PhotonGazeReceiver] Gazeデータの受信監視を解除しました。");
    }

    private void Update()
    {
        // タイムアウト検知
        if (_isActive && Time.time - _lastReceiveTime > gazeTimeoutSeconds)
        {
            _isActive = false;
            eventHub.LostGaze();
            Debug.Log("[PhotonGazeReceiver] 視線データのタイムアウト。LostGaze を発火しました。");
        }
    }

    // ─── Photonイベント受信 ─────────────────────────────

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != GazeEventCode) return;

        try
        {
            object[] data = (object[]) photonEvent.CustomData;
            if (data == null || data.Length < 9)
            {
                Debug.LogWarning($"[PhotonGazeReceiver] 不正なGazeデータ形式。dataLength={(data?.Length ?? 0)}");
                return;
            }

            Vector3 origin = new Vector3(
                System.Convert.ToSingle(data[0]),
                System.Convert.ToSingle(data[1]),
                System.Convert.ToSingle(data[2])
            );
            Vector3 direction = new Vector3(
                System.Convert.ToSingle(data[3]),
                System.Convert.ToSingle(data[4]),
                System.Convert.ToSingle(data[5])
            );
            Vector3 remoteHitPoint = new Vector3(
                System.Convert.ToSingle(data[6]),
                System.Convert.ToSingle(data[7]),
                System.Convert.ToSingle(data[8])
            );
            bool remoteHasHit = data.Length > 9 && System.Convert.ToBoolean(data[9]);

            // ヒット判定：送信側の値を使うか、ローカルRaycastを使うか
            Vector3 finalHitPoint = Vector3.zero;
            Vector3 finalNormal   = Vector3.up;
            bool    finalHasHit   = false;

            if (useRemoteHitPoint && remoteHasHit)
            {
                finalHitPoint = remoteHitPoint;
                finalNormal   = -direction; // 近似（法線未送信のため）
                finalHasHit   = true;
            }
            else
            {
                Ray ray = new Ray(origin, direction);
                int mask = hitLayerMask == 0 ? ~0 : (int)hitLayerMask;
                if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, mask))
                {
                    finalHitPoint = hit.point;
                    finalNormal   = hit.normal;
                    finalHasHit   = true;
                }
            }

            _lastReceiveTime = Time.time;
            _isActive = true;
            eventHub.UpdateGaze(origin, direction, finalHitPoint, finalNormal, finalHasHit);

            Debug.Log($"[PhotonGazeReceiver] Gazeデータ受信 → 掲示板に書き込みました。" +
                      $" origin={origin}, hasHit={finalHasHit}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PhotonGazeReceiver] データ解析エラー: {e.Message}");
        }
    }
}
