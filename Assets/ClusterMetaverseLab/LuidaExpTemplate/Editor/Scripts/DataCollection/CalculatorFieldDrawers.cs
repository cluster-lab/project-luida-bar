#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Renders a single DataCollectorField row inside the calculator Builder
/// ReorderableList. Each row has a fixed header line (name + source picker +
/// type badge + remove) and a source-kind-specific body underneath. All
/// drawers are pure-static; mutation is recorded via Undo.RecordObject on
/// the host config so the user can Cmd-Z any edit.
///
/// Active source kinds (6): Collected, GlobalStateRead, ConditionLookup,
/// JsExpression, Arithmetic, Conditional. The 5 legacy kinds are migrated
/// at load and never drawn.
/// </summary>
public static class CalculatorFieldDrawers
{
    public static float LineH    => EditorGUIUtility.singleLineHeight;
    public static float Spacing  => EditorGUIUtility.standardVerticalSpacing;
    public static float LineStep => LineH + Spacing;

    const float SourceDropdownWidth = 140f;
    const float BadgeWidth          = 46f;
    const float RemoveButtonWidth   = 22f;
    const float OpPickerWidth       = 48f;
    const float LeafKindWidth       = 110f;
    const float MinNameWidth        = 80f;
    const float Gap                 = 4f;
    const float IndentX             = 16f;
    const string RemoveButtonText   = "✕";

    // The 6 active source kinds, displayed in this user-friendly order.
    static readonly DataFieldSourceKind[] ActiveSources = new[]
    {
        DataFieldSourceKind.Collected,
        DataFieldSourceKind.GlobalStateRead,
        DataFieldSourceKind.ConditionLookup,
        DataFieldSourceKind.JsExpression,
        DataFieldSourceKind.Arithmetic,
        DataFieldSourceKind.Conditional,
    };
    static readonly string[] ActiveSourceLabels = new[]
    {
        "Collected",
        "Global state read",
        "Condition lookup",
        "Direct value",
        "+ − × ÷",
        "if ... else ...",
    };

    static readonly LeafSourceKind[] ActiveLeafKinds = new[]
    {
        LeafSourceKind.Collected,
        LeafSourceKind.GlobalStateRead,
        LeafSourceKind.ConditionLookup,
        LeafSourceKind.JsExpression,
    };
    static readonly string[] ActiveLeafLabels = new[]
    {
        "Collected",
        "Global state",
        "Condition",
        "Direct val",
    };

    static readonly DirectValueKind[] DirectKinds = new[]
    {
        DirectValueKind.Number,
        DirectValueKind.String,
        DirectValueKind.Bool,
        DirectValueKind.CustomJs,
    };
    static readonly string[] DirectKindLabels = new[]
    {
        "Number",
        "String",
        "Bool",
        "Custom JS",
    };

    // ─── Public API ─────────────────────────────────────────────────────

    public static float ComputeFieldHeight(DataCollectorField f)
    {
        return ComputeFieldLineCount(f) * LineStep + 2f;
    }

    static int ComputeFieldLineCount(DataCollectorField f)
    {
        if (f == null) return 1;
        int lines = 1; // header
        switch (f.source)
        {
            case DataFieldSourceKind.Collected:
            case DataFieldSourceKind.GlobalStateRead:
            case DataFieldSourceKind.ConditionLookup:
            case DataFieldSourceKind.JsExpression:
                lines += 1;
                break;
            case DataFieldSourceKind.Arithmetic:
                int n = f.arithmeticOperands != null ? f.arithmeticOperands.Count : 0;
                lines += n + 1; // operands + Add button
                break;
            case DataFieldSourceKind.Conditional:
                lines += 4; // if-lhs, op-rhs, then, else
                break;
            default:
                lines += 1;
                break;
        }
        return lines;
    }

    public static bool DrawField(Rect rect, DataCollectorField field, LuidaDataCollectorConfig config, System.Action onRemove)
    {
        if (field == null) return false;
        float y = rect.y;

        Rect removeRect = new Rect(rect.x + rect.width - RemoveButtonWidth, y, RemoveButtonWidth, LineH);
        Rect badgeRect  = new Rect(removeRect.x - Gap - BadgeWidth, y, BadgeWidth, LineH);
        Rect sourceRect = new Rect(badgeRect.x - Gap - SourceDropdownWidth, y, SourceDropdownWidth, LineH);
        Rect nameRect   = new Rect(rect.x, y, Mathf.Max(MinNameWidth, sourceRect.x - rect.x - Gap), LineH);

        EditorGUI.BeginChangeCheck();
        string newName = EditorGUI.TextField(nameRect, field.fieldName ?? string.Empty);
        if (EditorGUI.EndChangeCheck() && newName != field.fieldName)
        {
            RecordEdit(config, "Edit field name");
            field.fieldName = newName;
        }

        // Source dropdown — only the 6 active kinds shown.
        int activeIdx = System.Array.IndexOf(ActiveSources, field.source);
        if (activeIdx < 0) activeIdx = 0; // legacy enum value mid-migration; fall back
        int newIdx = EditorGUI.Popup(sourceRect, activeIdx, ActiveSourceLabels);
        if (newIdx != activeIdx)
        {
            RecordEdit(config, "Change field source");
            field.source = ActiveSources[newIdx];
            EnsureFieldDefaults(field);
        }

        CalculatorTypeBadge.DrawForField(badgeRect, field, config);

        if (GUI.Button(removeRect, RemoveButtonText))
        {
            onRemove?.Invoke();
            return true;
        }

        y += LineStep;

        Rect bodyRect = new Rect(rect.x + IndentX, y, rect.width - IndentX, rect.height - (y - rect.y));
        switch (field.source)
        {
            case DataFieldSourceKind.Collected:       DrawCollectedBody(bodyRect, field, config); break;
            case DataFieldSourceKind.GlobalStateRead: DrawGlobalStateReadBody(bodyRect, field, config); break;
            case DataFieldSourceKind.ConditionLookup: DrawConditionLookupBody(bodyRect, field, config); break;
            case DataFieldSourceKind.JsExpression:    DrawJsExpressionBody(bodyRect, field, config); break;
            case DataFieldSourceKind.Arithmetic:      DrawArithmeticBody(bodyRect, field, config); break;
            case DataFieldSourceKind.Conditional:     DrawConditionalBody(bodyRect, field, config); break;
        }

        return false;
    }

    // ─── Per-source body drawers ────────────────────────────────────────

    static void DrawCollectedBody(Rect rect, DataCollectorField f, LuidaDataCollectorConfig config)
    {
        Rect line = new Rect(rect.x, rect.y, rect.width, LineH);
        float labelWidth = 70f;
        Rect lblRect = new Rect(line.x, line.y, labelWidth, LineH);
        Rect dropRect = new Rect(lblRect.xMax, line.y, line.width - labelWidth, LineH);
        EditorGUI.LabelField(lblRect, "Label");
        DrawCandidatePopup(dropRect, config?.GetCollectedLabelNames(false) ?? new string[0],
            f.collectedLabel, picked =>
            {
                RecordEdit(config, "Pick Collected label");
                f.collectedLabel = picked;
            },
            emptyPlaceholder: "(no labels registered — add in Section A)");
    }

    static void DrawGlobalStateReadBody(Rect rect, DataCollectorField f, LuidaDataCollectorConfig config)
    {
        Rect line = new Rect(rect.x, rect.y, rect.width, LineH);

        float keyLabelWidth = 40f, typeWidth = 80f;
        Rect kLblRect = new Rect(line.x, line.y, keyLabelWidth, LineH);
        Rect keyRect  = new Rect(kLblRect.xMax, line.y, line.width - keyLabelWidth - typeWidth - Gap, LineH);
        Rect typeRect = new Rect(keyRect.xMax + Gap, line.y, typeWidth, LineH);

        EditorGUI.LabelField(kLblRect, "Key");
        EditorGUI.BeginChangeCheck();
        string newKey = EditorGUI.TextField(keyRect, f.globalStateKey ?? string.Empty);
        var newType = (CckCollectedValueType)EditorGUI.EnumPopup(typeRect, f.globalStateType);
        if (EditorGUI.EndChangeCheck())
        {
            RecordEdit(config, "Edit global state read");
            f.globalStateKey = newKey;
            f.globalStateType = newType;
        }
    }

    static void DrawConditionLookupBody(Rect rect, DataCollectorField f, LuidaDataCollectorConfig config)
    {
        Rect line = new Rect(rect.x, rect.y, rect.width, LineH);
        float labelWidth = 100f;
        Rect lblRect = new Rect(line.x, line.y, labelWidth, LineH);
        Rect dropRect = new Rect(lblRect.xMax, line.y, line.width - labelWidth, LineH);
        EditorGUI.LabelField(lblRect, "Variable name");
        DrawCandidatePopup(dropRect, ExperimentVariableNames.LoadForActiveScene(),
            f.conditionVariableName, picked =>
            {
                RecordEdit(config, "Pick condition variable");
                f.conditionVariableName = picked;
            },
            emptyPlaceholder: "(no experiment variables defined for this scene)");
    }

    static void DrawJsExpressionBody(Rect rect, DataCollectorField f, LuidaDataCollectorConfig config)
    {
        Rect line = new Rect(rect.x, rect.y, rect.width, LineH);

        float labelWidth = 50f, kindWidth = 90f;
        Rect lblRect  = new Rect(line.x, line.y, labelWidth, LineH);
        Rect kindRect = new Rect(lblRect.xMax, line.y, kindWidth, LineH);
        Rect valRect  = new Rect(kindRect.xMax + Gap, line.y, line.width - labelWidth - kindWidth - Gap, LineH);

        EditorGUI.LabelField(lblRect, "Value");

        // Type sub-kind picker (Number / String / Bool / Custom JS)
        int kIdx = System.Array.IndexOf(DirectKinds, f.directValueKind);
        if (kIdx < 0) kIdx = 0;
        int newKIdx = EditorGUI.Popup(kindRect, kIdx, DirectKindLabels);
        if (newKIdx != kIdx)
        {
            RecordEdit(config, "Change direct value kind");
            f.directValueKind = DirectKinds[newKIdx];
            // Reset to a sensible per-kind default when switching kinds.
            f.jsExpression = DefaultRawForKind(f.directValueKind);
        }

        DrawDirectValueEditor(valRect, ref f.jsExpression, f.directValueKind, config);
    }

    static void DrawArithmeticBody(Rect rect, DataCollectorField f, LuidaDataCollectorConfig config)
    {
        if (f.arithmeticOperands == null) f.arithmeticOperands = new List<OperandRow>();

        float y = rect.y;
        for (int i = 0; i < f.arithmeticOperands.Count; i++)
        {
            Rect line = new Rect(rect.x, y, rect.width, LineH);
            bool isLast = (i == f.arithmeticOperands.Count - 1);

            float opWidth = isLast ? 0f : OpPickerWidth;
            float removeWidth = 22f;
            float leafWidth = line.width - opWidth - removeWidth - (isLast ? 1 : 2) * Gap;

            Rect leafRect = new Rect(line.x, line.y, leafWidth, LineH);
            Rect opRect = new Rect(leafRect.xMax + Gap, line.y, opWidth, LineH);
            Rect removeRect = new Rect(line.width + rect.x - removeWidth, line.y, removeWidth, LineH);

            DrawLeaf(leafRect, f.arithmeticOperands[i].operand, config);

            if (!isLast)
            {
                EditorGUI.BeginChangeCheck();
                var newOp = (ArithmeticOp)EditorGUI.EnumPopup(opRect, f.arithmeticOperands[i].opToNext);
                if (EditorGUI.EndChangeCheck() && newOp != f.arithmeticOperands[i].opToNext)
                {
                    RecordEdit(config, "Edit arithmetic op");
                    f.arithmeticOperands[i].opToNext = newOp;
                }
            }

            if (GUI.Button(removeRect, RemoveButtonText))
            {
                RecordEdit(config, "Remove operand");
                f.arithmeticOperands.RemoveAt(i);
                GUIUtility.ExitGUI();
                return;
            }

            y += LineStep;
        }

        Rect addRect = new Rect(rect.x, y, rect.width, LineH);
        if (GUI.Button(addRect, "+ Add operand"))
        {
            RecordEdit(config, "Add operand");
            f.arithmeticOperands.Add(new OperandRow { operand = new OperandLeaf(), opToNext = ArithmeticOp.Add });
        }
    }

    static void DrawConditionalBody(Rect rect, DataCollectorField f, LuidaDataCollectorConfig config)
    {
        if (f.conditionalLhs  == null) f.conditionalLhs  = new OperandLeaf();
        if (f.conditionalRhs  == null) f.conditionalRhs  = new OperandLeaf();
        if (f.conditionalThen == null) f.conditionalThen = new OperandLeaf();
        if (f.conditionalElse == null) f.conditionalElse = new OperandLeaf();

        float y = rect.y;
        float prefix = 40f;

        // line 1: "if   <lhs leaf>"
        Rect line1 = new Rect(rect.x, y, rect.width, LineH);
        EditorGUI.LabelField(new Rect(line1.x, line1.y, prefix, LineH), "if");
        DrawLeaf(new Rect(line1.x + prefix, line1.y, line1.width - prefix, LineH), f.conditionalLhs, config);
        y += LineStep;

        // line 2: "<op>  <rhs leaf>"
        Rect line2 = new Rect(rect.x, y, rect.width, LineH);
        Rect opRect = new Rect(line2.x + prefix, line2.y, 60f, LineH);
        Rect rhsRect = new Rect(opRect.xMax + Gap, line2.y, line2.width - prefix - opRect.width - Gap, LineH);

        EditorGUI.BeginChangeCheck();
        var newOp = (ComparisonOp)EditorGUI.EnumPopup(opRect, f.conditionalOp);
        if (EditorGUI.EndChangeCheck() && newOp != f.conditionalOp)
        {
            RecordEdit(config, "Edit comparison op");
            f.conditionalOp = newOp;
        }
        DrawLeaf(rhsRect, f.conditionalRhs, config);
        y += LineStep;

        // line 3: "then <leaf>"
        Rect line3 = new Rect(rect.x, y, rect.width, LineH);
        EditorGUI.LabelField(new Rect(line3.x, line3.y, prefix, LineH), "then");
        DrawLeaf(new Rect(line3.x + prefix, line3.y, line3.width - prefix, LineH), f.conditionalThen, config);
        y += LineStep;

        // line 4: "else <leaf>"
        Rect line4 = new Rect(rect.x, y, rect.width, LineH);
        EditorGUI.LabelField(new Rect(line4.x, line4.y, prefix, LineH), "else");
        DrawLeaf(new Rect(line4.x + prefix, line4.y, line4.width - prefix, LineH), f.conditionalElse, config);
    }

    // ─── Operand-leaf drawer (one line) ─────────────────────────────────

    public static void DrawLeaf(Rect rect, OperandLeaf leaf, LuidaDataCollectorConfig config)
    {
        if (leaf == null) return;

        Rect kindRect = new Rect(rect.x, rect.y, LeafKindWidth, LineH);
        Rect badgeRect = new Rect(rect.x + rect.width - BadgeWidth, rect.y, BadgeWidth, LineH);
        Rect paramsRect = new Rect(kindRect.xMax + Gap, rect.y, badgeRect.x - kindRect.xMax - 2 * Gap, LineH);

        int activeIdx = System.Array.IndexOf(ActiveLeafKinds, leaf.kind);
        if (activeIdx < 0) activeIdx = 0;
        int newIdx = EditorGUI.Popup(kindRect, activeIdx, ActiveLeafLabels);
        if (newIdx != activeIdx)
        {
            RecordEdit(config, "Change leaf kind");
            leaf.kind = ActiveLeafKinds[newIdx];
        }

        switch (leaf.kind)
        {
            case LeafSourceKind.Collected:        DrawLeafCollected(paramsRect, leaf, config); break;
            case LeafSourceKind.GlobalStateRead:  DrawLeafGlobalStateRead(paramsRect, leaf, config); break;
            case LeafSourceKind.ConditionLookup:  DrawLeafCondition(paramsRect, leaf, config); break;
            case LeafSourceKind.JsExpression:     DrawLeafJsExpression(paramsRect, leaf, config); break;
        }

        CalculatorTypeBadge.DrawForLeaf(badgeRect, leaf, config);
    }

    static void DrawLeafCollected(Rect rect, OperandLeaf leaf, LuidaDataCollectorConfig config)
    {
        DrawCandidatePopup(rect, config?.GetCollectedLabelNames(false) ?? new string[0],
            leaf.label, picked =>
            {
                RecordEdit(config, "Pick operand label");
                leaf.label = picked;
            },
            emptyPlaceholder: "(no labels)");
    }

    static void DrawLeafGlobalStateRead(Rect rect, OperandLeaf leaf, LuidaDataCollectorConfig config)
    {
        float typeWidth = 70f;
        Rect keyRect  = new Rect(rect.x, rect.y, rect.width - typeWidth - Gap, LineH);
        Rect typeRect = new Rect(keyRect.xMax + Gap, rect.y, typeWidth, LineH);

        EditorGUI.BeginChangeCheck();
        string newKey = EditorGUI.TextField(keyRect, leaf.stateKey ?? string.Empty);
        var newType = (CckCollectedValueType)EditorGUI.EnumPopup(typeRect, leaf.stateType);
        if (EditorGUI.EndChangeCheck())
        {
            RecordEdit(config, "Edit leaf state read");
            leaf.stateKey = newKey;
            leaf.stateType = newType;
        }
    }

    static void DrawLeafCondition(Rect rect, OperandLeaf leaf, LuidaDataCollectorConfig config)
    {
        DrawCandidatePopup(rect, ExperimentVariableNames.LoadForActiveScene(),
            leaf.label, picked =>
            {
                RecordEdit(config, "Pick leaf condition variable");
                leaf.label = picked;
            },
            emptyPlaceholder: "(no vars)");
    }

    static void DrawLeafJsExpression(Rect rect, OperandLeaf leaf, LuidaDataCollectorConfig config)
    {
        float kindWidth = 80f;
        Rect kindRect = new Rect(rect.x, rect.y, kindWidth, LineH);
        Rect valRect  = new Rect(kindRect.xMax + Gap, rect.y, rect.width - kindWidth - Gap, LineH);

        int kIdx = System.Array.IndexOf(DirectKinds, leaf.directValueKind);
        if (kIdx < 0) kIdx = 0;
        int newKIdx = EditorGUI.Popup(kindRect, kIdx, DirectKindLabels);
        if (newKIdx != kIdx)
        {
            RecordEdit(config, "Change leaf direct value kind");
            leaf.directValueKind = DirectKinds[newKIdx];
            leaf.jsExpression = DefaultRawForKind(leaf.directValueKind);
        }

        DrawDirectValueEditor(valRect, ref leaf.jsExpression, leaf.directValueKind, config);
    }

    // ─── Shared helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Popup-only candidate picker used by Collected and Condition lookup.
    /// Always shows the dropdown; if `current` isn't in `candidates`, prepends
    /// a synthetic "<current> (not registered)" entry so the user can see what
    /// they have without an extra text field. Empty candidate list shows a
    /// disabled placeholder.
    /// </summary>
    static void DrawCandidatePopup(Rect rect, string[] candidates, string current, System.Action<string> onPick, string emptyPlaceholder)
    {
        if (candidates == null) candidates = new string[0];

        if (candidates.Length == 0 && string.IsNullOrEmpty(current))
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.Popup(rect, 0, new[] { emptyPlaceholder });
            }
            return;
        }

        var display = new List<string>(candidates);
        int idx = display.IndexOf(current ?? string.Empty);
        bool stale = idx < 0 && !string.IsNullOrEmpty(current);
        if (stale)
        {
            display.Insert(0, current + "  (not registered)");
            idx = 0;
        }
        if (idx < 0) idx = -1; // candidates exist but nothing chosen yet

        int newIdx = EditorGUI.Popup(rect, idx, display.ToArray());
        if (newIdx < 0) return;
        if (stale && newIdx == 0) return; // user kept the stale entry
        int candidateIdx = stale ? newIdx - 1 : newIdx;
        if (candidateIdx < 0 || candidateIdx >= candidates.Length) return;
        if (candidates[candidateIdx] != current) onPick?.Invoke(candidates[candidateIdx]);
    }

    /// <summary>
    /// Editor for the raw string backing a Direct value. Number/String use a
    /// text field (no quoting needed by the user — generator handles it).
    /// Bool uses a toggle. CustomJs uses a text field accepting raw JS.
    /// </summary>
    static void DrawDirectValueEditor(Rect rect, ref string raw, DirectValueKind kind, LuidaDataCollectorConfig config)
    {
        switch (kind)
        {
            case DirectValueKind.Number:
            {
                EditorGUI.BeginChangeCheck();
                string typed = EditorGUI.TextField(rect, raw ?? string.Empty);
                if (EditorGUI.EndChangeCheck() && typed != raw)
                {
                    RecordEdit(config, "Edit Number value");
                    raw = typed;
                }
                break;
            }
            case DirectValueKind.String:
            {
                EditorGUI.BeginChangeCheck();
                string typed = EditorGUI.TextField(rect, raw ?? string.Empty);
                if (EditorGUI.EndChangeCheck() && typed != raw)
                {
                    RecordEdit(config, "Edit String value");
                    raw = typed;
                }
                break;
            }
            case DirectValueKind.Bool:
            {
                bool current = !string.IsNullOrEmpty(raw) && raw.Trim().Equals("true", System.StringComparison.OrdinalIgnoreCase);
                EditorGUI.BeginChangeCheck();
                bool next = EditorGUI.Toggle(rect, current);
                if (EditorGUI.EndChangeCheck() && next != current)
                {
                    RecordEdit(config, "Edit Bool value");
                    raw = next ? "true" : "false";
                }
                break;
            }
            case DirectValueKind.CustomJs:
            {
                EditorGUI.BeginChangeCheck();
                string typed = EditorGUI.TextField(rect, raw ?? string.Empty);
                if (EditorGUI.EndChangeCheck() && typed != raw)
                {
                    RecordEdit(config, "Edit Custom JS");
                    raw = typed;
                }
                break;
            }
        }
    }

    static string DefaultRawForKind(DirectValueKind kind)
    {
        switch (kind)
        {
            case DirectValueKind.Number:   return "0";
            case DirectValueKind.String:   return "";
            case DirectValueKind.Bool:     return "false";
            case DirectValueKind.CustomJs: return "";
        }
        return "";
    }

    static void EnsureFieldDefaults(DataCollectorField f)
    {
        if (f == null) return;
        switch (f.source)
        {
            case DataFieldSourceKind.Arithmetic:
                if (f.arithmeticOperands == null || f.arithmeticOperands.Count == 0)
                {
                    f.arithmeticOperands = new List<OperandRow>
                    {
                        new OperandRow { operand = new OperandLeaf(), opToNext = ArithmeticOp.Add },
                        new OperandRow { operand = new OperandLeaf(), opToNext = ArithmeticOp.Add },
                    };
                }
                break;
            case DataFieldSourceKind.Conditional:
                if (f.conditionalLhs  == null) f.conditionalLhs  = new OperandLeaf();
                if (f.conditionalRhs  == null) f.conditionalRhs  = new OperandLeaf();
                if (f.conditionalThen == null) f.conditionalThen = new OperandLeaf();
                if (f.conditionalElse == null) f.conditionalElse = new OperandLeaf();
                break;
        }
    }

    static void RecordEdit(Object target, string label)
    {
        if (target == null) return;
        Undo.RecordObject(target, label);
        EditorUtility.SetDirty(target);
    }
}
#endif
