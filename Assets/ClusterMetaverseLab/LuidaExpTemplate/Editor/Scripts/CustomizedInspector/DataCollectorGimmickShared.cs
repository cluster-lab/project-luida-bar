#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UI helpers shared by both the legacy LuidaSendDataToCollectorGimmick
/// inspector and the new merged LuidaDataCollectionGimmick inspector.
///
/// The two gimmicks happen to expose identical field NAMES for the
/// "Push data" sub-controls (label / valueType / boolValue / floatValue /
/// integerValue / vector2Value / vector3Value), so these helpers operate on
/// the SerializedObject without needing the concrete class.
/// </summary>
public static class DataCollectorGimmickShared
{
    // ─── Section header + ⚙ button ─────────────────────────────────────

    public static void DrawSectionHeaderWithConfigButton(string title)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        var icon = EditorGUIUtility.IconContent("d_Settings@2x");
        if (icon == null || icon.image == null) icon = new GUIContent("⚙");
        icon.tooltip = "Open the LUIDA Data Collector configuration window";
        if (GUILayout.Button(icon, EditorStyles.miniButton, GUILayout.Width(28), GUILayout.Height(20)))
        {
            DataCollectorConfigTab.ShowWindow();
        }
        EditorGUILayout.EndHorizontal();
    }

    // ─── Config asset lookup (migrates on load) ────────────────────────

    public static LuidaDataCollectorConfig FindBuilderConfig()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName)) return null;
        string configPath = $"{DataCollectorCreateMenu.DataCollectorConfigFolderPath}{sceneName}.asset";
        var cfg = AssetDatabase.LoadAssetAtPath<LuidaDataCollectorConfig>(configPath);
        if (cfg != null) LuidaDataCollectorConfigMigrator.Migrate(cfg);
        return cfg;
    }

    // ─── Scene-presence warning ────────────────────────────────────────

    public static void DrawSceneSanityWarnings()
    {
        if (Object.FindObjectOfType<LuidaDataCollector>() == null)
        {
            EditorGUILayout.HelpBox(
                "No LUIDA-DataCollector exists in this scene. This gimmick will silently no-op until you add one " +
                "(GameObject → LUIDA → Data Collector).",
                MessageType.Warning);
        }
    }

    // ─── Label field (dropdown + Manual mode) ──────────────────────────

    public static void DrawLabelField(SerializedObject so, LuidaDataCollectorConfig config)
    {
        var labelProp = so.FindProperty("label");
        string current = labelProp.stringValue ?? "";

        string[] registered = config?.GetCollectedLabelNames(true) ?? new string[0];

        // Count String-typed labels that are intentionally hidden from this gimmick.
        // The data-collection fake gimmick routes the value through CCK ConstantValue,
        // which can't carry strings — they only flow through the "Push data to
        // collector" state-listening action. Surface this so users don't wonder why
        // their String labels are missing from the dropdown.
        int hiddenStringLabels = 0;
        if (config != null)
        {
            foreach (var l in config.collectedLabels)
            {
                if (l != null && !string.IsNullOrEmpty(l.label) && l.type == CollectedValueType.String)
                    hiddenStringLabels++;
            }
        }

        if (registered.Length == 0)
        {
            // No candidates — surface the only sensible next action: open the
            // config window and add a label there. Manual typing is intentionally
            // not offered (it would let users desync from the registry).
            string emptyHint = hiddenStringLabels > 0
                ? $"No CCK-compatible collected-data items defined yet ({hiddenStringLabels} String-typed item(s) exist but can't be sent by this gimmick — use the \"Push data to collector\" state-listening action for strings)."
                : "No collected-data items defined yet. Add one in the Data Collector configuration window first.";
            EditorGUILayout.HelpBox(emptyHint, MessageType.Info);
            if (GUILayout.Button("Add data to collect…", GUILayout.Height(24)))
            {
                DataCollectorConfigTab.ShowWindow();
            }
            return;
        }

        // Popup-only picker (no manual-entry sentinel). If the current value
        // is unknown to the registry, prepend a synthetic "<value> (not
        // registered)" row so the user sees what's there and can re-pick.
        var display = new List<string>(registered);
        int selectedIdx = display.IndexOf(current);
        bool stale = selectedIdx < 0 && !string.IsNullOrEmpty(current);
        if (stale)
        {
            display.Insert(0, current + "  (not registered)");
            selectedIdx = 0;
        }

        int newIdx = EditorGUILayout.Popup("Label", selectedIdx < 0 ? -1 : selectedIdx, display.ToArray());
        if (newIdx < 0) return;
        if (stale && newIdx == 0) return; // user kept the stale entry
        int registeredIdx = stale ? newIdx - 1 : newIdx;
        if (registeredIdx >= 0 && registeredIdx < registered.Length && registered[registeredIdx] != current)
        {
            labelProp.stringValue = registered[registeredIdx];
        }

        if (!string.IsNullOrEmpty(labelProp.stringValue) && !LuidaDataCollectorJsGenerator.IsValidFieldName(labelProp.stringValue))
        {
            EditorGUILayout.HelpBox(
                $"'{labelProp.stringValue}' is not a valid identifier. Use letters, digits, underscores; must not start with a digit.",
                MessageType.Error);
        }

        if (hiddenStringLabels > 0)
        {
            EditorGUILayout.HelpBox(
                $"{hiddenStringLabels} String-typed item(s) registered but hidden — CCK can't transport strings, so they can only be set via the \"Push data to collector\" state-listening action.",
                MessageType.None);
        }
    }

    // ─── Value type field (auto-locks to registered entry) ─────────────

    public static void DrawValueTypeField(SerializedObject so, string currentLabel, LuidaDataCollectorConfig config)
    {
        var typeProp = so.FindProperty("valueType");
        var current = (CckCollectedValueType)typeProp.intValue;

        CollectedLabel registered = config?.FindCollectedLabel(currentLabel);
        bool autoLocked = registered != null && registered.type != CollectedValueType.String;

        if (autoLocked)
        {
            CckCollectedValueType expected = MapToCckType(registered.type);
            if (expected != current)
            {
                typeProp.intValue = (int)expected;
                current = expected;
            }
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Value Type", current);
            }
            EditorGUILayout.HelpBox(
                $"Type is locked to the registry entry ({registered.type}). Edit in the Data Collector config window to change.",
                MessageType.None);
        }
        else
        {
            var picked = (CckCollectedValueType)EditorGUILayout.EnumPopup("Value Type", current);
            if (picked != current)
            {
                typeProp.intValue = (int)picked;
            }
        }
    }

    // ─── Typed value editor ────────────────────────────────────────────

    public static void DrawTypedValueField(SerializedObject so, CckCollectedValueType valueType)
    {
        SerializedProperty prop = null;
        switch (valueType)
        {
            case CckCollectedValueType.Bool:    prop = so.FindProperty("boolValue"); break;
            case CckCollectedValueType.Float:   prop = so.FindProperty("floatValue"); break;
            case CckCollectedValueType.Integer: prop = so.FindProperty("integerValue"); break;
            case CckCollectedValueType.Vector2: prop = so.FindProperty("vector2Value"); break;
            case CckCollectedValueType.Vector3: prop = so.FindProperty("vector3Value"); break;
        }
        if (prop != null) EditorGUILayout.PropertyField(prop, new GUIContent("Value"));
    }

    // ─── Register controls ─────────────────────────────────────────────

    public static void DrawRegisterControls(string label, CckCollectedValueType valueType, LuidaDataCollectorConfig config)
    {
        EditorGUILayout.Space();
        if (!LuidaDataCollectorJsGenerator.IsValidFieldName(label)) return;

        if (config == null)
        {
            EditorGUILayout.HelpBox("DataCollector config will be auto-created on register.", MessageType.None);
            if (GUILayout.Button($"Create config + register '{label}' as {valueType}"))
            {
                RegisterLabel(label, valueType);
            }
            return;
        }

        var existing = config.FindCollectedLabel(label);
        var desiredType = MapCckToCollected(valueType);
        if (existing == null)
        {
            if (GUILayout.Button($"Register label '{label}' as {desiredType}"))
            {
                RegisterLabel(label, valueType);
            }
        }
        else if (existing.type != desiredType)
        {
            EditorGUILayout.HelpBox(
                $"Registered type is {existing.type}, gimmick is set to {desiredType}.",
                MessageType.Warning);
            if (GUILayout.Button($"Update registered type to {desiredType}"))
            {
                Undo.RecordObject(config, "Update registered type");
                existing.type = desiredType;
                EditorUtility.SetDirty(config);
                DataCollectorJsSaver.WriteAndCombine(config);
            }
        }
        else
        {
            EditorGUILayout.HelpBox($"Registered as {existing.type} ✓", MessageType.Info);
        }
    }

    static void RegisterLabel(string label, CckCollectedValueType valueType)
    {
        var config = DataCollectorCreateMenu.FindOrCreateBuilderConfig();
        if (config == null)
        {
            EditorUtility.DisplayDialog("LUIDA", "Could not locate or create the DataCollector config asset. Is the scene saved?", "OK");
            return;
        }
        LuidaDataCollectorConfigMigrator.Migrate(config);

        var existing = config.FindCollectedLabel(label);
        Undo.RecordObject(config, "Register collected label");
        var t = MapCckToCollected(valueType);
        if (existing == null)
        {
            config.collectedLabels.Add(new CollectedLabel { label = label, type = t });
        }
        else
        {
            existing.type = t;
        }
        EditorUtility.SetDirty(config);
        DataCollectorJsSaver.WriteAndCombine(config);
    }

    public static CckCollectedValueType MapToCckType(CollectedValueType t)
    {
        switch (t)
        {
            case CollectedValueType.Bool:    return CckCollectedValueType.Bool;
            case CollectedValueType.Float:   return CckCollectedValueType.Float;
            case CollectedValueType.Integer: return CckCollectedValueType.Integer;
            case CollectedValueType.Vector2: return CckCollectedValueType.Vector2;
            case CollectedValueType.Vector3: return CckCollectedValueType.Vector3;
        }
        return CckCollectedValueType.Integer;
    }

    public static CollectedValueType MapCckToCollected(CckCollectedValueType t)
    {
        switch (t)
        {
            case CckCollectedValueType.Bool:    return CollectedValueType.Bool;
            case CckCollectedValueType.Float:   return CollectedValueType.Float;
            case CckCollectedValueType.Integer: return CollectedValueType.Integer;
            case CckCollectedValueType.Vector2: return CollectedValueType.Vector2;
            case CckCollectedValueType.Vector3: return CollectedValueType.Vector3;
        }
        return CollectedValueType.Integer;
    }

    // ─── Trigger signal section (target / key / item dropdowns) ────────

    /// <summary>
    /// Draws the standard "Gimmick Signal" section that every fake gimmick
    /// shows. Coerces Player → Item (CCK rejects Player keys outside PlayerLocalUI).
    /// </summary>
    public static void DrawTriggerSignalSection(SerializedObject so)
    {
        EditorGUILayout.LabelField("Gimmick Signal", EditorStyles.boldLabel);

        var targetProp = so.FindProperty("target");
        if (targetProp.enumValueIndex == (int)CustomGimmickTarget.Player)
        {
            targetProp.enumValueIndex = (int)CustomGimmickTarget.Item;
        }
        var allowedValues = new[] { CustomGimmickTarget.Item, CustomGimmickTarget.Global, CustomGimmickTarget.This };
        var allowedLabels = new[] { "Item", "Global", "This" };
        int activeIdx = System.Array.IndexOf(allowedValues, (CustomGimmickTarget)targetProp.enumValueIndex);
        if (activeIdx < 0) activeIdx = 0;
        int newActiveIdx = EditorGUILayout.Popup("Trigger Target", activeIdx, allowedLabels);
        if (newActiveIdx != activeIdx)
        {
            targetProp.enumValueIndex = (int)allowedValues[newActiveIdx];
        }
        EditorGUILayout.PropertyField(so.FindProperty("key"), new GUIContent("Trigger Key"));

        var targetEnum = (CustomGimmickTarget)targetProp.enumValueIndex;
        if (targetEnum == CustomGimmickTarget.Item)
        {
            EditorGUILayout.PropertyField(so.FindProperty("item"), new GUIContent("Target Item"));
        }

        string hint = targetEnum == CustomGimmickTarget.Global
            ? "Triggered by a global signal.\nUse $.setStateCompat('global', '<key>', true) to fire, or wire a CCK trigger gimmick to this signal."
            : "Triggered from the same item.\nUse $.sendSignalCompat('this', '<key>') from ClusterScript, or wire a CCK trigger gimmick to this signal.";
        EditorGUILayout.HelpBox(hint, MessageType.Info);
    }
}
#endif
