using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 【Application / AppManager】
/// Local Worker アプリ全体の初期化を担う「支配人」。
/// 
/// 責務:
///  1. Photonへの接続とルーム参加管理（旧LocalWorkerManager.Startおよびコールバックを移植）
///  2. VRカメラの自動解決（CenterEyeAnchor / Camera.main のフォールバック）
///  3. 接続状態のログ出力
///
/// アタッチ場所: Hierarchy内の [AppManager] という空のGameObject
/// </summary>
public class LocalWorkerAppManager : MonoBehaviourPunCallbacks
{
    [Header("Photon 設定")]
    [Tooltip("参加するPhotonルーム名")]
    [SerializeField] private string roomName = "MeshVRRoom";

    [Tooltip("最大参加人数")]
    [SerializeField] private int maxPlayers = 4;

    [Header("VRカメラ")]
    [Tooltip("HMDのカメラTransform（通常はXR OriginのCamera Offset/Main Camera）。空欄の場合は自動解決を試みる。")]
    [SerializeField] private Transform vrCamera;

    [Header("アバター（任意）")]
    [Tooltip("ONにすると、Photon経由で自分のアバターをInstantiateする。OFFは観察モード。")]
    [SerializeField] private bool instantiateAvatar = false;

    [Tooltip("instantiateAvatar=TRUEの時にPhotonがInstatiateするPrefab名（Resources/配下に配置）")]
    [SerializeField] private string avatarPrefabName = "LocalWorkerAvatar";

    // ─── ライフサイクル ─────────────────────────────────

    private void Start()
    {
        ResolveVRCamera();
        ConnectToPhoton();
    }

    // ─── VRカメラ解決 ─────────────────────────────────

    /// <summary>
    /// VRカメラを解決する。優先順位: Inspector指定 > CenterEyeAnchor > Camera.main > シーン内の任意のCamera
    /// 旧 LocalWorkerManager.TryResolveVRCamera() のアルゴリズムを移植。
    /// </summary>
    private void ResolveVRCamera()
    {
        if (vrCamera != null)
        {
            Debug.Log($"[LocalWorkerAppManager] VRカメラ（Inspector指定）: {vrCamera.name}");
            return;
        }

        // Meta XR SDK の標準GameObject名で検索
        GameObject centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null)
        {
            vrCamera = centerEye.transform;
            Debug.Log("[LocalWorkerAppManager] VRカメラ（CenterEyeAnchor）を自動解決しました。");
            return;
        }

        if (Camera.main != null)
        {
            vrCamera = Camera.main.transform;
            Debug.Log("[LocalWorkerAppManager] VRカメラ（Camera.main）を自動解決しました。");
            return;
        }

        Camera anyCam = Object.FindFirstObjectByType<Camera>();
        if (anyCam != null)
        {
            vrCamera = anyCam.transform;
            Debug.Log($"[LocalWorkerAppManager] VRカメラ（シーン内カメラ: {anyCam.name}）を自動解決しました。");
            return;
        }

        Debug.LogWarning("[LocalWorkerAppManager] VRカメラが見つかりませんでした。" +
                         " XR Origin がシーンに存在するか確認してください。");
    }

    // ─── Photon接続 ─────────────────────────────────

    private void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[LocalWorkerAppManager] 既にPhotonへ接続済みです。ルーム参加を試みます。");
            TryJoinRoom();
            return;
        }

        PhotonNetwork.NickName = "LocalWorker_" + Random.Range(1000, 9999);
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log($"[LocalWorkerAppManager] Photonへ接続開始。NickName={PhotonNetwork.NickName}");
    }

    private void TryJoinRoom()
    {
        RoomOptions options = new RoomOptions { MaxPlayers = (byte)maxPlayers };
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
        Debug.Log($"[LocalWorkerAppManager] ルーム '{roomName}' への参加を試みます。");
    }

    // ─── Photonコールバック ─────────────────────────────

    public override void OnConnectedToMaster()
    {
        Debug.Log("<color=green>[LocalWorkerAppManager] Photon Masterへ接続完了。</color>");
        TryJoinRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"<color=green>[LocalWorkerAppManager] ルーム '{PhotonNetwork.CurrentRoom.Name}' に参加しました。" +
                  $" 現在人数: {PhotonNetwork.CurrentRoom.PlayerCount}</color>");

        if (instantiateAvatar)
        {
            try
            {
                Vector3 spawnPos = vrCamera != null ? vrCamera.position : Vector3.zero;
                Quaternion spawnRot = vrCamera != null ? vrCamera.rotation : Quaternion.identity;
                GameObject avatar = PhotonNetwork.Instantiate(avatarPrefabName, spawnPos, spawnRot);
                Debug.Log($"[LocalWorkerAppManager] アバター '{avatarPrefabName}' をInstantiateしました。");

                // Rigidbodyがある場合は重力をOFF（VR空間での浮遊アバターのため）
                Rigidbody rb = avatar.GetComponent<Rigidbody>();
                if (rb != null) { rb.useGravity = false; rb.isKinematic = true; }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LocalWorkerAppManager] アバターInstantiate失敗: {e.Message}" +
                               $" Resources/{avatarPrefabName} が存在するか確認してください。");
            }
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[LocalWorkerAppManager] ルーム参加失敗。 code={returnCode}, message={message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[LocalWorkerAppManager] Photon切断。 cause={cause}" +
                         " アライメント機能はオフラインでも継続動作します。");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[LocalWorkerAppManager] Remote Expert が参加しました: {newPlayer.NickName}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[LocalWorkerAppManager] Remote Expert が退出しました: {otherPlayer.NickName}");
    }

    // ─── 公開プロパティ ─────────────────────────────────

    /// <summary>現在解決済みのVRカメラTransform（他のSystemが参照したい場合用）</summary>
    public Transform VRCamera => vrCamera;
}
