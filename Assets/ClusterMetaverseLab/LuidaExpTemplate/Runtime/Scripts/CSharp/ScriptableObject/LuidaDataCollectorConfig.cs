#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Source kind for a data-collector field.
///
/// The 5 [Obsolete] members are kept ONLY so old serialized assets still
/// load. The Migrate() routine rewrites them to the current kinds on first
/// load. Live code (UI + generator) only emits/dispatches the 6 active kinds.
/// </summary>
public enum DataFieldSourceKind
{
    // Legacy — serialization compat only. UI hides; Migrate() rewrites.
    [Obsolete("Migrated to Collected on load")]        LegacyCckGimmickSent      = 0,
    [Obsolete("Migrated to GlobalStateRead on load")] LegacyStateRead            = 1,
    [Obsolete("Migrated to Collected on load")]        LegacyCollectedDataLookup = 3,
    [Obsolete("Migrated to JsExpression on load")]     LegacyLiteral             = 4,
    [Obsolete("Migrated to JsExpression on load")]     LegacyCustomJsExpression  = 5,

    // Active user-facing kinds:
    ConditionLookup  = 2,
    Arithmetic       = 6,
    Conditional      = 7,
    Collected        = 8,
    GlobalStateRead  = 9,
    JsExpression     = 10,
}

/// <summary>
/// Operand kind inside Arithmetic operands or Conditional branches.
/// Same legacy-vs-active scheme as DataFieldSourceKind.
/// </summary>
public enum LeafSourceKind
{
    [Obsolete("Migrated to Collected on load")]       LegacyCollected       = 0,
    [Obsolete("Migrated to JsExpression on load")]    LegacyLiteral         = 1,
    [Obsolete("Migrated to GlobalStateRead on load")] LegacyStateRead       = 2,
    [Obsolete("Migrated to ConditionLookup on load")] LegacyConditionLookup = 3,
    [Obsolete("Migrated to JsExpression on load")]    LegacyCustomJs        = 4,

    Collected        = 5,
    GlobalStateRead  = 6,
    ConditionLookup  = 7,
    JsExpression     = 8,
}

/// <summary>
/// Narrow value type that LuidaSendDataToCollectorGimmick can send through
/// CCK. Mirrors ClusterVR.CreatorKit.ParameterType indices (Bool=1, Float=2,
/// ...) so they pass straight to LuidaFakeGimmick.PatchStatementToConstant.
/// String/Quaternion are intentionally excluded — they cannot flow through
/// CCK ConstantValue.
/// </summary>
public enum CckCollectedValueType
{
    Bool    = 1,
    Float   = 2,
    Integer = 3,
    Vector2 = 4,
    Vector3 = 5,
}

/// <summary>
/// Broad value type for Section A registry entries — superset of
/// CckCollectedValueType (same integer values) plus String for action-only
/// labels that can't flow through CCK.
/// </summary>
public enum CollectedValueType
{
    Bool    = 1,
    Float   = 2,
    Integer = 3,
    Vector2 = 4,
    Vector3 = 5,
    String  = 6,
}

/// <summary>Scope kept around for migrating legacy StateRead fields; not used in new UI.</summary>
public enum CckStateScope
{
    Global,
    This,
    Owner,
}

/// <summary>Literal kind kept around for migrating legacy Literal fields/leaves; not used in new UI.</summary>
public enum LiteralKind
{
    Bool,
    Number,
    String,
}

/// <summary>
/// Sub-kind for the "Direct value" source (DataFieldSourceKind.JsExpression / LeafSourceKind.JsExpression).
/// Number/String/Bool emit typed JS literals (no manual quoting needed by the user).
/// CustomJs emits the user's raw JS verbatim wrapped in parens (the previous merged behavior).
/// </summary>
public enum DirectValueKind
{
    Number,
    String,
    Bool,
    CustomJs,
}

public enum ArithmeticOp
{
    [InspectorName("+")] Add,
    [InspectorName("−")] Sub,
    [InspectorName("×")] Mul,
    [InspectorName("÷")] Div,
}

public enum ComparisonOp
{
    [InspectorName("==")] Eq,
    [InspectorName("≠")]  NotEq,
    [InspectorName("<")]  Lt,
    [InspectorName("≤")]  LtEq,
    [InspectorName(">")]  Gt,
    [InspectorName("≥")]  GtEq,
}

[Serializable]
public class CollectedLabel
{
    public string label;
    public CollectedValueType type = CollectedValueType.Integer;
}

[Serializable]
public class OperandLeaf
{
    public LeafSourceKind kind = LeafSourceKind.Collected;

    // Collected / ConditionLookup
    public string label;

    // GlobalStateRead
    public string stateKey;
    public CckCollectedValueType stateType = CckCollectedValueType.Integer;

    // JsExpression (= "Direct value"). jsExpression holds the raw string;
    // interpretation depends on directValueKind:
    //   Number   → parsed as JS numeric literal
    //   String   → emitted quoted/escaped
    //   Bool     → "true"/"false"
    //   CustomJs → emitted verbatim in parens (legacy "JsExpression" behavior)
    public string jsExpression = "";
    public DirectValueKind directValueKind = DirectValueKind.Number;

    // Legacy fields kept for one-shot migration. Read once by Migrate(), then dead.
    public LiteralKind legacyLiteralKind;
    public string legacyLiteralValue;
    public string legacyCustomJs;
    public CckStateScope legacyStateScope;
}

[Serializable]
public class OperandRow
{
    public OperandLeaf operand = new OperandLeaf();
    public ArithmeticOp opToNext = ArithmeticOp.Add;
}

[Serializable]
public class DataCollectorField
{
    public string fieldName;
    public DataFieldSourceKind source = DataFieldSourceKind.Collected;

    // Collected
    public string collectedLabel;

    // GlobalStateRead
    public string globalStateKey;
    public CckCollectedValueType globalStateType = CckCollectedValueType.Integer;

    // ConditionLookup
    public string conditionVariableName;

    // JsExpression (= "Direct value"). See OperandLeaf comment above.
    public string jsExpression = "";
    public DirectValueKind directValueKind = DirectValueKind.Number;

    // Arithmetic
    public List<OperandRow> arithmeticOperands = new List<OperandRow>();

    // Conditional
    public OperandLeaf conditionalLhs  = new OperandLeaf();
    public ComparisonOp conditionalOp  = ComparisonOp.Eq;
    public OperandLeaf conditionalRhs  = new OperandLeaf();
    public OperandLeaf conditionalThen = new OperandLeaf();
    public OperandLeaf conditionalElse = new OperandLeaf();

    // Legacy fields kept for one-shot migration only.
    public CckCollectedValueType legacyCckGimmickValueType;
    public CckStateScope legacyStateScope;
    public string legacyStateKey;
    public CckCollectedValueType legacyStateType;
    public string legacyCollectedDataLabel;
    public LiteralKind legacyLiteralKind;
    public string legacyLiteralValue;
    public string legacyCustomJsExpression;
}

/// <summary>
/// Per-scene config asset that drives DataCollector calculator JS generation.
/// Stored at Assets/_Experiment_/Settings/DataCollectorConfig/{SceneName}.asset.
/// </summary>
[CreateAssetMenu(fileName = "LuidaDataCollectorConfig", menuName = "LUIDA/Data Collector Config")]
public class LuidaDataCollectorConfig : ScriptableObject
{
    /// <summary>0 = legacy schema (pre-refinement). 1 = current. Bumped by Migrate().</summary>
    public int schemaVersion = 0;

    /// <summary>Section A — labels & types the gimmick / action can write.</summary>
    public List<CollectedLabel> collectedLabels = new List<CollectedLabel>();

    /// <summary>Section B — fields to be saved into the uploaded JSON dict.</summary>
    public List<DataCollectorField> fields = new List<DataCollectorField>();

    [TextArea(6, 30)]
    public string customJsSuffix = "";

    /// <summary>Builder by default; Code mode is opt-in via the window toggle.</summary>
    public bool useCustomCodeMode = false;

    [TextArea(6, 30)]
    public string rawJs = "";

    public DataCollectorField FindFieldByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return fields.Find(f => f != null && f.fieldName == name);
    }

    public CollectedLabel FindCollectedLabel(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return collectedLabels.Find(l => l != null && l.label == name);
    }

    /// <summary>
    /// Returns the registered labels. If cckCompatibleOnly is true, String-
    /// typed entries are filtered out (those can't be written by the CCK gimmick).
    /// </summary>
    public string[] GetCollectedLabelNames(bool cckCompatibleOnly)
    {
        var names = new List<string>();
        foreach (var l in collectedLabels)
        {
            if (l == null || string.IsNullOrEmpty(l.label)) continue;
            if (cckCompatibleOnly && l.type == CollectedValueType.String) continue;
            names.Add(l.label);
        }
        return names.ToArray();
    }

    // Legacy compat — old call site in LuidaSendDataToCollectorGimmickEditor used this.
    // Maps to the new registry (CCK-compatible labels only).
    public string[] GetCckFieldNames() => GetCollectedLabelNames(true);
}

/// <summary>
/// One-shot upgrade for assets saved with the previous schema.
/// </summary>
public static class LuidaDataCollectorConfigMigrator
{
    public const int CurrentSchemaVersion = 2;

    public static void Migrate(LuidaDataCollectorConfig c)
    {
        if (c == null || c.schemaVersion >= CurrentSchemaVersion) return;

        if (c.schemaVersion < 1) MigrateToV1(c);
        if (c.schemaVersion < 2) MigrateToV2(c);

        c.schemaVersion = CurrentSchemaVersion;
        EditorUtility.SetDirty(c);
    }

    /// <summary>
    /// v2 — "JsExpression" source split into Number/String/Bool/CustomJs sub-kinds
    /// (still under the same source-kind integer to preserve serialization).
    /// Existing JsExpression items are preserved by setting directValueKind = CustomJs.
    /// </summary>
    static void MigrateToV2(LuidaDataCollectorConfig c)
    {
        foreach (var f in c.fields)
        {
            if (f == null) continue;
            if (f.source == DataFieldSourceKind.JsExpression)
                f.directValueKind = DirectValueKind.CustomJs;

            if (f.arithmeticOperands != null)
            {
                foreach (var row in f.arithmeticOperands)
                {
                    if (row?.operand?.kind == LeafSourceKind.JsExpression)
                        row.operand.directValueKind = DirectValueKind.CustomJs;
                }
            }
            CoerceLeafJsExprToCustom(f.conditionalLhs);
            CoerceLeafJsExprToCustom(f.conditionalRhs);
            CoerceLeafJsExprToCustom(f.conditionalThen);
            CoerceLeafJsExprToCustom(f.conditionalElse);
        }
    }

    static void CoerceLeafJsExprToCustom(OperandLeaf leaf)
    {
        if (leaf != null && leaf.kind == LeafSourceKind.JsExpression)
            leaf.directValueKind = DirectValueKind.CustomJs;
    }

    /// <summary>
    /// v1 — the original migration from the pre-refinement schema (legacy enum integers
    /// for CckGimmickSent/StateRead/CollectedDataLookup/Literal/CustomJsExpression → new kinds).
    /// </summary>
    static void MigrateToV1(LuidaDataCollectorConfig c)
    {
#pragma warning disable CS0618 // Type or member is obsolete — we explicitly read legacy values here
        foreach (var f in c.fields)
        {
            if (f == null) continue;
            int rawSource = (int)f.source;
            switch (rawSource)
            {
                case 0: // LegacyCckGimmickSent → Collected
                    f.source = DataFieldSourceKind.Collected;
                    f.collectedLabel = f.fieldName;
                    EnsureCollectedLabel(c, f.fieldName, (CollectedValueType)(int)f.legacyCckGimmickValueType);
                    break;

                case 1: // LegacyStateRead → GlobalStateRead (force Global)
                    f.source = DataFieldSourceKind.GlobalStateRead;
                    f.globalStateKey = f.legacyStateKey;
                    f.globalStateType = f.legacyStateType;
                    if (f.legacyStateScope != CckStateScope.Global)
                    {
                        Debug.LogWarning($"[LUIDA] Field '{f.fieldName}' had state scope {f.legacyStateScope}; coerced to Global by migration.");
                    }
                    break;

                case 3: // LegacyCollectedDataLookup → Collected
                    f.source = DataFieldSourceKind.Collected;
                    f.collectedLabel = f.legacyCollectedDataLabel;
                    break;

                case 4: // LegacyLiteral → JsExpression
                    f.source = DataFieldSourceKind.JsExpression;
                    f.jsExpression = LiteralToJs(f.legacyLiteralKind, f.legacyLiteralValue);
                    break;

                case 5: // LegacyCustomJsExpression → JsExpression
                    f.source = DataFieldSourceKind.JsExpression;
                    f.jsExpression = f.legacyCustomJsExpression ?? string.Empty;
                    break;

                // 2 ConditionLookup / 6 Arithmetic / 7 Conditional carry over unchanged.
            }

            // Recurse into the operand leaves of Arithmetic / Conditional fields.
            if (f.arithmeticOperands != null)
            {
                foreach (var row in f.arithmeticOperands)
                    if (row != null) MigrateLeaf(row.operand);
            }
            MigrateLeaf(f.conditionalLhs);
            MigrateLeaf(f.conditionalRhs);
            MigrateLeaf(f.conditionalThen);
            MigrateLeaf(f.conditionalElse);
        }
#pragma warning restore CS0618
    }

    static void MigrateLeaf(OperandLeaf leaf)
    {
        if (leaf == null) return;
#pragma warning disable CS0618
        int raw = (int)leaf.kind;
        switch (raw)
        {
            case 0: // LegacyCollected → Collected (no data shape change; label stays)
                leaf.kind = LeafSourceKind.Collected;
                break;
            case 1: // LegacyLiteral → JsExpression
                leaf.kind = LeafSourceKind.JsExpression;
                leaf.jsExpression = LiteralToJs(leaf.legacyLiteralKind, leaf.legacyLiteralValue);
                break;
            case 2: // LegacyStateRead → GlobalStateRead (force Global)
                leaf.kind = LeafSourceKind.GlobalStateRead;
                if (leaf.legacyStateScope != CckStateScope.Global)
                    Debug.LogWarning($"[LUIDA] Operand leaf had state scope {leaf.legacyStateScope}; coerced to Global.");
                break;
            case 3: // LegacyConditionLookup → ConditionLookup (label is the variable name)
                leaf.kind = LeafSourceKind.ConditionLookup;
                break;
            case 4: // LegacyCustomJs → JsExpression
                leaf.kind = LeafSourceKind.JsExpression;
                leaf.jsExpression = leaf.legacyCustomJs ?? string.Empty;
                break;
        }
#pragma warning restore CS0618
    }

    static void EnsureCollectedLabel(LuidaDataCollectorConfig c, string name, CollectedValueType type)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (c.FindCollectedLabel(name) != null) return;
        c.collectedLabels.Add(new CollectedLabel { label = name, type = type });
    }

    static string LiteralToJs(LiteralKind kind, string raw)
    {
        switch (kind)
        {
            case LiteralKind.Bool:
                bool b = !string.IsNullOrEmpty(raw) &&
                         (raw.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) || raw.Trim() == "1");
                return b ? "true" : "false";
            case LiteralKind.Number:
                return string.IsNullOrEmpty(raw) ? "0" : raw.Trim();
            case LiteralKind.String:
                return "\"" + EscapeJsString(raw ?? string.Empty) + "\"";
        }
        return "undefined";
    }

    static string EscapeJsString(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"':  sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:   sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
#endif
