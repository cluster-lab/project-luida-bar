#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Merged data-collection fake gimmick. Runs up to three pipeline phases in
/// fixed order on a single trigger:
///
///   1. Push data        — writes (label, value) into the staging dict
///                          ($.groupState.collectedData) via global state
///                          luida_collect_&lt;label&gt;.
///   2. Save pushed data — fires `exp_recordCustomData` so the calculator
///                          snapshots the staging dict and pushes one row into
///                          the upload buffer.
///   3. Upload saved data — fires `exp_uploadCustomData` so the buffer is flushed
///                          to the LUIDA backend via $.callExternal.
///
/// Disabled phases are no-ops (they patch their underlying statement to
/// write to a `luida_noop_*` key that nobody reads).
///
/// Replaces the three separate gimmicks LuidaSendDataToCollectorGimmick,
/// LuidaProcessDataAndSaveToCollectionGimmick, and
/// LuidaUploadCollectedDataGimmick — those are kept around (deprecated) so
/// existing scenes don't break.
/// </summary>
[ExecuteInEditMode]
public class LuidaDataCollectionGimmick : LuidaFakeGimmick
{
    protected override string TargetPrefabPath
        => "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/DataCollection";

    [Header("Pipeline phases (run in order)")]
    [SerializeField] public bool doAdd    = true;   // Phase 1 — "Push data"
    [SerializeField] public bool doSave   = false;  // Phase 2 — "Save pushed data"
    [SerializeField] public bool doSubmit = false;  // Phase 3 — "Upload saved data"

    [Header("Push data (used only when 'Push data' is enabled)")]
    [SerializeField] public string label;
    [SerializeField] public CckCollectedValueType valueType = CckCollectedValueType.Integer;

    [SerializeField] public bool    boolValue;
    [SerializeField] public float   floatValue;
    [SerializeField] public int     integerValue;
    [SerializeField] public Vector2 vector2Value;
    [SerializeField] public Vector3 vector3Value;

    protected override void OnAfterCopiedComponentSetup()
    {
        // Phase 1 — Push data into staging dict (via luida_collect_<label>)
        if (doAdd && !string.IsNullOrEmpty(label))
        {
            object typedValue;
            switch (valueType)
            {
                case CckCollectedValueType.Bool:    typedValue = boolValue; break;
                case CckCollectedValueType.Float:   typedValue = floatValue; break;
                case CckCollectedValueType.Integer: typedValue = integerValue; break;
                case CckCollectedValueType.Vector2: typedValue = vector2Value; break;
                case CckCollectedValueType.Vector3: typedValue = vector3Value; break;
                default:                            typedValue = false; break;
            }
            PatchStatementAt(CopiedComponent, 0,
                LuidaSendDataToCollectorGimmick.ComposeStateKey(label),
                (int)valueType, typedValue);
        }
        else
        {
            // No-op slot — write a Bool false to a key nobody reads.
            PatchStatementAt(CopiedComponent, 0, "luida_noop_add", 1, false);
        }

        // Phase 2 — Save pushed data (fire exp_recordCustomData). Signal, not Bool: the
        // LUIDA-DataCollector's listener only re-fires on a fresh timestamp, and
        // a constant Bool's encoded timestamp never changes (true → epoch+1ms),
        // so a Bool write would only trigger the first save in a session.
        PatchStatementAtSignal(CopiedComponent, 1,
            doSave ? "exp_recordCustomData" : "luida_noop_save");

        // Phase 3 — Upload saved data (fire exp_uploadCustomData). Same Signal rationale.
        PatchStatementAtSignal(CopiedComponent, 2,
            doSubmit ? "exp_uploadCustomData" : "luida_noop_submit");
    }

    // Player target is not meaningful here (the data sink is a singleton item).
    // Mirrors LuidaSendDataToCollectorGimmick.ResolveTarget.
    protected override CustomGimmickTarget ResolveTarget(CustomGimmickTarget configured)
    {
        return configured == CustomGimmickTarget.Player ? CustomGimmickTarget.Item : configured;
    }
}
#endif
