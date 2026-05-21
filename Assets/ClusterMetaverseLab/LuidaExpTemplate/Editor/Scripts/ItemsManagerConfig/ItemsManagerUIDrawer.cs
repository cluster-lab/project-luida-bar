using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using ClusterVR.CreatorKit.Item.Implements;
using ClusterVR.CreatorKit.World.Implements.TextView;

public static class ItemsManagerUIDrawer
{
    [System.Serializable]
    public class OscArgument
    {
        public enum OscValueType { Boolean, Number, String }
        public OscValueType Type = OscValueType.String;
        public string Value = "";
    }

    [System.Serializable]
    private class OscArgumentListWrapper
    {
        public List<OscArgument> Arguments = new List<OscArgument>();
    }

    private static string docFilePath = "Assets/Doc/LUIDA-StateListeningItemScriptDoc.md";
    private static readonly string codeFontPath = "Assets/Fonts/FiraCode-Regular.ttf";

    private static readonly string[] HumanoidBoneNames =
    {
        "Hips", "LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg",
        "LeftFoot", "RightFoot", "Spine", "Chest", "Neck", "Head",
        "LeftShoulder", "RightShoulder", "LeftUpperArm", "RightUpperArm",
        "LeftLowerArm", "RightLowerArm", "LeftHand", "RightHand",
        "LeftToes", "RightToes", "LeftEye", "RightEye", "Jaw",
        "LeftThumbProximal", "LeftThumbIntermediate", "LeftThumbDistal",
        "LeftIndexProximal", "LeftIndexIntermediate", "LeftIndexDistal",
        "LeftMiddleProximal", "LeftMiddleIntermediate", "LeftMiddleDistal",
        "LeftRingProximal", "LeftRingIntermediate", "LeftRingDistal",
        "LeftLittleProximal", "LeftLittleIntermediate", "LeftLittleDistal",
        "RightThumbProximal", "RightThumbIntermediate", "RightThumbDistal",
        "RightIndexProximal", "RightIndexIntermediate", "RightIndexDistal",
        "RightMiddleProximal", "RightMiddleIntermediate", "RightMiddleDistal",
        "RightRingProximal", "RightRingIntermediate", "RightRingDistal",
        "RightLittleProximal", "RightLittleIntermediate", "RightLittleDistal",
        "UpperChest",
    };

    private static readonly StateListeningAction[] AvailableStateListeningActions =
    {
        // Item — visibility & text
        new StateListeningAction("Show item", "$.setStateCompat('this', 'exp_showItem', true);", _category: "Item"),
        new StateListeningAction("Hide item", "$.setStateCompat('this', 'exp_showItem', false);", _category: "Item"),
        new StateListeningAction("Set text", "$.subNode('Text').setText(`{_text_}`);", new[] { "text" }, _category: "Item"),

        // Item — transform (requires MovableItem on the item)
        new StateListeningAction("Set position", "$.setPosition(new Vector3({_x_}, {_y_}, {_z_}))", new[] { "x", "y", "z" }, _category: "Item transform"),
        new StateListeningAction("Add position", "$.setPosition($.getPosition().add(new Vector3({_x_}, {_y_}, {_z_})))",
            new[] { "x", "y", "z" }, _category: "Item transform"),
        new StateListeningAction("Set rotation",
            "$.setRotation(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_})))", new[] { "x", "y", "z" }, _category: "Item transform"),
        new StateListeningAction("Add rotation",
            "$.setRotation($.getRotation().multiply(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_}))))",
            new[] { "x", "y", "z" }, _category: "Item transform"),

        // Item child — visibility & transform
        new StateListeningAction("Show child", "$.subNode('{_childName_}').setEnabled(true)", new[] { "childName" }, _category: "Item child"),
        new StateListeningAction("Hide child", "$.subNode('{_childName_}').setEnabled(false)", new[] { "childName" }, _category: "Item child"),
        new StateListeningAction("Set child position", "$.subNode('{_childName_}').setPosition(new Vector3({_x_}, {_y_}, {_z_}))", new[] { "childName", "x", "y", "z" }, _category: "Item child"),
        new StateListeningAction("Add child position", "$.subNode('{_childName_}').setPosition($.subNode('{_childName_}').getPosition().add(new Vector3({_x_}, {_y_}, {_z_})))",
            new[] { "childName", "x", "y", "z" }, _category: "Item child"),
        new StateListeningAction("Set child rotation",
            "$.subNode('{_childName_}').setRotation(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_})))", new[] { "childName", "x", "y", "z" }, _category: "Item child"),
        new StateListeningAction("Add child rotation",
            "$.subNode('{_childName_}').setRotation($.subNode('{_childName_}').getRotation().multiply(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_}))))",
            new[] { "childName", "x", "y", "z" }, _category: "Item child"),

        // State machine
        new StateListeningAction("To next state", "$.sendSignalCompat('this', 'state_triggerTransition');", _category: "State machine"),
        new StateListeningAction("Sleep", "{_seconds_}", new[] { "seconds" }, _category: "State machine"),

        // Data collection
        new StateListeningAction("Send data to collector", "if (!$.groupState.collectedData) $.groupState.collectedData = {};\n    let collectedData = $.groupState.collectedData;\n    collectedData['{_label_}'] = {_value_};\n    $.groupState.collectedData = collectedData;", new[] { "label", "value" }, _displayLabel: "Push data to collector", _category: "Data collection"),
        new StateListeningAction("Process and save collected data", "$.sendSignalCompat('this', 'exp_recordCustomData');", _displayLabel: "Save pushed data in collector", _category: "Data collection"),
        new StateListeningAction("Upload collected data", "$.sendSignalCompat('this', 'exp_uploadCustomData');", _displayLabel: "Upload saved data from collector", _category: "Data collection"),

        // Participant — transform
        new StateListeningAction("Set participant position",
            "PARTICIPANTS[{_participantIndex_}].setPosition(new Vector3({_x_}, {_y_}, {_z_}));",
            new[] { "participantIndex", "x", "y", "z" }, _category: "Participant transform"),
        new StateListeningAction("Add participant position",
            "(() => { var p = PARTICIPANTS[{_participantIndex_}] && PARTICIPANTS[{_participantIndex_}].getPosition(); if (p) PARTICIPANTS[{_participantIndex_}].setPosition(p.add(new Vector3({_x_}, {_y_}, {_z_}))); })();",
            new[] { "participantIndex", "x", "y", "z" }, _category: "Participant transform"),
        new StateListeningAction("Set participant rotation",
            "PARTICIPANTS[{_participantIndex_}].setRotation(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_})));",
            new[] { "participantIndex", "x", "y", "z" }, _category: "Participant transform"),
        new StateListeningAction("Add participant rotation",
            "(() => { var r = PARTICIPANTS[{_participantIndex_}] && PARTICIPANTS[{_participantIndex_}].getRotation(); if (r) PARTICIPANTS[{_participantIndex_}].setRotation(r.multiply(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_})))); })();",
            new[] { "participantIndex", "x", "y", "z" }, _category: "Participant transform"),

        // Participant — feedback & bone tracking
        new StateListeningAction("Send Haptics",
            "PARTICIPANTS[{_participantId_}].send('haptics', {target: {_target_}, frequency: {_frequency_}, amplitude: {_amplitude_}, duration: {_duration_}});",
            new[] { "participantId", "target", "frequency", "amplitude", "duration" }, _category: "Participant"),
        new StateListeningAction("Send via OSC", "PARTICIPANTS[{_participantId_}].send('sendOsc', {address: '{_address_}', values: [{_values_}] });", new[] { "participantId", "address", "values" }, _category: "Participant"),
        new StateListeningAction("Sync with participant bone",
            "(() => {\n" +
            "    try {\n" +
            "        const player = PARTICIPANTS[{_participantIndex_}];\n" +
            "        if (!player || !player.exists()) return;\n" +
            "        const bone = HumanoidBone.{_bone_};\n" +
            "        const bonePosWorld = player.getHumanoidBonePosition(bone);\n" +
            "        const boneRotWorld = player.getHumanoidBoneRotation(bone);\n" +
            "        const posOffset = new Vector3(parseFloat('{_posX_}'), parseFloat('{_posY_}'), parseFloat('{_posZ_}'));\n" +
            "        const rotOffset = new Quaternion().setFromEulerAngles(new Vector3(parseFloat('{_rotX_}'), parseFloat('{_rotY_}'), parseFloat('{_rotZ_}')));\n" +
            "        if (bonePosWorld) $.setPosition(bonePosWorld.add(posOffset));\n" +
            "        if (boneRotWorld) $.setRotation(rotOffset.multiply(boneRotWorld));\n" +
            "    } catch (e) {\n" +
            "        $.log('[SyncWithParticipantBone] ' + e + '. Ensure MovableItem is on this item and bone name is valid.');\n" +
            "    }\n" +
            "})();",
            new[] { "participantIndex", "bone", "posX", "posY", "posZ", "rotX", "rotY", "rotZ" }, _category: "Participant"),

        // Avatar
        new StateListeningAction("Assign avatar to participant",
            "$.worldItemReference('LUIDA-AvatarSpawner').send('luida_assign_avatar', { avatarID: '{_avatarID_}', participantIndex: {_participantIndex_} });",
            new[] { "avatarID", "participantIndex" }, _category: "Avatar"),
        new StateListeningAction("Unassign avatar from participant",
            "$.worldItemReference('LUIDA-AvatarSpawner').send('luida_unassign_avatar', { participantIndex: {_participantIndex_} });",
            new[] { "participantIndex" }, _category: "Avatar"),
    };

    /// <summary>Definition of a ClusterScript event that can be attached as an always-on handler.
    /// jsWrapperFormat must contain literal "{body}" where the indented action code will be substituted.</summary>
    [Serializable]
    public struct EventDefinition
    {
        /// <summary>Serialization key + Add-menu label ("Start", "Update", "$.onCollide", ...).</summary>
        public string eventType;
        /// <summary>Compact label for the in-cell event-handler button (falls back to eventType).</summary>
        public string buttonLabel;
        /// <summary>JS template containing literal "{body}" placeholder.</summary>
        public string jsWrapperFormat;
        /// <summary>Parameter signature shown in the popup header, e.g. "(collision)". Empty for parameterless events.</summary>
        public string parameterSignature;
        /// <summary>Optional Add-menu category. Use "/" for sub-categories.</summary>
        public string category;
        /// <summary>Tooltip / popup HelpBox text.</summary>
        public string description;

        public EventDefinition(string _eventType, string _jsWrapperFormat,
            string _buttonLabel = null, string _parameterSignature = "",
            string _category = null, string _description = null)
        {
            eventType = _eventType;
            jsWrapperFormat = _jsWrapperFormat;
            buttonLabel = _buttonLabel;
            parameterSignature = _parameterSignature ?? "";
            category = _category;
            description = _description;
        }

        public string GetMenuLabel() => eventType;
        public string GetButtonLabel() => string.IsNullOrEmpty(buttonLabel) ? eventType : buttonLabel;
        public string GetMenuPath() => string.IsNullOrEmpty(category) ? GetMenuLabel() : $"{category}/{GetMenuLabel()}";
    }

    public static readonly EventDefinition[] AvailableEventDefinitions =
    {
        // Lifecycle — map to base script's empty Start/Update stubs (StateListeningItemBase.js:239-240).
        // Per-item script is loaded into CSCombiner slot 1 (base in slot 0); user override wins via JS function hoisting.
        new EventDefinition("Start",
            "function Start() {\n{body}\n}",
            _buttonLabel: "Start",
            _category: "Lifecycle",
            _description: "Runs once when the script first loads. Do NOT use $.onStart — reserved by LUIDA."),
        new EventDefinition("Update",
            "function Update(deltaTime) {\n{body}\n}",
            _buttonLabel: "Update",
            _parameterSignature: "(deltaTime)",
            _category: "Lifecycle",
            _description: "Runs every frame. Do NOT use $.onUpdate — reserved by LUIDA."),

        // Interaction
        new EventDefinition("$.onInteract",
            "$.onInteract((player) => {\n{body}\n});",
            _buttonLabel: "On interact",
            _parameterSignature: "(player)",
            _category: "Interaction",
            _description: "Player interacts with this item. Requires a Collider on the item."),
        new EventDefinition("$.onUse",
            "$.onUse((isDown, player) => {\n{body}\n});",
            _buttonLabel: "On use",
            _parameterSignature: "(isDown, player)",
            _category: "Interaction",
            _description: "Player presses/releases the use button while grabbing this item. Requires GrabbableItem."),
        new EventDefinition("$.onGrab",
            "$.onGrab((isGrab, isLeftHand, player) => {\n{body}\n});",
            _buttonLabel: "On grab",
            _parameterSignature: "(isGrab, isLeftHand, player)",
            _category: "Interaction",
            _description: "Player grabs/releases this item. Requires GrabbableItem."),

        // Physics
        new EventDefinition("$.onCollide",
            "$.onCollide((collision) => {\n{body}\n});",
            _buttonLabel: "On collide",
            _parameterSignature: "(collision)",
            _category: "Physics",
            _description: "This item collides with another. Requires a physics body."),
        new EventDefinition("$.onPhysicsUpdate",
            "$.onPhysicsUpdate((deltaTime) => {\n{body}\n});",
            _buttonLabel: "On physics update",
            _parameterSignature: "(deltaTime)",
            _category: "Physics",
            _description: "Fixed-rate physics tick. Requires a physics body."),

        // Vehicle
        new EventDefinition("$.onRide",
            "$.onRide((isGetOn, player) => {\n{body}\n});",
            _buttonLabel: "On ride",
            _parameterSignature: "(isGetOn, player)",
            _category: "Vehicle",
            _description: "Player gets on/off this item. Requires RidableItem."),
        new EventDefinition("$.onSteer",
            "$.onSteer((input, player) => {\n{body}\n});",
            _buttonLabel: "On steer",
            _parameterSignature: "(input, player)",
            _category: "Vehicle",
            _description: "Player steers this item (Vector2 input). Requires RidableItem."),
        new EventDefinition("$.onSteerAdditionalAxis",
            "$.onSteerAdditionalAxis((input, player) => {\n{body}\n});",
            _buttonLabel: "On steer (axis 2)",
            _parameterSignature: "(input, player)",
            _category: "Vehicle",
            _description: "Secondary steering axis. Requires RidableItem."),

        // Input
        new EventDefinition("$.onTextInput",
            "$.onTextInput((text, meta, status) => {\n{body}\n});",
            _buttonLabel: "On text input",
            _parameterSignature: "(text, meta, status)",
            _category: "Input",
            _description: "Response from PARTICIPANTS[n].requestTextInput()."),
    };

    internal static bool TryGetEventDefinition(string eventType, out EventDefinition def)
    {
        if (!string.IsNullOrEmpty(eventType))
        {
            for (int i = 0; i < AvailableEventDefinitions.Length; i++)
            {
                if (AvailableEventDefinitions[i].eventType == eventType)
                {
                    def = AvailableEventDefinitions[i];
                    return true;
                }
            }
        }
        def = default;
        return false;
    }

    private static GUIStyle _codeTextAreaStyle;

    private static readonly Color[] ItemColumnAccents = new[]
    {
        new Color(0.35f, 0.60f, 0.90f), // blue
        new Color(0.40f, 0.78f, 0.50f), // green
        new Color(0.95f, 0.65f, 0.35f), // orange
        new Color(0.75f, 0.50f, 0.90f), // purple
        new Color(0.40f, 0.82f, 0.82f), // teal
        new Color(0.95f, 0.55f, 0.72f), // pink
        new Color(0.90f, 0.80f, 0.35f), // yellow
        new Color(0.60f, 0.80f, 0.95f), // sky
    };

    private static Color GetItemColumnAccent(int columnIndex)
    {
        return ItemColumnAccents[((columnIndex % ItemColumnAccents.Length) + ItemColumnAccents.Length) % ItemColumnAccents.Length];
    }

    private static Color GetRowStripeColor(int rowIndex)
    {
        return rowIndex % 2 == 0
            ? new Color(0.40f, 0.55f, 0.75f, 0.18f)  // cool stripe
            : new Color(0.55f, 0.55f, 0.58f, 0.10f); // warm stripe
    }

    private static Color GetCellTint(int columnIndex, int rowIndex)
    {
        Color accent = GetItemColumnAccent(columnIndex);
        float alpha = rowIndex % 2 == 0 ? 0.22f : 0.14f;
        return new Color(accent.r, accent.g, accent.b, alpha);
    }

    private static ItemsManagerConfigTab.ListenerDragPayload _pendingListenerPayload;

    private static Texture _dupIconTex;
    private static bool _dupIconResolved;
    private static GUIStyle _dragLabelStyle;

    private static GUIStyle GetDragLabelStyle()
    {
        if (_dragLabelStyle == null)
        {
            _dragLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            Color c = EditorGUIUtility.isProSkin
                ? new Color(0.92f, 0.92f, 0.92f)
                : new Color(0.20f, 0.20f, 0.20f);
            _dragLabelStyle.normal.textColor = c;
            _dragLabelStyle.hover.textColor = c;
        }
        return _dragLabelStyle;
    }

    private static GUIContent DupButtonContent(string tooltip)
    {
        if (!_dupIconResolved)
        {
            _dupIconResolved = true;
            string[] candidates = { "TreeEditor.Duplicate", "d_TreeEditor.Duplicate", "Clipboard", "d_Clipboard" };
            foreach (var name in candidates)
            {
                GUIContent content = null;
                try { content = EditorGUIUtility.IconContent(name); } catch { }
                if (content != null && content.image != null)
                {
                    _dupIconTex = content.image;
                    break;
                }
            }
        }
        return _dupIconTex != null
            ? new GUIContent(_dupIconTex, tooltip)
            : new GUIContent("+", tooltip);
    }

    // ─── "Push data to collector" action helpers ────────────────────────
    // The action snippet is:
    //   collectedData['{_label_}'] = {_value_};
    // We render label as a registry-backed dropdown and value as a type-aware
    // editor whose output is assembled into variableValues["value"] as the JS
    // expression to substitute. Vector components and the string buffer are
    // stashed in auxiliary keys (value_x/y/z, value_str) so the per-component
    // UI state survives repaints without round-tripping through the JS text.

    private const string NumberPattern = @"-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?";
    private static readonly Regex Vector2Regex = new Regex(
        @"^\s*new\s+Vector2\s*\(\s*(" + NumberPattern + @")\s*,\s*(" + NumberPattern + @")\s*\)\s*$", RegexOptions.Compiled);
    private static readonly Regex Vector3Regex = new Regex(
        @"^\s*new\s+Vector3\s*\(\s*(" + NumberPattern + @")\s*,\s*(" + NumberPattern + @")\s*,\s*(" + NumberPattern + @")\s*\)\s*$", RegexOptions.Compiled);

    /// <summary>Looks up the CollectedValueType for the picked label. Returns null when label is empty or not registered.</summary>
    private static CollectedValueType? ResolvePushCollectorType(string label, LuidaDataCollectorConfig config)
    {
        if (string.IsNullOrEmpty(label) || config == null) return null;
        var entry = config.FindCollectedLabel(label);
        return entry?.type;
    }

    /// <summary>JS expression used as the initial value when the picked label's type changes.</summary>
    private static string DefaultValueJsFor(CollectedValueType type)
    {
        switch (type)
        {
            case CollectedValueType.Bool:    return "false";
            case CollectedValueType.Float:   return "0";
            case CollectedValueType.Integer: return "0";
            case CollectedValueType.Vector2: return "new Vector2(0, 0)";
            case CollectedValueType.Vector3: return "new Vector3(0, 0, 0)";
            case CollectedValueType.String:  return "''";
        }
        return "";
    }

    /// <summary>Best-effort: strip a single matched pair of surrounding ' or " quotes from a JS string literal.</summary>
    private static string StripStringQuotes(string js)
    {
        if (string.IsNullOrEmpty(js)) return string.Empty;
        string s = js.Trim();
        if (s.Length >= 2 && (s[0] == '\'' || s[0] == '"') && s[s.Length - 1] == s[0])
            return UnescapeJsString(s.Substring(1, s.Length - 2));
        return js;
    }

    private static string UnescapeJsString(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                char n = s[i + 1];
                switch (n)
                {
                    case '\\': sb.Append('\\'); i++; break;
                    case '\'': sb.Append('\''); i++; break;
                    case '"':  sb.Append('"');  i++; break;
                    case 'n':  sb.Append('\n'); i++; break;
                    case 'r':  sb.Append('\r'); i++; break;
                    case 't':  sb.Append('\t'); i++; break;
                    default:   sb.Append(s[i]); break;
                }
            }
            else sb.Append(s[i]);
        }
        return sb.ToString();
    }

    private static string EscapeJsStringLiteral(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\'': sb.Append("\\'"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:   sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    /// <summary>Compact ⚙ button that opens the LUIDA Data Collector window.</summary>
    private static void DrawCollectorConfigGearButton(Rect rect)
    {
        var btn = EditorGUIUtility.IconContent("d_Settings@2x");
        if (btn == null || btn.image == null) btn = new GUIContent("⚙");
        btn.tooltip = "Open the LUIDA Data Collector configuration window";
        if (GUI.Button(rect, btn, EditorStyles.miniButton))
        {
            DataCollectorConfigTab.ShowWindow();
        }
    }

    /// <summary>
    /// Height of the "Push data to collector" action body (excluding the action
    /// row + conditional rows shared with other actions). Mirrors DrawPushDataToCollectorAction.
    /// </summary>
    private static float GetPushDataToCollectorActionHeight(StateListenerAction action, float lineHeight, float spacing)
    {
        var config = DataCollectorGimmickShared.FindBuilderConfig();
        string[] registered = config?.GetCollectedLabelNames(false) ?? new string[0];

        if (registered.Length == 0)
        {
            // helpbox (2 lines + spacing) + button (1 line + spacing)
            return lineHeight * 3 + spacing * 2;
        }

        action.variableValues.TryGetValue("label", out string currentLabel);
        currentLabel ??= "";

        float h = lineHeight + spacing; // label dropdown row

        var resolvedType = ResolvePushCollectorType(currentLabel, config);
        if (!resolvedType.HasValue)
        {
            h += lineHeight + spacing; // value (raw JS) row
            if (!string.IsNullOrEmpty(currentLabel))
                h += lineHeight + spacing; // stale-label warning
            return h;
        }

        switch (resolvedType.Value)
        {
            case CollectedValueType.Vector2:
                h += (lineHeight + spacing) * 2; // "Value" label + x/y inline
                break;
            case CollectedValueType.Vector3:
                h += (lineHeight + spacing) * 2; // "Value" label + x/y/z inline
                break;
            case CollectedValueType.String:
                h += lineHeight + spacing; // single value row
                break;
            default:
                h += lineHeight + spacing; // single-row editor
                break;
        }
        return h;
    }

    /// <summary>
    /// Draws the body of the "Push data to collector" action: a registry-backed
    /// label dropdown (or empty-state CTA when no labels exist) and a type-aware
    /// value editor whose output is assembled into variableValues["value"] as the
    /// JS expression substituted into the action snippet.
    /// </summary>
    private static void DrawPushDataToCollectorAction(
        Rect rect, ref float currentY, float lineHeight, float spacing,
        StateListenerAction action, StateListeningItemData itemDataAsset)
    {
        float labelColumnWidth = 85f;
        var config = DataCollectorGimmickShared.FindBuilderConfig();
        string[] registered = config?.GetCollectedLabelNames(false) ?? new string[0];

        if (registered.Length == 0)
        {
            Rect helpRect = new Rect(rect.x + 15, currentY, rect.width - 30, lineHeight * 2);
            EditorGUI.HelpBox(helpRect,
                "No collected-data items defined yet. Add one in the Data Collector configuration window first.",
                MessageType.Info);
            currentY += lineHeight * 2 + spacing;

            Rect btnRect = new Rect(rect.x + 15, currentY, rect.width - 30, lineHeight);
            if (GUI.Button(btnRect, new GUIContent("Add data to collect…", "Open the LUIDA Data Collector configuration window")))
            {
                DataCollectorConfigTab.ShowWindow();
            }
            currentY += lineHeight + spacing;
            return;
        }

        action.variableValues.TryGetValue("label", out string currentLabel);
        currentLabel ??= "";

        // ─── Row 1: Label dropdown + ⚙ ─────────────────────────────────
        Rect labelLabelRect = new Rect(rect.x + 15, currentY, labelColumnWidth, lineHeight);
        EditorGUI.LabelField(labelLabelRect, "Label");

        const float gearWidth = 26f, gearGap = 4f;
        Rect labelDropdownRect = new Rect(
            labelLabelRect.xMax, currentY,
            rect.width - labelColumnWidth - 15 - gearWidth - gearGap, lineHeight);
        Rect gearRect = new Rect(labelDropdownRect.xMax + gearGap, currentY, gearWidth, lineHeight);

        var displayList = new List<string>(registered);
        int selectedIdx = displayList.IndexOf(currentLabel);
        bool stale = selectedIdx < 0 && !string.IsNullOrEmpty(currentLabel);
        if (stale)
        {
            displayList.Insert(0, currentLabel + "  (not registered)");
            selectedIdx = 0;
        }
        int newIdx = EditorGUI.Popup(labelDropdownRect, selectedIdx, displayList.ToArray());
        DrawCollectorConfigGearButton(gearRect);

        if (newIdx >= 0)
        {
            string newLabel;
            if (stale && newIdx == 0) newLabel = currentLabel; // kept stale entry
            else
            {
                int registeredIdx = stale ? newIdx - 1 : newIdx;
                newLabel = (registeredIdx >= 0 && registeredIdx < registered.Length) ? registered[registeredIdx] : currentLabel;
            }
            if (newLabel != currentLabel)
            {
                Undo.RecordObject(itemDataAsset, "Change Collector Label");
                action.variableValues["label"] = newLabel;
                // Reset value-related state when switching to a different label —
                // type may have changed, so old per-component aux state is invalid.
                var newType = ResolvePushCollectorType(newLabel, config);
                action.variableValues["value"] = newType.HasValue ? DefaultValueJsFor(newType.Value) : "";
                action.variableValues.Remove("value_x");
                action.variableValues.Remove("value_y");
                action.variableValues.Remove("value_z");
                action.variableValues.Remove("value_str");
                EditorUtility.SetDirty(itemDataAsset);
                currentLabel = newLabel;
            }
        }
        currentY += lineHeight + spacing;

        // ─── Row 2+: Typed value editor ────────────────────────────────
        var resolvedType = ResolvePushCollectorType(currentLabel, config);
        if (!resolvedType.HasValue)
        {
            // Stale or empty label — fall back to a raw JS text field so the
            // generated snippet still has something sane.
            Rect rawLabelRect = new Rect(rect.x + 15, currentY, labelColumnWidth, lineHeight);
            Rect rawFieldRect = new Rect(rawLabelRect.xMax, currentY, rect.width - labelColumnWidth - 30, lineHeight);
            EditorGUI.LabelField(rawLabelRect, "Value (JS)");
            action.variableValues.TryGetValue("value", out string rawCurrent);
            string rawNew = EditorGUI.TextField(rawFieldRect, rawCurrent ?? "");
            if (rawNew != rawCurrent)
            {
                Undo.RecordObject(itemDataAsset, "Edit Collector Value");
                action.variableValues["value"] = rawNew;
                EditorUtility.SetDirty(itemDataAsset);
            }
            currentY += lineHeight + spacing;

            if (!string.IsNullOrEmpty(currentLabel))
            {
                Rect noteRect = new Rect(rect.x + 15, currentY, rect.width - 30, lineHeight);
                EditorGUI.HelpBox(noteRect,
                    $"Label '{currentLabel}' is not registered. Pick one above or add it in the Data Collector config.",
                    MessageType.Warning);
                currentY += lineHeight + spacing;
            }
            return;
        }

        DrawPushCollectorTypedValueEditor(rect, ref currentY, lineHeight, spacing,
            action, itemDataAsset, resolvedType.Value, labelColumnWidth);
    }

    /// <summary>
    /// Typed value editor switched by the registered label's CollectedValueType.
    /// Writes the JS expression to variableValues["value"] and keeps per-component
    /// aux state in value_x / value_y / value_z / value_str so the per-field UI
    /// survives repaints without parsing the assembled JS each frame.
    /// </summary>
    private static void DrawPushCollectorTypedValueEditor(
        Rect rect, ref float currentY, float lineHeight, float spacing,
        StateListenerAction action, StateListeningItemData itemDataAsset,
        CollectedValueType type, float labelColumnWidth)
    {
        action.variableValues.TryGetValue("value", out string currentJs);
        currentJs ??= "";

        switch (type)
        {
            case CollectedValueType.Bool:
            {
                // Normalize stale free-form text to a clean "true"/"false" on first draw.
                string trimmed = currentJs.Trim();
                bool isCleanBool = trimmed == "true" || trimmed == "false";
                if (!isCleanBool)
                {
                    bool inferred = trimmed.Equals("true", StringComparison.OrdinalIgnoreCase);
                    action.variableValues["value"] = inferred ? "true" : "false";
                    EditorUtility.SetDirty(itemDataAsset);
                    currentJs = action.variableValues["value"];
                }

                Rect lblRect = new Rect(rect.x + 15, currentY, labelColumnWidth, lineHeight);
                Rect fieldRect = new Rect(lblRect.xMax, currentY, rect.width - labelColumnWidth - 30, lineHeight);
                EditorGUI.LabelField(lblRect, "Value");
                bool current = currentJs == "true";
                bool picked = EditorGUI.Toggle(fieldRect, current);
                if (picked != current)
                {
                    Undo.RecordObject(itemDataAsset, "Edit Collector Value");
                    action.variableValues["value"] = picked ? "true" : "false";
                    EditorUtility.SetDirty(itemDataAsset);
                }
                currentY += lineHeight + spacing;
                break;
            }

            case CollectedValueType.Float:
            case CollectedValueType.Integer:
            {
                if (string.IsNullOrEmpty(currentJs))
                {
                    action.variableValues["value"] = "0";
                    currentJs = "0";
                    EditorUtility.SetDirty(itemDataAsset);
                }

                Rect lblRect = new Rect(rect.x + 15, currentY, labelColumnWidth, lineHeight);
                Rect fieldRect = new Rect(lblRect.xMax, currentY, rect.width - labelColumnWidth - 30, lineHeight);
                EditorGUI.LabelField(lblRect, "Value");
                string newText = EditorGUI.TextField(fieldRect, currentJs);
                if (newText != currentJs)
                {
                    Undo.RecordObject(itemDataAsset, "Edit Collector Value");
                    action.variableValues["value"] = newText;
                    EditorUtility.SetDirty(itemDataAsset);
                }
                currentY += lineHeight + spacing;
                break;
            }

            case CollectedValueType.String:
            {
                // Seed aux + re-emit value as a clean JS string literal on first draw
                // so older free-form values get normalized to 'text' form.
                bool wasSeededThisDraw = !action.variableValues.ContainsKey("value_str");
                string strBuf;
                if (wasSeededThisDraw)
                {
                    strBuf = StripStringQuotes(currentJs);
                    action.variableValues["value_str"] = strBuf;
                    action.variableValues["value"] = "'" + EscapeJsStringLiteral(strBuf) + "'";
                    EditorUtility.SetDirty(itemDataAsset);
                }
                else
                {
                    action.variableValues.TryGetValue("value_str", out strBuf);
                    strBuf ??= string.Empty;
                }

                Rect lblRect = new Rect(rect.x + 15, currentY, labelColumnWidth, lineHeight);
                Rect fieldRect = new Rect(lblRect.xMax, currentY, rect.width - labelColumnWidth - 30, lineHeight);
                EditorGUI.LabelField(lblRect, "Value");
                string newStr = EditorGUI.TextField(fieldRect, strBuf);
                if (newStr != strBuf)
                {
                    Undo.RecordObject(itemDataAsset, "Edit Collector Value");
                    action.variableValues["value_str"] = newStr;
                    action.variableValues["value"] = "'" + EscapeJsStringLiteral(newStr) + "'";
                    EditorUtility.SetDirty(itemDataAsset);
                }
                currentY += lineHeight + spacing;
                break;
            }

            case CollectedValueType.Vector2:
            case CollectedValueType.Vector3:
            {
                int dim = type == CollectedValueType.Vector2 ? 2 : 3;
                string[] aux = { "value_x", "value_y", "value_z" };

                // Seed aux keys from the current JS expression once. Also re-emit
                // the assembled "value" so a pre-migration raw JS body that we
                // couldn't parse doesn't keep producing broken JS.
                bool wasSeededThisDraw = !action.variableValues.ContainsKey(aux[0]);
                if (wasSeededThisDraw)
                {
                    string[] seeded = SeedVectorAuxFromJs(currentJs, dim);
                    for (int i = 0; i < dim; i++) action.variableValues[aux[i]] = seeded[i];
                    action.variableValues["value"] = AssembleVectorJs(action, dim);
                    EditorUtility.SetDirty(itemDataAsset);
                }

                EditorGUI.LabelField(new Rect(rect.x + 15, currentY, rect.width - 30, lineHeight), "Value");
                currentY += lineHeight + spacing;

                float axisLabelW = 14f, axisFieldW = 50f, axisGap = 8f;
                float x = rect.x + 15;
                string[] axes = { "x", "y", "z" };
                bool changed = false;
                for (int i = 0; i < dim; i++)
                {
                    EditorGUI.LabelField(new Rect(x, currentY, axisLabelW, lineHeight), axes[i]);
                    action.variableValues.TryGetValue(aux[i], out string axisCurrent);
                    axisCurrent ??= "0";
                    string axisNew = EditorGUI.TextField(new Rect(x + axisLabelW, currentY, axisFieldW, lineHeight), axisCurrent);
                    if (axisNew != axisCurrent)
                    {
                        Undo.RecordObject(itemDataAsset, "Edit Collector Value");
                        action.variableValues[aux[i]] = axisNew;
                        changed = true;
                    }
                    x += axisLabelW + axisFieldW + axisGap;
                }
                if (changed)
                {
                    action.variableValues["value"] = AssembleVectorJs(action, dim);
                    EditorUtility.SetDirty(itemDataAsset);
                }
                currentY += lineHeight + spacing;
                break;
            }
        }
    }

    /// <summary>Best-effort: extract Vector2/Vector3 components from "new VectorN(x, y[, z])". Falls back to "0" per axis.</summary>
    private static string[] SeedVectorAuxFromJs(string js, int dim)
    {
        var result = new string[] { "0", "0", "0" };
        if (string.IsNullOrEmpty(js)) return result;
        var rx = dim == 2 ? Vector2Regex : Vector3Regex;
        var m = rx.Match(js);
        if (m.Success)
        {
            for (int i = 0; i < dim; i++) result[i] = m.Groups[i + 1].Value;
        }
        return result;
    }

    private static string AssembleVectorJs(StateListenerAction action, int dim)
    {
        action.variableValues.TryGetValue("value_x", out string xv);
        action.variableValues.TryGetValue("value_y", out string yv);
        action.variableValues.TryGetValue("value_z", out string zv);
        xv = string.IsNullOrWhiteSpace(xv) ? "0" : xv.Trim();
        yv = string.IsNullOrWhiteSpace(yv) ? "0" : yv.Trim();
        zv = string.IsNullOrWhiteSpace(zv) ? "0" : zv.Trim();
        return dim == 2
            ? $"new Vector2({xv}, {yv})"
            : $"new Vector3({xv}, {yv}, {zv})";
    }

    public static void DrawGUI(ItemsManagerConfigTab editor)
    {
        if (Event.current.type == EventType.Layout)
        {
            editor._cellRects.Clear();
        }

        TryStartPendingDrag();

        EditorGUI.BeginChangeCheck();

        DrawHeader(editor);

        EditorGUILayout.BeginHorizontal();
        DrawMainGrid(editor);

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical();
        TextAsset markdownAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(docFilePath);
        EditorGUILayout.HelpBox("Script Doc\n↓↓↓↓↓", MessageType.Info);
        GUI.enabled = false;
        EditorGUILayout.ObjectField(markdownAsset, typeof(TextAsset), false, GUILayout.Width(100));
        GUI.enabled = true;
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            // Changes are primarily saved via Undo/SetDirty and explicit save calls.
        }
    }

    private static void DrawHeader(ItemsManagerConfigTab editor)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name", GUILayout.Width(120));
        editor.newItemName = EditorGUILayout.TextField(editor.newItemName, GUILayout.Width(180));

        bool isNameInvalid = string.IsNullOrEmpty(editor.newItemName) || editor.stateListeningItems.Any(i => i != null && i.name == editor.newItemName);
        EditorGUI.BeginDisabledGroup(isNameInvalid);

        if (GUILayout.Button(new GUIContent("+ Add Item", "Create a new item in the scene that can run code during different states."), GUILayout.Width(180)))
        {
            ItemsManagerAssetUtil.CreateStateListeningItem(editor);
            GUIUtility.hotControl = 0; // unfocus text field
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
    }

    private static void DrawMainGrid(ItemsManagerConfigTab editor)
    {
        GUIStyle removeButtonStyle = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.red }, hover = { textColor = Color.red } };

        EditorGUILayout.BeginVertical(GUILayout.MaxWidth(1800f));
        editor._horizontalScrollPosition = EditorGUILayout.BeginScrollView(editor._horizontalScrollPosition, false, false, GUILayout.ExpandWidth(true));

        DrawItemHeaders(editor, removeButtonStyle);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        editor.scrollPositionY = EditorGUILayout.BeginScrollView(editor.scrollPositionY, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.ExpandHeight(true));

        DrawAlwaysOnRow(editor);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        DrawStateRows(editor, removeButtonStyle);

        EditorGUILayout.EndScrollView(); // scrollPositionY
        EditorGUILayout.EndScrollView(); // _horizontalScrollPosition
        EditorGUILayout.EndVertical();
    }

    private static void DrawItemHeaders(ItemsManagerConfigTab editor, GUIStyle removeButtonStyle)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("State ↓  Item →", EditorStyles.boldLabel, GUILayout.Width(215));
        GUILayout.Space(5);

        int columnIndex = 0;
        foreach (var item in editor._cachedItems)
        {
            if (item == null) { columnIndex++; continue; }

            GUI.backgroundColor = GetItemColumnAccent(columnIndex);
            EditorGUILayout.BeginHorizontal("box", GUILayout.Width(240));

            EditorGUILayout.LabelField(item.name, EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(item, typeof(GameObject), true);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(DupButtonContent("Duplicate this item along with all its actions."), GUILayout.Width(25), GUILayout.Height(20)))
            {
                ItemsManagerAssetUtil.DuplicateStateListeningItem(item, editor);
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button(new GUIContent("X", "Delete this item, its script, and its saved actions."), removeButtonStyle, GUILayout.Width(25), GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("Confirm Removal", $"Are you sure you want to remove '{item.name}' and its associated assets (JS script and StateListenerData asset)?", "Yes, Remove", "No"))
                {
                    ItemsManagerAssetUtil.RemoveStateListeningItem(item, editor);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            columnIndex++;
        }
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
    }

    private static void DrawAlwaysOnRow(ItemsManagerConfigTab editor)
    {
        EditorGUILayout.LabelField(
            new GUIContent("Always-on event handlers (run regardless of state)",
                "ClusterScript event handlers attached to each item. Click an event to edit its actions."),
            EditorStyles.largeLabel);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(215));
        EditorGUILayout.HelpBox(
            "Click an event to edit its actions. Use \"+ Add event\" to register a new handler. " +
            "$.onStart / $.onUpdate / $.onReceive are reserved by LUIDA.",
            MessageType.Info);
        EditorGUILayout.EndVertical();
        GUILayout.Space(5);

        int columnIndex = 0;
        foreach (var item in editor._cachedItems)
        {
            if (item == null) { columnIndex++; continue; }
            DrawAlwaysOnCell(editor, item, columnIndex);
            columnIndex++;
        }
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
    }

    private static void DrawAlwaysOnCell(ItemsManagerConfigTab editor, GameObject item, int columnIndex)
    {
        Color accent = GetItemColumnAccent(columnIndex);
        Color cellBgColor = new Color(accent.r, accent.g, accent.b, 0.18f);
        Rect cellRect = EditorGUILayout.BeginVertical("box", GUILayout.Width(238.5f), GUILayout.MinHeight(80));
        EditorGUI.DrawRect(cellRect, cellBgColor);

        string itemDataAssetPath = ItemsManagerAssetUtil.GetItemDataAssetPath(item);
        var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(itemDataAssetPath);
        if (data == null)
        {
            EditorGUILayout.HelpBox($"Asset not found for {item.name}.", MessageType.Error);
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            return;
        }
        // Pre-GUI assets serialize eventHandlers as null until first re-save.
        if (data.eventHandlers == null) data.eventHandlers = new List<EventHandlerData>();

        for (int i = 0; i < data.eventHandlers.Count; i++)
        {
            DrawEventHandlerButton(editor, item, data, i);
        }

        GUILayout.Space(2);
        Rect addRect = GUILayoutUtility.GetRect(
            new GUIContent("+ Add event ▾"), GUI.skin.button,
            GUILayout.Height(20), GUILayout.Width(232));
        if (GUI.Button(addRect, new GUIContent("+ Add event ▾",
                                              "Register a new ClusterScript event handler.")))
        {
            ShowAddEventDropdown(editor, item, data, addRect);
        }

        DrawLegacyImplementationFoldout(editor, item, data);

        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
    }

    private static void DrawEventHandlerButton(
        ItemsManagerConfigTab editor, GameObject item,
        StateListeningItemData data, int handlerIndex)
    {
        var handler = data.eventHandlers[handlerIndex];
        EditorGUILayout.BeginHorizontal();

        int actionCount = handler.actions?.Count ?? 0;
        TryGetEventDefinition(handler.eventType, out var def);
        string displayLabel = string.IsNullOrEmpty(handler.eventType)
            ? "(unset event)"
            : (string.IsNullOrEmpty(def.eventType) ? handler.eventType : def.GetButtonLabel());
        string badge = actionCount == 0 ? " (no actions)" : $" ({actionCount} action{(actionCount > 1 ? "s" : "")})";
        var content = new GUIContent(displayLabel + badge,
            string.IsNullOrEmpty(def.description)
                ? $"Click to edit actions for the {handler.eventType} handler."
                : def.description);

        if (GUILayout.Button(content, GUILayout.Height(22), GUILayout.ExpandWidth(true)))
        {
            EventHandlerActionsWindow.Show(data, handlerIndex, item, editor);
        }

        var removeStyle = new GUIStyle(GUI.skin.button)
        {
            normal = { textColor = Color.red },
            hover = { textColor = Color.red },
            fontStyle = FontStyle.Bold
        };
        if (GUILayout.Button(new GUIContent("×", "Remove this event handler."),
                             removeStyle, GUILayout.Width(22), GUILayout.Height(22)))
        {
            if (EditorUtility.DisplayDialog(
                "Remove event handler",
                $"Remove the '{displayLabel}' event handler from '{item.name}'?\nIts actions will be lost.",
                "Remove", "Cancel"))
            {
                Undo.RecordObject(data, "Remove event handler");
                data.eventHandlers.RemoveAt(handlerIndex);
                EditorUtility.SetDirty(data);
                EditorGUILayout.EndHorizontal();
                GUIUtility.ExitGUI();
                return;
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private static void ShowAddEventDropdown(
        ItemsManagerConfigTab editor, GameObject item,
        StateListeningItemData data, Rect anchor)
    {
        if (data.eventHandlers == null) data.eventHandlers = new List<EventHandlerData>();
        var usedTypes = new HashSet<string>(
            data.eventHandlers.Where(h => h != null && !string.IsNullOrEmpty(h.eventType))
                              .Select(h => h.eventType));

        var menu = new GenericMenu();
        foreach (var def in AvailableEventDefinitions)
        {
            var label = new GUIContent(def.GetMenuPath());
            if (usedTypes.Contains(def.eventType))
            {
                menu.AddDisabledItem(label, true);
            }
            else
            {
                var captured = def;
                var capturedData = data;
                var capturedEditor = editor;
                menu.AddItem(label, false, () =>
                {
                    Undo.RecordObject(capturedData, "Add event handler");
                    capturedData.eventHandlers.Add(new EventHandlerData(captured.eventType));
                    EditorUtility.SetDirty(capturedData);
                    if (capturedEditor != null) capturedEditor.Repaint();
                });
            }
        }
        menu.DropDown(anchor);
    }

    private static void DrawLegacyImplementationFoldout(
        ItemsManagerConfigTab editor, GameObject item, StateListeningItemData data)
    {
        string raw = data.otherImplementation ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return;
        if (ItemsManagerAssetUtil.IsLegacyBoilerplate(raw)) return;

        string sessionKey = $"luida_legacy_{item.GetInstanceID()}";
        bool wasOpen = SessionState.GetBool(sessionKey, false);

        GUILayout.Space(4);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(
            new GUIContent("⚠ Legacy free-form code",
                "Pre-existing code from before the structured GUI. Appended verbatim to the generated .js. " +
                "Migrate to structured event handlers above when possible, then click 'Clear legacy code'."),
            EditorStyles.miniBoldLabel);

        bool nowOpen = EditorGUILayout.Foldout(wasOpen, $"({raw.Length} chars)", true);
        if (nowOpen != wasOpen) SessionState.SetBool(sessionKey, nowOpen);

        if (nowOpen)
        {
            Rect ta = EditorGUILayout.GetControlRect(false, 75, GUILayout.Width(225f), GUILayout.MaxHeight(75));
            DrawHoverableTextArea(ta, raw, newValue =>
            {
                Undo.RecordObject(data, "Edit legacy free-form code for " + item.name);
                data.otherImplementation = newValue;
                EditorUtility.SetDirty(data);
            }, editor, isColored: true);

            if (GUILayout.Button(new GUIContent("Clear legacy code",
                "Remove the legacy free-form code once you've migrated it into structured event handlers.")))
            {
                if (EditorUtility.DisplayDialog(
                    "Clear legacy code",
                    $"Permanently remove the legacy free-form code from '{item.name}'?",
                    "Clear", "Cancel"))
                {
                    Undo.RecordObject(data, "Clear legacy free-form code");
                    data.otherImplementation = string.Empty;
                    EditorUtility.SetDirty(data);
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private static void DrawStateRows(ItemsManagerConfigTab editor, GUIStyle removeButtonStyle)
    {
        EditorGUILayout.LabelField("Actions per state", EditorStyles.largeLabel, GUILayout.Width(300));

        GUI.backgroundColor = Color.white;

        int rowIndex = 0;
        foreach (var stateName in editor._cachedStateNames)
        {
            int stateID = Array.IndexOf(editor._cachedStateNames, stateName);
            Color rowBgColor = GetRowStripeColor(rowIndex);

            Rect rowRect = EditorGUILayout.BeginHorizontal("box");
            EditorGUI.DrawRect(rowRect, rowBgColor);

            EditorGUILayout.LabelField(stateName, EditorStyles.boldLabel, GUILayout.Width(200), GUILayout.ExpandHeight(true));
            GUILayout.Space(15);

            int columnIndex = 0;
            foreach (var item in editor._cachedItems)
            {
                if (item == null) { columnIndex++; continue; }
                DrawCell(editor, item, stateName, stateID, columnIndex, rowIndex, removeButtonStyle);
                columnIndex++;
            }

            EditorGUILayout.EndHorizontal();
            rowIndex++;
        }
        GUI.backgroundColor = Color.white;
    }

    private static void DrawCell(ItemsManagerConfigTab editor, GameObject item, string stateName, int stateID, int columnIndex, int rowIndex, GUIStyle removeButtonStyle)
    {
        Color cellBgColor = GetCellTint(columnIndex, rowIndex);
        Rect cellRectInner = EditorGUILayout.BeginVertical("box", GUILayout.Width(240), GUILayout.MinHeight(20));
        EditorGUI.DrawRect(cellRectInner, cellBgColor);

        editor.stateListenersByItem.TryGetValue(item, out var listenersList);
        var listener = listenersList?.FirstOrDefault(l => l.stateID == stateID);

        if (listener != null)
        {
            string itemDataAssetPath = ItemsManagerAssetUtil.GetItemDataAssetPath(item);
            var itemDataAsset = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(itemDataAssetPath);

            DrawReorderableList(editor, item, stateID, "OnStateStart");
            GUILayout.Space(5);
            DrawReorderableList(editor, item, stateID, "DuringState");
            GUILayout.Space(5);
            DrawReorderableList(editor, item, stateID, "OnStateExit");
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            Rect stripRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.centeredGreyMiniLabel, GUILayout.Height(20), GUILayout.ExpandWidth(true));
            DrawListenerDragHandle(stripRect, new ItemsManagerConfigTab.ListenerDragPayload
            {
                sourceAsset = itemDataAsset,
                sourceItem = item,
                sourceStateID = stateID,
                sourceListener = listener,
            });

            var dupContent = new GUIContent("Duplicate ▾", "Duplicate these actions to another state on this item.");
            Rect dupButtonRect = GUILayoutUtility.GetRect(dupContent, GUI.skin.button, GUILayout.Height(20), GUILayout.Width(80));
            if (GUI.Button(dupButtonRect, dupContent))
            {
                ShowListenerDuplicateDropdown(item, stateID, editor, dupButtonRect);
            }
            if (GUILayout.Button(new GUIContent("Clear", "Remove all actions for this item in this state."), removeButtonStyle, GUILayout.Height(20), GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("Confirm Clear", $"Are you sure you want to clear all actions for state '{stateName}' on item '{item.name}'?", "Yes, Clear", "No"))
                {
                    ItemsManagerAssetUtil.RemoveStateListener(item, stateID, editor);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button(new GUIContent("Use this state", "Add actions that will run during this state."), GUILayout.Height(20)))
            {
                ItemsManagerAssetUtil.AddStateListener(item, stateID, editor);
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndVertical();

        if (Event.current.type == EventType.Repaint)
            editor._cellRects[(item, stateID)] = cellRectInner;

        HandleListenerDrop(cellRectInner, editor, item, stateID, listener != null);

        GUILayout.Space(10);
    }
    
    #region ReorderableList UI
    
    public static void SetupReorderableLists(ItemsManagerConfigTab editor)
    {
        editor._reorderableLists.Clear();
        foreach (var item in editor.stateListeningItems)
        {
            if (item == null || !editor.stateListenersByItem.TryGetValue(item, out var listeners)) continue;

            string itemDataAssetPath = ItemsManagerAssetUtil.GetItemDataAssetPath(item);
            StateListeningItemData itemDataAsset = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(itemDataAssetPath);
            if (itemDataAsset == null) continue;

            foreach (var listener in listeners)
            {
                CreateReorderableList(editor, item, itemDataAsset, listener, listener.onStateStartedActions, "When entering", "OnStateStart");
                CreateReorderableList(editor, item, itemDataAsset, listener, listener.duringStateActions, "While in state", "DuringState");
                CreateReorderableList(editor, item, itemDataAsset, listener, listener.onStateExitedActions, "When leaving", "OnStateExit");
            }
        }
    }

    /// <summary>Seed text for a freshly created Customized Action. When the action lives inside an event handler,
    /// the seed leads with a comment listing that event's callback parameters so the user can discover what's available.</summary>
    private static string BuildCustomizedActionSeed(string eventType)
    {
        const string defaultBody = "// Your custom ClusterScript code here\n";
        if (string.IsNullOrEmpty(eventType)) return defaultBody;
        if (!TryGetEventDefinition(eventType, out var def)) return defaultBody;

        string sig = def.parameterSignature;
        if (string.IsNullOrEmpty(sig)) return defaultBody;
        string trimmed = sig.Trim();
        if (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
            trimmed = trimmed.Substring(1, trimmed.Length - 2);
        trimmed = trimmed.Trim();
        if (string.IsNullOrEmpty(trimmed)) return defaultBody;

        return $"// Available parameter(s) from {eventType}: {trimmed}\n" + defaultBody;
    }

    /// <summary>
    /// Apply the user's action-type selection to a single StateListenerAction. Called from the
    /// GenericMenu callback that backs the action-type dropdown — extracted from the inline path
    /// so it can run asynchronously after the menu closes.
    /// </summary>
    /// <param name="newIndex">0 = "Select action" (clear); customizedActionIndex = "Customized Action"; otherwise 1-based index into AvailableStateListeningActions.</param>
    /// <param name="eventType">When set, indicates the action lives inside the corresponding always-on event handler.
    /// Used to seed the Customized Action body with a comment listing that event's callback parameters.</param>
    private static void ApplyActionSelection(StateListeningItemData itemDataAsset, StateListenerAction action, int newIndex, int customizedActionIndex, GameObject itemGO, ItemsManagerConfigTab editor, string eventType = null)
    {
        Undo.RecordObject(itemDataAsset, "Change Action Type");
        if (newIndex == 0)
        {
            action.predefinedActionTemplate = default;
            action.customAction = "";
            action.variableValues.Clear();
        }
        else if (newIndex == customizedActionIndex)
        {
            action.predefinedActionTemplate = new StateListeningAction("Customized Action", "", null);
            action.customAction = BuildCustomizedActionSeed(eventType);
            action.variableValues.Clear();
        }
        else
        {
            action.predefinedActionTemplate = AvailableStateListeningActions[newIndex - 1];
            action.customAction = "";
            action.variableValues.Clear();
            if (action.predefinedActionTemplate.variables != null)
            {
                foreach (var varName in action.predefinedActionTemplate.variables)
                {
                    if (action.predefinedActionTemplate.actionType == "Send Haptics" && varName == "target")
                    {
                        action.variableValues[varName] = "''";
                    }
                    else
                    {
                        action.variableValues[varName] = new StateListenerAction(action.predefinedActionTemplate).variableValues[varName];
                    }
                }
            }

            if (action.predefinedActionTemplate.actionType == "Set text")
            {
                var textChild = itemGO.transform.Find("Text");
                if (textChild == null)
                {
                    var newTextGO = new GameObject("Text");
                    newTextGO.transform.SetParent(itemGO.transform, false);
                    newTextGO.AddComponent<TextView>();
                    Undo.RegisterCreatedObjectUndo(newTextGO, "Create Text child with TextView");
                }
                else if (textChild.GetComponent<TextView>() == null)
                {
                    Undo.AddComponent<TextView>(textChild.gameObject);
                }
            }
        }
        EditorUtility.SetDirty(itemDataAsset);
        if (editor != null) editor._needsRebuild = true;
    }

    /// <summary>Returns the always-on event-handler ReorderableList for use inside EventHandlerActionsWindow.
    /// Filters Sleep out of the action menu (Sleep emits a bare numeric value that only the per-state action loop
    /// in StateListeningItemBase.js interprets), and allows the "If" conditional toggle regardless of state context.</summary>
    internal static ReorderableList CreateEventHandlerReorderableList(
        ItemsManagerConfigTab editor, GameObject itemGO,
        StateListeningItemData itemDataAsset, EventHandlerData handler)
    {
        if (handler.actions == null) handler.actions = new List<StateListenerAction>();
        return CreateReorderableList(
            editor, itemGO, itemDataAsset, listener: null, actions: handler.actions,
            header: "Actions", keySuffix: null,
            allowConditionalUnconditionally: true,
            excludeActionTypes: new HashSet<string> { "Sleep" },
            eventType: handler.eventType);
    }

    /// <summary>Builds the StateListenerAction ReorderableList. Two call paths:
    /// (1) per-state cells pass a listener + keySuffix and register the list in editor._reorderableLists for redraw caching;
    /// (2) the always-on event-handler popup passes listener=null/keySuffix=null and gets the list back without cache pollution.</summary>
    private static ReorderableList CreateReorderableList(
        ItemsManagerConfigTab editor, GameObject itemGO,
        StateListeningItemData itemDataAsset, StateListener listener,
        List<StateListenerAction> actions, string header, string keySuffix,
        bool allowConditionalUnconditionally = false,
        HashSet<string> excludeActionTypes = null,
        string eventType = null)
    {
        string key = (listener != null && keySuffix != null) ? $"{itemGO.GetInstanceID()}_{listener.stateID}_{keySuffix}" : null;
        bool isCurrentStateTrialRelated = listener != null && ItemsManagerAssetUtil.IsTrialRelatedState(listener.stateID, editor.stateList);
        bool isConditionalAllowed = isCurrentStateTrialRelated || allowConditionalUnconditionally;

        var rl = new ReorderableList(actions, typeof(StateListenerAction), true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, header, EditorStyles.boldLabel),
            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (actions == null || index < 0 || index >= actions.Count) return;
                var action = actions[index];

                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float currentY = rect.y + spacing / 2;
                float currentX = rect.x;
                float availableWidth = rect.width;

                float ifButtonAndSpacingWidth = isConditionalAllowed ? 35 + spacing : 0;
                float dupButtonAndSpacingWidth = 22 + spacing;
                float dropdownWidth = availableWidth - ifButtonAndSpacingWidth - dupButtonAndSpacingWidth;
                Rect dropdownRect = new Rect(currentX, currentY, dropdownWidth, lineHeight);

                // Selection key is actionType; display label may differ. Categories nest under slash-separated paths.
                int customizedActionIndex = AvailableStateListeningActions.Length + 1;
                int selectedIndex = 0;
                if (!string.IsNullOrEmpty(action.predefinedActionTemplate.actionType))
                {
                    if (action.predefinedActionTemplate.actionType == "Customized Action")
                    {
                        selectedIndex = customizedActionIndex;
                    }
                    else
                    {
                        int matchIdx = Array.FindIndex(AvailableStateListeningActions, a => a.actionType == action.predefinedActionTemplate.actionType);
                        selectedIndex = matchIdx >= 0 ? matchIdx + 1 : 0;
                    }
                }

                string buttonLabel = selectedIndex == 0
                    ? "Select action"
                    : selectedIndex == customizedActionIndex
                        ? "Customized Action"
                        : AvailableStateListeningActions[selectedIndex - 1].GetDisplayLabel();

                if (GUI.Button(dropdownRect, new GUIContent(buttonLabel), EditorStyles.popup))
                {
                    var capturedAction = action;
                    var capturedItemDataAsset = itemDataAsset;
                    var capturedItemGO = itemGO;
                    var capturedEditor = editor;
                    int capturedSelectedIndex = selectedIndex;
                    int capturedCustomizedIndex = customizedActionIndex;
                    string capturedEventType = eventType;

                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Select action"), capturedSelectedIndex == 0, () =>
                    {
                        ApplyActionSelection(capturedItemDataAsset, capturedAction, 0, capturedCustomizedIndex, capturedItemGO, capturedEditor, capturedEventType);
                    });
                    menu.AddSeparator("");
                    for (int i = 0; i < AvailableStateListeningActions.Length; i++)
                    {
                        int captured = i + 1;
                        var a = AvailableStateListeningActions[i];
                        if (excludeActionTypes != null && excludeActionTypes.Contains(a.actionType)) continue;
                        menu.AddItem(new GUIContent(a.GetMenuPath()), capturedSelectedIndex == captured, () =>
                        {
                            ApplyActionSelection(capturedItemDataAsset, capturedAction, captured, capturedCustomizedIndex, capturedItemGO, capturedEditor, capturedEventType);
                        });
                    }
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Customized Action"), capturedSelectedIndex == capturedCustomizedIndex, () =>
                    {
                        ApplyActionSelection(capturedItemDataAsset, capturedAction, capturedCustomizedIndex, capturedCustomizedIndex, capturedItemGO, capturedEditor, capturedEventType);
                    });
                    menu.DropDown(dropdownRect);
                }
                currentX += dropdownWidth + spacing;

                Rect dupRect = new Rect(currentX, currentY, 22, lineHeight);
                if (GUI.Button(dupRect, DupButtonContent("Duplicate this action below this one.")))
                {
                    ItemsManagerAssetUtil.DuplicateAction(itemDataAsset, actions, index);
                    editor._needsRebuild = true;
                    GUIUtility.ExitGUI();
                }
                currentX += 22 + spacing;

                if (isConditionalAllowed)
                {
                    Rect ifToggleRect = new Rect(currentX, currentY, 35, lineHeight);
                    bool newIsConditional = GUI.Toggle(ifToggleRect, action.isConditional, "If", GUI.skin.button);
                    if (newIsConditional != action.isConditional)
                    {
                        Undo.RecordObject(itemDataAsset, "Toggle Conditional Action");
                        action.isConditional = newIsConditional;
                        if (!action.isConditional)
                        {
                            action.conditionVariable = null;
                            action.conditionValue = null;
                        }
                        EditorUtility.SetDirty(itemDataAsset);
                    }
                }
                currentY += lineHeight + spacing;

                if (isConditionalAllowed && action.isConditional)
                {
                    Rect varLabelRect = new Rect(rect.x + 15, currentY, EditorGUIUtility.labelWidth * 0.7f, lineHeight);
                    Rect varDropdownRect = new Rect(varLabelRect.xMax, currentY, rect.width - varLabelRect.width - 15, lineHeight);
                    EditorGUI.LabelField(varLabelRect, "Var Name");

                    var conditionVarNames = new List<string> { "[Select Variable]" };
                    if (editor._cachedExperimentVariables != null)
                    {
                        conditionVarNames.AddRange(editor._cachedExperimentVariables.Select(v => v.name).Distinct());
                    }

                    int selectedVarIndex = string.IsNullOrEmpty(action.conditionVariable) ? 0 : conditionVarNames.IndexOf(action.conditionVariable);
                    if (selectedVarIndex == -1) selectedVarIndex = 0;

                    int newSelectedVarIndex = EditorGUI.Popup(varDropdownRect, selectedVarIndex, conditionVarNames.ToArray());
                    currentY += lineHeight + spacing;

                    if (newSelectedVarIndex != selectedVarIndex)
                    {
                        Undo.RecordObject(itemDataAsset, "Change Condition Variable");
                        action.conditionVariable = (newSelectedVarIndex > 0) ? conditionVarNames[newSelectedVarIndex] : null;
                        action.conditionValue = null;
                        EditorUtility.SetDirty(itemDataAsset);
                    }

                    if (!string.IsNullOrEmpty(action.conditionVariable) && newSelectedVarIndex > 0)
                    {
                        var selectedExpVar = editor._cachedExperimentVariables?.FirstOrDefault(v => v.name == action.conditionVariable);
                        if (selectedExpVar != null && selectedExpVar.values != null && selectedExpVar.values.Length > 0)
                        {
                            Rect valLabelRect = new Rect(rect.x + 15, currentY, EditorGUIUtility.labelWidth * 0.7f, lineHeight);
                            Rect valDropdownRect = new Rect(valLabelRect.xMax, currentY, rect.width - valLabelRect.width - 15, lineHeight);
                            EditorGUI.LabelField(valLabelRect, "Is Value");

                            var conditionValOptions = new List<string> { "[Select Value]" };
                            conditionValOptions.AddRange(selectedExpVar.values);

                            int selectedValIndex = action.conditionValue == null ? 0 : conditionValOptions.IndexOf(action.conditionValue);
                            if (selectedValIndex == -1) selectedValIndex = 0;

                            int newSelectedValIndex = EditorGUI.Popup(valDropdownRect, selectedValIndex, conditionValOptions.ToArray());
                            currentY += lineHeight + spacing;

                            if (newSelectedValIndex != selectedValIndex)
                            {
                                Undo.RecordObject(itemDataAsset, "Change Condition Value");
                                action.conditionValue = (newSelectedValIndex > 0) ? conditionValOptions[newSelectedValIndex] : null;
                                EditorUtility.SetDirty(itemDataAsset);
                            }
                        }
                        else if (selectedExpVar != null)
                        {
                            Rect noValuesRect = new Rect(rect.x + 15, currentY, rect.width - 15, lineHeight);
                            EditorGUI.HelpBox(noValuesRect, $"Variable '{action.conditionVariable}' has no defined values.", MessageType.Info);
                            currentY += lineHeight + spacing;
                        }
                    }
                }

                bool requiresMovableItem = new[] { "Set position", "Add position", "Set rotation", "Add rotation", "Sync with participant bone" }.Contains(action.predefinedActionTemplate.actionType);
                if (requiresMovableItem && itemGO.GetComponent<MovableItem>() == null)
                {
                    Rect warningRect = new Rect(rect.x, currentY, rect.width, lineHeight * 2);
                    EditorGUI.HelpBox(warningRect, $"Warning: '{action.predefinedActionTemplate.actionType}' requires a MovableItem component on '{itemGO.name}'.", MessageType.Warning);
                    currentY += lineHeight * 2 + spacing;
                }

                if (action.predefinedActionTemplate.actionType == "Customized Action")
                {
                    Rect textAreaRect = new Rect(rect.x + 15, currentY, rect.width - 15, lineHeight * 3);
                    DrawHoverableTextArea(textAreaRect, action.customAction ?? "", (newValue) => {
                        Undo.RecordObject(itemDataAsset, "Edit Custom Action");
                        action.customAction = newValue;
                        EditorUtility.SetDirty(itemDataAsset);
                    }, editor, isColored: true);
                    currentY += lineHeight * 3 + spacing;
                }
                else if (action.predefinedActionTemplate.variables != null && action.predefinedActionTemplate.variables.Length > 0)
                {
                    if (action.predefinedActionTemplate.actionType == "Send via OSC")
                    {
                        float labelWidth = 85f;
                        
                        Rect idLabelRect = new Rect(rect.x + 15, currentY, labelWidth, lineHeight);
                        Rect idFieldRect = new Rect(idLabelRect.xMax, currentY, rect.width - 15 - labelWidth, lineHeight);

                        EditorGUI.LabelField(idLabelRect, "Participant #");
                        action.variableValues.TryGetValue("participantId", out string currentId);
                        string newId = EditorGUI.TextField(idFieldRect, currentId ?? "");

                        newId = ValidateParticipantId(newId);

                        if (newId != currentId)
                        {
                            Undo.RecordObject(itemDataAsset, "Edit OSC Participant #");
                            action.variableValues["participantId"] = newId;
                            EditorUtility.SetDirty(itemDataAsset);
                        }
                        currentY += lineHeight + spacing;

                        Rect addressLabelRect = new Rect(rect.x + 15, currentY, labelWidth, lineHeight);
                        Rect addressFieldRect = new Rect(addressLabelRect.xMax, currentY, rect.width - 15 - labelWidth, lineHeight);

                        EditorGUI.LabelField(addressLabelRect, "Address");
                        action.variableValues.TryGetValue("address", out string currentAddress);
                        string newAddress = EditorGUI.TextField(addressFieldRect, currentAddress ?? "");
                    
                        if (newAddress != currentAddress)
                        {
                            Undo.RecordObject(itemDataAsset, "Edit OSC Address");
                            action.variableValues["address"] = newAddress;
                            EditorUtility.SetDirty(itemDataAsset);
                        }
                        currentY += lineHeight + spacing;

                        action.variableValues.TryGetValue("values_json", out string currentValuesJson);
                        var wrapper = new OscArgumentListWrapper();
                        if (!string.IsNullOrEmpty(currentValuesJson))
                        {
                            try { JsonUtility.FromJsonOverwrite(currentValuesJson, wrapper); }
                            catch { wrapper.Arguments = new List<OscArgument>(); }
                        }
                        if (wrapper.Arguments == null) wrapper.Arguments = new List<OscArgument>();

                        EditorGUI.BeginChangeCheck();

                        EditorGUI.LabelField(new Rect(rect.x + 15, currentY, rect.width - 15, lineHeight), "Values");
                        currentY += lineHeight + spacing;

                        Rect sizeLabelRect = new Rect(rect.x + 30, currentY, labelWidth, lineHeight);
                        Rect sizeFieldRect = new Rect(sizeLabelRect.xMax, currentY, rect.width - 45 - labelWidth, lineHeight);
                        
                        EditorGUI.LabelField(sizeLabelRect, "Size");
                        int newCount = EditorGUI.IntField(sizeFieldRect, wrapper.Arguments.Count);
                        currentY += lineHeight + spacing;
                        
                        if (newCount < 0) newCount = 0;
                        while (newCount > wrapper.Arguments.Count) wrapper.Arguments.Add(new OscArgument());
                        while (newCount < wrapper.Arguments.Count) wrapper.Arguments.RemoveAt(wrapper.Arguments.Count - 1);

                        for (int i = 0; i < wrapper.Arguments.Count; i++)
                        {
                            var arg = wrapper.Arguments[i];
                            float elX = rect.x + 30;
                            float elWidth = rect.width - 45;
                            float typeWidth = 80;
                            float valueWidth = elWidth - typeWidth - 5;

                            EditorGUI.LabelField(new Rect(elX, currentY, 20, lineHeight), $"[{i}]");
                            arg.Type = (OscArgument.OscValueType)EditorGUI.EnumPopup(new Rect(elX + 20, currentY, typeWidth, lineHeight), arg.Type);
                            arg.Value = EditorGUI.TextField(new Rect(elX + 20 + typeWidth + 5, currentY, valueWidth, lineHeight), arg.Value ?? "");
                            currentY += lineHeight + spacing;
                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(itemDataAsset, "Edit OSC Values");
                            action.variableValues["values_json"] = JsonUtility.ToJson(wrapper);
                            action.variableValues["values"] = GenerateOscValuesJsString(wrapper.Arguments);
                            EditorUtility.SetDirty(itemDataAsset);
                        }
                    }
                    else if (action.predefinedActionTemplate.actionType == "Send Haptics")
                    {
                        var variables = action.predefinedActionTemplate.variables;
                        foreach (string variableName in variables)
                        {
                            action.variableValues.TryGetValue(variableName, out string currentValue);
                            currentValue ??= "";
                            
                            float labelWidth = 85f;
                            if (variableName == "target" || variableName == "frequency" || variableName == "amplitude" || variableName == "duration")
                            {
                                labelWidth = 70f;
                            }

                            Rect labelRect = new Rect(rect.x + 15, currentY, labelWidth, lineHeight);
                            string label = variableName == "participantId" ? "Participant #" : variableName;
                            EditorGUI.LabelField(labelRect, label);
                            Rect fieldRect = new Rect(labelRect.xMax, currentY, rect.width - labelRect.width - 15, lineHeight);
                            
                            if (variableName == "target")
                            {
                                string displayValue = currentValue.Trim('\'');
                                string newValueFromField = EditorGUI.TextField(fieldRect, displayValue);
                                string newValueToStore = $"'{newValueFromField}'";

                                if (newValueToStore != currentValue)
                                {
                                    Undo.RecordObject(itemDataAsset, "Edit Variable " + variableName);
                                    action.variableValues[variableName] = newValueToStore;
                                    EditorUtility.SetDirty(itemDataAsset);
                                }
                            }
                            else
                            {
                                string newValue = EditorGUI.TextField(fieldRect, currentValue);
                                if (variableName == "participantId")
                                {
                                    newValue = ValidateParticipantId(newValue);
                                }

                                if (newValue != currentValue)
                                {
                                    Undo.RecordObject(itemDataAsset, "Edit Variable " + variableName);
                                    action.variableValues[variableName] = newValue;
                                    EditorUtility.SetDirty(itemDataAsset);
                                }
                            }
                            currentY += lineHeight + spacing;
                        }
                    }
                    else if (action.predefinedActionTemplate.actionType == "Assign avatar to participant")
                    {
                        float labelWidth = 85f;

                        // avatarID dropdown
                        Rect avatarLabelRect = new Rect(rect.x + 15, currentY, labelWidth, lineHeight);
                        Rect avatarFieldRect = new Rect(avatarLabelRect.xMax, currentY, rect.width - 15 - labelWidth, lineHeight);
                        EditorGUI.LabelField(avatarLabelRect, "Avatar ID");

                        action.variableValues.TryGetValue("avatarID", out string currentAvatarID);
                        currentAvatarID ??= "";

                        // Registry is scoped to the state-listener's own scene. With
                        // additive scene loading the active scene may differ from
                        // itemGO.scene, so read from the GameObject's scene to keep
                        // each scene's avatar set isolated.
                        string avatarSceneFolder = AvatarsConfigAssetUtil.SanitizeSceneFolderName(itemGO.scene.name);
                        string avatarRegistryPath = avatarSceneFolder != null
                            ? AvatarsConfigAssetUtil.GetRegistryPath(avatarSceneFolder)
                            : null;
                        var avatarRegistry = avatarRegistryPath != null
                            ? AssetDatabase.LoadAssetAtPath<AvatarRegistry>(avatarRegistryPath)
                            : null;
                        if (avatarRegistry != null && avatarRegistry.entries.Count > 0)
                        {
                            string[] avatarIDs = avatarRegistry.GetAvatarIDs();
                            int selectedAvatarIdx = System.Array.IndexOf(avatarIDs, currentAvatarID);
                            if (selectedAvatarIdx < 0) selectedAvatarIdx = 0;
                            int newAvatarIdx = EditorGUI.Popup(avatarFieldRect, selectedAvatarIdx, avatarIDs);
                            string newAvatarID = avatarIDs[newAvatarIdx];
                            if (newAvatarID != currentAvatarID)
                            {
                                Undo.RecordObject(itemDataAsset, "Change Avatar ID");
                                action.variableValues["avatarID"] = newAvatarID;
                                EditorUtility.SetDirty(itemDataAsset);
                            }
                        }
                        else
                        {
                            string newAvatarID = EditorGUI.TextField(avatarFieldRect, currentAvatarID);
                            if (newAvatarID != currentAvatarID)
                            {
                                Undo.RecordObject(itemDataAsset, "Change Avatar ID");
                                action.variableValues["avatarID"] = newAvatarID;
                                EditorUtility.SetDirty(itemDataAsset);
                            }
                        }
                        currentY += lineHeight + spacing;

                        // participantIndex
                        Rect pIdxLabelRect = new Rect(rect.x + 15, currentY, labelWidth, lineHeight);
                        Rect pIdxFieldRect = new Rect(pIdxLabelRect.xMax, currentY, rect.width - 15 - labelWidth, lineHeight);
                        EditorGUI.LabelField(pIdxLabelRect, "Participant #");
                        action.variableValues.TryGetValue("participantIndex", out string currentPIdx);
                        string newPIdx = EditorGUI.TextField(pIdxFieldRect, currentPIdx ?? "1");
                        newPIdx = ValidateParticipantId(newPIdx);
                        if (newPIdx != currentPIdx)
                        {
                            Undo.RecordObject(itemDataAsset, "Change Participant Index");
                            action.variableValues["participantIndex"] = newPIdx;
                            EditorUtility.SetDirty(itemDataAsset);
                        }
                        currentY += lineHeight + spacing;

                    }
                    else if (action.predefinedActionTemplate.actionType == "Unassign avatar from participant")
                    {
                        float labelWidth = 85f;
                        Rect pIdxLabelRect = new Rect(rect.x + 15, currentY, labelWidth, lineHeight);
                        Rect pIdxFieldRect = new Rect(pIdxLabelRect.xMax, currentY, rect.width - 15 - labelWidth, lineHeight);
                        EditorGUI.LabelField(pIdxLabelRect, "Participant #");
                        action.variableValues.TryGetValue("participantIndex", out string currentPIdx);
                        string newPIdx = EditorGUI.TextField(pIdxFieldRect, currentPIdx ?? "1");
                        newPIdx = ValidateParticipantId(newPIdx);
                        if (newPIdx != currentPIdx)
                        {
                            Undo.RecordObject(itemDataAsset, "Change Participant Index");
                            action.variableValues["participantIndex"] = newPIdx;
                            EditorUtility.SetDirty(itemDataAsset);
                        }
                        currentY += lineHeight + spacing;
                    }
                    else if (action.predefinedActionTemplate.actionType == "Set participant position"
                          || action.predefinedActionTemplate.actionType == "Add participant position"
                          || action.predefinedActionTemplate.actionType == "Set participant rotation"
                          || action.predefinedActionTemplate.actionType == "Add participant rotation")
                    {
                        float labelWidth = 85f;

                        // Row 1: Participant #
                        Rect pIdxLabelRect = new Rect(rect.x + 15, currentY, labelWidth, lineHeight);
                        Rect pIdxFieldRect = new Rect(pIdxLabelRect.xMax, currentY, rect.width - 15 - labelWidth, lineHeight);
                        EditorGUI.LabelField(pIdxLabelRect, "Participant #");
                        action.variableValues.TryGetValue("participantIndex", out string currentPIdx);
                        string newPIdx = EditorGUI.TextField(pIdxFieldRect, currentPIdx ?? "1");
                        newPIdx = ValidateParticipantId(newPIdx);
                        if (newPIdx != currentPIdx)
                        {
                            Undo.RecordObject(itemDataAsset, "Change Participant Index");
                            action.variableValues["participantIndex"] = newPIdx;
                            EditorUtility.SetDirty(itemDataAsset);
                        }
                        currentY += lineHeight + spacing;

                        // Row 2: x / y / z inline (rotation values are Euler degrees)
                        bool isRotation = action.predefinedActionTemplate.actionType.EndsWith("rotation");
                        string axisRowLabel = isRotation ? "Euler (deg)" : "Position";
                        EditorGUI.LabelField(new Rect(rect.x + 15, currentY, rect.width - 15, lineHeight), axisRowLabel);
                        currentY += lineHeight + spacing;

                        float axisLabelWidth = 14f;
                        float axisFieldWidth = 50f;
                        float axisSpacing = 8f;
                        string[] axes = { "x", "y", "z" };
                        float ax = rect.x + 15;
                        for (int i = 0; i < 3; i++)
                        {
                            string variableName = axes[i];
                            EditorGUI.LabelField(new Rect(ax, currentY, axisLabelWidth, lineHeight), variableName);
                            action.variableValues.TryGetValue(variableName, out string currentValue);
                            string newValue = EditorGUI.TextField(new Rect(ax + axisLabelWidth, currentY, axisFieldWidth, lineHeight), currentValue ?? "0");
                            if (newValue != currentValue)
                            {
                                Undo.RecordObject(itemDataAsset, "Edit Variable " + variableName);
                                action.variableValues[variableName] = newValue;
                                EditorUtility.SetDirty(itemDataAsset);
                            }
                            ax += axisLabelWidth + axisFieldWidth + axisSpacing;
                        }
                        currentY += lineHeight + spacing;
                    }
                    else if (action.predefinedActionTemplate.actionType == "Sync with participant bone")
                    {
                        float labelWidth = 85f;

                        // Row 1: Participant #
                        Rect pIdxLabelRect = new Rect(rect.x + 15, currentY, labelWidth, lineHeight);
                        Rect pIdxFieldRect = new Rect(pIdxLabelRect.xMax, currentY, rect.width - 15 - labelWidth, lineHeight);
                        EditorGUI.LabelField(pIdxLabelRect, "Participant #");
                        action.variableValues.TryGetValue("participantIndex", out string currentPIdx);
                        string newPIdx = EditorGUI.TextField(pIdxFieldRect, currentPIdx ?? "1");
                        newPIdx = ValidateParticipantId(newPIdx);
                        if (newPIdx != currentPIdx)
                        {
                            Undo.RecordObject(itemDataAsset, "Change Participant Index");
                            action.variableValues["participantIndex"] = newPIdx;
                            EditorUtility.SetDirty(itemDataAsset);
                        }
                        currentY += lineHeight + spacing;

                        // Row 2: Bone
                        Rect boneLabelRect = new Rect(rect.x + 15, currentY, labelWidth, lineHeight);
                        Rect boneFieldRect = new Rect(boneLabelRect.xMax, currentY, rect.width - 15 - labelWidth, lineHeight);
                        EditorGUI.LabelField(boneLabelRect, "Bone");
                        action.variableValues.TryGetValue("bone", out string currentBone);
                        int selectedBoneIdx = System.Array.IndexOf(HumanoidBoneNames, currentBone);
                        if (selectedBoneIdx < 0) selectedBoneIdx = System.Array.IndexOf(HumanoidBoneNames, "Head");
                        if (selectedBoneIdx < 0) selectedBoneIdx = 0;
                        int newBoneIdx = EditorGUI.Popup(boneFieldRect, selectedBoneIdx, HumanoidBoneNames);
                        string newBone = HumanoidBoneNames[newBoneIdx];
                        if (newBone != currentBone)
                        {
                            Undo.RecordObject(itemDataAsset, "Change Bone");
                            action.variableValues["bone"] = newBone;
                            EditorUtility.SetDirty(itemDataAsset);
                        }
                        currentY += lineHeight + spacing;

                        // Rows 3-6: Pos offset label / x y z / Rot offset label / x y z
                        string[][] offsetRows = new[]
                        {
                            new[] { "Pos offset", "posX", "posY", "posZ" },
                            new[] { "Rot offset", "rotX", "rotY", "rotZ" },
                        };
                        float axisLabelWidth = 14f;
                        float axisFieldWidth = 40f;
                        float axisSpacing = 8f;
                        string[] axes = { "x", "y", "z" };

                        foreach (var row in offsetRows)
                        {
                            // Label on its own row
                            EditorGUI.LabelField(new Rect(rect.x + 15, currentY, rect.width - 15, lineHeight), row[0]);
                            currentY += lineHeight + spacing;

                            // x/y/z on the next row, inline
                            float x = rect.x + 15;
                            for (int i = 0; i < 3; i++)
                            {
                                string variableName = row[i + 1];
                                EditorGUI.LabelField(new Rect(x, currentY, axisLabelWidth, lineHeight), axes[i]);
                                action.variableValues.TryGetValue(variableName, out string currentValue);
                                string newValue = EditorGUI.TextField(new Rect(x + axisLabelWidth, currentY, axisFieldWidth, lineHeight), currentValue ?? "0");
                                if (newValue != currentValue)
                                {
                                    Undo.RecordObject(itemDataAsset, "Edit Variable " + variableName);
                                    action.variableValues[variableName] = newValue;
                                    EditorUtility.SetDirty(itemDataAsset);
                                }
                                x += axisLabelWidth + axisFieldWidth + axisSpacing;
                            }
                            currentY += lineHeight + spacing;
                        }
                    }
                    else if (action.predefinedActionTemplate.actionType == "Send data to collector")
                    {
                        DrawPushDataToCollectorAction(rect, ref currentY, lineHeight, spacing, action, itemDataAsset);
                    }
                    else
                    {
                        var variables = action.predefinedActionTemplate.variables;
                        bool allSingleChar = variables.All(v => v.Length == 1);

                        if (allSingleChar)
                        {
                            float labelWidth = 18f, fieldWidth = 40f, spacingH = 8f;
                            float x = rect.x + 15;
                            foreach (string variableName in variables)
                            {
                                EditorGUI.LabelField(new Rect(x, currentY, labelWidth, lineHeight), variableName);
                                action.variableValues.TryGetValue(variableName, out string currentValue);
                                string newValue = EditorGUI.TextField(new Rect(x + labelWidth, currentY, fieldWidth, lineHeight), currentValue ?? "");
                                if (newValue != currentValue)
                                {
                                    Undo.RecordObject(itemDataAsset, "Edit Variable " + variableName);
                                    action.variableValues[variableName] = newValue;
                                    EditorUtility.SetDirty(itemDataAsset);
                                }
                                x += labelWidth + fieldWidth + spacingH;
                            }
                            currentY += lineHeight + spacing;
                        }
                        else
                        {
                            foreach (string variableName in variables)
                            {
                                action.variableValues.TryGetValue(variableName, out string currentValue);
                                currentValue ??= "";

                                if (action.predefinedActionTemplate.actionType == "Set text" && variableName == "text")
                                {
                                    EditorGUI.LabelField(new Rect(rect.x + 15, currentY, rect.width - 15, lineHeight), variableName);
                                    currentY += lineHeight + spacing;
                                    float textAreaHeight = lineHeight * 2;
                                    Rect textAreaRect = new Rect(rect.x + 15, currentY, rect.width - 15, textAreaHeight);

                                    DrawHoverableTextArea(textAreaRect, currentValue, (newValue) => {
                                        Undo.RecordObject(itemDataAsset, "Edit Variable " + variableName);
                                        action.variableValues[variableName] = newValue;
                                        EditorUtility.SetDirty(itemDataAsset);
                                    }, editor, isColored: false);

                                    currentY += textAreaHeight + spacing;
                                }
                                else
                                {
                                    Rect labelRect = new Rect(rect.x + 15, currentY, EditorGUIUtility.labelWidth * 0.6f, lineHeight);
                                    EditorGUI.LabelField(labelRect, variableName);
                                    Rect fieldRect = new Rect(labelRect.xMax, currentY,
                                        rect.width - labelRect.width - 15,
                                        lineHeight);
                                    string newValue = EditorGUI.TextField(fieldRect, currentValue);
                                    if (newValue != currentValue)
                                    {
                                        Undo.RecordObject(itemDataAsset, "Edit Variable " + variableName);
                                        action.variableValues[variableName] = newValue;
                                        EditorUtility.SetDirty(itemDataAsset);
                                    }
                                    currentY += lineHeight + spacing;
                                }
                            }
                        }
                    }
                }
            },
        elementHeightCallback = index =>
        {
            if (actions == null || index < 0 || index >= actions.Count) return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var action = actions[index];
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float height = lineHeight + spacing;

            if (isConditionalAllowed && action.isConditional)
            {
                height += lineHeight + spacing;
                if (!string.IsNullOrEmpty(action.conditionVariable))
                {
                    var selectedExpVar = editor._cachedExperimentVariables?.FirstOrDefault(v => v.name == action.conditionVariable);
                    if (selectedExpVar != null)
                    {
                        height += lineHeight + spacing;
                    }
                }
            }

            bool requiresMovableItem = new[] { "Set position", "Add position", "Set rotation", "Add rotation", "Sync with participant bone" }.Contains(action.predefinedActionTemplate.actionType);
            if (requiresMovableItem && itemGO.GetComponent<MovableItem>() == null)
            {
                 height += lineHeight * 2 + spacing;
            }

            if (action.predefinedActionTemplate.actionType == "Customized Action")
            {
                height += lineHeight * 3 + spacing;
            }
            else if (action.predefinedActionTemplate.variables != null && action.predefinedActionTemplate.variables.Length > 0)
            {
                if (action.predefinedActionTemplate.actionType == "Send via OSC")
                {
                    height += (lineHeight + spacing);
                    height += (lineHeight + spacing); 
                    height += (lineHeight + spacing) * 2; 

                    action.variableValues.TryGetValue("values_json", out string currentValuesJson);
                    var wrapper = new OscArgumentListWrapper();
                    if (!string.IsNullOrEmpty(currentValuesJson))
                    {
                        try { JsonUtility.FromJsonOverwrite(currentValuesJson, wrapper); }
                        catch { wrapper.Arguments = new List<OscArgument>(); }
                    }
                    if (wrapper.Arguments == null) wrapper.Arguments = new List<OscArgument>();
                    
                    height += wrapper.Arguments.Count * (lineHeight + spacing);
                }
                else if (action.predefinedActionTemplate.actionType == "Assign avatar to participant")
                {
                    // avatarID row + participantIndex row
                    height += (lineHeight + spacing) * 2;
                }
                else if (action.predefinedActionTemplate.actionType == "Unassign avatar from participant")
                {
                    // participantIndex row
                    height += lineHeight + spacing;
                }
                else if (action.predefinedActionTemplate.actionType == "Sync with participant bone")
                {
                    // participant row, bone row, pos offset label, pos xyz, rot offset label, rot xyz
                    height += (lineHeight + spacing) * 6;
                }
                else if (action.predefinedActionTemplate.actionType == "Set participant position"
                      || action.predefinedActionTemplate.actionType == "Add participant position"
                      || action.predefinedActionTemplate.actionType == "Set participant rotation"
                      || action.predefinedActionTemplate.actionType == "Add participant rotation")
                {
                    // participant row, axis-row label, xyz inline
                    height += (lineHeight + spacing) * 3;
                }
                else if (action.predefinedActionTemplate.actionType == "Send data to collector")
                {
                    height += GetPushDataToCollectorActionHeight(action, lineHeight, spacing);
                }
                else
                {
                    var variables = action.predefinedActionTemplate.variables;
                    bool allSingleChar = variables.All(v => v.Length == 1);
                    if (allSingleChar)
                    {
                        height += lineHeight + spacing;
                    }
                    else
                    {
                        foreach (string variableName in variables)
                        {
                            if (action.predefinedActionTemplate.actionType == "Set text" && variableName == "text")
                            {
                                height += (lineHeight + spacing) * 3;
                            }
                            else
                            {
                                height += lineHeight + spacing;
                            }
                        }
                    }
                }
            }
            return height + spacing;
        },
        onAddCallback = list =>
        {
            Undo.RecordObject(itemDataAsset, "Add Action");
            actions.Add(new StateListenerAction());
            EditorUtility.SetDirty(itemDataAsset);
        },
        onRemoveCallback = list =>
        {
            Undo.RecordObject(itemDataAsset, "Remove Action");
            if (list.index >= 0 && list.index < actions.Count)
            {
                actions.RemoveAt(list.index);
            }
            EditorUtility.SetDirty(itemDataAsset);
        },
        onReorderCallback = list =>
        {
            Undo.RecordObject(itemDataAsset, "Reorder Actions");
            EditorUtility.SetDirty(itemDataAsset);
        }
    };
        if (key != null) editor._reorderableLists[key] = rl;
        return rl;
    }

    private static void DrawReorderableList(ItemsManagerConfigTab editor, GameObject item, int stateID, string keySuffix)
    {
        var key = $"{item.GetInstanceID()}_{stateID}_{keySuffix}";
        if (editor._reorderableLists.TryGetValue(key, out var rl))
        {
            rl.DoLayoutList();
        }
    }

    #region Context menus

    private static void ShowListenerDuplicateDropdown(GameObject item, int sourceStateID, ItemsManagerConfigTab editor, Rect buttonRect)
    {
        var menu = new GenericMenu();
        editor.stateListenersByItem.TryGetValue(item, out var listeners);
        var occupiedStateIds = listeners != null
            ? new HashSet<int>(listeners.Select(l => l.stateID))
            : new HashSet<int>();

        bool anyTarget = false;
        for (int i = 0; i < editor._cachedStateNames.Length; i++)
        {
            if (i == sourceStateID || occupiedStateIds.Contains(i)) continue;
            int targetStateId = i;
            string targetStateName = editor._cachedStateNames[i];
            menu.AddItem(new GUIContent(targetStateName), false, () =>
            {
                ItemsManagerAssetUtil.DuplicateListenerToState(item, sourceStateID, targetStateId, editor);
            });
            anyTarget = true;
        }
        if (!anyTarget)
        {
            menu.AddDisabledItem(new GUIContent("No empty states on this item"));
        }
        menu.DropDown(buttonRect);
    }

    #endregion

    #region Drag and Drop

    private static void TryStartPendingDrag()
    {
        var evt = Event.current;
        if (evt.type == EventType.MouseDrag)
        {
            if (_pendingListenerPayload != null)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
                DragAndDrop.SetGenericData(ItemsManagerConfigTab.DragKeyListener, _pendingListenerPayload);
                DragAndDrop.StartDrag("Move listener");
                _pendingListenerPayload = null;
                evt.Use();
            }
        }
        else if (evt.type == EventType.MouseUp || evt.type == EventType.DragExited)
        {
            _pendingListenerPayload = null;
        }
    }

    private static void DrawListenerDragHandle(Rect rect, ItemsManagerConfigTab.ListenerDragPayload payload)
    {
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);
        GUI.Label(rect, new GUIContent("≡  Drag to move", "Drag to move these actions to another cell."), GetDragLabelStyle());

        var evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
        {
            _pendingListenerPayload = payload;
            evt.Use();
        }
    }

    private static void HandleListenerDrop(Rect rect, ItemsManagerConfigTab editor,
                                           GameObject targetItem, int targetStateID, bool cellOccupied)
    {
        var evt = Event.current;
        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
        if (!rect.Contains(evt.mousePosition)) return;

        var payload = DragAndDrop.GetGenericData(ItemsManagerConfigTab.DragKeyListener) as ItemsManagerConfigTab.ListenerDragPayload;
        if (payload == null) return;

        bool sameCell = payload.sourceItem == targetItem && payload.sourceStateID == targetStateID;

        if (evt.type == EventType.DragUpdated)
        {
            if (sameCell) DragAndDrop.visualMode = DragAndDropVisualMode.None;
            else if (cellOccupied) DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                EditorGUI.DrawRect(rect, new Color(0.3f, 1f, 0.4f, 0.12f));
            }
            evt.Use();
        }
        else
        {
            if (!sameCell && !cellOccupied)
            {
                DragAndDrop.AcceptDrag();
                ItemsManagerAssetUtil.MoveListener(payload, editor, targetItem, targetStateID);
                editor._needsRebuild = true;
            }
            evt.Use();
        }
    }

    #endregion

    private static string ValidateParticipantId(string id)
    {
        if (int.TryParse(id, out int intId) && intId <= 0)
        {
            return "1";
        }
        return id;
    }

    private static string GenerateOscValuesJsString(List<OscArgument> args)
    {
        if (args == null || args.Count == 0)
        {
            return "";
        }

        var stringParts = new List<string>();
        foreach (var arg in args)
        {
            switch (arg.Type)
            {
                case OscArgument.OscValueType.Boolean:
                    bool.TryParse(arg.Value, out bool boolVal);
                    stringParts.Add(boolVal ? "true" : "false");
                    break;
                case OscArgument.OscValueType.Number:
                    // Use InvariantCulture to ensure '.' is the decimal separator.
                    double.TryParse(arg.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double numVal);
                    stringParts.Add(numVal.ToString(CultureInfo.InvariantCulture));
                    break;
                case OscArgument.OscValueType.String:
                default:
                    // Escape single quotes and backslashes, then wrap the result in single quotes.
                    string escaped = (arg.Value ?? "")
                        .Replace("\\", "\\\\")
                        .Replace("'", "\\'");
                    stringParts.Add($"'{escaped}'");
                    break;
            }
        }
        return string.Join(", ", stringParts);
    }

    #endregion

    #region Hover-to-Zoom TextArea Feature

    private static void InitializeStyles()
    {
        _codeTextAreaStyle = new GUIStyle(EditorStyles.textArea);
        Font codeFont = AssetDatabase.LoadAssetAtPath<Font>(codeFontPath);
        if (codeFont != null)
        {
            _codeTextAreaStyle.font = codeFont;
        }
        
        _codeTextAreaStyle.richText = true;
    }
    
    private static void DrawHoverableTextArea(Rect rect, string text, Action<string> onUpdate, ItemsManagerConfigTab editor, bool isColored)
    {
        if (_codeTextAreaStyle == null)
        {
            InitializeStyles();
        }

        string displayText = (isColored && !string.IsNullOrEmpty(text)) ? HighlightJsSyntax(text) : text;

        if (GUI.Button(rect, displayText, _codeTextAreaStyle))
        {
            if (!EditorWindow.HasOpenInstances<TextAreaOverlayWindow>())
            {
                Rect screenRect = GUIUtility.GUIToScreenRect(rect);
                float zoomWidth = Math.Max(450f, screenRect.width * 2f);
                float zoomHeight = Math.Max(200f, screenRect.height * 3f);
                Rect popupRect = new Rect(screenRect.x, screenRect.y, zoomWidth, zoomHeight);

                TextAreaOverlayWindow.Show(popupRect, text, onUpdate, _codeTextAreaStyle);
            }
        }
    }

    #endregion
    
    #region Syntax Highlighting
    
    private const string JsKeywordColor = "#569CD6";
    private const string JsStringColor = "#CE9178";
    private const string JsCommentColor = "#6A9955";
    private const string JsNumberColor = "#B5CEA8";
    private const string JsFunctionColor = "#DCDCAA";
    private const string JsPunctuationColor = "#D4D4D4";

    private static readonly Regex JsSyntaxRegex = new Regex(
        @"(?<comment>//.*|/\*[\s\S]*?\*/)|" +
        @"(?<string>"".*?""|'.*?'|`.*?`)|" +
        @"(?<keyword>\b(if|else|for|while|var|let|const|function|return|new|true|false|null|this|try|catch|finally|switch|case|default|break|continue|delete|typeof|instanceof|in|void)\b)|" +
        @"(?<number>\b\d+(\.\d+)?([eE][+-]?\d+)?\b)|" +
        @"(?<function>\b[a-zA-Z_]\w*(?=\s*\())|" +
        @"(?<punctuation>[{}\[\]();,.=+\-*/%&|<>!~?:]+)",
        RegexOptions.Compiled | RegexOptions.Multiline
    );
    
    public static string HighlightJsSyntax(string code)
    {
        return JsSyntaxRegex.Replace(code, match =>
        {
            if (match.Groups["comment"].Success)
                return $"<color={JsCommentColor}>{match.Value}</color>";
            if (match.Groups["string"].Success)
                return $"<color={JsStringColor}>{match.Value}</color>";
            if (match.Groups["keyword"].Success)
                return $"<color={JsKeywordColor}>{match.Value}</color>";
            if (match.Groups["number"].Success)
                return $"<color={JsNumberColor}>{match.Value}</color>";
            if (match.Groups["function"].Success)
                return $"<color={JsFunctionColor}>{match.Value}</color>";
            if (match.Groups["punctuation"].Success)
                return $"<color={JsPunctuationColor}>{match.Value}</color>";
            
            return match.Value;
        });
    }

    #endregion
}
