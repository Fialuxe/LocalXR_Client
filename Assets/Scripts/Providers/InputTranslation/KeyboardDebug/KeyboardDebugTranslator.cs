using UnityEngine;

/// <summary>
/// 【翻訳者 / Provider - KeyboardDebug】
/// PCのキーボード入力をアライメントのIntentに翻訳して掲示板（EventHub）に書き込む。
/// 
/// 目的: HMDやVRコントローラが不要な完全オフラインのテスト環境を実現する。
/// このコンポーネントがONの間、WASDキーでメッシュが動けばEventChannelが機能している証明となる。
///
/// アタッチ場所: Hierarchy内の [InputProviders] という空のGameObject (後述のUnity Editor操作を参照)
/// </summary>
public class KeyboardDebugTranslator : MonoBehaviour
{
    [Header("Event Channel (必須)")]
    [Tooltip("このTranslatorが書き込む掲示板。AlignmentEventHub.assetをここへドラッグ。")]
    [SerializeField] private AlignmentEventHubSO eventHub;

    [Header("移動設定")]
    [Tooltip("WASDキーでの1フレームあたりの移動速度 (m/s)")]
    [SerializeField] private float moveSpeed = 1.0f;

    [Header("回転設定")]
    [Tooltip("QEキーでのY軸回転速度 (度/s)")]
    [SerializeField] private float rotateSpeed = 45.0f;

    [Header("スケール設定")]
    [Tooltip("RFキーでのスケール変化速度")]
    [SerializeField] private float scaleSpeed = 0.5f;

    // ─── ライフサイクル ─────────────────────────────────

    private void Awake()
    {
        if (eventHub == null)
        {
            Debug.LogError($"[KeyboardDebugTranslator] {gameObject.name}: " +
                           "AlignmentEventHubSO がアサインされていません！コンポーネントを停止します。");
            this.enabled = false;
        }
    }

    private void Start()
    {
        Debug.Log("[KeyboardDebugTranslator] 起動しました。" +
                  " WASD=移動, QE: Y NM:Y回転（時計/反時計）, RF=スケール拡縮, Space=保存, Backspace=リセット");
    }

    // ─── 毎フレーム: 入力を取得して掲示板に書き込む ────────────────

    private void Update()
    {
        float dt = Time.deltaTime;

        // ── 移動 (WASD) ──────────────────────────────────────
        // 旧MeshAlignmentManagerでのカメラ相対移動を参考にしつつ、
        // カメラ相対は後のVRControllerで取り扱うため、ここではWorld座標でのシンプルな移動とする。
        float x = Input.GetAxis("Horizontal"); // A=-1, D=+1
        float z = Input.GetAxis("Vertical");   // S=-1, W=+1
        bool moveUp   = Input.GetKey(KeyCode.E);
        bool moveDown = Input.GetKey(KeyCode.Q);
        float y = (moveUp ? 1f : 0f) + (moveDown ? -1f : 0f);

        Vector3 moveDelta = new Vector3(x, y, z) * moveSpeed * dt;
        if (moveDelta.sqrMagnitude > 0.00001f)
        {
            Debug.Log($"[KeyboardDebugTranslator] 移動入力を検知: rawInput=({x:F2},{y:F2},{z:F2})");
            eventHub.RequestMove(moveDelta);
        }

        // ── 回転 (Y軸: ←→ or 専用キー) ──────────────────────────
        float rotInput = 0f;
        if (Input.GetKey(KeyCode.N)) rotInput = +1f;  // 時計回り
        if (Input.GetKey(KeyCode.M))  rotInput = -1f;  // 反時計回り

        if (Mathf.Abs(rotInput) > 0.001f)
        {
            float angle = rotInput * rotateSpeed * dt;
            Debug.Log($"[KeyboardDebugTranslator] 回転入力を検知: rotInput={rotInput:F2}, angle={angle:F2}度");
            eventHub.RequestRotate(Vector3.up, angle);
        }

        // ── スケール (RF) ──────────────────────────────────────
        float scaleInput = 0f;
        if (Input.GetKey(KeyCode.R)) scaleInput = +1f;  // 拡大
        if (Input.GetKey(KeyCode.F)) scaleInput = -1f;  // 縮小

        if (Mathf.Abs(scaleInput) > 0.001f)
        {
            float scaleDelta = scaleInput * scaleSpeed * dt;
            Debug.Log($"[KeyboardDebugTranslator] スケール入力を検知: scaleDelta={scaleDelta:F4}");
            eventHub.RequestScale(scaleDelta);
        }

        // ── 保存 (Space) ──────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[KeyboardDebugTranslator] 保存キー(Space)が押されました。掲示板に書き込みます。");
            eventHub.RequestSave();
        }

        // ── リセット (Backspace) ──────────────────────────────────
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            Debug.Log("[KeyboardDebugTranslator] リセットキー(Backspace)が押されました。掲示板に書き込みます。");
            eventHub.RequestReset();
        }
    }
}
