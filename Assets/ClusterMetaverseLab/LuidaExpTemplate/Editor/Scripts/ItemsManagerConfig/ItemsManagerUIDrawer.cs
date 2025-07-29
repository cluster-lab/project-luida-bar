using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using ClusterVR.CreatorKit.Item.Implements;
using ClusterVR.CreatorKit.World.Implements.TextView;

public static class ItemsManagerUIDrawer
{
    private static string docFilePath = "Assets/Doc/LUIDA-StateListeningItemScriptDoc.md";
    private static readonly string codeFontPath = "Assets/Fonts/FiraCode-Regular.ttf";
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
        new StateListeningAction("Send data to collector", "if (!$.groupState.collectedData) $.groupState.collectedData = {};\n    let collectedData = $.groupState.collectedData;\n    collectedData['{_label_}'] = {_value_};\n    $.groupState.collectedData = collectedData;", new[] { "label", "value" }),
        new StateListeningAction("Process and save collected data", "$.sendSignalCompat('this', 'exp_recordCustomData');"),
        new StateListeningAction("Upload collected data", "$.sendSignalCompat('this', 'exp_uploadCustomData');"),
        new StateListeningAction("Set text", "$.subNode('Text').setText(`{_text_}`);", new[] { "text" }),
        new StateListeningAction("Send Haptics",
            "if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0]; \n $.state.player.send('haptics', {target: {_target_}, frequency: {_frequency_}, amplitude: {_amplitude_}, duration: {_duration_}});",
            new[] { "target", "frequency", "amplitude", "duration" }),
        new StateListeningAction("Sleep", "{_seconds_}", new[] { "seconds" }),
    };
    
    private static GUIStyle _codeTextAreaStyle; // Custom style for monospaced font

    // ... (Other methods like DrawGUI, DrawHeader, etc. remain the same) ...
    public static void DrawGUI(ItemsManagerConfigTab editor)
    {
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
        EditorGUILayout.LabelField("New Item Name", GUILayout.Width(120));
        editor.newItemName = EditorGUILayout.TextField(editor.newItemName, GUILayout.Width(180));

        bool isNameInvalid = string.IsNullOrEmpty(editor.newItemName) || editor.stateListeningItems.Any(i => i != null && i.name == editor.newItemName);
        EditorGUI.BeginDisabledGroup(isNameInvalid);

        if (GUILayout.Button("+ Add state-listening item", GUILayout.Width(180)))
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
        EditorGUILayout.LabelField("State Name \\ Item Name", EditorStyles.boldLabel, GUILayout.Width(215));
        GUILayout.Space(5);

        bool isHeaderDarkColumn = true;
        foreach (var item in editor._cachedItems)
        {
            if (item == null) continue;

            GUI.backgroundColor = isHeaderDarkColumn ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.7f, 0.7f, 0.7f);
            EditorGUILayout.BeginHorizontal("box", GUILayout.Width(240));

            EditorGUILayout.LabelField(item.name, EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(item, typeof(GameObject), true);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("X", removeButtonStyle, GUILayout.Width(25), GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("Confirm Removal", $"Are you sure you want to remove '{item.name}' and its associated assets (JS script and StateListenerData asset)?", "Yes, Remove", "No"))
                {
                    ItemsManagerAssetUtil.RemoveStateListeningItem(item, editor);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            isHeaderDarkColumn = !isHeaderDarkColumn;
        }
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
    }
    
    private static void DrawOtherImplementationRow(ItemsManagerConfigTab editor)
    {
        EditorGUILayout.LabelField("Functions, events, variables not listening to the state machine", EditorStyles.largeLabel);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(215));

        EditorGUILayout.HelpBox("DON'T use $.onStart and $.onUpdate here! Implement function Start and Update instead.", MessageType.Warning);
        EditorGUILayout.EndVertical();
        GUILayout.Space(5);

        bool isCellDark = true;
        foreach (var item in editor._cachedItems)
        {
            if (item == null) continue;
            Color cellBgColor = isCellDark ? new Color(0.15f, 0.15f, 0.15f, 0.5f) : new Color(0.75f, 0.75f, 0.75f, 0.5f);
            Rect cellRect = EditorGUILayout.BeginVertical("box", GUILayout.Width(238.5f), GUILayout.MinHeight(80));
            EditorGUI.DrawRect(cellRect, cellBgColor);

            string itemDataAssetPath = ItemsManagerAssetUtil.GetItemDataAssetPath(item);
            var itemDataAsset = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(itemDataAssetPath);

            if (itemDataAsset != null)
            {
                string currentOtherImpl = itemDataAsset.otherImplementation ?? string.Empty;

                Rect taRect = EditorGUILayout.GetControlRect(false, 75, GUILayout.Width(233.5f), GUILayout.MaxHeight(75));
                // MODIFIED CALL: Pass 'isColored: true' for JS code
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
            isCellDark = !isCellDark;
        }
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
    }

    private static void DrawStateRows(ItemsManagerConfigTab editor, GUIStyle removeButtonStyle)
    {
        EditorGUILayout.LabelField("Actions listening to the state machine", EditorStyles.largeLabel, GUILayout.Width(300));

        GUI.backgroundColor = Color.white;
        bool isBlueRow = true;

        foreach (var stateName in editor._cachedStateNames)
        {
            int stateID = Array.IndexOf(editor._cachedStateNames, stateName);
            Color rowBgColor = isBlueRow ? new Color(0.6f, 0.6f, 0.8f, 0.3f) : new Color(0.7f, 0.7f, 0.7f, 0.3f);

            Rect rowRect = EditorGUILayout.BeginHorizontal("box");
            EditorGUI.DrawRect(rowRect, rowBgColor);

            EditorGUILayout.LabelField(stateName, EditorStyles.boldLabel, GUILayout.Width(200), GUILayout.ExpandHeight(true));
            GUILayout.Space(15);

            bool isCellDarkColumn = true;
            foreach (var item in editor._cachedItems)
            {
                if (item == null) continue;
                DrawCell(editor, item, stateName, stateID, isCellDarkColumn, removeButtonStyle);
                isCellDarkColumn = !isCellDarkColumn;
            }

            EditorGUILayout.EndHorizontal();
            isBlueRow = !isBlueRow;
        }
        GUI.backgroundColor = Color.white;
    }

    private static void DrawCell(ItemsManagerConfigTab editor, GameObject item, string stateName, int stateID, bool isDark, GUIStyle removeButtonStyle)
    {
        Color cellBgColor = isDark ? new Color(0.2f, 0.2f, 0.2f, 0.5f) : new Color(0.8f, 0.8f, 0.8f, 0.5f);
        Rect cellRectInner = EditorGUILayout.BeginVertical("box", GUILayout.Width(240), GUILayout.MinHeight(20));
        EditorGUI.DrawRect(cellRectInner, cellBgColor);

        editor.stateListenersByItem.TryGetValue(item, out var listenersList);
        var listener = listenersList?.FirstOrDefault(l => l.stateID == stateID);

        if (listener != null)
        {
            DrawReorderableList(editor, item, stateID, "OnStateStart");
            GUILayout.Space(5);
            DrawReorderableList(editor, item, stateID, "DuringState");
            GUILayout.Space(5);
            DrawReorderableList(editor, item, stateID, "OnStateExit");
            GUILayout.Space(5);

            if (GUILayout.Button("Remove Listener", removeButtonStyle, GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("Confirm Listener Removal", $"Are you sure you want to remove the state listener for state '{stateName}' on item '{item.name}'?", "Yes, Remove", "No"))
                {
                    ItemsManagerAssetUtil.RemoveStateListener(item, stateID, editor);
                    GUIUtility.ExitGUI();
                }
            }
        }
        else
        {
            if (GUILayout.Button("Add Listener", GUILayout.Height(20)))
            {
                ItemsManagerAssetUtil.AddStateListener(item, stateID, editor);
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndVertical();
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
                CreateReorderableList(editor, item, itemDataAsset, listener, listener.onStateStartedActions, "On State Start", "OnStateStart");
                CreateReorderableList(editor, item, itemDataAsset, listener, listener.duringStateActions, "During State", "DuringState");
                CreateReorderableList(editor, item, itemDataAsset, listener, listener.onStateExitedActions, "On State End", "OnStateExit");
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
                float dropdownWidth = availableWidth - ifButtonAndSpacingWidth;
                Rect dropdownRect = new Rect(currentX, currentY, dropdownWidth, lineHeight);

                var options = AvailableStateListeningActions.Select(a => a.actionType).ToList();
                options.Insert(0, "Select Action");
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
                                action.variableValues[varName] = new StateListenerAction(action.predefinedActionTemplate).variableValues[varName];
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

                bool requiresMovableItem = new[] { "Set position", "Add position", "Set rotation", "Add rotation" }.Contains(action.predefinedActionTemplate.actionType);
                if (requiresMovableItem && itemGO.GetComponent<MovableItem>() == null)
                {
                    Rect warningRect = new Rect(rect.x, currentY, rect.width, lineHeight * 2);
                    EditorGUI.HelpBox(warningRect, $"Warning: '{action.predefinedActionTemplate.actionType}' requires a MovableItem component on '{itemGO.name}'.", MessageType.Warning);
                    currentY += lineHeight * 2 + spacing;
                }

                if (action.predefinedActionTemplate.actionType == "Customized Action")
                {
                    Rect textAreaRect = new Rect(rect.x + 15, currentY, rect.width - 15, lineHeight * 3);
                    // MODIFIED CALL: Pass 'isColored: true' for JS code
                    DrawHoverableTextArea(textAreaRect, action.customAction ?? "", (newValue) => {
                        Undo.RecordObject(itemDataAsset, "Edit Custom Action");
                        action.customAction = newValue;
                        EditorUtility.SetDirty(itemDataAsset);
                    }, editor, isColored: true);
                    currentY += lineHeight * 3 + spacing;
                }
                else if (action.predefinedActionTemplate.variables != null && action.predefinedActionTemplate.variables.Length > 0)
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
                                
                                // MODIFIED CALL: Pass 'isColored: false' for plain text
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
                                Rect fieldRect = new Rect(labelRect.xMax, currentY, rect.width - labelRect.width - 15, lineHeight);
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

                bool requiresMovableItem = new[] { "Set position", "Add position", "Set rotation", "Add rotation" }.Contains(action.predefinedActionTemplate.actionType);
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
        
        // NEW: Enable rich text for the style to render color tags.
        _codeTextAreaStyle.richText = true;
    }
    
    private static void DrawHoverableTextArea(Rect rect, string text, Action<string> onUpdate, ItemsManagerConfigTab editor, bool isColored)
    {
        if (_codeTextAreaStyle == null)
        {
            InitializeStyles();
        }

        // Apply syntax highlighting if requested
        string displayText = (isColored && !string.IsNullOrEmpty(text)) ? HighlightJsSyntax(text) : text;

        if (GUI.Button(rect, displayText, _codeTextAreaStyle))
        {
            if (!EditorWindow.HasOpenInstances<TextAreaOverlayWindow>())
            {
                Rect screenRect = GUIUtility.GUIToScreenRect(rect);
                float zoomWidth = Math.Max(450f, screenRect.width * 2f);
                float zoomHeight = Math.Max(200f, screenRect.height * 3f);
                Rect popupRect = new Rect(screenRect.x, screenRect.y, zoomWidth, zoomHeight);

                // Show the popup with the *original, uncolored* text for editing
                TextAreaOverlayWindow.Show(popupRect, text, onUpdate, _codeTextAreaStyle);
            }
        }
    }

    #endregion
    
    #region Syntax Highlighting
    
    // Color definitions (VSCode Dark+ theme inspired)
    private const string JsKeywordColor = "#569CD6";
    private const string JsStringColor = "#CE9178";
    private const string JsCommentColor = "#6A9955";
    private const string JsNumberColor = "#B5CEA8";
    private const string JsFunctionColor = "#DCDCAA";
    private const string JsPunctuationColor = "#D4D4D4";

    // Regex to find different parts of JS syntax using named capture groups
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
        // First, escape any existing angle brackets to prevent them from being treated as rich text tags.
        // code = code.Replace("<", "<noparse><</noparse>").Replace(">", "<noparse>></noparse>");

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
            
            return match.Value; // Return original value if no group matches (shouldn't happen)
        });
    }

    #endregion
    
    #region Documentation

    private static void DrawDocEntry(string actionName, string description, string parametersInfo = null, bool requiresMovableItem = false, string jsFunctionSignature = null)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(20);
        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField(actionName, EditorStyles.boldLabel);
        string helpText = description;
        if (requiresMovableItem)
        {
            helpText += "\n(Requires `MovableItem` component on this item)";
        }
        if (!string.IsNullOrEmpty(parametersInfo) && parametersInfo != "None")
        {
            helpText += $"\nInput Fields (for predefined action): {parametersInfo}";
        }
        if (!string.IsNullOrEmpty(jsFunctionSignature))
        {
            helpText += ("\nEquivalent ClusterScript function: " + jsFunctionSignature);
        }
        else if (actionName == "Sleep")
        {
            helpText += "\nNo equivalent ClusterScript function. If necessary, try adding two Customized actions before and after a Sleep action.";
        }
        EditorGUILayout.HelpBox(helpText, MessageType.None);

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(3);
    }

    #endregion
}
