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
        new StateListeningAction("Show item", "$.setStateCompat('this', 'exp_showItem', true);"),
        new StateListeningAction("Hide item", "$.setStateCompat('this', 'exp_showItem', false);"),
        new StateListeningAction("Set position", "$.setPosition(new Vector3({_x_}, {_y_}, {_z_}))", new[] { "x", "y", "z" }),
        new StateListeningAction("Add position", "$.setPosition($.getPosition().add(new Vector3({_x_}, {_y_}, {_z_})))",
            new[] { "x", "y", "z" }),
        new StateListeningAction("Set rotation",
            "$.setRotation(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_})))", new[] { "x", "y", "z" }),
        new StateListeningAction("Add rotation",
            "$.setRotation($.getRotation().multiply(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_}))))",
            new[] { "x", "y", "z" }),
        new StateListeningAction("Show child", "$.subNode('{_childName_}').setEnabled(true)", new[] { "childName" }),
        new StateListeningAction("Hide child", "$.subNode('{_childName_}').setEnabled(false)", new[] { "childName" }),
        new StateListeningAction("Set child position", "$.subNode('{_childName_}').setPosition(new Vector3({_x_}, {_y_}, {_z_}))", new[] { "childName", "x", "y", "z" }),
        new StateListeningAction("Add child position", "$.subNode('{_childName_}').setPosition($.subNode('{_childName_}').getPosition().add(new Vector3({_x_}, {_y_}, {_z_})))",
            new[] { "childName", "x", "y", "z" }),
        new StateListeningAction("Set child rotation",
            "$.subNode('{_childName_}').setRotation(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_})))", new[] { "childName", "x", "y", "z" }),
        new StateListeningAction("Add child rotation",
            "$.subNode('{_childName_}').setRotation($.subNode('{_childName_}').getRotation().multiply(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_}))))",
            new[] { "childName", "x", "y", "z" }),
        new StateListeningAction("To next state", "$.sendSignalCompat('this', 'state_triggerTransition');"),
        new StateListeningAction("Send data to collector", "if (!$.groupState.collectedData) $.groupState.collectedData = {};\n    let collectedData = $.groupState.collectedData;\n    collectedData['{_label_}'] = {_value_};\n    $.groupState.collectedData = collectedData;", new[] { "label", "value" }, _displayLabel: "Add value to collector"),
        new StateListeningAction("Process and save collected data", "$.sendSignalCompat('this', 'exp_recordCustomData');", _displayLabel: "Save row to buffer"),
        new StateListeningAction("Upload collected data", "$.sendSignalCompat('this', 'exp_uploadCustomData');", _displayLabel: "Submit collected data"),
        new StateListeningAction("Set text", "$.subNode('Text').setText(`{_text_}`);", new[] { "text" }),
        new StateListeningAction("Send Haptics",
            "PARTICIPANTS[{_participantId_}].send('haptics', {target: {_target_}, frequency: {_frequency_}, amplitude: {_amplitude_}, duration: {_duration_}});",
            new[] { "participantId", "target", "frequency", "amplitude", "duration" }),
        new StateListeningAction("Send via OSC", "PARTICIPANTS[{_participantId_}].send('sendOsc', {address: '{_address_}', values: [{_values_}] });", new[] { "participantId", "address", "values" }),
        new StateListeningAction("Sleep", "{_seconds_}", new[] { "seconds" }),
        new StateListeningAction("Assign avatar to participant",
            "$.worldItemReference('LUIDA-AvatarSpawner').send('luida_assign_avatar', { avatarID: '{_avatarID_}', participantIndex: {_participantIndex_} });",
            new[] { "avatarID", "participantIndex" }),
        new StateListeningAction("Unassign avatar from participant",
            "$.worldItemReference('LUIDA-AvatarSpawner').send('luida_unassign_avatar', { participantIndex: {_participantIndex_} });",
            new[] { "participantIndex" }),
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
            new[] { "participantIndex", "bone", "posX", "posY", "posZ", "rotX", "rotY", "rotZ" }),
    };

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

        DrawOtherImplementationRow(editor);
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

    private static void DrawOtherImplementationRow(ItemsManagerConfigTab editor)
    {
        EditorGUILayout.LabelField(new GUIContent("Always-on code (runs regardless of state)", "Functions, events, and variables that run regardless of which state is active."), EditorStyles.largeLabel);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(215));

        EditorGUILayout.HelpBox("DON'T use $.onStart and $.onUpdate here! Implement function Start and Update instead.", MessageType.Warning);
        EditorGUILayout.EndVertical();
        GUILayout.Space(5);

        int otherRowColumnIndex = 0;
        foreach (var item in editor._cachedItems)
        {
            if (item == null) { otherRowColumnIndex++; continue; }
            Color accent = GetItemColumnAccent(otherRowColumnIndex);
            Color cellBgColor = new Color(accent.r, accent.g, accent.b, 0.18f);
            Rect cellRect = EditorGUILayout.BeginVertical("box", GUILayout.Width(238.5f), GUILayout.MinHeight(80));
            EditorGUI.DrawRect(cellRect, cellBgColor);

            string itemDataAssetPath = ItemsManagerAssetUtil.GetItemDataAssetPath(item);
            var itemDataAsset = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(itemDataAssetPath);

            if (itemDataAsset != null)
            {
                string currentOtherImpl = itemDataAsset.otherImplementation ?? string.Empty;

                Rect taRect = EditorGUILayout.GetControlRect(false, 75, GUILayout.Width(233.5f), GUILayout.MaxHeight(75));
                DrawHoverableTextArea(taRect, currentOtherImpl, (newValue) =>
                {
                    Undo.RecordObject(itemDataAsset, "Edit Other Implementation for " + item.name);
                    itemDataAsset.otherImplementation = newValue;
                    EditorUtility.SetDirty(itemDataAsset);
                }, editor, isColored: true);
            }
            else
            {
                EditorGUILayout.HelpBox($"Asset not found for {item.name}.", MessageType.Error);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            otherRowColumnIndex++;
        }
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
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

    private static void CreateReorderableList(ItemsManagerConfigTab editor, GameObject itemGO, StateListeningItemData itemDataAsset, StateListener listener, List<StateListenerAction> actions, string header, string keySuffix)
    {
        var key = $"{itemGO.GetInstanceID()}_{listener.stateID}_{keySuffix}";
        bool isCurrentStateTrialRelated = ItemsManagerAssetUtil.IsTrialRelatedState(listener.stateID, editor.stateList);

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

                float ifButtonAndSpacingWidth = isCurrentStateTrialRelated ? 35 + spacing : 0;
                float dupButtonAndSpacingWidth = 22 + spacing;
                float dropdownWidth = availableWidth - ifButtonAndSpacingWidth - dupButtonAndSpacingWidth;
                Rect dropdownRect = new Rect(currentX, currentY, dropdownWidth, lineHeight);

                // Display labels: fall back to actionType when no displayLabel is set.
                // Selection logic below still keys off actionType (the serialization key).
                var options = AvailableStateListeningActions.Select(a => a.GetDisplayLabel()).ToList();
                options.Insert(0, "Select action");
                options.Add("Customized Action");

                int selectedIndex = 0;
                if (!string.IsNullOrEmpty(action.predefinedActionTemplate.actionType)) {
                    selectedIndex = (action.predefinedActionTemplate.actionType == "Customized Action")
                        ? options.Count - 1
                        : AvailableStateListeningActions.ToList().FindIndex(a => a.actionType == action.predefinedActionTemplate.actionType) + 1;
                    if (selectedIndex < 0) selectedIndex = 0;
                }

                int newIndex = EditorGUI.Popup(dropdownRect, selectedIndex, options.ToArray());
                currentX += dropdownWidth + spacing;

                Rect dupRect = new Rect(currentX, currentY, 22, lineHeight);
                if (GUI.Button(dupRect, DupButtonContent("Duplicate this action below this one.")))
                {
                    ItemsManagerAssetUtil.DuplicateAction(itemDataAsset, actions, index);
                    editor._needsRebuild = true;
                    GUIUtility.ExitGUI();
                }
                currentX += 22 + spacing;

                if (isCurrentStateTrialRelated)
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

                if (isCurrentStateTrialRelated && action.isConditional)
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

                if (newIndex != selectedIndex)
                {
                    Undo.RecordObject(itemDataAsset, "Change Action Type");
                    if (newIndex == 0)
                    {
                        action.predefinedActionTemplate = default;
                        action.customAction = "";
                        action.variableValues.Clear();
                    }
                    else if (newIndex == options.Count - 1)
                    {
                        action.predefinedActionTemplate = new StateListeningAction("Customized Action", "", null);
                        action.customAction = "// Your custom ClusterScript code here\n";
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
                                // MODIFIED: For Haptics target, initialize with quotes
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

                        var avatarRegistry = AssetDatabase.LoadAssetAtPath<AvatarRegistry>(AvatarsConfigAssetUtil.RegistryPath);
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
                                    // For the "Send data to collector" action's label input, append a small ⚙
                                    // button that opens the LUIDA Data Collector window so users can manage
                                    // registered labels without leaving the action UI.
                                    bool isCollectorLabelField =
                                        action.predefinedActionTemplate.actionType == "Send data to collector"
                                        && variableName == "label";
                                    float configButtonWidth = isCollectorLabelField ? 26f : 0f;
                                    Rect fieldRect = new Rect(labelRect.xMax, currentY,
                                        rect.width - labelRect.width - 15 - configButtonWidth - (configButtonWidth > 0 ? 4f : 0f),
                                        lineHeight);
                                    string newValue = EditorGUI.TextField(fieldRect, currentValue);
                                    if (newValue != currentValue)
                                    {
                                        Undo.RecordObject(itemDataAsset, "Edit Variable " + variableName);
                                        action.variableValues[variableName] = newValue;
                                        EditorUtility.SetDirty(itemDataAsset);
                                    }
                                    if (isCollectorLabelField)
                                    {
                                        Rect btnRect = new Rect(fieldRect.xMax + 4f, currentY, configButtonWidth, lineHeight);
                                        var btn = EditorGUIUtility.IconContent("d_Settings@2x");
                                        if (btn == null || btn.image == null) btn = new GUIContent("⚙");
                                        btn.tooltip = "Open the LUIDA Data Collector configuration window";
                                        if (GUI.Button(btnRect, btn, EditorStyles.miniButton))
                                        {
                                            DataCollectorConfigTab.ShowWindow();
                                        }
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

            if (isCurrentStateTrialRelated && action.isConditional)
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
        editor._reorderableLists[key] = rl;
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
