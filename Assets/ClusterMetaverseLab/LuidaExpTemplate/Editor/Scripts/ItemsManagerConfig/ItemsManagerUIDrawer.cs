using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using System;
using ClusterVR.CreatorKit.Item.Implements;
using ClusterVR.CreatorKit.World.Implements.TextView;

public static class ItemsManagerUIDrawer
{
    private static readonly StateListeningAction[] AvailableStateListeningActions =
    {
        new StateListeningAction("Show item", "$.setStateCompat('this', 'exp_showItem', true);"),
        new StateListeningAction("Hide item", "$.setStateCompat('this', 'exp_showItem', false);"),
        new StateListeningAction("To next state", "$.sendSignalCompat('this', 'state_triggerTransition');"),
        new StateListeningAction("Capture data into collection", "$.sendSignalCompat('this', 'exp_recordCustomData');"),
        new StateListeningAction("Upload collected data", "$.sendSignalCompat('this', 'exp_uploadCustomData');"),
        new StateListeningAction("Set text", "$.subNode('Text').setText(`{_text_}`);", new[] { "text" }),
        new StateListeningAction("Sleep", "{_seconds_}", new[] { "seconds" }),
        new StateListeningAction("Send Haptics",
            "if (!$.state.player) $.state.player = $.getPlayersNear($.getPosition(), Infinity)[0]; \n $.state.player.send('haptics', {target: {_target_}, frequency: {_frequency_}, amplitude: {_amplitude_}, duration: {_duration_}});",
            new[] { "target", "frequency", "amplitude", "duration" }),
        new StateListeningAction("Set position", "$.setPosition(new Vector3({_x_}, {_y_}, {_z_}))", new[] { "x", "y", "z" }),
        new StateListeningAction("Add position", "$.setPosition($.getPosition().add(new Vector3({_x_}, {_y_}, {_z_})))",
            new[] { "x", "y", "z" }),
        new StateListeningAction("Set rotation",
            "$.setRotation(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_})))", new[] { "x", "y", "z" }),
        new StateListeningAction("Add rotation",
            "$.setRotation($.getRotation().multiply(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_}))))",
            new[] { "x", "y", "z" }),
        new StateListeningAction("Show child", "$.subNode('{_childName_}').setEnabled(true)"),
        new StateListeningAction("Hide child", "$.subNode('{_childName_}').setEnabled(false)"),
        new StateListeningAction("Set child's position", "$.subNode('{_childName_}').setPosition(new Vector3({_x_}, {_y_}, {_z_}))", new[] { "childName", "x", "y", "z" }),
        new StateListeningAction("Add child's position", "$.subNode('{_childName_}').setPosition($.subNode('{_childName_}').getPosition().add(new Vector3({_x_}, {_y_}, {_z_})))",
            new[] { "childName", "x", "y", "z" }),
        new StateListeningAction("Set child's rotation",
            "$.subNode('{_childName_}').setRotation(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_})))", new[] { "childName", "x", "y", "z" }),
        new StateListeningAction("Add child's rotation",
            "$.subNode('{_childName_}').setRotation($.subNode('{_childName_}').getRotation().multiply(new Quaternion().setFromEulerAngles(new Vector3({_x_}, {_y_}, {_z_}))))",
            new[] { "childName", "x", "y", "z" }),
    };

    public static void DrawGUI(ItemsManagerConfigTab editor)
    {
        EditorGUI.BeginChangeCheck();

        DrawHeader(editor);

        EditorGUILayout.BeginHorizontal();
        DrawMainGrid(editor);
        DrawDocumentation(editor);
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
        
        editor.scrollPositionY = EditorGUILayout.BeginScrollView(editor.scrollPositionY, false, true, GUILayout.ExpandHeight(true));

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
        EditorGUILayout.LabelField("State Name | Item Name", EditorStyles.boldLabel, GUILayout.Width(215));
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
        EditorGUILayout.LabelField("Custom implementation not listening to any state", EditorStyles.largeLabel);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(215));
        EditorGUILayout.HelpBox("Implement ClusterScript callbacks (e.g., $.onInteract, $.onGrab, ...) or your custom functions here.", MessageType.Info);
        EditorGUILayout.HelpBox("DON'T use $.onUpdate here! Implement function Update instead.", MessageType.Warning);
        EditorGUILayout.EndVertical();
        GUILayout.Space(5);

        bool isCellDark = true;
        foreach (var item in editor._cachedItems)
        {
            if (item == null) continue;
            Color cellBgColor = isCellDark ? new Color(0.25f, 0.25f, 0.25f, 0.5f) : new Color(0.75f, 0.75f, 0.75f, 0.5f);
            Rect cellRect = EditorGUILayout.BeginVertical("box", GUILayout.Width(240), GUILayout.MinHeight(75));
            EditorGUI.DrawRect(cellRect, cellBgColor);

            string itemDataAssetPath = ItemsManagerAssetUtil.GetItemDataAssetPath(item);
            var itemDataAsset = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(itemDataAssetPath);

            if (itemDataAsset != null)
            {
                string currentOtherImpl = itemDataAsset.otherImplementation ?? string.Empty;
                EditorGUI.BeginChangeCheck();
                string newOtherImpl = EditorGUILayout.TextArea(currentOtherImpl, GUILayout.Width(235), GUILayout.MaxHeight(75));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(itemDataAsset, "Edit Other Implementation for " + item.name);
                    itemDataAsset.otherImplementation = newOtherImpl;
                    EditorUtility.SetDirty(itemDataAsset);
                }
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
        EditorGUILayout.LabelField("Pre-defined or customized actions listening to states", EditorStyles.largeLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox("Select available actions to run when entering/during/exiting any state. You can also write your custom scripts by selecting the 'Customized action' option.", MessageType.Info);
        EditorGUILayout.EndHorizontal();

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
                    string newCustomAction = EditorGUI.TextArea(textAreaRect, action.customAction ?? ""); 
                    if (newCustomAction != action.customAction)
                    {
                        Undo.RecordObject(itemDataAsset, "Edit Custom Action");
                        action.customAction = newCustomAction;
                        EditorUtility.SetDirty(itemDataAsset);
                    }
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
                                string newValue = EditorGUI.TextArea(textAreaRect, currentValue);
                                if (newValue != currentValue)
                                {
                                    Undo.RecordObject(itemDataAsset, "Edit Variable " + variableName);
                                    action.variableValues[variableName] = newValue;
                                    EditorUtility.SetDirty(itemDataAsset);
                                }
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
    
    private static void DrawDocumentation(ItemsManagerConfigTab editor)
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(380), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("Documentation for Customized Actions or Other Implementation", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox("Guidance for predefined actions and custom JavaScript functions available in this item manager.", MessageType.None);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("--------------- Variable: CONDITION ---------------", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "⋅ Contains values from your configured experimental variables for the current trial.\n" +
            "⋅ Use `CONDITION[\"your_variable_name\"]` in 'Customized Action' code blocks of trial-related states (e.g., Trial - Start).",
            MessageType.Info);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("--- Predefined Actions & Equivalent ClusterScript Functions ---", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Below are predefined actions selectable from dropdowns, and their equivalent ClusterScript functions you can use in 'Customized Action' code blocks.", MessageType.Info);

        editor.docScrollPositionY = EditorGUILayout.BeginScrollView(editor.docScrollPositionY, false, true, GUILayout.ExpandHeight(true));

        Func<StateListeningAction, string> getJsSignature = (action) => {
            if (action.actionType == "Sleep") return null;
            string funcName = string.Concat(action.actionType.Split(' ').Select(word => char.ToUpper(word[0]) + word.Substring(1)));
            return action.variables != null && action.variables.Length > 0 ? $"{funcName}({string.Join(", ", action.variables)})" : $"{funcName}()";
        };

        Func<StateListeningAction, string> getParamsInfoForUI = (action) => {
            return action.variables != null && action.variables.Length > 0 ? string.Join(", ", action.variables.Select(v => $"`{v}`")) : "None";
        };

        EditorGUILayout.LabelField("Item Visibility", EditorStyles.boldLabel);
        foreach(var action in AvailableStateListeningActions.Where(a => a.actionType.Contains("Show item") || a.actionType.Contains("Hide item")))
        {
            DrawDocEntry(action.actionType, action.actionType.Contains("Show") ? "Makes the item visible." : "Makes the item invisible.", getParamsInfoForUI(action), false, getJsSignature(action));
        }
        
        EditorGUILayout.LabelField("State Flow Control", EditorStyles.boldLabel);
        foreach(var action in AvailableStateListeningActions.Where(a => a.actionType == "To next state"))
        {
            DrawDocEntry(action.actionType, "Triggers a transition to the next experiment state.", getParamsInfoForUI(action), false, getJsSignature(action));
        }

        EditorGUILayout.LabelField("Item Manipulation", EditorStyles.boldLabel);
        foreach(var action in AvailableStateListeningActions.Where(a => new[] {"Set text", "Set position", "Add position", "Set rotation", "Add rotation"}.Contains(a.actionType)))
        {
            string desc = "";
            bool needsMovable = false;
            if (action.actionType == "Set text") desc = "Sets text on a child 'Text' sub-node.";
            else {
                needsMovable = true;
                if (action.actionType == "Set position") desc = "Sets item's world position.";
                else if (action.actionType == "Add position") desc = "Offsets item's world position.";
                else if (action.actionType == "Set rotation") desc = "Sets item's world rotation (Euler degrees).";
                else if (action.actionType == "Add rotation") desc = "Adds to item's world rotation (Euler degrees).";
            }
            DrawDocEntry(action.actionType, desc, getParamsInfoForUI(action), needsMovable, getJsSignature(action));
        }
        
        EditorGUILayout.LabelField("Data Logging", EditorStyles.boldLabel);
        foreach(var action in AvailableStateListeningActions.Where(a => a.actionType.Contains("data")))
        {
            if (action.actionType == "Capture data into collection")
            {
                DrawDocEntry(action.actionType, "Signals LUIDA's DataCollector to capture data as configured in the 'Data Collector' tab of the LUIDA Config Window, and save it to the collected data set.", getParamsInfoForUI(action), false, getJsSignature(action));
            }
            if (action.actionType == "Upload collected data")
            {
                DrawDocEntry(action.actionType, "Signals LUIDA's DataCollector to upload the collected data.", getParamsInfoForUI(action), false, getJsSignature(action));
            }
        }

        EditorGUILayout.LabelField("User Feedback & Utilities", EditorStyles.boldLabel);
        foreach(var action in AvailableStateListeningActions.Where(a => a.actionType == "Send Haptics" || a.actionType == "Sleep"))
        {
            string desc = (action.actionType == "Send Haptics")
                ? "Sends haptic feedback to the player. Target can be \"left\", \"right\", or null (for both hands). Duration is in seconds."
                : "Pauses execution of subsequent actions in the current list for the specified duration in seconds.";
            DrawDocEntry(action.actionType, desc, getParamsInfoForUI(action), false, getJsSignature(action));
        }
    
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
    
    #endregion
}
