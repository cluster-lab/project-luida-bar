using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClusterVR.CreatorKit.Item.Implements;

public static class ItemsManagerAssetUtil
{
    private const string PrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/StateListeningItem.prefab";
    private const string ScriptFolderFormat = "Assets/_Experiment_/Scripts/StateManagement/{0}";
    private const string ScriptTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/StateManagement/StateListeningItemTemplate.js";
    private const string ExpManagersWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/LUIDA-ExpManagers.prefab";
    private const string ConditionManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionManager.prefab";
    private const string DataCollectorPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/CustomDataCollection/LUIDA-DataCollector.prefab";
    private const string defaultOtherImplementation = @"// function Start() { }
// function Update(deltaTime) { }
// $.onCollide((collision) => { });
// $.onGrab((isGrab, isLeftHand, player) => { });
// $.onInteract((player) => { });
// $.onUse((isDown, player) => { });
// $.onPhysicsUpdate((deltaTime) => { });
// $.onReceive((messageType, arg, sender) => { });
";
    
    public static bool IsApplyingAssetsToScripts = false;

    #region Data Refreshing
    
    public static void RefreshStateList(ItemsManagerConfigTab editor)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string listPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
        editor.stateList = AssetDatabase.LoadAssetAtPath<StateList>(listPath);
    }
    
    public static void RefreshExperimentVariablesCache(ItemsManagerConfigTab editor)
    {
        editor._cachedExperimentVariables.Clear();
        string sceneName = SceneManager.GetActiveScene().name;
        editor._experimentVariablesAssetPath = $"Assets/_Experiment_/Settings/ExperimentVariables/{sceneName}.js";

        if (!File.Exists(editor._experimentVariablesAssetPath)) return;

        string jsContent = File.ReadAllText(editor._experimentVariablesAssetPath);

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
                    editor._cachedExperimentVariables.Add(new ItemsManagerConfigTab.EditorExperimentVariable { name = name, values = values });
                }
            }
        };

        parseAndAdd("within_subjects_variables");
        parseAndAdd("between_subjects_variables");
    }

    public static void RefreshStateListeningItems(ItemsManagerConfigTab editor)
    {
        editor.stateListeningItems.Clear();
        var currentItemsInScene = new List<GameObject>();

        var allSceneRootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        List<GameObject> potentialItems = new List<GameObject>();
        foreach (var rootGO in allSceneRootGameObjects)
        {
            potentialItems.AddRange(rootGO.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject));
        }

        foreach (var obj in potentialItems.Distinct())
        {
            if (obj != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == PrefabPath)
            {
                if (!currentItemsInScene.Contains(obj))
                    currentItemsInScene.Add(obj);
            }
        }
        editor.stateListeningItems = currentItemsInScene;
        editor.stateListenersByItem.Clear();

        string sceneName = SceneManager.GetActiveScene().name;
        string listenerDataFolder = Path.Combine(string.Format(ScriptFolderFormat, sceneName), "StateListeners");
        Directory.CreateDirectory(listenerDataFolder);

        foreach (var item in editor.stateListeningItems)
        {
            if (item == null) continue;
            string assetPath = GetItemDataAssetPath(item);
            StateListeningItemData data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<StateListeningItemData>();
                data.stateListeners = Array.Empty<StateListener>();
                data.otherImplementation = defaultOtherImplementation;
                AssetDatabase.CreateAsset(data, assetPath);
                AssetDatabase.SaveAssets();
            }
            editor.stateListenersByItem[item] = data.stateListeners != null ? data.stateListeners.ToList() : new List<StateListener>();
        }
    }
    
    #endregion

    #region Asset & Item Modification

    public static void CreateStateListeningItem(ItemsManagerConfigTab editor)
    {
        if (string.IsNullOrEmpty(editor.newItemName))
        {
            EditorUtility.DisplayDialog("Error", "New item name cannot be empty.", "OK");
            return;
        }
        if (editor.stateListeningItems.Any(i => i != null && i.name == editor.newItemName))
        {
            EditorUtility.DisplayDialog("Error", $"An item named '{editor.newItemName}' already exists.", "OK");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError($"Prefab not found at path: {PrefabPath}"); return; }
        
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = editor.newItemName;
        Undo.RegisterCreatedObjectUndo(go, "Create StateListeningItem " + editor.newItemName);

        EnableAccessToConditions(go);
        AddDataCollectorToWorldItemReferenceList(go);

        string scene = SceneManager.GetActiveScene().name;
        string scriptFolder = string.Format(ScriptFolderFormat, scene);
        Directory.CreateDirectory(scriptFolder);
        string jsPath = Path.Combine(scriptFolder, editor.newItemName + ".js");

        if (!File.Exists(ScriptTemplatePath))
        {
            Debug.LogError($"Script template not found at: {ScriptTemplatePath}");
            Undo.DestroyObjectImmediate(go);
            return;
        }
        AssetDatabase.CopyAsset(ScriptTemplatePath, jsPath);
        AssetDatabase.Refresh();

        var combiner = go.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner != null)
        {
            var newScriptAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(jsPath);
            if (newScriptAsset != null)
            {
                combiner.ReplaceScript(newScriptAsset, 1, null, 0, true);
                EditorUtility.SetDirty(combiner);
            }
        }

        string listenerDataFolder = Path.Combine(scriptFolder, "StateListeners");
        Directory.CreateDirectory(listenerDataFolder);
        string assetPath = Path.Combine(listenerDataFolder, editor.newItemName + ".asset");
        StateListeningItemData data = ScriptableObject.CreateInstance<StateListeningItemData>();
        data.stateListeners = Array.Empty<StateListener>();
        data.otherImplementation = defaultOtherImplementation;
        AssetDatabase.CreateAsset(data, assetPath);
        
        editor._needsRebuild = true;
        editor.newItemName = string.Empty;
    }

    public static void RemoveStateListeningItem(GameObject item, ItemsManagerConfigTab editor)
    {
        if (item == null) return;
        string scene = SceneManager.GetActiveScene().name;
        string folder = string.Format(ScriptFolderFormat, scene);
        string jsPath = Path.Combine(folder, item.name + ".js");
        string assetPath = GetItemDataAssetPath(item);

        AssetDatabase.DeleteAsset(jsPath);
        AssetDatabase.DeleteAsset(assetPath);
        Undo.DestroyObjectImmediate(item);

        if (editor.stateListenersByItem.ContainsKey(item)) editor.stateListenersByItem.Remove(item);
        
        AssetDatabase.Refresh();
        editor._needsRebuild = true;
    }
    
    public static void AddStateListener(GameObject item, int stateIndex, ItemsManagerConfigTab editor)
    {
        if (item == null || !editor.stateListenersByItem.TryGetValue(item, out var listeners)) return;
        if (listeners.Any(l => l.stateID == stateIndex))
        {
            EditorUtility.DisplayDialog("Error", $"Listener for state ID {stateIndex} already exists on item '{item.name}'.", "OK");
            return;
        }
        
        string assetPath = GetItemDataAssetPath(item);
        var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);
        if (data == null) { Debug.LogError($"Could not find StateListeningItemData for {item.name}."); return; }
        
        Undo.RecordObject(data, "Add State Listener");
        var newListener = new StateListener { stateID = stateIndex };
        listeners.Add(newListener);
        data.stateListeners = listeners.ToArray();
        EditorUtility.SetDirty(data);
        editor._needsRebuild = true;
    }

    public static void RemoveStateListener(GameObject item, int stateID, ItemsManagerConfigTab editor)
    {
        string itemDataAssetPath = GetItemDataAssetPath(item);
        var itemDataAsset = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(itemDataAssetPath);

        if (itemDataAsset != null && editor.stateListenersByItem.TryGetValue(item, out var listenersList))
        {
            var listenerToRemove = listenersList.FirstOrDefault(l => l.stateID == stateID);
            if(listenerToRemove != null)
            {
                Undo.RecordObject(itemDataAsset, "Remove State Listener");
                listenersList.Remove(listenerToRemove);
                itemDataAsset.stateListeners = listenersList.ToArray();
                EditorUtility.SetDirty(itemDataAsset);
                editor._needsRebuild = true;
            }
        }
    }

    #endregion
    
    #region JS Generation and Saving

    public static void ApplyAssetsToScripts(ItemsManagerConfigTab editor)
    {
        IsApplyingAssetsToScripts = true;
        SaveAllItemsToAssets(editor);
        IsApplyingAssetsToScripts = false;

        /*
        Type csCombinerType = Type.GetType("Assets.KaomoLab.CSCombiner.CSCombiner, Assembly-CSharp-Editor");
        if (csCombinerType != null)
        {
            var method = csCombinerType.GetMethod("CombineAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, null);
        }
        */
    }

    private static void SaveAllItemsToAssets(ItemsManagerConfigTab editor)
    {
        foreach (var item in editor.stateListeningItems.Where(i => i != null))
        {
            SaveItemToAsset(item, editor);
        }
        AssetDatabase.SaveAssets();
    }

    private static void SaveItemToAsset(GameObject item, ItemsManagerConfigTab editor)
    {
        if (!item) return;

        string jsContentForItem = GenerateActionsObjectsForItem(item, editor);
        string assetPath = GetItemDataAssetPath(item);
        var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);

        string otherImplForJS = data != null ? (data.otherImplementation ?? string.Empty) : string.Empty;
        
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(jsContentForItem)) lines.Add(jsContentForItem);
        if (!string.IsNullOrWhiteSpace(otherImplForJS)) lines.Add(otherImplForJS);

        string jsPath = Path.Combine(string.Format(ScriptFolderFormat, SceneManager.GetActiveScene().name), item.name + ".js");
        File.WriteAllText(jsPath, string.Join("\n\n", lines).Trim());
        AssetDatabase.ImportAsset(jsPath, ImportAssetOptions.ForceUpdate); 

        if (data != null && editor.stateListenersByItem.TryGetValue(item, out var currentListenersInDict))
        {
            var currentListenersArray = currentListenersInDict?.ToArray() ?? Array.Empty<StateListener>();
            if (!(data.stateListeners ?? Array.Empty<StateListener>()).SequenceEqual(currentListenersArray))
            {
                Undo.RecordObject(data, "Update State Listeners in Asset for " + item.name);
                data.stateListeners = currentListenersArray;
                EditorUtility.SetDirty(data);
            }
        }
    }

    private static string GenerateActionObject(StateListenerAction action)
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

    private static string GenerateActionsObjectsForItem(GameObject item, ItemsManagerConfigTab editor)
    {
        if (item == null) return string.Empty;
        if (!editor.stateListenersByItem.TryGetValue(item, out var listeners))
        {
            listeners = new List<StateListener>();
        }

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

    private static void AppendActionsForType(System.Text.StringBuilder sb, List<StateListener> listeners, Func<StateListener, List<StateListenerAction>> actionSelector)
    {
        bool hasAppendedAnyState = false;
        foreach (var listener in listeners.OrderBy(l => l.stateID))
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

    #endregion
    
    #region Helpers

    public static string GetItemDataAssetPath(GameObject item)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string folder = Path.Combine(string.Format(ScriptFolderFormat, sceneName), "StateListeners");
        return Path.Combine(folder, item.name + ".asset");
    }

    public static bool IsTrialRelatedState(int stateID, StateList stateList)
    {
        if (stateList == null || stateList.States == null || stateList.States.Length == 0 || stateID < 0 || stateID >= stateList.States.Length) return false;

        int trialStartIndex = Array.FindIndex(stateList.States, s => s.StateName.Equals("Trial - Start", StringComparison.OrdinalIgnoreCase));
        int trialRestIndex = Array.FindIndex(stateList.States, s => s.StateName.Equals("Trial - Rest", StringComparison.OrdinalIgnoreCase));

        return trialStartIndex != -1 && trialRestIndex != -1 && trialStartIndex <= trialRestIndex && stateID >= trialStartIndex && stateID <= trialRestIndex;
    }

    private static void EnableAccessToConditions(GameObject item)
    {
        if (item == null) return;
        var itemGroupMember = item.GetComponent<ItemGroupMember>();
        if (itemGroupMember == null) return;

        foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) != ExpManagersWrapperPrefabPath) continue;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == ConditionManagerPrefabPath)
                {
                    ItemGroupHost host = child.GetComponent<ItemGroupHost>();
                    if (host != null)
                    {
                        SerializedObject serializedItemGroupMember = new SerializedObject(itemGroupMember);
                        serializedItemGroupMember.FindProperty("host").objectReferenceValue = host;
                        serializedItemGroupMember.ApplyModifiedProperties();
                    }
                }
            }
        }
    }
    
    private static void AddDataCollectorToWorldItemReferenceList(GameObject stateListeningItem)
    {
        GameObject dataCollector = null;
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            string prefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(obj));
            if (prefabPath == DataCollectorPrefabPath) dataCollector = obj;
        }
        
        if (dataCollector)
        {
            var refList = stateListeningItem.GetComponent<WorldItemReferenceList>();
            if (!refList)
            {
                refList = stateListeningItem.AddComponent(typeof(WorldItemReferenceList)) as WorldItemReferenceList;
            }
            
            SerializedObject serializedRefList = new SerializedObject(refList);
            var prop = serializedRefList.FindProperty("worldItemReferences");
            prop.InsertArrayElementAtIndex(0);
            var refItem = prop.GetArrayElementAtIndex(0);
            refItem.FindPropertyRelative("id").stringValue = "luida-data-collector";
            refItem.FindPropertyRelative("item").objectReferenceValue = dataCollector.GetComponent<Item>();
            serializedRefList.ApplyModifiedProperties();
        }
        else
            Debug.LogError("LUIDA-DataCollector prefab instance not found in the scene.");
    }

    #endregion
}
