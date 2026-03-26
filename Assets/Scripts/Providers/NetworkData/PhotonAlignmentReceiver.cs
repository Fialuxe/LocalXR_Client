using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

/// <summary>
/// [Provider - NetworkData (Photon Receiver)]
/// Receives alignment data from the Photon network
/// and writes it to the event hub (AlignmentEventHubSO).
///
/// Role Separation:
/// - This class is only responsible for "receiving -> writing to event hub".
/// - "Event hub -> Photon sending" is handled by PhotonAlignmentSender.
/// - This class NEVER directly manipulates mesh Transforms.
///
/// Attach to: [InputProviders] or [NetworkData] empty GameObject in Hierarchy
/// </summary>
public class PhotonAlignmentReceiver : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public const byte AlignmentEventCode = 101;
    public const byte SyncRequestEventCode = 104;

    [Header("Event Channel (Required)")]
    [Tooltip("Event hub to write received data to. Drag AlignmentEventHub.asset here.")]
    [SerializeField] private AlignmentEventHubSO eventHub;

    [Header("Target (Required)")]
    [Tooltip("Target mesh to apply received Transform to (absolute position override).")]
    [SerializeField] private Transform targetMesh;

    // --- Lifecycle ---

    private void Awake()
    {
        bool hasError = false;
        if (eventHub == null)
        {
            Debug.LogError("[PhotonAlignmentReceiver] AlignmentEventHubSO is not assigned! Disabling component.");
            this.enabled = false; return;
        }
        if (targetMesh == null)
        {
            Debug.LogWarning("[PhotonAlignmentReceiver] targetMesh is not assigned. Expecting dynamic assignment via OnTargetMeshSpawned.");
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
        if (eventHub != null) eventHub.OnTargetMeshSpawned += HandleMeshSpawned;
        Debug.Log("[PhotonAlignmentReceiver] Started Photon event monitoring (receiver).");
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
        if (eventHub != null) eventHub.OnTargetMeshSpawned -= HandleMeshSpawned;
        Debug.Log("[PhotonAlignmentReceiver] Stopped Photon event monitoring.");
    }

    // --- Photon Event Reception ---

    private void HandleMeshSpawned(Transform spawnedMesh)
    {
        targetMesh = spawnedMesh;
        Debug.Log($"[PhotonAlignmentReceiver] targetMesh dynamically assigned: {targetMesh.name}");
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == SyncRequestEventCode)
        {
            Debug.Log("[PhotonAlignmentReceiver] SyncRequest (Event 104) received. Writing sync broadcast request to event hub.");
            if (eventHub != null) eventHub.RequestSyncBroadcast();
            return;
        }

        if (photonEvent.Code != AlignmentEventCode) return;

        try
        {
            object[] data = (object[]) photonEvent.CustomData;
            if (data == null || data.Length < 10)
            {
                Debug.LogWarning($"[PhotonAlignmentReceiver] Invalid data format received. " +
                                 $"dataLength={(data?.Length ?? 0)}");
                return;
            }

            // Compatible with legacy NetworkedDataReceiver.OnPhotonEvent() Code 101 format
            Vector3 pos = new Vector3(
                System.Convert.ToSingle(data[0]),
                System.Convert.ToSingle(data[1]),
                System.Convert.ToSingle(data[2])
            );
            Quaternion rot = new Quaternion(
                System.Convert.ToSingle(data[3]),
                System.Convert.ToSingle(data[4]),
                System.Convert.ToSingle(data[5]),
                System.Convert.ToSingle(data[6])
            );
            Vector3 scale = new Vector3(
                System.Convert.ToSingle(data[7]),
                System.Convert.ToSingle(data[8]),
                System.Convert.ToSingle(data[9])
            );

            Debug.Log($"[PhotonAlignmentReceiver] Alignment data received from Photon." +
                      $" position:{pos}, rotation:{rot.eulerAngles}, scale:{scale}");

            // NOTE: Received data uses absolute coordinates, so applied directly rather than via EventChannel.
            // Data arrives as absolute values (not deltas), so the design differs from delta-based approaches.
            // In the future, an "absolute position set request" could be added to EventHub for full decoupling.
            targetMesh.position   = pos;
            targetMesh.rotation   = rot;
            targetMesh.localScale = scale;

            Debug.Log($"[PhotonAlignmentReceiver] Received data applied to mesh (spatial sync complete).");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PhotonAlignmentReceiver] Error during data parsing: {e.Message}");
        }
    }
}
