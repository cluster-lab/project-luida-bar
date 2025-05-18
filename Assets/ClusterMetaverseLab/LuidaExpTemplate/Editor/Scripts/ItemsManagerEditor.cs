using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClusterVR.CreatorKit.Item.Implements;

public class ItemsManagerEditor : EditorWindow
{
    private bool _needsRebuild = true;
    private string[] _cachedStateNames = Array.Empty<string>();
    private GameObject[] _cachedItems = Array.Empty<GameObject>();
    private Dictionary<string, ReorderableList> _reorderableLists = new Dictionary<string, ReorderableList>();

    private static StateListeningAction[] AvailableStateListeningActions =
    {
        new StateListeningAction("Show item", "$.setStateCompat('this', 'exp_showItem', true);"),
        new StateListeningAction("Hide item", "$.setStateCompat('this', 'exp_showItem', false);"),
        new StateListeningAction("To next state", "$.sendSignalCompat('this', 'state_triggerTransition');"),
        new StateListeningAction("Record custom data", "$.sendSignalCompat('this', 'exp_recordCustomData');"),
        new StateListeningAction("Upload recorded data", "$.sendSignalCompat('this', 'exp_uploadCustomData');"),
        new StateListeningAction("Set text", "$.subNode('Text').setText('{_text_}');", new[] { "text" }),
        new StateListeningAction("Sleep", "{_seconds_}", new[] { "seconds" }),
        new StateListeningAction("Send Haptics",
            "$.state.player.send('haptics', {target: {_target_}, frequency: {_frequency_}, amplitude: {_amplitude_}, duration: {_duration_}});",
            new[] { "target", "frequency", "amplitude", "haptics_duration" }),
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
    private Dictionary<GameObject, string> otherImplementationByItem = new Dictionary<GameObject, string>();

    private Vector2 scrollPositionY;
    private bool isSubscribed = false;

    // [MenuItem("Window/Luida Editor/Items Manager")] // Uncomment to add to Unity menu
    // public static void ShowWindow() => GetWindow<ItemsManagerEditor>("Items Manager");

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
            TabbedEditor.OnItemsManagerTabLostFocus += ApplyAssetsToScripts;
            isSubscribed = true;
        }
        RefreshStateList();
    }

    public void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged; 

        // ApplyAssetsToScripts(); 
        if (isSubscribed)
        {
            TabbedEditor.OnEditorClosed -= ApplyAssetsToScripts;
            TabbedEditor.OnItemsManagerTabLostFocus -= ApplyAssetsToScripts;
            isSubscribed = false;
        }
    }

    private void OnHierarchyChanged() => _needsRebuild = true;
    private void OnProjectChanged() => _needsRebuild = true;
    public void OnLostFocus() => ApplyAssetsToScripts();

    #endregion

    private void RefreshStateList()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string listPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
        stateList = AssetDatabase.LoadAssetAtPath<StateList>(listPath);
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

        scrollPositionY = EditorGUILayout.BeginScrollView(scrollPositionY, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("State Name | Item Name", EditorStyles.boldLabel, GUILayout.Width(215));
        bool isHeaderDarkColumn = true;
        for (int i = 0; i < _cachedItems.Length; i++)
        {
            var item = _cachedItems[i];
            if (item == null) continue; // Skip if item was destroyed
            GUI.backgroundColor = isHeaderDarkColumn ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.7f, 0.7f, 0.7f);

            EditorGUILayout.BeginVertical("box", GUILayout.Width(240));
            EditorGUILayout.LabelField(item.name, EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(item, typeof(GameObject), true); 

            if (GUILayout.Button("X", removeButtonStyle, GUILayout.Width(25), GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("Confirm Removal", $"Are you sure you want to remove '{item.name}' and its associated assets (JS script and StateListenerData asset)?", "Yes, Remove", "No"))
                {
                    RemoveStateListeningItem(item);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            isHeaderDarkColumn = !isHeaderDarkColumn;
        }
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
                if (item == null) continue; // Skip if item was destroyed
                Color cellBgColor = isCellDarkColumn ? new Color(0.2f, 0.2f, 0.2f, 0.5f) : new Color(0.8f, 0.8f, 0.8f, 0.5f);
                Rect cellRect = EditorGUILayout.BeginVertical("box", GUILayout.Width(240), GUILayout.MinHeight(100));
                EditorGUI.DrawRect(cellRect, cellBgColor);

                stateListenersByItem.TryGetValue(item, out var listenersList);
                var listener = listenersList?.FirstOrDefault(l => l.stateID == stateID);

                if (listener != null)
                {
                    DrawReorderableList(item, stateID, "OnStateStart", "On State Start Actions");
                    DrawReorderableList(item, stateID, "DuringState", "During State Actions");
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
        EditorGUILayout.EndScrollView();

        if (EditorGUI.EndChangeCheck())
        {
            // Changes are primarily saved via Undo/SetDirty and explicit save calls.
        }
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
                Debug.LogWarning($"StateListeningItemData asset not found for {item.name} at {itemDataAssetPath} during ReorderableList setup.");
                continue;
            }

            foreach (var listener in listeners)
            {
                CreateReorderableList(item, itemDataAsset, listener, listener.onStateStartedActions, "On State Start", "OnStateStart");
                CreateReorderableList(item, itemDataAsset, listener, listener.duringStateActions, "During State", "DuringState");
                CreateReorderableList(item, itemDataAsset, listener, listener.onStateExitedActions, "On State End", "OnStateExit");
            }
        }
    }

    private void CreateReorderableList(GameObject itemGO, StateListeningItemData itemDataAsset, StateListener listener, List<StateListenerAction> actions, string header, string keySuffix)
    {
        var key = $"{itemGO.GetInstanceID()}_{listener.stateID}_{keySuffix}";
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

                Rect dropdownRect = new Rect(rect.x, currentY, rect.width, lineHeight);
                var options = AvailableStateListeningActions.Select(a => a.actionType).ToList();
                options.Insert(0, "Select Action");
                options.Add("Customized Action");

                int selectedIndex = 0;
                if (!string.IsNullOrEmpty(action.predefinedActionTemplate.actionType))
                {
                    selectedIndex = (action.predefinedActionTemplate.actionType == "Customized Action")
                        ? options.Count - 1
                        : AvailableStateListeningActions.ToList().FindIndex(a => a.actionType == action.predefinedActionTemplate.actionType) + 1;
                }
                
                int newIndex = EditorGUI.Popup(dropdownRect, selectedIndex, options.ToArray());
                currentY += lineHeight + spacing;

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
                    }
                    EditorUtility.SetDirty(itemDataAsset);
                }
                
                // Warning if MovableItem is necessary but does not exist
                bool requiresMovableItem = action.predefinedActionTemplate.actionType == "Set position" ||
                                           action.predefinedActionTemplate.actionType == "Add position" ||
                                           action.predefinedActionTemplate.actionType == "Set rotation" ||
                                           action.predefinedActionTemplate.actionType == "Add rotation";
                if (requiresMovableItem && itemGO.GetComponent<MovableItem>() == null)
                {
                    Rect warningRect = new Rect(rect.x, currentY, rect.width, lineHeight * 2); // Adjust height as needed
                    EditorGUI.HelpBox(warningRect, $"Warning: '{action.predefinedActionTemplate.actionType}' requires a MovableItem component on '{itemGO.name}'.", MessageType.Warning);
                    currentY += lineHeight * 2 + spacing;
                }

                if (action.predefinedActionTemplate.actionType == "Customized Action")
                {
                    Rect textAreaRect = new Rect(rect.x, currentY, rect.width, lineHeight * 3);
                    string newCustomAction = EditorGUI.TextArea(textAreaRect, action.customAction);
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
                        Rect labelRect = new Rect(rect.x, currentY, EditorGUIUtility.labelWidth * 0.6f, lineHeight);
                        Rect fieldRect = new Rect(labelRect.xMax, currentY, rect.width - labelRect.width, lineHeight);
                        
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
                float height = lineHeight + spacing;

                if (action.predefinedActionTemplate.actionType == "Customized Action")
                {
                    height += lineHeight * 3 + spacing;
                }
                else if (action.predefinedActionTemplate.variables != null)
                {
                    height += (lineHeight + spacing) * action.predefinedActionTemplate.variables.Length;
                }
                return height + spacing;
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
            actions.RemoveAt(list.index);
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
        stateListeningItems.Clear(); // Clear before repopulating
        var currentItemsInScene = new List<GameObject>(); // Temporary list to hold found items

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
        stateListeningItems = currentItemsInScene; // Assign validated items

        // Clear dictionaries before repopulating based on current scene items
        stateListenersByItem.Clear();
        otherImplementationByItem.Clear();


        string sceneName = SceneManager.GetActiveScene().name;
        string baseFolder = string.Format(ScriptFolderFormat, sceneName);
        string listenerDataFolder = Path.Combine(baseFolder, "StateListeners");
        Directory.CreateDirectory(listenerDataFolder);

        foreach (var item in stateListeningItems) // Iterate only over currently valid items
        {
            if (item == null) continue;
            string assetPath = GetItemDataAssetPath(item);
            StateListeningItemData data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<StateListeningItemData>();
                data.stateListeners = Array.Empty<StateListener>();
                data.otherImplementation = string.Empty;
                AssetDatabase.CreateAsset(data, assetPath);
                AssetDatabase.SaveAssets();
            }
            stateListenersByItem[item] = data.stateListeners != null ? data.stateListeners.ToList() : new List<StateListener>();
            otherImplementationByItem[item] = data.otherImplementation ?? string.Empty;
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
        data.otherImplementation = string.Empty;
        AssetDatabase.CreateAsset(data, assetPath);
        AssetDatabase.Refresh();

        stateListeningItems.Add(go);
        stateListenersByItem[go] = data.stateListeners.ToList();
        otherImplementationByItem[go] = data.otherImplementation;
        
        _needsRebuild = true;
        newItemName = string.Empty;
    }

    private string GenerateActionObject(StateListenerAction action)
    {
        string actionCode = action.GetActionContent();

        if (action.predefinedActionTemplate.actionType == "Sleep")
        {
            return $"{{ type: \"sleep\", value: {actionCode} }}";
        }

        actionCode = (actionCode ?? "").Trim().Replace("\n", "\n            ");
        return $"{{ type: \"exec\", action: () => {{\n            {actionCode}\n        }} }}";
    }
    
    private string GenerateActionsObjectsForItem(GameObject item)
    {
        if (item == null || !stateListenersByItem.TryGetValue(item, out var listeners) || listeners.Count == 0)
            return string.Empty;

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
        foreach (var listener in listeners)
        {
            var actions = actionSelector(listener);
            if (actions.Count == 0) continue;
            sb.AppendLine($"    {listener.stateID}: [");
            foreach (var action in actions)
            {
                sb.AppendLine($"        {GenerateActionObject(action)},");
            }
            sb.AppendLine("    ],");
        }
    }

    private void SaveItemToAsset(GameObject item)
    {
        if (!item) return;
        if (!stateListeningItems.Contains(item) && !stateListenersByItem.ContainsKey(item)) {
            return;
        }

        string scene = SceneManager.GetActiveScene().name;
        string folder = string.Format(ScriptFolderFormat, scene);
        Directory.CreateDirectory(folder);

        string jsContentForItem = GenerateActionsObjectsForItem(item);
        string otherImpl = otherImplementationByItem.GetValueOrDefault(item, "");
        
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(jsContentForItem)) lines.Add(jsContentForItem);
        if (!string.IsNullOrWhiteSpace(otherImpl)) lines.Add(otherImpl);

        string jsPath = Path.Combine(folder, item.name + ".js");
        File.WriteAllText(jsPath, string.Join("\n\n", lines).Trim());
        AssetDatabase.ImportAsset(jsPath, ImportAssetOptions.ForceUpdate); 

        string assetPath = GetItemDataAssetPath(item);
        var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);
        if (data != null)
        {
            if (stateListenersByItem.TryGetValue(item, out var currentListeners))
            {
                data.stateListeners = currentListeners.ToArray();
            }
            if (otherImplementationByItem.TryGetValue(item, out var currentOtherImpl))
            {
                data.otherImplementation = currentOtherImpl;
            }
            EditorUtility.SetDirty(data); 
        }
        else
        {
            Debug.LogError($"StateListeningItemData asset not found for {item.name} at {assetPath} during SaveItemToAsset.");
        }
    }

    private void SaveAllItemsToAssets()
    {
        // Filter out null items that might have been destroyed
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
            // Check if this root object is an instance of the RequiredObjectsWrapperPrefab
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == RequiredObjectsWrapperPrefabPath)
            {
                // Search for ItemGroupHost component in its children (or on itself)
                ItemGroupHost host = obj.GetComponentInChildren<ItemGroupHost>(true); // true to include inactive
                if (host != null)
                {
                    return host;
                }
            }
        }
        // Fallback: If not found in a wrapper, search all ItemGroupHosts in the scene.
        // This might be less precise if there are multiple, but better than nothing.
        var allHosts = FindObjectsOfType<ItemGroupHost>(true); // Find all instances, including inactive
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
            // If LuidaStateListeningItem prefab is expected to always have ItemGroupMember,
            // this might indicate a prefab setup issue.
            Debug.LogWarning($"ItemGroupMember component not found on the newly created item: {item.name}. Cannot link to ItemGroupHost.");
            return;
        }

        ItemGroupHost host = FindItemGroupHostInScene();
        if (host != null)
        {
            SerializedObject serializedItemGroupMember = new SerializedObject(itemGroupMember);
            SerializedProperty hostProperty = serializedItemGroupMember.FindProperty("host"); // "host" is the typical field name

            if (hostProperty != null)
            {
                hostProperty.objectReferenceValue = host;
                serializedItemGroupMember.ApplyModifiedProperties();
                // Debug.Log($"Successfully linked {item.name} (ItemGroupMember) to {host.gameObject.name} (ItemGroupHost)."); // Optional
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
}
