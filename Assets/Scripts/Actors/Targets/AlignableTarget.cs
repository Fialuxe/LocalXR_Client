using UnityEngine;

/// <summary>
/// 【役者 / Actor】
/// アライメントの「対象物」（World側）に直接アタッチするコンポーネント。
/// AlignmentEventHubSO（掲示板）を購読し、書き込みがあれば自分のTransformを変更する。
///
/// - 誰が命令を出したか（コントローラ、キーボード、Photon）を一切知らない。
/// - 移動・回転・スケールのみに責任を持つ（単一責任の原則）。
/// - Photon通信はここに含まない。
/// </summary>
public class AlignableTarget : MonoBehaviour
{
    [Header("Event Channel")]
    [Tooltip("必須: このAlignableMeshが操作される際に使用する掲示板(EventHub)")]
    [SerializeField] private AlignmentEventHubSO eventHub;

    // ─── ライフサイクル ─────────────────────────────────

    private void Awake()
    {
        if (eventHub == null)
        {
            Debug.LogError($"[AlignableTarget] {gameObject.name}: AlignmentEventHubSO が " +
                           "Inspector からアサインされていません！コンポーネントを停止します。");
            this.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (eventHub == null) return;

        eventHub.OnMoveRequested    += HandleMove;
        eventHub.OnRotateRequested  += HandleRotate;
        eventHub.OnScaleRequested   += HandleScale;

        Debug.Log($"[AlignableTarget] {gameObject.name}: 掲示板(EventHub)の購読を開始しました。");
    }

    private void OnDisable()
    {
        if (eventHub == null) return;

        // 購読解除を忘れると、Destroyされたオブジェクトに対してイベントが飛び続け、
        // NullReferenceExceptionの原因になる。必ずここで解除する。
        eventHub.OnMoveRequested    -= HandleMove;
        eventHub.OnRotateRequested  -= HandleRotate;
        eventHub.OnScaleRequested   -= HandleScale;

        Debug.Log($"[AlignableTarget] {gameObject.name}: 掲示板(EventHub)の購読を解除しました。");
    }

    // ─── ハンドラ（掲示板から呼び出される処理） ────────────────

    /// <param name="delta">World空間でのフレーム単位移動量</param>
    private void HandleMove(Vector3 delta)
    {
        transform.position += delta;
        Debug.Log($"[AlignableTarget] {gameObject.name}: 移動指示を受信・適用しました。" +
                  $" delta={delta}, 現在位置={transform.position}");
    }

    /// <param name="axis">回転軸 (World空間)</param>
    /// <param name="angle">回転角度（度）</param>
    private void HandleRotate(Vector3 axis, float angle)
    {
        // Space.World を使う理由:
        // メッシュが初期から90°傾いている場合、Space.Self だとローカルY軸も傾いており
        // 「水平に回したい」意図とズレる。Space.World なら常に世界の垂直軸を中心に回転できる。
        transform.Rotate(axis, angle, Space.World);
        Debug.Log($"[AlignableTarget] {gameObject.name}: 回転指示を受信・適用しました。" +
                  $" axis={axis}(World空間), angle={angle}度, 現在回転={transform.rotation.eulerAngles}");
    }

    /// <param name="delta">スケール変化量（正で拡大、負で縮小）</param>
    private void HandleScale(float delta)
    {
        Vector3 newScale = transform.localScale + Vector3.one * delta;
        // スケールが0以下にならないよう下限を設ける
        newScale = Vector3.Max(newScale, Vector3.one * 0.01f);
        transform.localScale = newScale;
        Debug.Log($"[AlignableTarget] {gameObject.name}: スケール指示を受信・適用しました。" +
                  $" delta={delta}, 現在スケール={transform.localScale}");
    }
}
