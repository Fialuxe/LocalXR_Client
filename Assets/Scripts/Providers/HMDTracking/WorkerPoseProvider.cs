using UnityEngine;

/// <summary>
/// [Provider - HMD Tracking / Data Collection]
/// Reads the HMD (Camera Rig) Transform every frame
/// and writes it to the event hub (WorkerPoseEventHubSO).
///
/// -- Extension Policy --
/// - Phase 1: Set only Head (CenterEyeAnchor, etc.) in trackingTargets.
/// - Phase 2: Simply add LeftHand / RightHand Transforms via Inspector.
///   This class uses an array, so no code changes are needed.
/// - Phase 3: Finger joints and other fine-grained points can be added the same way.
///
/// Attach to: [WorkerPoseTracker] empty GameObject
/// </summary>
public class WorkerPoseProvider : MonoBehaviour
{
    [Header("Event Channel (Required)")]
    [Tooltip("Event hub to write pose data to. Drag WorkerPoseEventHub.asset here.")]
    [SerializeField] private WorkerPoseEventHubSO eventHub;

    [Header("Tracking Targets")]
    [Tooltip("List of Transforms to track. In Phase 2, simply add hands here.")]
    [SerializeField] private TrackingTarget[] trackingTargets = new TrackingTarget[]
    {
        new TrackingTarget { pointId = "head", target = null }
    };

    [System.Serializable]
    public struct TrackingTarget
    {
        [Tooltip("Point ID ('head', 'hand_l', 'hand_r', etc.)")]
        public string pointId;

        [Tooltip("Transform to follow (uses Camera.main if empty)")]
        public Transform target;
    }

    private WorkerPoseEventHubSO.TrackingPointData[] _buffer;

    // --- Lifecycle ---

    private void Awake()
    {
        if (eventHub == null)
        {
            Debug.LogError($"[WorkerPoseProvider] {gameObject.name}: WorkerPoseEventHubSO is not assigned!");
            this.enabled = false; return;
        }
    }

    private void Start()
    {
        // Auto-resolve Camera.main for unassigned head targets
        for (int i = 0; i < trackingTargets.Length; i++)
        {
            if (trackingTargets[i].target == null && trackingTargets[i].pointId == "head")
            {
                if (Camera.main != null)
                {
                    trackingTargets[i].target = Camera.main.transform;
                    Debug.Log($"[WorkerPoseProvider] Auto-assigned '{trackingTargets[i].pointId}' to Camera.main.");
                }
                else
                {
                    Debug.LogWarning("[WorkerPoseProvider] Camera.main not found. Please assign manually in Inspector.");
                }
            }
        }

        _buffer = new WorkerPoseEventHubSO.TrackingPointData[trackingTargets.Length];
    }

    private void Update()
    {
        int activeCount = 0;

        for (int i = 0; i < trackingTargets.Length; i++)
        {
            if (trackingTargets[i].target == null) continue;

            _buffer[activeCount] = new WorkerPoseEventHubSO.TrackingPointData
            {
                PointId = trackingTargets[i].pointId,
                Position = trackingTargets[i].target.position,
                Rotation = trackingTargets[i].target.rotation,
            };
            activeCount++;
        }

        if (activeCount > 0)
        {
            // Write only active points to the event hub
            if (activeCount < _buffer.Length)
            {
                var trimmed = new WorkerPoseEventHubSO.TrackingPointData[activeCount];
                System.Array.Copy(_buffer, trimmed, activeCount);
                eventHub.UpdatePose(trimmed);
            }
            else
            {
                eventHub.UpdatePose(_buffer);
            }
        }
    }
}
