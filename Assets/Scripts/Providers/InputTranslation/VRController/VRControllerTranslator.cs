using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// 【翻訳者 / Provider - VRController】
/// HMDに付属するVRコントローラーの入力を読み取り、アライメントのIntentに翻訳して
/// 掲示板（AlignmentEventHubSO）に書き込む。
///
/// ━━ アルゴリズム由来 ━━
/// 旧 MeshAlignmentManager.HandleVRControllerInput() のロジックを参考に、
/// Event Channel パターンに沿って完全に書き直したもの。
///  - 右スティック → XZ平面移動（カメラ向き基準）
///  - 右トリガー  → Y軸（高さ）移動
///  - 左スティックX → Y軸回転
///  - どちらかのグリップ押下 → アライメントモードON
///  - どちらかのMenuボタン → 保存要求
///
/// アタッチ場所: Hierarchy内の [InputProviders] という空のGameObject
/// （または XR Origin / Camera Rig 配下に置いても可）
/// </summary>
public class VRControllerTranslator : MonoBehaviour
{
    [Header("Event Channel (必須)")]
    [Tooltip("AlignmentEventHub.assetをここへドラッグ。")]
    [SerializeField] private AlignmentEventHubSO eventHub;

    [Header("操作設定（説明書）")]
    [SerializeField] private VRControllerConfig config = new VRControllerConfig();

    // ─── 内部状態 ─────────────────────────────────────
    private bool _alignmentModeActive = false;

    // ─── ライフサイクル ─────────────────────────────────

    private void Awake()
    {
        if (eventHub == null)
        {
            Debug.LogError($"[VRControllerTranslator] {gameObject.name}: " +
                           "AlignmentEventHubSO がアサインされていません！コンポーネントを停止します。");
            this.enabled = false;
        }
    }

    private void Start()
    {
        Debug.Log("[VRControllerTranslator] 起動しました。グリップを握るとアライメントモードがONになります。");
    }

    // ─── 毎フレーム処理 ─────────────────────────────────

    private void Update()
    {
        InputDevice left  = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        bool leftValid  = left.isValid;
        bool rightValid = right.isValid;

        if (!leftValid && !rightValid)
        {
            // 毎フレーム出ると煩いので、前フレームも両方無効だった場合はスキップ。
            // 将来的にはバグ対応としてタイマーで間引くのが望ましい。
            return;
        }

        // ─ グリップ判定: アライメントモード ON/OFF ─────────────────
        bool leftGrip  = leftValid  && TryGetGrip(left,  out float lg)  && lg  > config.gripThreshold;
        bool rightGrip = rightValid && TryGetGrip(right, out float rg)  && rg  > config.gripThreshold;

        bool prevMode = _alignmentModeActive;
        _alignmentModeActive = leftGrip || rightGrip;

        if (_alignmentModeActive && !prevMode)
            Debug.Log("<color=green>[VRControllerTranslator] アライメントモード: ON (グリップ検知)</color>");
        else if (!_alignmentModeActive && prevMode)
            Debug.Log("<color=yellow>[VRControllerTranslator] アライメントモード: OFF (グリップ解除)</color>");

        if (!_alignmentModeActive) return;

        // ─ 右コントローラー: XZ平面移動（カメラ向き基準）+ 高さ移動 ─────
        if (rightValid)
        {
            // スティックによるXZ移動
            if (right.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightStick)
                && rightStick.magnitude > config.stickDeadzone)
            {
                // カメラの向きを基準にしたWorld座標移動（旧アルゴリズム踏襲）
                Vector3 camFwd   = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
                Vector3 camRight = Camera.main != null ? Camera.main.transform.right   : Vector3.right;
                camFwd.y = 0f;  camFwd.Normalize();
                camRight.y = 0f; camRight.Normalize();

                Vector3 intentMove = (camRight * rightStick.x + camFwd * rightStick.y)
                                     * config.moveSpeed * Time.deltaTime;

                Debug.Log($"[VRControllerTranslator] XZ移動: intent={intentMove}");
                eventHub.RequestMove(intentMove);
            }

            // 右トリガーによるY軸（高さ）移動
            if (right.TryGetFeatureValue(CommonUsages.trigger, out float rightTrigger)
                && rightTrigger > 0.1f)
            {
                Vector3 vertMove = Vector3.up * rightTrigger * config.verticalSpeed * Time.deltaTime;
                Debug.Log($"[VRControllerTranslator] Y軸移動（上昇）: intent={vertMove}");
                eventHub.RequestMove(vertMove);
            }
        }

        // ─ 左コントローラー: Y軸回転 ─────────────────────────────
        if (leftValid
            && left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftStick)
            && Mathf.Abs(leftStick.x) > config.stickDeadzone)
        {
            float angle = leftStick.x * config.rotateSpeed * Time.deltaTime;
            Debug.Log($"[VRControllerTranslator] Y軸回転: angle={angle:F2}度");
            eventHub.RequestRotate(Vector3.up, angle);
        }

        // ─ Menuボタン: 保存要求 ─────────────────────────────────────
        bool menuPressed = (rightValid && right.TryGetFeatureValue(CommonUsages.menuButton, out bool rm) && rm)
                        || (leftValid  && left.TryGetFeatureValue(CommonUsages.menuButton,  out bool lm) && lm);
        if (menuPressed)
        {
            Debug.Log("[VRControllerTranslator] Menuボタン検知 → 保存要求を掲示板に書き込みます。");
            eventHub.RequestSave();
        }
    }

    // ─── ヘルパー ─────────────────────────────────────

    private static bool TryGetGrip(InputDevice device, out float value)
        => device.TryGetFeatureValue(CommonUsages.grip, out value);
}
