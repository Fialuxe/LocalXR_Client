using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

/// <summary>
/// 【翻訳者 / Provider - NetworkData (Mode受信)】
/// Photonネットワークから届いた視線モード変更データを受信し、
/// 掲示板（GazeEventHubSO）に書き込む。
///
/// Photon Event Code: 103
/// Payload: object[] { (int)mode }
///
/// アタッチ場所: Hierarchy内の [InputProviders] または [NetworkData] という空のGameObject
/// </summary>
public class PhotonGazeModeReceiver : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public const byte GazeModeEventCode = 103;

    [Header("Event Channel (必須)")]
    [Tooltip("受信データを書き込む掲示板。GazeEventHub.assetをここへドラッグ。")]
    [SerializeField] private GazeEventHubSO eventHub;

    // ─── ライフサイクル ─────────────────────────────────

    private void Awake()
    {
        if (eventHub == null)
        {
            Debug.LogError($"[PhotonGazeModeReceiver] {gameObject.name}: GazeEventHubSO がアサインされていません！");
            this.enabled = false; return;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
        Debug.Log("[PhotonGazeModeReceiver] 視線モード変更データの受信監視を開始しました。");
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
        Debug.Log("[PhotonGazeModeReceiver] 視線モード変更データの受信監視を解除しました。");
    }

    // ─── Photonイベント受信 ─────────────────────────────

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != GazeModeEventCode) return;

        try
        {
            object[] data = (object[]) photonEvent.CustomData;
            if (data == null || data.Length < 1)
            {
                Debug.LogWarning($"[PhotonGazeModeReceiver] 不正なModeデータ形式。dataLength={(data?.Length ?? 0)}");
                return;
            }

            // data[0] は int (GazeVisualizationMode)
            int modeInt = System.Convert.ToInt32(data[0]);

            // 掲示板に翻訳して代筆する
            eventHub.RequestModeChange(modeInt);

            Debug.Log($"[PhotonGazeModeReceiver] Modeデータ受信 → 掲示板に書き込みました。 mode={modeInt}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PhotonGazeModeReceiver] データ解析エラー: {e.Message}");
        }
    }
}
