using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClusterVR.CreatorKit.Item.Implements;
using ClusterVR.CreatorKit.World.Implements.TextView;

public class ItemsManagerEditor : EditorWindow
{
    private bool _needsRebuild = true;
    private string[] _cachedStateNames = Array.Empty<string>();
    private GameObject[] _cachedItems = Array.Empty<GameObject>();
    private Dictionary<string, ReorderableList> _reorderableLists = new Dictionary<string, ReorderableList>();
    private const string defaultOtherImplementation = @"// function Start() { }
// function Update(deltaTime) { }
// $.onCollide((collision) => { });
// $.onGrab((isGrab, isLeftHand, player) => { });
// $.onInteract((player) => { });
// $.onUse((isDown, player) => { });
// $.onPhysicsUpdate((deltaTime) => { });
// $.onReceive((messageType, arg, sender) => { });
";

    private static StateListeningAction[] AvailableStateListeningActions =
    {
        new StateListeningAction("Show item", "$.setStateCompat('this', 'exp_showItem', true);"),
        new StateListeningAction("Hide item", "$.setStateCompat('this', 'exp_showItem', false);"),
        new StateListeningAction("To next state", "$.sendSignalCompat('this', 'state_triggerTransition');"),
        new StateListeningAction("Record custom data", "$.sendSignalCompat('this', 'exp_recordCustomData');"),
        new StateListeningAction("Upload recorded data", "$.sendSignalCompat('this', 'exp_uploadCustomData');"),
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
    };

    private string newItemName = string.Empty;

    private const string PrefabPath =
        "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/StateListeningItem.prefab";
    private const string ScriptFolderFormat = "Assets/_Experiment_/Scripts/StateManagement/{0}";
    private const string ScriptTemplatePath =
        "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/StateManagement/StateListeningItemTemplate.js";
    private const string RequiredObjectsWrapperPrefabPath =
        "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";

    private List<GameObject> stateListeningItems = new List<GameObject>();
    private StateList stateList = null;

    private Dictionary<GameObject, List<StateListener>> stateListenersByItem =
        new Dictionary<GameObject, List<StateListener>>();
    
    private Vector2 scrollPositionY;
    private Vector2 docScrollPositionY;
    private Vector2 _horizontalScrollPosition;
    private bool isSubscribed = false;

    [Serializable]
    private class EditorExperimentVariable
    {
        public string name;
        public string[] values;
    }
    private List<EditorExperimentVariable> _cachedExperimentVariables = new List<EditorExperimentVariable>();
    private string _experimentVariablesAssetPath;

    #region Unity Callbacks

    public void OnEnable()
    {
        _needsRebuild = true;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.projectChanged += OnProjectChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        if (!isSubscribed)
        {
            TabbedEditor.OnEditorClosed += ApplyAssetsToScripts;
            TabbedEditor.OnEditorClosed += OnDisable;
            TabbedEditor.OnItemsManagerTabLostFocus += ApplyAssetsToScripts;
            isSubscribed = true;
        }
        RefreshStateList();
        RefreshExperimentVariablesCache();
    }

    public void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        if (isSubscribed)
        {
            TabbedEditor.OnEditorClosed -= ApplyAssetsToScripts;
            TabbedEditor.OnEditorClosed -= OnDisable;
            TabbedEditor.OnItemsManagerTabLostFocus -= ApplyAssetsToScripts;
            isSubscribed = false;
        }
    }

    private void OnHierarchyChanged() => _needsRebuild = true;
    private void OnProjectChanged() => _needsRebuild = true;

    #endregion

    private void RefreshStateList()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string listPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
        stateList = AssetDatabase.LoadAssetAtPath<StateList>(listPath);
    }
    
    private void RefreshExperimentVariablesCache()
    {
        _cachedExperimentVariables.Clear();
        string sceneName = SceneManager.GetActiveScene().name;
        _experimentVariablesAssetPath = $"Assets/_Experiment_/Settings/ExperimentVariables/{sceneName}.js";

        if (!File.Exists(_experimentVariablesAssetPath))
        {
            // Debug.LogWarning($"Experiment variables JS file not found at: {_experimentVariablesAssetPath}");
            return;
        }

        string jsContent = File.ReadAllText(_experimentVariablesAssetPath);

        Action<string> parseAndAdd = (varType) =>
        {
            string pattern = $@"const {varType} = \[(.*?)\];";
            Match match = Regex.Match(jsContent, pattern, RegexOptions.Singleline);
            if (match.Success)
            {
                string arrayContent = match.Groups[1].Value;
                var variableMatches = Regex.Matches(arrayContent, @"\{\s*name:\s*""([^""]*)"",\s*values:\s*\[([^\]]*)\][^}]*\}", RegexOptions.Singleline);
                foreach (Match variableMatch in variableMatches)
                {
                    string name = variableMatch.Groups[1].Value;
                    string valuesString = variableMatch.Groups[2].Value;
                    string[] values = string.IsNullOrEmpty(valuesString)
                        ? Array.Empty<string>()
                        : valuesString.Split(',').Select(v => v.Trim().Trim('"')).Where(v => !string.IsNullOrEmpty(v)).ToArray();
                    _cachedExperimentVariables.Add(new EditorExperimentVariable { name = name, values = values });
                }
            }
        };

        parseAndAdd("within_subjects_variables");
        parseAndAdd("between_subjects_variables");
    }

    public void OnGUI()
    {
        GUIStyle removeButtonStyle = new GUIStyle(GUI.skin.button)
        {
            normal = { textColor = Color.red },
            hover = { textColor = Color.red }
        };

        if (_needsRebuild)
        {
            RefreshStateList();
            RefreshExperimentVariablesCache();
            RefreshStateListeningItems();
            _cachedStateNames = stateList != null && stateList.States != null
                ? stateList.States.Select(s => s.StateName).ToArray()
                : Array.Empty<string>();
            _cachedItems = stateListeningItems.ToArray();
            SetupReorderableLists();
            _needsRebuild = false;
        }

        if (stateList == null)
        {
            EditorGUILayout.HelpBox("No StateList asset found for this scene. Create one or check path.", MessageType.Warning);
            if (GUILayout.Button("Attempt to reload StateList")) RefreshStateList();
            return;
        }
        if (_cachedStateNames.Length == 0 && stateList != null)
        {
            EditorGUILayout.HelpBox("StateList is loaded, but no states are defined in it.", MessageType.Info);
        }

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("New Item Name", GUILayout.Width(120));
        newItemName = EditorGUILayout.TextField(newItemName, GUILayout.Width(180));
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newItemName) || stateListeningItems.Any(i => i != null && i.name == newItemName));
        if (GUILayout.Button("+ Add state-listening item", GUILayout.Width(180)))
        {
            CreateStateListeningItem();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);

        // Main horizontal layout for [Scrollable Content | Fixed Documentation]
        EditorGUILayout.BeginHorizontal();

        // --- SCROLLABLE CONTENT (Left Part, max width 1000px) ---
        EditorGUILayout.BeginVertical(GUILayout.MaxWidth(1200f));

        _horizontalScrollPosition = EditorGUILayout.BeginScrollView(_horizontalScrollPosition, false, false, GUILayout.ExpandWidth(true));

        // Item names header (first row of the grid)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("State Name | Item Name", EditorStyles.boldLabel, GUILayout.Width(215));
        bool isHeaderDarkColumn = true;
        GUILayout.Space(5);
        for (int i = 0; i < _cachedItems.Length; i++)
        {
            var item = _cachedItems[i];
            if (item == null) continue; // Skip if item was destroyed
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
                    RemoveStateListeningItem(item);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            isHeaderDarkColumn = !isHeaderDarkColumn;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Inner vertical scroll for the main content grid (states, actions, etc.)
        // No ExpandWidth here, as outer scroll handles horizontal. ExpandHeight to fill vertical space.
        scrollPositionY = EditorGUILayout.BeginScrollView(scrollPositionY, false, true, GUILayout.ExpandHeight(true));

        // --- OTHER IMPLEMENTATION SECTION ---
        EditorGUILayout.LabelField("Custom implementation not listening to any state", EditorStyles.largeLabel);
        EditorGUILayout.BeginHorizontal(); // Horizontal layout for "Other Implementation" columns

        EditorGUILayout.BeginVertical(GUILayout.Width(215));
        EditorGUILayout.HelpBox("Implement ClusterScript callbacks (e.g., $.onInteract, $.onGrab, ...) or your custom functions here.", MessageType.Info);
        EditorGUILayout.HelpBox("DON'T use $.onUpdate here! Implement function Update instead.", MessageType.Warning);
        EditorGUILayout.EndVertical();
        // EditorGUILayout.LabelField("", GUILayout.Width(215)); // Spacer for the first column
        GUILayout.Space(5);

        bool isOtherImplCellDark = true;
        foreach (var item in _cachedItems) // _cachedItems is populated in _needsRebuild block
        {
            if (item == null) continue;
            Color cellBgColor = isOtherImplCellDark ? new Color(0.25f, 0.25f, 0.25f, 0.5f) : new Color(0.75f, 0.75f, 0.75f, 0.5f);
            Rect cellRect = EditorGUILayout.BeginVertical("box", GUILayout.Width(240), GUILayout.MinHeight(75));
            EditorGUI.DrawRect(cellRect, cellBgColor);

            // Directly load and use the StateListeningItemData asset
            string itemDataAssetPath = GetItemDataAssetPath(item);
            var itemDataAsset = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(itemDataAssetPath);

            if (itemDataAsset == null)
            {
                EditorGUILayout.HelpBox($"Asset not found for {item.name}. Cannot edit 'Other Implementation'.", MessageType.Error);
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
                isOtherImplCellDark = !isOtherImplCellDark;
                continue;
            }

            string currentOtherImpl = itemDataAsset.otherImplementation ?? string.Empty;

            EditorGUI.BeginChangeCheck();
            string newOtherImpl = EditorGUILayout.TextArea(currentOtherImpl, GUILayout.Width(235), GUILayout.MaxHeight(75));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(itemDataAsset, "Edit Other Implementation for " + item.name);
                itemDataAsset.otherImplementation = newOtherImpl; // Directly modify the ScriptableObject
                EditorUtility.SetDirty(itemDataAsset); // Mark asset dirty
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            isOtherImplCellDark = !isOtherImplCellDark;
        }
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
        // --- END OF OTHER IMPLEMENTATION SECTION ---

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("Pre-defined or customized actions listening to states", EditorStyles.largeLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox("Select available actions to run when entering/during/exiting any state. You can also write your custom scripts by selecting the 'Customized action' option.", MessageType.Info);
        // EditorGUILayout.HelpBox("DON'T implement any ClusterScript callbacks here.", MessageType.Warning);
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = Color.white;
        bool isBlueRow = true;
        foreach (var stateName in _cachedStateNames)
        {
            int stateID = Array.IndexOf(_cachedStateNames, stateName);
            Color rowBgColor = isBlueRow ? new Color(0.6f, 0.6f, 0.8f, 0.3f) : new Color(0.7f, 0.7f, 0.7f, 0.3f);

            Rect rowRect = EditorGUILayout.BeginHorizontal("box");
            EditorGUI.DrawRect(rowRect, rowBgColor);

            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            EditorGUILayout.LabelField(stateName, EditorStyles.boldLabel, GUILayout.ExpandHeight(true), GUILayout.MinWidth(190));
            EditorGUILayout.EndVertical();
            GUILayout.Space(15);

            bool isCellDarkColumn = true;
            foreach (var item in _cachedItems)
            {
                if (item == null) continue;
                Color cellBgColor = isCellDarkColumn ? new Color(0.2f, 0.2f, 0.2f, 0.5f) : new Color(0.8f, 0.8f, 0.8f, 0.5f);
                Rect cellRectInner = EditorGUILayout.BeginVertical("box", GUILayout.Width(240), GUILayout.MinHeight(50));
                EditorGUI.DrawRect(cellRectInner, cellBgColor);

                stateListenersByItem.TryGetValue(item, out var listenersList);
                var listener = listenersList?.FirstOrDefault(l => l.stateID == stateID);

                if (listener != null)
                {
                    DrawReorderableList(item, stateID, "OnStateStart", "On State Start Actions");
                    GUILayout.Space(5);
                    DrawReorderableList(item, stateID, "DuringState", "During State Actions");
                    GUILayout.Space(5);
                    DrawReorderableList(item, stateID, "OnStateExit", "On State End Actions");

                    GUILayout.Space(5);
                    if (GUILayout.Button("Remove Listener", removeButtonStyle, GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Confirm Listener Removal",
                                $"Are you sure you want to remove the state listener for state '{stateName}' on item '{item.name}'?",
                                "Yes, Remove",
                                "No"))
                        {
                            string itemDataAssetPath = GetItemDataAssetPath(item);
                            var itemDataAsset = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(itemDataAssetPath);

                            if (itemDataAsset != null) {
                                Undo.RecordObject(itemDataAsset, "Remove State Listener");
                            }

                            if (listenersList != null) {
                                listenersList.Remove(listener);
                            }

                            SaveItemToAsset(item);

                            _needsRebuild = true;
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("Add Listener", GUILayout.Height(40)))
                    {
                        AddStateListener(stateID, item);
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
                isCellDarkColumn = !isCellDarkColumn;
            }
            EditorGUILayout.EndHorizontal();
            isBlueRow = !isBlueRow;
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView(); // End inner vertical scroll (scrollPositionY)

        EditorGUILayout.EndScrollView(); // End outer horizontal scroll (_horizontalScrollPosition)

        EditorGUILayout.EndVertical(); // End scrollable part

        // --- FIXED DOCUMENTATION PART ---
        DrawDocumentation();

        EditorGUILayout.EndHorizontal(); // End main container

        if (EditorGUI.EndChangeCheck())
        {
            // Changes are primarily saved via Undo/SetDirty and explicit save calls.
        }
    }

    private bool IsTrialRelatedState(int stateID)
    {
        if (stateList == null || stateList.States == null || stateList.States.Length == 0)
        {
            return false;
        }

        if (stateID < 0 || stateID >= stateList.States.Length)
        {
            return false;
        }

        int trialStartIndex = -1;
        int trialRestIndex = -1;

        for (int i = 0; i < stateList.States.Length; i++)
        {
            if (stateList.States[i].StateName.Equals("Trial - Start", StringComparison.OrdinalIgnoreCase))
            {
                trialStartIndex = i;
            }
            else if (stateList.States[i].StateName.Equals("Trial - Rest", StringComparison.OrdinalIgnoreCase))
            {
                trialRestIndex = i;
            }
        }
        
        if (trialStartIndex == -1 || trialRestIndex == -1)
        {
            return false;
        }
        if (trialStartIndex > trialRestIndex)
        {
            return false;
        }
        return stateID >= trialStartIndex && stateID <= trialRestIndex;
    }

    #region ReorderableList Setup & Draw

    private void SetupReorderableLists()
    {
        _reorderableLists.Clear();
        foreach (var item in stateListeningItems)
        {
            if (item == null || !stateListenersByItem.TryGetValue(item, out var listeners)) continue;

            string itemDataAssetPath = GetItemDataAssetPath(item);
            StateListeningItemData itemDataAsset = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(itemDataAssetPath);
            if (itemDataAsset == null)
            {
                // Debug.LogWarning($"StateListeningItemData asset not found for {item.name} at {itemDataAssetPath} during ReorderableList setup.");
                continue;
            }

            foreach (var listener in listeners)
            {
                CreateReorderableList(item, itemDataAsset, listener, listener.onStateStartedActions, "On State Start", "OnStateStart", listener.stateID);
                CreateReorderableList(item, itemDataAsset, listener, listener.duringStateActions, "During State", "DuringState", listener.stateID);
                CreateReorderableList(item, itemDataAsset, listener, listener.onStateExitedActions, "On State End", "OnStateExit", listener.stateID);
            }
        }
    }

    private void CreateReorderableList(GameObject itemGO, StateListeningItemData itemDataAsset, StateListener listener, List<StateListenerAction> actions, string header, string keySuffix, int stateIdForConditionalUI)
    {
        var key = $"{itemGO.GetInstanceID()}_{listener.stateID}_{keySuffix}";
        bool isCurrentStateTrialRelated = IsTrialRelatedState(stateIdForConditionalUI);

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
                float currentX = rect.x; // Start X position for drawing elements on the current line
                float availableWidth = rect.width; // Total available width for the element

                // Calculate width needed for the "If" button if it's shown
                float ifButtonAndSpacingWidth = 0;
                if (isCurrentStateTrialRelated)
                {
                    ifButtonAndSpacingWidth = 35 + spacing; // 35 for button, spacing for after
                }

                // Action Dropdown
                // Adjust width to leave space for the "If" button if it's present on the right
                float dropdownWidth = availableWidth - ifButtonAndSpacingWidth;
                Rect dropdownRect = new Rect(currentX, currentY, dropdownWidth, lineHeight);
                
                var options = AvailableStateListeningActions.Select(a => a.actionType).ToList();
                options.Insert(0, "Select Action");
                options.Add("Customized Action");

                int selectedIndex = 0; 
                if (!string.IsNullOrEmpty(action.predefinedActionTemplate.actionType)) {
                    selectedIndex = (action.predefinedActionTemplate.actionType == "Customized Action")
                        ? options.Count -1
                        : AvailableStateListeningActions.ToList().FindIndex(a => a.actionType == action.predefinedActionTemplate.actionType) + 1;
                    if (selectedIndex < 0) selectedIndex = 0; 
                }
                
                int newIndex = EditorGUI.Popup(dropdownRect, selectedIndex, options.ToArray());
                
                // Update currentX to position the "If" button next to the dropdown
                currentX += dropdownWidth + spacing;

                // "If" button for trial-related states (drawn after the dropdown)
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
                currentY += lineHeight + spacing; // Move to next line for subsequent elements

                // Conditional UI (drawn below the first line, starting from rect.x for alignment)
                if (isCurrentStateTrialRelated && action.isConditional)
                {
                    // Dropdown for CONDITION variables
                    Rect varLabelRect = new Rect(rect.x + 15, currentY, EditorGUIUtility.labelWidth * 0.7f, lineHeight);
                    Rect varDropdownRect = new Rect(varLabelRect.xMax, currentY, rect.width - varLabelRect.width - 15, lineHeight);
                    EditorGUI.LabelField(varLabelRect, "Var Name");

                    var conditionVarNames = new List<string> { "[Select Variable]" };
                    if (_cachedExperimentVariables != null) // Guard against null
                    {
                        conditionVarNames.AddRange(_cachedExperimentVariables.Select(v => v.name).Distinct());
                    }
                    
                    int selectedVarIndex = 0; 
                    if (!string.IsNullOrEmpty(action.conditionVariable))
                    {
                        selectedVarIndex = conditionVarNames.IndexOf(action.conditionVariable);
                        if (selectedVarIndex == -1) selectedVarIndex = 0; 
                    }
                    
                    int newSelectedVarIndex = EditorGUI.Popup(varDropdownRect, selectedVarIndex, conditionVarNames.ToArray());
                    currentY += lineHeight + spacing;

                    if (newSelectedVarIndex != selectedVarIndex)
                    {
                        Undo.RecordObject(itemDataAsset, "Change Condition Variable");
                        action.conditionVariable = (newSelectedVarIndex > 0) ? conditionVarNames[newSelectedVarIndex] : null;
                        action.conditionValue = null; 
                        EditorUtility.SetDirty(itemDataAsset);
                    }

                    // Dropdown for selected variable's values
                    if (!string.IsNullOrEmpty(action.conditionVariable) && newSelectedVarIndex > 0)
                    {
                        EditorExperimentVariable selectedExpVar = _cachedExperimentVariables?.FirstOrDefault(v => v.name == action.conditionVariable);
                        if (selectedExpVar != null && selectedExpVar.values != null && selectedExpVar.values.Length > 0)
                        {
                            Rect valLabelRect = new Rect(rect.x + 15, currentY, EditorGUIUtility.labelWidth * 0.7f, lineHeight);
                            Rect valDropdownRect = new Rect(valLabelRect.xMax, currentY, rect.width - valLabelRect.width - 15, lineHeight);
                            EditorGUI.LabelField(valLabelRect, "Is Value");

                            var conditionValOptions = new List<string> { "[Select Value]" };
                            conditionValOptions.AddRange(selectedExpVar.values);

                            int selectedValIndex = 0; 
                            if (action.conditionValue != null) 
                            {
                                selectedValIndex = conditionValOptions.IndexOf(action.conditionValue);
                                if (selectedValIndex == -1) selectedValIndex = 0;
                            }
                            
                            int newSelectedValIndex = EditorGUI.Popup(valDropdownRect, selectedValIndex, conditionValOptions.ToArray());
                            currentY += lineHeight + spacing;

                            if (newSelectedValIndex != selectedValIndex)
                            {
                                Undo.RecordObject(itemDataAsset, "Change Condition Value");
                                action.conditionValue = (newSelectedValIndex > 0) ? conditionValOptions[newSelectedValIndex] : null;
                                EditorUtility.SetDirty(itemDataAsset);
                            }
                        }
                        else if (selectedExpVar != null) // Variable exists but has no values
                        {
                            Rect noValuesRect = new Rect(rect.x + 15, currentY, rect.width - 15, lineHeight);
                            EditorGUI.HelpBox(noValuesRect, $"Variable '{action.conditionVariable}' has no defined values.", MessageType.Info);
                            currentY += lineHeight + spacing;
                        }
                    }
                }
                
                // Logic for changing action type (needs to happen after newIndex is determined but before variable/custom action fields)
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
                        
                        // --- Ensure Text child and TextView for 'Set text' action ---
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
                            else
                            {
                                if (textChild.GetComponent<TextView>() == null)
                                {
                                    Undo.AddComponent<TextView>(textChild.gameObject);
                                }
                            }
                        }
                    }
                    EditorUtility.SetDirty(itemDataAsset);
                }
                
                // MovableItem warning
                bool requiresMovableItem = action.predefinedActionTemplate.actionType == "Set position" ||
                                           action.predefinedActionTemplate.actionType == "Add position" ||
                                           action.predefinedActionTemplate.actionType == "Set rotation" ||
                                           action.predefinedActionTemplate.actionType == "Add rotation";
                if (requiresMovableItem && itemGO.GetComponent<MovableItem>() == null)
                {
                    Rect warningRect = new Rect(rect.x, currentY, rect.width, lineHeight * 2); 
                    EditorGUI.HelpBox(warningRect, $"Warning: '{action.predefinedActionTemplate.actionType}' requires a MovableItem component on '{itemGO.name}'.", MessageType.Warning);
                    currentY += lineHeight * 2 + spacing;
                }

                // Custom Action TextArea or Predefined Action Variables
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
                    foreach (string variableName in action.predefinedActionTemplate.variables)
                    {
                        Rect labelRect = new Rect(rect.x + 15, currentY, EditorGUIUtility.labelWidth * 0.6f, lineHeight);
                        Rect fieldRect = new Rect(labelRect.xMax, currentY, rect.width - labelRect.width - 15, lineHeight);

                        EditorGUI.LabelField(labelRect, variableName);
                        action.variableValues.TryGetValue(variableName, out string currentValue);
                        currentValue ??= "";

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
            },
            elementHeightCallback = index =>
            {
                if (actions == null || index < 0 || index >= actions.Count) return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                var action = actions[index];
                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float height = lineHeight + spacing; // For the action type dropdown (and "If" button on the same line)

                if (isCurrentStateTrialRelated && action.isConditional)
                {
                    height += lineHeight + spacing; // For Condition Variable dropdown
                    if (!string.IsNullOrEmpty(action.conditionVariable))
                    {
                        EditorExperimentVariable selectedExpVar = _cachedExperimentVariables?.FirstOrDefault(v => v.name == action.conditionVariable);
                        if (selectedExpVar != null && selectedExpVar.values != null && selectedExpVar.values.Length > 0)
                        {
                            height += lineHeight + spacing; // For Condition Value dropdown
                        }
                        else if (selectedExpVar != null) // Variable exists but no values
                        {
                            height += lineHeight + spacing; // For "no values" HelpBox
                        }
                    }
                }
                
                bool requiresMovableItem = action.predefinedActionTemplate.actionType == "Set position" ||
                                           action.predefinedActionTemplate.actionType == "Add position" ||
                                           action.predefinedActionTemplate.actionType == "Set rotation" ||
                                           action.predefinedActionTemplate.actionType == "Add rotation";

                if (requiresMovableItem && itemGO.GetComponent<MovableItem>() == null)
                {
                     height += lineHeight * 2 + spacing; 
                }
                
                if (action.predefinedActionTemplate.actionType == "Customized Action")
                {
                    height += lineHeight * 3 + spacing; 
                }
                else if (action.predefinedActionTemplate.variables != null)
                {
                    height += (lineHeight + spacing) * action.predefinedActionTemplate.variables.Length; 
                }
                return height + spacing; // Extra bottom spacing
            }
        };

        rl.onAddCallback = list =>
        {
            Undo.RecordObject(itemDataAsset, "Add Action");
            actions.Add(new StateListenerAction());
            EditorUtility.SetDirty(itemDataAsset);
        };
        rl.onRemoveCallback = list =>
        {
            Undo.RecordObject(itemDataAsset, "Remove Action");
            if (list.index >= 0 && list.index < actions.Count) // Add bounds check for safety
            {
                actions.RemoveAt(list.index);
            }
            EditorUtility.SetDirty(itemDataAsset);
        };
        rl.onReorderCallback = list =>
        {
            Undo.RecordObject(itemDataAsset, "Reorder Actions");
            EditorUtility.SetDirty(itemDataAsset);
        };
        _reorderableLists[key] = rl;
    }
    
    private void DrawReorderableList(GameObject item, int stateID, string keySuffix, string header)
    {
        var key = $"{item.GetInstanceID()}_{stateID}_{keySuffix}";
        if (_reorderableLists.TryGetValue(key, out var rl))
        {
            rl.DoLayoutList();
        }
    }

    #endregion

    #region Helper methods

    private string GetItemDataAssetPath(GameObject item)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string folder = string.Format(ScriptFolderFormat, sceneName) + "/StateListeners";
        return Path.Combine(folder, item.name + ".asset");
    }

    private void RefreshStateListeningItems()
    {
        stateListeningItems.Clear();
        var currentItemsInScene = new List<GameObject>();

        var allSceneRootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        List<GameObject> potentialItems = new List<GameObject>();
        foreach (var rootGO in allSceneRootGameObjects)
        {
            potentialItems.AddRange(rootGO.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject));
        }

        foreach (var obj in potentialItems.Distinct())
        {
            if (obj == null) continue;
            string prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
            if (prefabAssetPath == PrefabPath)
            {
                if (!currentItemsInScene.Contains(obj))
                    currentItemsInScene.Add(obj);
            }
        }
        stateListeningItems = currentItemsInScene;

        stateListenersByItem.Clear();

        string sceneName = SceneManager.GetActiveScene().name;
        string baseFolder = string.Format(ScriptFolderFormat, sceneName);
        string listenerDataFolder = Path.Combine(baseFolder, "StateListeners");
        Directory.CreateDirectory(listenerDataFolder);

        foreach (var item in stateListeningItems)
        {
            if (item == null) continue;
            string assetPath = GetItemDataAssetPath(item);
            StateListeningItemData data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);
            bool newAssetCreated = false;
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<StateListeningItemData>();
                data.stateListeners = Array.Empty<StateListener>();
                data.otherImplementation = defaultOtherImplementation;
                AssetDatabase.CreateAsset(data, assetPath);
                AssetDatabase.SaveAssets();
                newAssetCreated = true;
            }
            stateListenersByItem[item] = data.stateListeners != null ? data.stateListeners.ToList() : new List<StateListener>();
        }
    }

    private void AddStateListener(int stateIndex, GameObject item)
    {
        if (item == null) return;
        if (!stateListenersByItem.ContainsKey(item))
        {
            stateListenersByItem[item] = new List<StateListener>();
        }

        if (stateListenersByItem[item].Any(l => l.stateID == stateIndex))
        {
            EditorUtility.DisplayDialog("Error", $"Listener for state ID {stateIndex} already exists on item '{item.name}'.", "OK");
            return;
        }

        string itemDataAssetPath = GetItemDataAssetPath(item);
        var itemDataAsset = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(itemDataAssetPath);
        if (itemDataAsset == null)
        {
            Debug.LogError($"Could not find or load StateListeningItemData for {item.name} at {itemDataAssetPath}. Listener not added.");
            return;
        }

        Undo.RecordObject(itemDataAsset, "Add State Listener");

        var newListener = new StateListener { stateID = stateIndex };
        stateListenersByItem[item].Add(newListener);

        List<StateListener> currentListeners = itemDataAsset.stateListeners != null ? itemDataAsset.stateListeners.ToList() : new List<StateListener>();
        currentListeners.Add(newListener);
        itemDataAsset.stateListeners = currentListeners.ToArray();

        EditorUtility.SetDirty(itemDataAsset);
        _needsRebuild = true;
    }

    private void RemoveStateListeningItem(GameObject item)
    {
        if (item == null) return;
        string scene = SceneManager.GetActiveScene().name;
        string folder = string.Format(ScriptFolderFormat, scene);
        string jsPath = Path.Combine(folder, item.name + ".js");
        string assetPath = GetItemDataAssetPath(item);

        AssetDatabase.DeleteAsset(jsPath);
        AssetDatabase.DeleteAsset(assetPath);
        Undo.DestroyObjectImmediate(item);

        if (stateListenersByItem.ContainsKey(item)) stateListenersByItem.Remove(item);
        
        AssetDatabase.Refresh();
        _needsRebuild = true;
    }

    private void CreateStateListeningItem()
    {
        if (string.IsNullOrEmpty(newItemName))
        {
            EditorUtility.DisplayDialog("Error", "New item name cannot be empty.", "OK");
            return;
        }
        if (stateListeningItems.Any(i => i != null && i.name == newItemName))
        {
            EditorUtility.DisplayDialog("Error", $"An item named '{newItemName}' already exists.", "OK");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at path: {PrefabPath}");
            return;
        }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = newItemName;
        Undo.RegisterCreatedObjectUndo(go, "Create StateListeningItem " + newItemName);

        EnableAccessToConditions(go);

        string scene = SceneManager.GetActiveScene().name;
        string scriptFolder = string.Format(ScriptFolderFormat, scene);
        Directory.CreateDirectory(scriptFolder);

        string jsPath = Path.Combine(scriptFolder, newItemName + ".js");
        if (!File.Exists(ScriptTemplatePath))
        {
            Debug.LogError($"Script template not found at: {ScriptTemplatePath}");
            Undo.DestroyObjectImmediate(go);
            return;
        }
        AssetDatabase.CopyAsset(ScriptTemplatePath, jsPath);
        AssetDatabase.Refresh();

        ScriptableClusterScriptCombiner combiner = go.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner == null)
        {
            Debug.LogError($"ScriptableClusterScriptCombiner not found on instantiated prefab '{go.name}'. Cannot assign script.");
        }
        else
        {
            var newScriptAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(jsPath);
            if (newScriptAsset == null)
            {
                Debug.LogError($"Failed to load newly created JavaScriptAsset at '{jsPath}'.");
            }
            else
            {
                combiner.ReplaceScript(newScriptAsset, 1, null, 0, true);
                EditorUtility.SetDirty(combiner);
                EditorUtility.SetDirty(newScriptAsset);
            }
        }

        string listenerDataFolder = Path.Combine(scriptFolder, "StateListeners");
        Directory.CreateDirectory(listenerDataFolder);
        string assetPath = Path.Combine(listenerDataFolder, newItemName + ".asset");
        StateListeningItemData data = ScriptableObject.CreateInstance<StateListeningItemData>();
        data.stateListeners = Array.Empty<StateListener>();
        data.otherImplementation = defaultOtherImplementation;
        AssetDatabase.CreateAsset(data, assetPath);
        AssetDatabase.SaveAssets();

        if (!stateListeningItems.Contains(go)) stateListeningItems.Add(go);
        stateListenersByItem[go] = data.stateListeners.ToList();

        _needsRebuild = true;
        newItemName = string.Empty;
    }

    private string GenerateActionObject(StateListenerAction action)
    {
        string actionCode = action.GetActionContent();

        if (action.predefinedActionTemplate.actionType == "Sleep")
        {
            bool isNumeric = double.TryParse(actionCode, out double sleepValue);
            return $"{{ type: \"sleep\", value: {(isNumeric ? sleepValue.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0")} }}";
        }

        actionCode = (actionCode ?? "").Trim().Replace("\n", "\n            ");
        return $"{{ type: \"exec\", action: () => {{\n            {actionCode}\n        }} }}";
    }

    private string GenerateActionsObjectsForItem(GameObject item)
    {
        if (item == null) return string.Empty;

        stateListenersByItem.TryGetValue(item, out var listeners);
        listeners ??= new List<StateListener>();

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("const stateEnterActions = {");
        AppendActionsForType(sb, listeners, l => l.onStateStartedActions);
        sb.AppendLine("};\n");

        sb.AppendLine("const duringStateActions = {");
        AppendActionsForType(sb, listeners, l => l.duringStateActions);
        sb.AppendLine("};\n");

        sb.AppendLine("const stateExitActions = {");
        AppendActionsForType(sb, listeners, l => l.onStateExitedActions);
        sb.AppendLine("};");

        return sb.ToString();
    }

    private void AppendActionsForType(System.Text.StringBuilder sb, List<StateListener> listeners, Func<StateListener, List<StateListenerAction>> actionSelector)
    {
        bool hasAppendedAnyState = false;
        foreach (var listener in listeners)
        {
            var actions = actionSelector(listener);
            if (actions == null || actions.Count == 0) continue;

            if (hasAppendedAnyState) sb.AppendLine(",");
            sb.Append($"    {listener.stateID}: [\n");
            for (int i = 0; i < actions.Count; i++)
            {
                sb.Append($"        {GenerateActionObject(actions[i])}");
                if (i < actions.Count - 1) sb.AppendLine(","); else sb.AppendLine();
            }
            sb.Append("    ]");
            hasAppendedAnyState = true;
        }
        if (hasAppendedAnyState) sb.AppendLine();
    }

    private void SaveItemToAsset(GameObject item)
    {
        if (!item) return;

		if (stateListenersByItem.TryGetValue(item, out var listenersList))
        {
            int removedCount = listenersList.RemoveAll(listener => listener.stateID == -1);
        }

        string scene = SceneManager.GetActiveScene().name;
        string folder = string.Format(ScriptFolderFormat, scene);
        Directory.CreateDirectory(folder);

        string jsContentForItem = GenerateActionsObjectsForItem(item);

        string assetPath = GetItemDataAssetPath(item);
        var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);

        string otherImplForJS = string.Empty;
        if (data != null) {
            otherImplForJS = data.otherImplementation ?? string.Empty; // Read directly from asset for JS
        } else {
            // This case should ideally not happen if RefreshStateListeningItems ensures assets exist.
            // If an item is in stateListeningItems, its asset should have been loaded or created.
            Debug.LogWarning($"StateListeningItemData asset not found for {item.name} at {assetPath} during JS generation. 'Other Implementation' will be empty in JS.");
        }
        
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(jsContentForItem)) lines.Add(jsContentForItem);
        if (!string.IsNullOrWhiteSpace(otherImplForJS)) lines.Add(otherImplForJS);

        string jsPath = Path.Combine(folder, item.name + ".js");
        File.WriteAllText(jsPath, string.Join("\n\n", lines).Trim());
        AssetDatabase.ImportAsset(jsPath, ImportAssetOptions.ForceUpdate); 

        // Asset data for 'otherImplementation' is modified directly in OnGUI and SetDirty.
        // Here, we only need to handle stateListeners.
        if (data != null)
        {
            bool listenersChanged = false;
            if (stateListenersByItem.TryGetValue(item, out var currentListenersInDict))
            {
                var dataStateListeners = data.stateListeners ?? Array.Empty<StateListener>();
                var currentListenersArray = currentListenersInDict?.ToArray() ?? Array.Empty<StateListener>();

                if (!dataStateListeners.SequenceEqual(currentListenersArray))
                {
                    Undo.RecordObject(data, "Update State Listeners in Asset for " + item.name);
                    data.stateListeners = currentListenersArray;
                    listenersChanged = true;
                }
            }
            if (listenersChanged) {
                EditorUtility.SetDirty(data);
            }
            // No explicit save of data.otherImplementation here as it's handled by OnGUI's direct modification.
        }
        else
        {
            // This was already logged above for JS generation.
            // If data is null here, it means the asset is missing, which Refresh should handle.
            // If item still exists but asset gone, _needsRebuild might be appropriate.
        }
    }

    private void SaveAllItemsToAssets()
    {
        var validItems = stateListeningItems.Where(item => item != null).ToList();
        foreach (var item in validItems)
        {
            SaveItemToAsset(item);
        }
        AssetDatabase.SaveAssets();
    }

    private void ApplyAssetsToScripts()
    {
        SaveAllItemsToAssets();

        Type csCombinerType = Type.GetType("Assets.KaomoLab.CSCombiner.CSCombiner, Assembly-CSharp-Editor");
        if (csCombinerType != null)
        {
            var method = csCombinerType.GetMethod("CombineAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method != null)
            {
                method.Invoke(null, null);
            }
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            var luidaWindow = Resources.FindObjectsOfTypeAll<TabbedEditor>().FirstOrDefault();
            if (luidaWindow != null) luidaWindow.Close();
            RefreshStateListeningItems();
            ApplyAssetsToScripts();
        }
    }

    #endregion

    #region Helper methods to enable access to experimental conditions

    private ItemGroupHost FindItemGroupHostInScene()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == RequiredObjectsWrapperPrefabPath)
            {
                ItemGroupHost host = obj.GetComponentInChildren<ItemGroupHost>(true);
                if (host != null)
                {
                    return host;
                }
            }
        }
        var allHosts = FindObjectsOfType<ItemGroupHost>(true);
        if (allHosts.Length > 0)
        {
            if (allHosts.Length > 1)
            {
                Debug.LogWarning("Multiple ItemGroupHost components found in the scene. Attempting to link to the first one found. " +
                                 "For more precise linking, ensure it's part of the ExpTemplateRequiredObjects prefab structure.");
            }
            return allHosts[0];
        }
        return null;
    }

    private void EnableAccessToConditions(GameObject item)
    {
        if (item == null) return;

        var itemGroupMember = item.GetComponent<ItemGroupMember>();
        if (itemGroupMember == null)
        {
            Debug.LogWarning($"ItemGroupMember component not found on the newly created item: {item.name}. Cannot link to ItemGroupHost.");
            return;
        }

        ItemGroupHost host = FindItemGroupHostInScene();
        if (host != null)
        {
            SerializedObject serializedItemGroupMember = new SerializedObject(itemGroupMember);
            SerializedProperty hostProperty = serializedItemGroupMember.FindProperty("host");

            if (hostProperty != null)
            {
                hostProperty.objectReferenceValue = host;
                serializedItemGroupMember.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogError($"Unable to find 'host' property in ItemGroupMember on {item.name}.");
            }
        }
        else
        {
            Debug.LogWarning("ItemGroupHost not found in the scene (expected within a GameObject instantiated from '" + RequiredObjectsWrapperPrefabPath + "'). " +
                             $"{item.name} will not be able to access shared group states for conditions.");
        }
    }

    #endregion

    #region Documentation
    
    private void DrawDocEntry(string actionName, string description, string parametersInfo = null, bool requiresMovableItem = false, string jsFunctionSignature = null)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(20); // Indent for sub-sections
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
        else if (actionName == "Sleep") // Special handling for Sleep
        {
            helpText += "\nNo equivalent ClusterScript function. If necessary, try adding two Customized actions before and after a Sleep action.";
        }
        EditorGUILayout.HelpBox(helpText, MessageType.None);

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(3);
    }
    
    private void DrawDocumentation()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(380), GUILayout.ExpandHeight(true)); // Slightly wider for more text
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
        EditorGUILayout.HelpBox(
            "Below are predefined actions selectable from dropdowns, and their equivalent ClusterScript functions you can use in 'Customized Action' code blocks. ",
            MessageType.Info);

        docScrollPositionY = EditorGUILayout.BeginScrollView(docScrollPositionY, false, true, GUILayout.ExpandHeight(true));

        // Helper to generate JS function signature string from action.actionType and action.variables
        Func<StateListeningAction, string> getJsSignature = (action) => {
            if (action.actionType == "Sleep") return null; // Sleep is handled differently

            // Sanitize actionType to a valid JS function name (PascalCase, no spaces)
            string funcName = string.Concat(action.actionType.Split(' ').Select(word => char.ToUpper(word[0]) + word.Substring(1)));
            
            if (action.variables != null && action.variables.Length > 0)
            {
                return $"{funcName}({string.Join(", ", action.variables)})";
            }
            return $"{funcName}()";
        };

        // Helper to get parameter info string for UI fields
        Func<StateListeningAction, string> getParamsInfoForUI = (action) => {
            if (action.variables != null && action.variables.Length > 0)
            {
                return string.Join(", ", action.variables.Select(v => $"`{v}`"));
            }
            return "None";
        };
        
        // Iterate through AvailableStateListeningActions, grouped by assumed category
        // (You might want to add an explicit category field to StateListeningAction struct for more robust grouping)

        EditorGUILayout.LabelField("Item Visibility", EditorStyles.boldLabel);
        foreach(var action in AvailableStateListeningActions.Where(a => a.actionType == "Show item" || a.actionType == "Hide item"))
        {
            DrawDocEntry(action.actionType, 
                         action.actionType == "Show item" ? "Makes the item visible." : "Makes the item invisible.",
                         getParamsInfoForUI(action), false, getJsSignature(action));
        }
        
        EditorGUILayout.LabelField("State Flow Control", EditorStyles.boldLabel);
        foreach(var action in AvailableStateListeningActions.Where(a => a.actionType == "To next state"))
        {
            DrawDocEntry(action.actionType, "Triggers a transition to the next experiment state.",
                         getParamsInfoForUI(action), false, getJsSignature(action));
        }

        EditorGUILayout.LabelField("Item Manipulation", EditorStyles.boldLabel);
        foreach(var action in AvailableStateListeningActions.Where(a => new[] {"Set text", "Set position", "Add position", "Set rotation", "Add rotation"}.Contains(a.actionType)))
        {
            string desc = "";
            bool needsMovable = false;
            if (action.actionType == "Set text") desc = "Sets text on a child 'Text' sub-node.";
            else if (action.actionType == "Set position") { desc = "Sets item's world position."; needsMovable = true; }
            else if (action.actionType == "Add position") { desc = "Offsets item's world position."; needsMovable = true; }
            else if (action.actionType == "Set rotation") { desc = "Sets item's world rotation (Euler degrees)."; needsMovable = true; }
            else if (action.actionType == "Add rotation") { desc = "Adds to item's world rotation (Euler degrees)."; needsMovable = true; }
            DrawDocEntry(action.actionType, desc, getParamsInfoForUI(action), needsMovable, getJsSignature(action));
        }
        
        EditorGUILayout.LabelField("Data Logging", EditorStyles.boldLabel);
        foreach(var action in AvailableStateListeningActions.Where(a => a.actionType == "Record custom data" || a.actionType == "Upload recorded data"))
        {
            DrawDocEntry(action.actionType, 
                         action.actionType == "Record custom data" ? "Signals LUIDA's DataRecorder to log configured data." : "Signals LUIDA's DataRecorder to upload accumulated data.",
                         getParamsInfoForUI(action), false, getJsSignature(action));
        }

        EditorGUILayout.LabelField("User Feedback & Utilities", EditorStyles.boldLabel);
        foreach(var action in AvailableStateListeningActions.Where(a => a.actionType == "Send Haptics" || a.actionType == "Sleep"))
        {
            string desc = "";
            if (action.actionType == "Send Haptics") desc = "Sends haptic feedback to the player. Target can be \"left\", \"right\", or null (for both hands). Duration is in seconds.";
            else if (action.actionType == "Sleep") desc = "Pauses execution of subsequent actions in the current list (On State Start, During State, On State End) for the specified duration in seconds.";
            DrawDocEntry(action.actionType, desc, getParamsInfoForUI(action), false, getJsSignature(action));
        }
    
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    #endregion
}
