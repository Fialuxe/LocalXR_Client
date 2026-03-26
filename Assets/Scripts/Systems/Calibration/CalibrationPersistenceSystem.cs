using UnityEngine;

/// <summary>
/// 【裏方 / System - Calibration (ローカル保存)】
/// 掲示板（AlignmentEventHubSO）に「保存要求」が来た瞬間に、
/// 対象メッシュの現在Transformを PlayerPrefs に保存する役割を担う。
///
/// アルゴリズム由来:
/// 旧 MeshAlignmentManager.SaveAlignment() と AlignmentPersistence クラスの
/// PlayerPrefs書き込みアルゴリズムをそのまま活用。
/// ただし「誰が保存を命令したか」は一切知らず、掲示板からのイベントだけを聞く。
///
/// アタッチ場所: Hierarchy内の [Systems] という空のGameObject
/// </summary>
public class CalibrationPersistenceSystem : MonoBehaviour
{
    [Header("Event Channel (必須)")]
    [Tooltip("保存トリガーを受け取る掲示板。AlignmentEventHub.assetをここへドラッグ。")]
    [SerializeField] private AlignmentEventHubSO eventHub;

    [Header("保存対象 (必須)")]
    [Tooltip("現在のTransformを保存・復元する対象メッシュ。AlignableTargetがアタッチされたオブジェクトを指定。")]
    [SerializeField] private Transform targetMesh;

    [Header("保存設定")]
    [Tooltip("PlayerPrefsに書き込む際のキープレフィックス")]
    [SerializeField] private string saveKeyPrefix = "Calibration_LocalWorker_";

    [Tooltip("起動時に保存済みデータが存在すれば自動で復元するか")]
    [SerializeField] private bool autoLoadOnStart = true;

    // ─── ライフサイクル ─────────────────────────────────

    private void Awake()
    {
        if (eventHub == null)
        {
            Debug.LogError($"[CalibrationPersistenceSystem] {gameObject.name}: " +
                           "AlignmentEventHubSO がアサインされていません！コンポーネントを停止します。");
            this.enabled = false; return;
        }
        if (targetMesh == null)
        {
            // ScannedRoomSpawnerが動的にInstantiateする場合、OnTargetMeshSpawnedで後から受け取る。
            // Inspector指定でも問題なく動作する。
            Debug.LogWarning($"[CalibrationPersistenceSystem] {gameObject.name}: " +
                             "targetMesh が未アサインです。OnTargetMeshSpawned で動的に設定されることを期待します。");
        }
    }

    private void Start()
    {
        if (autoLoadOnStart) TryAutoLoad();
    }

    private void OnEnable()
    {
        if (eventHub == null) return;
        eventHub.OnSaveRequested      += HandleSave;
        eventHub.OnResetRequested     += HandleReset;
        eventHub.OnTargetMeshSpawned  += HandleMeshSpawned;
        Debug.Log("[CalibrationPersistenceSystem] 掲示板の購読を開始しました（Save, Reset, MeshSpawned）。");
    }

    private void OnDisable()
    {
        if (eventHub == null) return;
        eventHub.OnSaveRequested      -= HandleSave;
        eventHub.OnResetRequested     -= HandleReset;
        eventHub.OnTargetMeshSpawned  -= HandleMeshSpawned;
        Debug.Log("[CalibrationPersistenceSystem] 掲示板の購読を解除しました。");
    }

    // ─── ハンドラ ─────────────────────────────────────

    private void HandleMeshSpawned(Transform spawnedMesh)
    {
        targetMesh = spawnedMesh;
        Debug.Log($"[CalibrationPersistenceSystem] targetMesh を動的に設定しました: {targetMesh.name}");
        // メッシュが届いてから自動ロードを実行する
        if (autoLoadOnStart) TryAutoLoad();
    }

    private void HandleSave()
    {
        try
        {
            // 旧 AlignmentPersistence.SaveTransform() のアルゴリズムを利用
            Vector3    pos   = targetMesh.position;
            Quaternion rot   = targetMesh.rotation;
            Vector3    scale = targetMesh.localScale;

            PlayerPrefs.SetFloat(saveKeyPrefix + "PosX",   pos.x);
            PlayerPrefs.SetFloat(saveKeyPrefix + "PosY",   pos.y);
            PlayerPrefs.SetFloat(saveKeyPrefix + "PosZ",   pos.z);
            PlayerPrefs.SetFloat(saveKeyPrefix + "RotX",   rot.x);
            PlayerPrefs.SetFloat(saveKeyPrefix + "RotY",   rot.y);
            PlayerPrefs.SetFloat(saveKeyPrefix + "RotZ",   rot.z);
            PlayerPrefs.SetFloat(saveKeyPrefix + "RotW",   rot.w);
            PlayerPrefs.SetFloat(saveKeyPrefix + "ScaleX", scale.x);
            PlayerPrefs.SetFloat(saveKeyPrefix + "ScaleY", scale.y);
            PlayerPrefs.SetFloat(saveKeyPrefix + "ScaleZ", scale.z);
            PlayerPrefs.Save();

            Debug.Log($"[CalibrationPersistenceSystem] 保存完了。 " +
                      $"position={pos}, rotation={rot.eulerAngles}, scale={scale}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CalibrationPersistenceSystem] 保存中にエラーが発生しました: {e.Message}");
        }
    }

    private void HandleReset()
    {
        string[] subKeys = { "PosX","PosY","PosZ","RotX","RotY","RotZ","RotW","ScaleX","ScaleY","ScaleZ" };
        foreach (var sub in subKeys) PlayerPrefs.DeleteKey(saveKeyPrefix + sub);
        PlayerPrefs.Save();
        Debug.Log("[CalibrationPersistenceSystem] 保存データをリセット（削除）しました。");
    }

    // ─── 起動時の自動読み込み ────────────────────────────

    private void TryAutoLoad()
    {
        if (!PlayerPrefs.HasKey(saveKeyPrefix + "PosX"))
        {
            Debug.Log("[CalibrationPersistenceSystem] 保存データが存在しません。デフォルト位置のまま起動します。");
            return;
        }

        try
        {
            targetMesh.position = new Vector3(
                PlayerPrefs.GetFloat(saveKeyPrefix + "PosX"),
                PlayerPrefs.GetFloat(saveKeyPrefix + "PosY"),
                PlayerPrefs.GetFloat(saveKeyPrefix + "PosZ")
            );
            targetMesh.rotation = new Quaternion(
                PlayerPrefs.GetFloat(saveKeyPrefix + "RotX"),
                PlayerPrefs.GetFloat(saveKeyPrefix + "RotY"),
                PlayerPrefs.GetFloat(saveKeyPrefix + "RotZ"),
                PlayerPrefs.GetFloat(saveKeyPrefix + "RotW")
            );
            targetMesh.localScale = new Vector3(
                PlayerPrefs.GetFloat(saveKeyPrefix + "ScaleX"),
                PlayerPrefs.GetFloat(saveKeyPrefix + "ScaleY"),
                PlayerPrefs.GetFloat(saveKeyPrefix + "ScaleZ")
            );
            Debug.Log($"[CalibrationPersistenceSystem] 保存データを自動復元しました。" +
                      $" position={targetMesh.position}, scale={targetMesh.localScale}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CalibrationPersistenceSystem] 自動復元中にエラーが発生しました: {e.Message}");
        }
    }
}
