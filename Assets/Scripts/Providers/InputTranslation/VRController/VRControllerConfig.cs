using System;
using UnityEngine;

/// <summary>
/// 【VRコントローラー専用「説明書」】
/// VRControllerTranslator が読む操作設定。
/// Inspectorからパラメータを変更するだけで操作感を調整できる（コード修正不要）。
/// </summary>
[Serializable]
public class VRControllerConfig
{
    [Header("移動")]
    [Tooltip("右スティック入力に掛ける移動速度倍率 (m/s)")]
    public float moveSpeed = 0.5f;

    [Tooltip("右トリガーによる上昇速度 (m/s)")]
    public float verticalSpeed = 0.5f;

    [Tooltip("スティック入力のデッドゾーン（この値以下は無視）")]
    [Range(0.01f, 0.5f)]
    public float stickDeadzone = 0.05f;

    [Header("回転")]
    [Tooltip("左スティックX軸によるY軸回転速度 (度/s)")]
    public float rotateSpeed = 30.0f;

    [Header("スケール")]
    [Tooltip("左トリガー長押しによるスケール変化速度")]
    public float scaleSpeed = 0.1f;

    [Header("グリップ設定")]
    [Tooltip("グリップ入力がこの閾値を超えるとアライメントモードON")]
    [Range(0.1f, 0.9f)]
    public float gripThreshold = 0.5f;
}
