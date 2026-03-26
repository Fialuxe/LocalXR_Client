using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [System - Networking / Sender]
/// Monitors the event hub (WorkerPoseEventHubSO) for pose updates
/// and sends data to other clients (RemoteXR) via Photon network.
///
/// -- Photon Event 105 Payload Design --
/// The structure remains the same regardless of the number of points (string ID based).
///   data[0] = int (point count N)
///   Then, 8 elements per point in a chunk:
///     [i*8+1] = string (PointId)
///     [i*8+2] = float (pos.x)
///     [i*8+3] = float (pos.y)
///     [i*8+4] = float (pos.z)
///     [i*8+5] = float (rot.x)
///     [i*8+6] = float (rot.y)
///     [i*8+7] = float (rot.z)
///     [i*8+8] = float (rot.w)
///
/// No changes to this class are needed when adding hands in Phase 2.
///
/// Attach to: [WorkerPoseTracker] empty GameObject
/// </summary>
public class PhotonWorkerPoseSender : MonoBehaviour
{
    public const byte WorkerPoseEventCode = 105;
    private const int ELEMENTS_PER_POINT = 8; // id + pos(3) + rot(4)

    [Header("Event Channel (Required)")]
    [Tooltip("Event hub to monitor for pose data. Drag WorkerPoseEventHub.asset here.")]
    [SerializeField] private WorkerPoseEventHubSO eventHub;

    [Header("Send Settings")]
    [Tooltip("Send rate limit (seconds). 0 = every frame.")]
    [SerializeField] private float sendInterval = 0.033f; // ~30fps

    private float _lastSendTime;

    // --- Lifecycle ---

    private void Awake()
    {
        if (eventHub == null)
        {
            Debug.LogError($"[PhotonWorkerPoseSender] {gameObject.name}: WorkerPoseEventHubSO is not assigned!");
            this.enabled = false; return;
        }
    }

    private void OnEnable()
    {
        if (eventHub != null) eventHub.OnPoseUpdated += HandlePoseUpdated;
        Debug.Log("[PhotonWorkerPoseSender] Started monitoring event hub.");
    }

    private void OnDisable()
    {
        if (eventHub != null) eventHub.OnPoseUpdated -= HandlePoseUpdated;
    }

    // --- Handler ---

    private void HandlePoseUpdated(WorkerPoseEventHubSO.TrackingPointData[] points)
    {
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom) return;

        // Rate limiting
        if (Time.time - _lastSendTime < sendInterval) return;
        _lastSendTime = Time.time;

        try
        {
            int count = points.Length;
            object[] data = new object[1 + count * ELEMENTS_PER_POINT];
            data[0] = count;

            for (int i = 0; i < count; i++)
            {
                int offset = 1 + i * ELEMENTS_PER_POINT;
                data[offset + 0] = points[i].PointId;
                data[offset + 1] = points[i].Position.x;
                data[offset + 2] = points[i].Position.y;
                data[offset + 3] = points[i].Position.z;
                data[offset + 4] = points[i].Rotation.x;
                data[offset + 5] = points[i].Rotation.y;
                data[offset + 6] = points[i].Rotation.z;
                data[offset + 7] = points[i].Rotation.w;
            }

            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(WorkerPoseEventCode, data, opts, SendOptions.SendUnreliable);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PhotonWorkerPoseSender] Send error: {e.Message}");
        }
    }
}
