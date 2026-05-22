#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Small colored type tag drawn next to a field name in the calculator
/// Builder list. Color reflects the value-type group; label is a short
/// abbreviation so the badge fits in tight rows.
/// </summary>
public static class CalculatorTypeBadge
{
    static GUIStyle _style;

    static GUIStyle Style
    {
        get
        {
            if (_style == null)
            {
                _style = new GUIStyle(EditorStyles.miniLabel);
                _style.alignment = TextAnchor.MiddleCenter;
                _style.fontStyle = FontStyle.Bold;
                _style.padding = new RectOffset(4, 4, 0, 0);
                _style.normal.textColor = Color.white;
            }
            return _style;
        }
    }

    /// <summary>Resolve badge label/color from a field. Walks to the registry for Collected.</summary>
    public static (string label, Color color) ForField(DataCollectorField f, LuidaDataCollectorConfig config)
    {
        if (f == null) return ("?", GrayBadge);
        switch (f.source)
        {
            case DataFieldSourceKind.Collected:
                return ForCollectedLabel(config, f.collectedLabel);
            case DataFieldSourceKind.GlobalStateRead:
                return ForCckType(f.globalStateType);
            case DataFieldSourceKind.Arithmetic:
                return ("Num", BlueBadge);
            case DataFieldSourceKind.Conditional:
                return ForConditionalBranches(f, config);
            case DataFieldSourceKind.ConditionLookup:
                return ("Auto", GrayBadge);
            case DataFieldSourceKind.JsExpression:
                return ForDirectValueKind(f.directValueKind);
        }
        return ("?", GrayBadge);
    }

    public static (string label, Color color) ForLeaf(OperandLeaf leaf, LuidaDataCollectorConfig config)
    {
        if (leaf == null) return ("?", GrayBadge);
        switch (leaf.kind)
        {
            case LeafSourceKind.Collected:        return ForCollectedLabel(config, leaf.label);
            case LeafSourceKind.GlobalStateRead:  return ForCckType(leaf.stateType);
            case LeafSourceKind.ConditionLookup:  return ("Auto", GrayBadge);
            case LeafSourceKind.JsExpression:     return ForDirectValueKind(leaf.directValueKind);
        }
        return ("?", GrayBadge);
    }

    static (string, Color) ForDirectValueKind(DirectValueKind k)
    {
        switch (k)
        {
            case DirectValueKind.Number:   return ("Num",  BlueBadge);
            case DirectValueKind.String:   return ("Str",  OrangeBadge);
            case DirectValueKind.Bool:     return ("Bool", GreenBadge);
            case DirectValueKind.CustomJs: return ("Auto", GrayBadge);
        }
        return ("Auto", GrayBadge);
    }

    public static (string label, Color color) ForCollectedValueType(CollectedValueType t)
    {
        switch (t)
        {
            case CollectedValueType.Bool:    return ("Bool", GreenBadge);
            case CollectedValueType.Integer: return ("Int",  BlueBadge);
            case CollectedValueType.Float:   return ("Num",  BlueBadge);
            case CollectedValueType.Vector2: return ("Vec2", PurpleBadge);
            case CollectedValueType.Vector3: return ("Vec3", PurpleBadge);
            case CollectedValueType.String:  return ("Str",  OrangeBadge);
        }
        return ("?", GrayBadge);
    }

    static (string, Color) ForCckType(CckCollectedValueType t)
    {
        switch (t)
        {
            case CckCollectedValueType.Bool:    return ("Bool", GreenBadge);
            case CckCollectedValueType.Integer: return ("Int",  BlueBadge);
            case CckCollectedValueType.Float:   return ("Num",  BlueBadge);
            case CckCollectedValueType.Vector2: return ("Vec2", PurpleBadge);
            case CckCollectedValueType.Vector3: return ("Vec3", PurpleBadge);
        }
        return ("?", GrayBadge);
    }

    static (string, Color) ForCollectedLabel(LuidaDataCollectorConfig config, string label)
    {
        if (config == null || string.IsNullOrEmpty(label)) return ("Auto", GrayBadge);
        var entry = config.FindCollectedLabel(label);
        if (entry == null) return ("Auto", GrayBadge);
        return ForCollectedValueType(entry.type);
    }

    static (string, Color) ForConditionalBranches(DataCollectorField f, LuidaDataCollectorConfig config)
    {
        var thn = ForLeaf(f.conditionalThen, config);
        var els = ForLeaf(f.conditionalElse, config);
        if (thn.label == els.label) return thn;
        return ("Mixed", GrayBadge);
    }

    public static void Draw(Rect rect, string label, Color bg)
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = bg;
        GUI.Box(rect, label, Style);
        GUI.backgroundColor = prev;
    }

    public static void DrawForField(Rect rect, DataCollectorField f, LuidaDataCollectorConfig config)
    {
        var (label, color) = ForField(f, config);
        Draw(rect, label, color);
    }

    public static void DrawForLeaf(Rect rect, OperandLeaf leaf, LuidaDataCollectorConfig config)
    {
        var (label, color) = ForLeaf(leaf, config);
        Draw(rect, label, color);
    }

    static readonly Color GreenBadge  = new Color(0.40f, 0.78f, 0.50f, 1f);
    static readonly Color BlueBadge   = new Color(0.35f, 0.60f, 0.90f, 1f);
    static readonly Color PurpleBadge = new Color(0.75f, 0.50f, 0.90f, 1f);
    static readonly Color OrangeBadge = new Color(0.95f, 0.65f, 0.35f, 1f);
    static readonly Color GrayBadge   = new Color(0.55f, 0.55f, 0.55f, 1f);
}
#endif
