using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

/// <summary>
/// [System - Networking (Photon Sender)]
/// Monitors the event hub (AlignmentEventHubSO) for save requests
/// and sends data to Remote Expert (other clients) via Photon network.
///
/// - This class does NOT write to the event hub. It only sends outward.
/// - When Photon is not connected, it skips sending without affecting internal EventChannel.
///   This means the system keeps working even when offline.
///
/// Attach to: [Systems] empty GameObject in Hierarchy
/// </summary>
public class PhotonAlignmentSender : MonoBehaviourPunCallbacks, IOnEventCallback
{
    // Inherited from legacy MeshAlignmentManager Photon Event code 101
    public const byte AlignmentEventCode = 101;

    [Header("Event Channel (Required)")]
    [Tooltip("Event hub to monitor for save triggers. Drag AlignmentEventHub.asset here.")]
    [SerializeField] private AlignmentEventHubSO eventHub;

    [Header("Send Target (Required)")]
    [Tooltip("Source mesh to read Transform data from for sending.")]
    [SerializeField] private Transform targetMesh;

    [Header("Realtime Broadcast Settings")]
    [Tooltip("Enable periodic broadcasting during continuous operations (not just on save).")]
    [SerializeField] private bool broadcastRealtime = false;

    [Tooltip("Realtime broadcast interval (seconds)")]
    [SerializeField] private float broadcastInterval = 0.1f;

    private float _lastBroadcastTime;

    // --- Lifecycle ---

    private void Awake()
    {
        if (eventHub == null)
        {
            Debug.LogError($"[PhotonAlignmentSender] AlignmentEventHubSO is not assigned! Disabling component.");
            this.enabled = false; return;
        }
        if (targetMesh == null)
        {
            Debug.LogWarning("[PhotonAlignmentSender] targetMesh is not assigned. Expecting dynamic assignment via OnTargetMeshSpawned.");
        }
    }

    private void OnEnable()
    {
        if (eventHub == null) return;
        eventHub.OnSaveRequested          += HandleSaveAndSend;
        eventHub.OnTargetMeshSpawned      += HandleMeshSpawned;
        eventHub.OnSyncBroadcastRequested += HandleSyncBroadcastRequest;
        Debug.Log("[PhotonAlignmentSender] Started monitoring event hub (sender).");
    }

    private void OnDisable()
    {
        if (eventHub == null) return;
        eventHub.OnSaveRequested          -= HandleSaveAndSend;
        eventHub.OnTargetMeshSpawned      -= HandleMeshSpawned;
        eventHub.OnSyncBroadcastRequested -= HandleSyncBroadcastRequest;
        Debug.Log("[PhotonAlignmentSender] Stopped monitoring event hub.");
    }

    private void Update()
    {
        if (!broadcastRealtime) return;
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom) return;
        if (Time.time - _lastBroadcastTime >= broadcastInterval)
        {
            SendAlignmentData();
            _lastBroadcastTime = Time.time;
        }
    }

    // --- Handlers ---

    private void HandleMeshSpawned(Transform spawnedMesh)
    {
        targetMesh = spawnedMesh;
        Debug.Log($"[PhotonAlignmentSender] targetMesh dynamically assigned: {targetMesh.name}");
    }

    private void HandleSaveAndSend()
    {
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[PhotonAlignmentSender] Not connected to network. " +
                             "Skipping Photon send. Local save is handled by a separate System.");
            return;
        }
        SendAlignmentData();
    }

    private void HandleSyncBroadcastRequest()
    {
        Debug.Log("[PhotonAlignmentSender] Sync broadcast request received from event hub. Sending current position.");
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[PhotonAlignmentSender] Not connected to network. Skipping sync broadcast.");
            return;
        }
        SendAlignmentData();
    }

    private void SendAlignmentData()
    {
        try
        {
            Vector3    pos   = targetMesh.position;
            Quaternion rot   = targetMesh.rotation;
            Vector3    scale = targetMesh.localScale;

            // Same data format as legacy BroadcastAlignment() (backward compatible)
            object[] data = new object[]
            {
                pos.x, pos.y, pos.z,
                rot.x, rot.y, rot.z, rot.w,
                scale.x, scale.y, scale.z
            };

            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(AlignmentEventCode, data, opts, SendOptions.SendReliable);

            Debug.Log($"[PhotonAlignmentSender] Event hub data sent to Photon. " +
                      $"position={pos}, rotation={rot.eulerAngles}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PhotonAlignmentSender] Error during Photon send: {e.Message}");
        }
    }

    // IOnEventCallback not used (receiving is handled by PhotonAlignmentReceiver)
    public void OnEvent(EventData photonEvent) { }
}
