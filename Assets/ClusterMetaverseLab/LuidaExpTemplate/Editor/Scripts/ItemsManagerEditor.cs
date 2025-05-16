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

public class ItemsManagerEditor : EditorWindow
{
    // ——— Caching & ReorderableList storage ———
    private bool _needsRebuild = true;
    private string[] _cachedStateNames = Array.Empty<string>();
    private GameObject[] _cachedItems = Array.Empty<GameObject>();
    private Dictionary<string, ReorderableList> _reorderableLists = new Dictionary<string, ReorderableList>();

    // ——— Existing fields ———
    private static StateListeningAction[] AvailableStateListeningActions = {
        new StateListeningAction("Show item", "$.setStateCompat('this', 'exp_showItem', true);"),
        new StateListeningAction("Hide item", "$.setStateCompat('this', 'exp_showItem', false);"),
        new StateListeningAction("To next state", "$.sendSignalCompat('this', 'state_triggerTransition');"),
        new StateListeningAction("Record custom data", "$.sendSignalCompat('this', 'exp_recordCustomData');"),
        new StateListeningAction("Upload recorded data", "$.sendSignalCompat('this', 'exp_uploadCustomData');"),
        new StateListeningAction("Set text", "$.subNode('Text').setText('xxx');"),
        new StateListeningAction("Sleep", "0"),
    };

    private string newItemName = string.Empty;
    private GameObject referenceObject = null;

    private const string PrefabPath               = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/StateListeningItem.prefab";
    private const string ScriptFolderFormat       = "Assets/_Experiment_/Scripts/StateManagement/{0}";
    private const string ScriptTemplatePath       = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/StateManagement/StateListeningItemTemplate.js";
    private const string WrapperPrefabPath        = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";
    private const string ConditionManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionManager.prefab";

    private List<GameObject> stateListeningItems = new List<GameObject>();
    private StateList stateList = null;
    private SerializedObject serializedStateList = null;
    private SerializedProperty statesProperty = null;
    private Dictionary<GameObject, List<StateListener>> stateListenersByItem = new Dictionary<GameObject, List<StateListener>>();
    private Dictionary<GameObject, string> otherImplementationByItem = new Dictionary<GameObject, string>();

    private Vector2 scrollPosition;
    private Vector2 scrollPositionX;
    private Vector2 scrollPositionY;
    private int selectedActionIndex = 0;
    private Dictionary<List<StateListenerAction>, bool> isAddingActionState = new Dictionary<List<StateListenerAction>, bool>();
    private string setTextInput = string.Empty;
    private double sleepTimeInput = 0.0;
    private bool isSubscribed = false;

    // [MenuItem("Window/Luida Editor/Items Manager")]
    // public static void ShowWindow() => GetWindow<ItemsManagerEditor>("Items Manager");

    #region Unity Callbacks

    public void OnEnable()
    {
        // mark for rebuild
        _needsRebuild = true;
        // listen for hierarchy/project changes
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.projectChanged   += OnProjectChanged;
        // subscribe once
        if (!isSubscribed)
        {
            TabbedEditor.OnEditorClosed             += ApplyAssetsToScripts;
            TabbedEditor.OnItemsManagerTabLostFocus += ApplyAssetsToScripts;
            isSubscribed = true;
        }
    }

    public void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.projectChanged   -= OnProjectChanged;
        ApplyAssetsToScripts();
        if (isSubscribed)
        {
            TabbedEditor.OnEditorClosed             -= ApplyAssetsToScripts;
            TabbedEditor.OnItemsManagerTabLostFocus -= ApplyAssetsToScripts;
            isSubscribed = false;
        }
    }

    private void OnHierarchyChanged() => _needsRebuild = true;
    private void OnProjectChanged()   => _needsRebuild = true;

    public void OnLostFocus() => ApplyAssetsToScripts();
    public void OnDestroy()   => Debug.Log("ItemsManagerEditor destroyed");

    #endregion

    public void OnGUI()
    {
        // Remove button style
        GUIStyle removeButtonStyle = new GUIStyle(GUI.skin.button);
        removeButtonStyle.normal.textColor = Color.red;
        removeButtonStyle.hover.textColor = Color.red;
        
        // 1) Rebuild caches only when needed
        if (_needsRebuild)
        {
            RefreshStateListeningItems();
            _cachedStateNames = stateList != null
                ? stateList.States.Select(s => s.StateName).ToArray()
                : Array.Empty<string>();
            _cachedItems = stateListeningItems.ToArray();
            SetupReorderableLists();
            _needsRebuild = false;
        }

        // 2) If no StateList, early-out
        if (stateList == null)
        {
            EditorGUILayout.HelpBox(
                "No StateList asset found for this scene. Create one under the StateList tab.",
                MessageType.Warning
            );
            return;
        }

        EditorGUI.BeginChangeCheck();

        // 3) Add-item controls
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("New Item Name", GUILayout.Width(120));
        newItemName = EditorGUILayout.TextField(newItemName, GUILayout.Width(180));
        EditorGUI.BeginDisabledGroup(
            string.IsNullOrEmpty(newItemName) ||
            stateListeningItems.Any(i => i.name == newItemName)
        );
        if (GUILayout.Button("+ Add state-listening item", GUILayout.Width(180)))
            CreateStateListeningItem(referenceObject);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);

        // 4) Table (Scrollable Y)
        scrollPositionY = EditorGUILayout.BeginScrollView(
            scrollPositionY, false, true,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true)
        );

        // Header row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("State Name | Item Name", GUILayout.Width(215));
        bool isDarkColumn = true; // Start with dark for columns
        for (int i = 0; i < _cachedItems.Length; i++)
        {
            var item = _cachedItems[i];
            
            // Set column background color
            GUI.backgroundColor = isDarkColumn ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.8f, 0.8f, 0.8f);

            EditorGUILayout.BeginVertical("box", GUILayout.Width(240));
            // Reference field
            EditorGUILayout.LabelField(item.name, EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            var newReference = (GameObject)EditorGUILayout.ObjectField(item, typeof(GameObject), true);

            // Update reference if changed
            if (newReference != item)
            {
                Undo.RecordObject(item, "Update Reference");
                item = newReference;
            }

            // Remove button
            if (GUILayout.Button("Remove", removeButtonStyle, GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog(
                        "Confirm Removal",
                        $"Are you sure you want to remove {item.name} and its associated assets?",
                        "Yes",
                        "No"))
                {
                    // Remove corresponding asset and JavaScript
                    string scene = SceneManager.GetActiveScene().name;
                    string folder = string.Format(ScriptFolderFormat, scene);
                    string jsPath = Path.Combine(folder, item.name + ".js");
                    string assetPath = Path.Combine(folder + "/StateListeners", item.name + ".asset");

                    if (File.Exists(jsPath)) File.Delete(jsPath);
                    if (File.Exists(assetPath)) AssetDatabase.DeleteAsset(assetPath);
                    
                    // Remove GameObject
                    Undo.DestroyObjectImmediate(item);

                    AssetDatabase.Refresh();
                    _needsRebuild = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);

            // Toggle column color for the next column
            isDarkColumn = !isDarkColumn;
        }
        EditorGUILayout.EndHorizontal();

        bool isBlueRow = true; // Start with blue for rows

        foreach (var name in _cachedStateNames)
        {
            // Set row background color
            GUI.backgroundColor = isBlueRow ? Color.blue : Color.gray;

            EditorGUILayout.BeginHorizontal("box"); // Start a new row with background color

            // State name column
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndVertical();

            // Items columns
            isDarkColumn = true; // Start with dark for columns
            foreach (var item in _cachedItems)
            {
                // Set column background color
                GUI.backgroundColor = isDarkColumn ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.8f, 0.8f, 0.8f);

                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal("box", GUILayout.Width(240));
                GUILayout.Space(20);
			    EditorGUILayout.BeginVertical();
                GUILayout.Space(20);

                stateListenersByItem.TryGetValue(item, out var list);
                var listener = list?.FirstOrDefault(l => l.stateID == Array.IndexOf(_cachedStateNames, name));

                if (listener != null)
                {
                    DrawReorderableList(item, Array.IndexOf(_cachedStateNames, name), "OnStateStart", "On State Start");
                    DrawReorderableList(item, Array.IndexOf(_cachedStateNames, name), "DuringState", "During State");
                    DrawReorderableList(item, Array.IndexOf(_cachedStateNames, name), "OnStateExit", "On State End");
                    // Add Remove button
                    if (GUILayout.Button("Remove", removeButtonStyle, GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Confirm Removal",
                                $"Are you sure you want to remove the state listener of {item.name}?",
                                "Yes",
                                "No"))
                        {
                            list.Remove(listener); // Remove the listener
                            SaveItemToAsset(item); // Save changes to the asset
                            _needsRebuild = true;  // Mark for rebuild
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("Add", GUILayout.Height(20)))
                        AddStateListener(Array.IndexOf(_cachedStateNames, name), item);
                }

                GUILayout.Space(20);
                EditorGUILayout.EndVertical();
                GUILayout.Space(20);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);

                // Toggle column color for the next column
                isDarkColumn = !isDarkColumn;
            }

            EditorGUILayout.EndHorizontal(); // End the row

            // Toggle the color for the next row
            isBlueRow = !isBlueRow;
        }

        // Reset background color to default
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();

        // 6) Save if user changed something
        if (EditorGUI.EndChangeCheck())
        {
            // SaveAllItemsToAssets();
        }
    }

    #region ReorderableList Setup & Draw

    private void SetupReorderableLists()
    {
        _reorderableLists.Clear();
        foreach (var item in stateListeningItems)
        {
            if (!stateListenersByItem.TryGetValue(item, out var listeners)) continue;
            foreach (var listener in listeners)
            {
                CreateReorderableList(item, listener.stateID, listener.onStateStartedActions, "On State Start", "OnStateStart");
                CreateReorderableList(item, listener.stateID, listener.duringStateActions,    "During State",  "DuringState");
                CreateReorderableList(item, listener.stateID, listener.onStateExitedActions,  "On State End",  "OnStateExit");
            }
        }
    }

    private void CreateReorderableList(GameObject item, int stateID, List<StateListenerAction> actions, string header, string keySuffix)
    {
        var key = $"{item.GetInstanceID()}_{stateID}_{keySuffix}";
        var rl = new ReorderableList(actions, typeof(StateListenerAction), true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, header, EditorStyles.boldLabel),
            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var action = actions[index];
                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;

                Rect dropdownRect = new Rect(rect.x, rect.y, rect.width, lineHeight);
                // Rect dropdownRect = new Rect(rect.x, rect.y + lineHeight + spacing, rect.width * 0.5f, lineHeight);
                Rect buttonRect = new Rect(rect.x + dropdownRect.width + 4, rect.y + lineHeight + spacing, rect.width * 0.5f - 4, lineHeight);

                // Action Type Dropdown
                var options = AvailableStateListeningActions.Select(a => a.actionType).ToList();
                options.Add("Customized Action");

                int selectedIndex = options.Count - 1; // Default to "Customized Action"
                int builtinIndex = AvailableStateListeningActions
                    .ToList()
                    .FindIndex(a => a.actionType == action.predefinedAction.actionType);

                if (builtinIndex != -1)
                {
                    selectedIndex = builtinIndex;
                }

                int newIndex = EditorGUI.Popup(dropdownRect, selectedIndex, options.ToArray());
                if (newIndex >= AvailableStateListeningActions.Length)
                {
                    action.predefinedAction = new StateListeningAction("Customized Action", action.customAction);
                }
                else if (newIndex != selectedIndex)
                {
                    if (newIndex < AvailableStateListeningActions.Length)
                    {
                        action.predefinedAction = AvailableStateListeningActions[newIndex];
                        action.customAction = ""; // Reset customAction if switching from customized
                    }
                    else
                    {
                        action.predefinedAction = new StateListeningAction("Customized Action", "");
                    }
                }

                // Show custom input only for "Set text" or "Sleep" or "Customized action"
                if (action.predefinedAction.actionType == "Set text")
                {
                    // Label
                    Rect labelRect = new Rect(rect.x, rect.y + lineHeight + spacing, 40f, lineHeight);
                    EditorGUI.LabelField(labelRect, "Text");

                    // Input Field next to label
                    Rect fieldRect = new Rect(labelRect.xMax + 4, labelRect.y, rect.width - labelRect.width - 4, lineHeight);
                    string val = GetSetTextValue(action.predefinedAction.codeSnippet);
                    val = EditorGUI.TextArea(fieldRect, val);
                    action.predefinedAction.codeSnippet = $"$.subNode('Text').setText(`{val}`);";
                }
                else if (action.predefinedAction.actionType == "Sleep")
                {
                    // Label
                    Rect labelRect = new Rect(rect.x, rect.y + lineHeight + spacing, 100f, lineHeight);
                    EditorGUI.LabelField(labelRect, "Sleep Time (s)");

                    // Input Field next to label
                    Rect fieldRect = new Rect(labelRect.xMax + 4, labelRect.y, rect.width - labelRect.width - 4, lineHeight);
                    string val = action.predefinedAction.codeSnippet;
                    val = EditorGUI.TextField(fieldRect, val);
                    action.predefinedAction.codeSnippet = val;
                }
                else if (action.predefinedAction.actionType == "Customized Action")
                {
                    Rect textAreaRect = new Rect(rect.x, rect.y + (lineHeight + spacing), rect.width, lineHeight * 2);
                    action.customAction = EditorGUI.TextArea(textAreaRect, action.customAction);
                }
            },
            elementHeightCallback = index =>
            {
                var action = actions[index];
                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float height = lineHeight + spacing; // Dropdown

                if (action.predefinedAction.actionType == "Set text" || action.predefinedAction.actionType == "Sleep")
                    height += lineHeight + spacing;

                if (action.predefinedAction.actionType == "Customized Action")
                    height += lineHeight * 2 + spacing;

                return height + 6f;
            }
        };
        rl.onAddCallback = list =>
        {
            actions.Add(new StateListenerAction());
            SaveItemToAsset(item);
            _needsRebuild = true;
        };
        rl.onRemoveCallback = list =>
        {
            actions.RemoveAt(list.index);
            SaveItemToAsset(item);
        };
        rl.onReorderCallback = list => SaveItemToAsset(item);

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

    private void RefreshStateListeningItems()
    {
        stateListeningItems.Clear();
        stateListenersByItem.Clear();
        otherImplementationByItem.Clear();

        var allObjs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjs)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (prefab != null && AssetDatabase.GetAssetPath(prefab) == PrefabPath)
                stateListeningItems.Add(obj);
        }

        string sceneName = SceneManager.GetActiveScene().name;
        string folder = string.Format(ScriptFolderFormat, sceneName) + "/StateListeners";

        foreach (var item in stateListeningItems)
        {
            string assetPath = Path.Combine(folder, item.name + ".asset");
            var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);
            if (data != null)
            {
                stateListenersByItem[item] = data.stateListeners.ToList();
                otherImplementationByItem[item] = data.otherImplementation;
            }
            else
            {
                stateListenersByItem[item] = new List<StateListener>();
                otherImplementationByItem[item] = string.Empty;
            }
        }

        string listPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
        stateList = AssetDatabase.LoadAssetAtPath<StateList>(listPath);
        if (stateList != null)
        {
            serializedStateList = new SerializedObject(stateList);
            statesProperty = serializedStateList.FindProperty("States");
        }
    }

    private void AddStateListener(int stateIndex, GameObject item)
    {
        if (!stateListenersByItem.ContainsKey(item))
            stateListenersByItem[item] = new List<StateListener>();

        if (stateListenersByItem[item].Any(l => l.stateID == stateIndex))
        {
            EditorUtility.DisplayDialog("Error", $"Listener for state {stateIndex} already exists on {item.name}.", "OK");
            return;
        }

        stateListenersByItem[item].Add(new StateListener { stateID = stateIndex });
        SaveItemToAsset(item);
    }

    private void CreateStateListeningItem(GameObject reference)
    {
        if (string.IsNullOrEmpty(newItemName)) return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = newItemName;
        Undo.RegisterCreatedObjectUndo(go, "Create StateListeningItem");

        EnableAccessToConditions(go);

        string scene = SceneManager.GetActiveScene().name;
        string scriptFolder = string.Format(ScriptFolderFormat, scene);
        if (!AssetDatabase.IsValidFolder(scriptFolder))
            Directory.CreateDirectory(scriptFolder);

        string jsPath = $"{scriptFolder}/{newItemName}.js";
        AssetDatabase.CopyAsset(ScriptTemplatePath, jsPath);
        AssetDatabase.Refresh();

        RefreshStateListeningItems();
        SaveItemToAsset(go);

        newItemName = string.Empty;
        referenceObject = null;
    }

    private JavaScriptAsset GetClusterScriptFromItem(GameObject item)
    {
        var combiner = item.GetComponent<ScriptableClusterScriptCombiner>();
        var scripts = combiner.GetClusterScripts();
        return scripts.Count > 1 ? scripts[1] as JavaScriptAsset : null;
    }

    private string GenerateStateFunction(string name, Func<StateListener, List<StateListenerAction>> sel, string extra = "")
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"function {name}({extra}) {{");
        sb.AppendLine("  const STATE_ID = $.state.state_id;");
        sb.AppendLine("  const CONDITION = $.groupState.currentCondition;");
        foreach (var kv in stateListenersByItem)
        {
            foreach (var lst in kv.Value)
            {
                var acts = sel(lst);
                if (acts.Count == 0) continue;
                sb.AppendLine($"  if (STATE_ID === {lst.stateID}) {{");
                foreach (var a in acts)
                    sb.AppendLine($"    {a.GetActionContent()}");
                sb.AppendLine("  }");
            }
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    private string GenerateActionsObjects()
    {
        return
            GenerateActionsObject(l => l.onStateStartedActions, "stateEnterActions") + "\n" +
            GenerateActionsObject(l => l.duringStateActions,    "duringStateActions") + "\n" +
            GenerateActionsObject(l => l.onStateExitedActions,  "stateExitActions");
    }

    private string GenerateActionsObject(Func<StateListener, List<StateListenerAction>> sel, string objName)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"const {objName} = {{");
        foreach (var kv in stateListenersByItem)
        {
            foreach (var lst in kv.Value)
            {
                var acts = sel(lst);
                if (acts.Count == 0) continue;
                sb.AppendLine($"    {lst.stateID}: [");
                foreach (var a in acts)
                    sb.AppendLine($"        {GenerateActionObject(a)},");
                sb.AppendLine("    ],");
            }
        }
        sb.AppendLine("};");
        return sb.ToString();
    }

    private string GenerateActionObject(StateListenerAction action)
    {
        if (action.predefinedAction.actionType != null && action.predefinedAction.actionType.Equals("Sleep", StringComparison.OrdinalIgnoreCase))
            return $"{{ type: \"sleep\", value: {action.predefinedAction.codeSnippet} }}";

        var code = string.IsNullOrEmpty(action.customAction)
            ? action.predefinedAction.codeSnippet
            : action.customAction;
        if (code != null) code = code.Trim().Replace("\n", "\n            ");
        return $"{{ type: \"exec\", action: () => {{\n            {code}\n        }} }}";
    }
    
    private string GenerateActionsObjectsForItem(GameObject item)
    {
        if (!stateListenersByItem.TryGetValue(item, out var listeners) || listeners.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();

        // stateEnterActions
        sb.AppendLine("const stateEnterActions = {");
        foreach (var lst in listeners)
        {
            var acts = lst.onStateStartedActions;
            if (acts.Count == 0) continue;
            sb.AppendLine($"    {lst.stateID}: [");
            foreach (var a in acts)
                sb.AppendLine($"        {GenerateActionObject(a)},");
            sb.AppendLine("    ],");
        }
        sb.AppendLine("};\n");

        // duringStateActions
        sb.AppendLine("const duringStateActions = {");
        foreach (var lst in listeners)
        {
            var acts = lst.duringStateActions;
            if (acts.Count == 0) continue;
            sb.AppendLine($"    {lst.stateID}: [");
            foreach (var a in acts)
                sb.AppendLine($"        {GenerateActionObject(a)},");
            sb.AppendLine("    ],");
        }
        sb.AppendLine("};\n");

        // stateExitActions
        sb.AppendLine("const stateExitActions = {");
        foreach (var lst in listeners)
        {
            var acts = lst.onStateExitedActions;
            if (acts.Count == 0) continue;
            sb.AppendLine($"    {lst.stateID}: [");
            foreach (var a in acts)
                sb.AppendLine($"        {GenerateActionObject(a)},");
            sb.AppendLine("    ],");
        }
        sb.AppendLine("};");

        return sb.ToString();
    }

    private string GetSetTextValue(string content)
    {
        var m = Regex.Match(content, @"\.setText\(`([^`]*)`\)");
        return m.Success ? m.Groups[1].Value : string.Empty;
    }

    private void EnableAccessToConditions(GameObject item)
    {
        var member = item.GetComponent<ItemGroupMember>();
        var wrapper = Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(o => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o) == WrapperPrefabPath);
        if (member == null || wrapper == null) return;
        var host = wrapper.GetComponentInChildren<ItemGroupHost>();
        if (host == null) return;
        var so = new SerializedObject(member);
        var prop = so.FindProperty("host");
        prop.objectReferenceValue = host;
        so.ApplyModifiedProperties();
    }

    private void SaveItemToAsset(GameObject item)
    {
        if (!item) return;
        if (stateListenersByItem.ContainsKey(item) == false)
        {
            Destroy(item);
            return;
        }
        
        string scene = SceneManager.GetActiveScene().name;
        string folder = string.Format(ScriptFolderFormat, scene);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        var lines = new[] {
            GenerateActionsObjectsForItem(item),
            otherImplementationByItem.GetValueOrDefault(item, "")
        };
        string jsPath = Path.Combine(folder, item.name + ".js");
        File.WriteAllText(jsPath, string.Join("\n", lines));
        AssetDatabase.ImportAsset(jsPath);

        string assetFolder = folder + "/StateListeners";
        if (!Directory.Exists(assetFolder)) Directory.CreateDirectory(assetFolder);

        string assetPath = Path.Combine(assetFolder, item.name + ".asset");
        var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath)
                   ?? ScriptableObject.CreateInstance<StateListeningItemData>();
        data.stateListeners = stateListenersByItem[item].ToArray();
        data.otherImplementation = otherImplementationByItem.GetValueOrDefault(item, "");
        if (!AssetDatabase.Contains(data))
            AssetDatabase.CreateAsset(data, assetPath);
        else
            EditorUtility.SetDirty(data);
    }

    private void SaveAllItemsToAssets()
    {
        foreach (var item in stateListeningItems)
            SaveItemToAsset(item);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void ApplyAssetsToScripts()
    {
        SaveAllItemsToAssets();
        Assets.KaomoLab.CSCombiner.CSCombiner.CombineAll();
    }

    #endregion
}
