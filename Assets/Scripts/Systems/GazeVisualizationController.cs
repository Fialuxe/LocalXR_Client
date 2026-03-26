using UnityEngine;

/// <summary>
/// 【役者 / Controller - Gaze Visualization Controller】
/// Frustum / Ray / SurfaceCircle の表示モードを一元管理する。
/// 掲示板（GazeEventHubSO）からのモード変更要求（OnModeChangeRequested）を購読し、
/// 実際の表示を切り替える。
///
/// アタッチ場所: [GazeVisualization] 空のGameObject（3つのViewControllerの親）
/// </summary>
public class GazeVisualizationController : MonoBehaviour
{
    public enum GazeVisualizationMode
    {
        Frustum,
        Ray,
        SurfaceCircle,
        All,
        None
    }

    [Header("Event Channel (必須)")]
    [Tooltip("状態変更を監視し、また変更を要求する掲示板。GazeEventHub.assetをここへドラッグ。")]
    [SerializeField] private GazeEventHubSO gazeHub;

    [Header("初期モード")]
    [SerializeField] private GazeVisualizationMode currentMode = GazeVisualizationMode.Frustum;

    [Header("各ViewControllerの親GameObject")]
    [Tooltip("FrustumViewController がアタッチされた GameObject")]
    [SerializeField] private GameObject frustumObject;

    [Tooltip("RayGazeViewController がアタッチされた GameObject")]
    [SerializeField] private GameObject rayObject;

    [Tooltip("SurfaceCircleViewController がアタッチされた GameObject")]
    [SerializeField] private GameObject circleObject;

    [Header("手動操作（デバッグ用）")]
    [Tooltip("PCでのテスト時にVキーでモード変更するか（HMD使用時は不要）")]
    [SerializeField] private bool enableKeyboardInput = true;

    [Tooltip("モード切り替えキー")]
    [SerializeField] private KeyCode cycleKey = KeyCode.V;

    // ─── ライフサイクル ─────────────────────────────────

    private void Awake()
    {
        if (gazeHub == null)
        {
            Debug.LogError($"[GazeVisualizationController] {gameObject.name}: GazeEventHubSO がアサインされていません！");
            this.enabled = false; return;
        }
    }

    private void Start()
    {
        ApplyMode(currentMode);
        Debug.Log($"[GazeVisualizationController] 起動。初期モード: {currentMode}");
    }

    private void OnEnable()
    {
        if (gazeHub == null) return;
        gazeHub.OnModeChangeRequested += HandleModeChangeReceived;
        Debug.Log("[GazeVisualizationController] 掲示板（モード変更）の購読を開始しました。");
    }

    private void OnDisable()
    {
        if (gazeHub == null) return;
        gazeHub.OnModeChangeRequested -= HandleModeChangeReceived;
    }

    private void Update()
    {
        if (enableKeyboardInput && Input.GetKeyDown(cycleKey))
        {
            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            CycleMode(reverse);
        }
    }

    // ─── モード切り替え（手動） ─────────────────────────────

    private static readonly GazeVisualizationMode[] CycleModes = {
        GazeVisualizationMode.Frustum,
        GazeVisualizationMode.Ray,
        GazeVisualizationMode.SurfaceCircle,
        GazeVisualizationMode.None
    };

    private void CycleMode(bool reverse)
    {
        int currentIndex = System.Array.IndexOf(CycleModes, currentMode);
        if (currentIndex == -1) currentIndex = 0; // Allモード等にいた場合のフォールバック

        int count = CycleModes.Length;
        int nextIndex = (currentIndex + (reverse ? count - 1 : 1)) % count;
        
        // ローカルでの手動変更（デバッグ用）は、自分自身の表示を変えるだけで、
        // 掲示板には書き込まない（送信ループを防ぐため。LocalXRは受信専用の想定）
        // もしローカルから相手のモードも変えたい場合は gazeHub.RequestModeChange() を呼ぶ。
        currentMode = CycleModes[nextIndex];
        ApplyMode(currentMode);
        Debug.Log($"[GazeVisualizationController] ローカル手動モード変更: {currentMode}");
    }

    // ─── モード受信（掲示板から） ───────────────────────────

    private void HandleModeChangeReceived(int modeInt)
    {
        if (!System.Enum.IsDefined(typeof(GazeVisualizationMode), modeInt))
        {
            Debug.LogWarning($"[GazeVisualizationController] 定義されていないモード値を受信しました: {modeInt}");
            return;
        }

        currentMode = (GazeVisualizationMode)modeInt;
        ApplyMode(currentMode);
        Debug.Log($"[GazeVisualizationController] 掲示板からモード変更を受け取り適用しました: {currentMode}");
    }

    // ─── 適用 ─────────────────────────────────────────

    private void ApplyMode(GazeVisualizationMode mode)
    {
        SetActive(frustumObject, mode == GazeVisualizationMode.Frustum || mode == GazeVisualizationMode.All);
        SetActive(rayObject,     mode == GazeVisualizationMode.Ray     || mode == GazeVisualizationMode.All);
        SetActive(circleObject,  mode == GazeVisualizationMode.SurfaceCircle || mode == GazeVisualizationMode.All);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    // ─── Inspector 表示確認 ──────────────────────────────

    private void OnGUI()
    {
        if (!enableKeyboardInput) return;
        
        // 右上に現在のモードを常時表示
        GUI.color = new Color(0f, 0.8f, 1f, 0.8f);
        GUI.Label(new Rect(Screen.width - 220, 10, 210, 30),
                  $"[Gaze Mode(Local)] {currentMode}  (V で切替)");
        GUI.color = Color.white;
    }
}
