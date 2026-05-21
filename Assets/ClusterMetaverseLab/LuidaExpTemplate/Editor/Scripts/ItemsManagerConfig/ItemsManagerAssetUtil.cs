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
                data.eventHandlers = new List<EventHandlerData>();
                data.otherImplementation = string.Empty;
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
        AddAvatarSpawnerToWorldItemReferenceList(go);

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
        data.eventHandlers = new List<EventHandlerData>();
        data.otherImplementation = string.Empty;
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

    public static GameObject DuplicateStateListeningItem(GameObject source, ItemsManagerConfigTab editor)
    {
        if (source == null) return null;

        string newName = EditorInputDialog.Show("Duplicate Item", "Enter a name for the copy:", source.name + "_Copy");
        if (string.IsNullOrEmpty(newName)) return null;
        if (!Regex.IsMatch(newName, @"^[A-Za-z0-9 _\-\.]+$"))
        {
            EditorUtility.DisplayDialog("Error", $"'{newName}' contains characters that are not allowed in a file name.", "OK");
            return null;
        }
        if (editor.stateListeningItems.Any(i => i != null && i.name == newName))
        {
            EditorUtility.DisplayDialog("Error", $"An item named '{newName}' already exists.", "OK");
            return null;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError($"Prefab not found at path: {PrefabPath}"); return null; }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = newName;
        Undo.RegisterCreatedObjectUndo(go, "Duplicate item " + newName);

        EnableAccessToConditions(go);
        AddDataCollectorToWorldItemReferenceList(go);

        string scene = SceneManager.GetActiveScene().name;
        string scriptFolder = string.Format(ScriptFolderFormat, scene);
        Directory.CreateDirectory(scriptFolder);
        string jsPath = Path.Combine(scriptFolder, newName + ".js");

        if (!File.Exists(ScriptTemplatePath))
        {
            Debug.LogError($"Script template not found at: {ScriptTemplatePath}");
            Undo.DestroyObjectImmediate(go);
            return null;
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
        string newAssetPath = Path.Combine(listenerDataFolder, newName + ".asset");
        var newData = ScriptableObject.CreateInstance<StateListeningItemData>();

        string sourceAssetPath = GetItemDataAssetPath(source);
        var sourceData = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(sourceAssetPath);
        if (sourceData != null && sourceData.stateListeners != null)
        {
            var cloned = new List<StateListener>(sourceData.stateListeners.Length);
            foreach (var l in sourceData.stateListeners) cloned.Add(l.DeepClone());
            newData.stateListeners = cloned.ToArray();

            var clonedEvents = new List<EventHandlerData>();
            if (sourceData.eventHandlers != null)
            {
                foreach (var h in sourceData.eventHandlers) if (h != null) clonedEvents.Add(h.DeepClone());
            }
            newData.eventHandlers = clonedEvents;

            newData.otherImplementation = sourceData.otherImplementation ?? string.Empty;
        }
        else
        {
            newData.stateListeners = Array.Empty<StateListener>();
            newData.eventHandlers = new List<EventHandlerData>();
            newData.otherImplementation = string.Empty;
        }
        AssetDatabase.CreateAsset(newData, newAssetPath);

        editor._needsRebuild = true;
        return go;
    }

    public static void DuplicateAction(StateListeningItemData asset, List<StateListenerAction> phaseList, int sourceIndex)
    {
        if (asset == null || phaseList == null || sourceIndex < 0 || sourceIndex >= phaseList.Count) return;
        Undo.RecordObject(asset, "Duplicate Action");
        phaseList.Insert(sourceIndex + 1, phaseList[sourceIndex].Clone());
        EditorUtility.SetDirty(asset);
    }

    public static void DuplicateListenerToState(GameObject item, int sourceStateID, int targetStateID, ItemsManagerConfigTab editor)
    {
        if (item == null) return;
        if (!editor.stateListenersByItem.TryGetValue(item, out var listeners)) return;

        var source = listeners.FirstOrDefault(l => l.stateID == sourceStateID);
        if (source == null) return;
        if (listeners.Any(l => l.stateID == targetStateID))
        {
            EditorUtility.DisplayDialog("Error", $"State ID {targetStateID} already has a listener on item '{item.name}'.", "OK");
            return;
        }

        string assetPath = GetItemDataAssetPath(item);
        var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);
        if (data == null) return;

        Undo.RecordObject(data, "Duplicate Listener");
        listeners.Add(source.DeepClone(targetStateID));
        data.stateListeners = listeners.ToArray();
        EditorUtility.SetDirty(data);
        editor._needsRebuild = true;
    }

    public static void MoveListener(
        ItemsManagerConfigTab.ListenerDragPayload payload,
        ItemsManagerConfigTab editor,
        GameObject targetItem, int targetStateID)
    {
        if (payload == null || payload.sourceListener == null) return;
        if (!editor.stateListenersByItem.TryGetValue(payload.sourceItem, out var sourceListeners)) return;
        if (!editor.stateListenersByItem.TryGetValue(targetItem, out var targetListeners)) return;
        if (targetListeners.Any(l => l.stateID == targetStateID)) return;

        string targetAssetPath = GetItemDataAssetPath(targetItem);
        var targetAsset = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(targetAssetPath);
        if (payload.sourceAsset != null) Undo.RecordObject(payload.sourceAsset, "Move Listener");
        if (targetAsset != null && targetAsset != payload.sourceAsset) Undo.RecordObject(targetAsset, "Move Listener");

        sourceListeners.Remove(payload.sourceListener);
        targetListeners.Add(payload.sourceListener.DeepClone(targetStateID));

        if (payload.sourceAsset != null) EditorUtility.SetDirty(payload.sourceAsset);
        if (targetAsset != null) EditorUtility.SetDirty(targetAsset);

        SyncListenersArray(editor, payload.sourceItem);
        if (payload.sourceItem != targetItem) SyncListenersArray(editor, targetItem);
    }

    public static List<StateListenerAction> GetPhaseList(StateListener l, string phaseKey)
    {
        if (l == null) return null;
        switch (phaseKey)
        {
            case "OnStateStart": return l.onStateStartedActions;
            case "DuringState": return l.duringStateActions;
            case "OnStateExit": return l.onStateExitedActions;
            default: return null;
        }
    }

    private static void SyncListenersArray(ItemsManagerConfigTab editor, GameObject item)
    {
        if (item == null) return;
        string path = GetItemDataAssetPath(item);
        var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(path);
        if (data != null && editor.stateListenersByItem.TryGetValue(item, out var list))
        {
            data.stateListeners = list.ToArray();
            EditorUtility.SetDirty(data);
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
        string eventHandlerBlocks = GenerateEventHandlersForItem(item, editor);
        string assetPath = GetItemDataAssetPath(item);
        var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);

        string legacyOther = data != null ? (data.otherImplementation ?? string.Empty) : string.Empty;
        if (IsLegacyBoilerplate(legacyOther)) legacyOther = string.Empty;

        var lines = new List<string>();

        // Conflict detector: structured Start/Update vs legacy function Start/Update.
        // Emit a comment only — never auto-strip legacy edits.
        string conflictHeader = BuildLegacyConflictHeader(data, legacyOther);
        if (!string.IsNullOrEmpty(conflictHeader)) lines.Add(conflictHeader);

        if (!string.IsNullOrWhiteSpace(jsContentForItem)) lines.Add(jsContentForItem);
        if (!string.IsNullOrWhiteSpace(eventHandlerBlocks)) lines.Add(eventHandlerBlocks);
        if (!string.IsNullOrWhiteSpace(legacyOther))
        {
            lines.Add("// ---- legacy free-form code (migrate to event handlers when possible) ----");
            lines.Add(legacyOther);
        }

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

    /// <summary>True when the string is empty or matches the historical defaultOtherImplementation
    /// boilerplate (all lines commented out, no real code). Normalizes line endings because
    /// the verbatim defaultOtherImplementation literal preserves whatever CRLF/LF mix existed in
    /// the source file, while Unity may rewrite asset text on save.</summary>
    internal static bool IsLegacyBoilerplate(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return true;
        string norm = s.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        string def = defaultOtherImplementation.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        return norm == def;
    }

    private static readonly Regex LegacyFunctionStartRegex = new Regex(@"^[ \t]*function\s+Start\s*\(", RegexOptions.Multiline);
    private static readonly Regex LegacyFunctionUpdateRegex = new Regex(@"^[ \t]*function\s+Update\s*\(", RegexOptions.Multiline);

    private static string BuildLegacyConflictHeader(StateListeningItemData data, string legacyOther)
    {
        if (data == null || data.eventHandlers == null) return null;
        if (string.IsNullOrWhiteSpace(legacyOther)) return null;
        bool structuredStart = data.eventHandlers.Any(h => h != null && h.eventType == "Start");
        bool structuredUpdate = data.eventHandlers.Any(h => h != null && h.eventType == "Update");
        bool legacyStart = structuredStart && LegacyFunctionStartRegex.IsMatch(legacyOther);
        bool legacyUpdate = structuredUpdate && LegacyFunctionUpdateRegex.IsMatch(legacyOther);
        if (!legacyStart && !legacyUpdate) return null;

        var sb = new System.Text.StringBuilder();
        if (legacyStart) sb.AppendLine("// [LUIDA WARNING] Both a structured Start handler and a legacy function Start() exist — the latter wins due to JS hoisting. Migrate the legacy version into the structured handler.");
        if (legacyUpdate) sb.AppendLine("// [LUIDA WARNING] Both a structured Update handler and a legacy function Update() exist — the latter wins due to JS hoisting. Migrate the legacy version into the structured handler.");
        return sb.ToString().TrimEnd();
    }

    private static string GenerateEventHandlersForItem(GameObject item, ItemsManagerConfigTab editor)
    {
        string assetPath = GetItemDataAssetPath(item);
        var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);
        if (data == null) return string.Empty;
        if (data.eventHandlers == null) data.eventHandlers = new List<EventHandlerData>();
        if (data.eventHandlers.Count == 0) return string.Empty;

        var sb = new System.Text.StringBuilder();
        bool first = true;
        foreach (var handler in data.eventHandlers)
        {
            if (handler == null || string.IsNullOrEmpty(handler.eventType)) continue;
            if (!ItemsManagerUIDrawer.TryGetEventDefinition(handler.eventType, out var def))
            {
                if (!first) sb.AppendLine();
                sb.AppendLine($"// [LUIDA] Unknown event '{handler.eventType}' — skipped");
                first = false;
                continue;
            }

            var bodyLines = new List<string>();

            // Start runs before $.onUpdate's first tick that populates CONDITION / PARTICIPANTS
            // (see StateListeningItemBase.js:19-20). Prime them so conditional actions and
            // PARTICIPANTS[...] references don't throw TypeError on the first frame.
            if (handler.eventType == "Start")
            {
                bodyLines.Add("    CONDITION = $.groupState.currentCondition;");
                bodyLines.Add("    PARTICIPANTS = [null].concat($.groupState.participants || []);");
            }

            if (handler.actions != null)
            {
                foreach (var action in handler.actions)
                {
                    // Sleep emits a bare numeric value (e.g. "3") that only the per-state action
                    // loop in StateListeningItemBase.js interprets via {type:"sleep"}. In an event
                    // handler body it would become a no-op expression statement. Defensive skip.
                    if (action != null && action.predefinedActionTemplate.actionType == "Sleep") continue;

                    string code = action?.GetActionContent();
                    if (string.IsNullOrWhiteSpace(code)) continue;
                    foreach (var ln in code.Replace("\r\n", "\n").Split('\n'))
                    {
                        bodyLines.Add("    " + ln);
                    }
                }
            }

            string body = bodyLines.Count > 0
                ? string.Join("\n", bodyLines)
                : "    // (no actions)";

            if (!first) sb.AppendLine();
            sb.AppendLine(def.jsWrapperFormat.Replace("{body}", body));
            first = false;
        }
        return sb.ToString().TrimEnd();
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
        return $"{{ type: \"exec\", action: (deltaTime) => {{\n            {actionCode}\n        }} }}";
    }

    private static string GenerateActionsObjectsForItem(GameObject item, ItemsManagerConfigTab editor)
    {
        if (item == null) return string.Empty;
        if (!editor.stateListenersByItem.TryGetValue(item, out var originalListeners))
        {
            originalListeners = new List<StateListener>();
        }
        
        var cleanedListeners = new List<StateListener>();
        var stateIdsSeen = new HashSet<int>();
        bool wasModified = false;
    
        for (int i = originalListeners.Count - 1; i >= 0; i--)
        {
            var listener = originalListeners[i];
    
            if (listener.stateID >= 0 && stateIdsSeen.Add(listener.stateID))
            {
                cleanedListeners.Add(listener);
            }
        }
        
        cleanedListeners.Reverse();
    
        if (cleanedListeners.Count != originalListeners.Count)
        {
            wasModified = true;
        }
    
        if (wasModified)
        {
            editor.stateListenersByItem[item] = cleanedListeners;
    
            string assetPath = GetItemDataAssetPath(item);
            var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);
            if (data != null)
            {
                Undo.RecordObject(data, "Clean Up and Deduplicate State Listeners");
                data.stateListeners = cleanedListeners.ToArray();
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
            }
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("const stateEnterActions = {");
        AppendActionsForType(sb, cleanedListeners, l => l.onStateStartedActions);
        sb.AppendLine("};\n");

        sb.AppendLine("const duringStateActions = {");
        AppendActionsForType(sb, cleanedListeners, l => l.duringStateActions);
        sb.AppendLine("};\n");

        sb.AppendLine("const stateExitActions = {");
        AppendActionsForType(sb, cleanedListeners, l => l.onStateExitedActions);
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

    private static void AddAvatarSpawnerToWorldItemReferenceList(GameObject stateListeningItem)
    {
        GameObject spawner = AvatarsConfigAssetUtil.FindSpawnerInScene();
        if (spawner == null) return; // Spawner not yet in scene — reference will be added when spawner is installed

        var refList = stateListeningItem.GetComponent<WorldItemReferenceList>();
        if (!refList)
        {
            refList = stateListeningItem.AddComponent(typeof(WorldItemReferenceList)) as WorldItemReferenceList;
        }

        // If an entry already exists with this id, rebind its item slot when missing
        // (e.g. after a previous spawner GameObject was deleted/recreated, leaving a
        // dangling null reference). Without this rebind, $.worldItemReference(...).send(...)
        // silently no-ops at runtime.
        SerializedObject serializedRefList = new SerializedObject(refList);
        var prop = serializedRefList.FindProperty("worldItemReferences");
        for (int i = 0; i < prop.arraySize; i++)
        {
            var entry = prop.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("id").stringValue != "LUIDA-AvatarSpawner") continue;

            var itemProp = entry.FindPropertyRelative("item");
            if (itemProp.objectReferenceValue == null)
            {
                itemProp.objectReferenceValue = spawner.GetComponent<Item>();
                serializedRefList.ApplyModifiedProperties();
            }
            return;
        }

        prop.InsertArrayElementAtIndex(prop.arraySize);
        var refItem = prop.GetArrayElementAtIndex(prop.arraySize - 1);
        refItem.FindPropertyRelative("id").stringValue = "LUIDA-AvatarSpawner";
        refItem.FindPropertyRelative("item").objectReferenceValue = spawner.GetComponent<Item>();
        serializedRefList.ApplyModifiedProperties();
    }

    /// <summary>
    /// Called by AvatarsConfigAssetUtil when a spawner is installed into the scene.
    /// Adds the spawner reference to all existing state-listening items.
    /// </summary>
    public static void AddAvatarSpawnerReferenceToAllItems()
    {
        GameObject spawner = AvatarsConfigAssetUtil.FindSpawnerInScene();
        if (spawner == null) return;

        var allSceneRootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var rootGO in allSceneRootGameObjects)
        {
            foreach (var transform in rootGO.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject) == PrefabPath)
                {
                    AddAvatarSpawnerToWorldItemReferenceList(transform.gameObject);
                }
            }
        }
    }

    #endregion
}
