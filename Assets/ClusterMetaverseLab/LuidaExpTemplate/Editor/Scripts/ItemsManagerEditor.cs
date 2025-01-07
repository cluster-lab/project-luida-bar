using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClusterVR.CreatorKit.Item;
using ClusterVR.CreatorKit.Item.Implements;
using System.Text.RegularExpressions;

public class ItemsManagerEditor : EditorWindow
{
    private string newItemName = "";
    private bool showCreateItemForm = false;
    private bool showCreateListenerForm = false;

    private const string prefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/StateListeningItem.prefab";
    private const string scriptFolderPathFormat = "Assets/_Experiment_/Scripts/StateManagement/{0}";
    private const string stateListeningItemScriptTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/StateManagement/StateListeningItemTemplate.js";

    private List<GameObject> stateListeningItems = new List<GameObject>();
    private GameObject selectedStateListeningItem;
    private SerializedObject selectedStateListeningItemSerialized;
    private JavaScriptAsset selectedStateListeningItemScript;
    private int selectedStateListeningItemScriptIndex;

    private StateList stateList;
    private SerializedObject serializedStateList;
    private SerializedProperty statesProperty;

    private int selectedStateIndex = 0;

    private Dictionary<int, StateListenerData> stateListeners = new Dictionary<int, StateListenerData>();
    private Dictionary<int, bool> stateListenerFoldout = new Dictionary<int, bool>();

    private Vector2 scrollPosition;

    private class StateListenerData
    {
        public List<ActionData> onWorldInitializedActions = new List<ActionData>();
        public string onWorldInitializedCustomAction = "";
        public bool onWorldInitializedFoldout = false;

        public List<ActionData> onStateStartedActions = new List<ActionData>();
        public string onStateStartedCustomAction = "";
        public bool onStateStartedFoldout = false;

        public List<ActionData> duringStateActions = new List<ActionData>();
        public string duringStateCustomAction = "";
        public bool duringStateFoldout = false;

        public List<ActionData> onStateExitedActions = new List<ActionData>();
        public string onStateExitedCustomAction = "";
        public bool onStateExitedFoldout = false;
    }

    private class ActionData
    {
        public string actionType;
        public string codeSnippet;
    }

    private string[] availableActions = new string[]
    {
        "Action 1",
        "Action 2",
        "Action 3"
    };

    private int selectedActionIndex = 0;

    public void OnEnable()
    {
    }

    public void OnGUI()
    {
        RefreshStateListeningItems();
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.BeginHorizontal();

        // Left Column
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 2));
        DrawLeftColumn();
        EditorGUILayout.EndVertical();

        // Right Column
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 2));
        DrawRightColumn();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void RefreshStateListeningItems()
    {
        stateListeningItems.Clear();
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(obj) != null)
            {
                string sourcePrefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(obj));
                if (sourcePrefabPath == prefabPath)
                {
                    stateListeningItems.Add(obj);
                }
            }
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string stateListPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
        stateList = AssetDatabase.LoadAssetAtPath<StateList>(stateListPath);

        if (stateList != null)
        {
            serializedStateList = new SerializedObject(stateList);
            statesProperty = serializedStateList.FindProperty("States");
        }
        else
        {
            serializedStateList = null;
            statesProperty = null;
        }
    }

    private void DrawLeftColumn()
    {
        EditorGUILayout.LabelField("State Listening Items", EditorStyles.boldLabel);

        foreach (GameObject item in stateListeningItems)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(item.name, EditorStyles.linkLabel))
            {
                selectedStateListeningItem = item;
                selectedStateListeningItemSerialized = new SerializedObject(selectedStateListeningItem);
                selectedStateListeningItemScript = GetClusterScriptFromItem(selectedStateListeningItem, out selectedStateListeningItemScriptIndex);
                LoadStateListeners();
            }
            EditorGUILayout.ObjectField(item, typeof(GameObject), true);
            EditorGUILayout.ObjectField(GetClusterScriptFromItem(item, out int scriptIndex), typeof(JavaScriptAsset), true);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button(showCreateItemForm ? "Hide Create Item Form" : "Create Item"))
        {
            showCreateItemForm = !showCreateItemForm;
        }

        if (showCreateItemForm)
        {
            newItemName = EditorGUILayout.TextField("Item Name", newItemName);
            if (GUILayout.Button("Create State Listening Item"))
            {
                CreateStateListeningItem();
            }
        }
    }

    private void DrawRightColumn()
    {
        if (selectedStateListeningItem != null)
        {
            EditorGUILayout.LabelField($"State Listeners for {selectedStateListeningItem.name}", EditorStyles.boldLabel);

            if (stateList != null && statesProperty != null)
            {
                for (int i = 0; i < statesProperty.arraySize; i++)
                {
                    SerializedProperty state = statesProperty.GetArrayElementAtIndex(i);
                    string stateName = state.FindPropertyRelative("StateName").stringValue;

                    if (!stateListenerFoldout.ContainsKey(i))
                    {
                        stateListenerFoldout[i] = false;
                    }

                    stateListenerFoldout[i] = EditorGUILayout.Foldout(stateListenerFoldout[i], stateName);

                    if (stateListenerFoldout[i])
                    {
                        DrawStateListener(i);
                    }
                }
            }

            if (GUILayout.Button("Apply Changes"))
            {
                ApplyChangesToScript();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button(showCreateListenerForm ? "Hide Create Listener Form" : "Add Listener"))
            {
                showCreateListenerForm = !showCreateListenerForm;
            }

            if (showCreateListenerForm)
            {
                if (stateList != null && statesProperty != null)
                {
                    List<string> stateNames = new List<string>();
                    for (int i = 0; i < statesProperty.arraySize; i++)
                    {
                        SerializedProperty state = statesProperty.GetArrayElementAtIndex(i);
                        stateNames.Add(state.FindPropertyRelative("StateName").stringValue);
                    }

                    selectedStateIndex = EditorGUILayout.Popup("Select State", selectedStateIndex, stateNames.ToArray());

                    if (GUILayout.Button("Add State Listener"))
                    {
                        AddStateListener(selectedStateIndex);
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("Select a State Listening Item to view its listeners.", EditorStyles.helpBox);
        }
    }

    private void DrawStateListener(int stateIndex)
    {
        if (!stateListeners.ContainsKey(stateIndex))
        {
            return;
        }

        StateListenerData listenerData = stateListeners[stateIndex];

        // On World Initialized
        listenerData.onWorldInitializedFoldout = EditorGUILayout.Foldout(listenerData.onWorldInitializedFoldout, "On World Initialized");
        if (listenerData.onWorldInitializedFoldout)
        {
            DrawActionsList(listenerData.onWorldInitializedActions);
            listenerData.onWorldInitializedCustomAction = EditorGUILayout.TextArea(listenerData.onWorldInitializedCustomAction, GUILayout.Height(50));
        }

        // On State Started
        listenerData.onStateStartedFoldout = EditorGUILayout.Foldout(listenerData.onStateStartedFoldout, "On State Started");
        if (listenerData.onStateStartedFoldout)
        {
            DrawActionsList(listenerData.onStateStartedActions);
            listenerData.onStateStartedCustomAction = EditorGUILayout.TextArea(listenerData.onStateStartedCustomAction, GUILayout.Height(50));
        }

        // During State
        listenerData.duringStateFoldout = EditorGUILayout.Foldout(listenerData.duringStateFoldout, "During State");
        if (listenerData.duringStateFoldout)
        {
            DrawActionsList(listenerData.duringStateActions);
            listenerData.duringStateCustomAction = EditorGUILayout.TextArea(listenerData.duringStateCustomAction, GUILayout.Height(50));
        }

        // On State Exited
        listenerData.onStateExitedFoldout = EditorGUILayout.Foldout(listenerData.onStateExitedFoldout, "On State Exited");
        if (listenerData.onStateExitedFoldout)
        {
            DrawActionsList(listenerData.onStateExitedActions);
            listenerData.onStateExitedCustomAction = EditorGUILayout.TextArea(listenerData.onStateExitedCustomAction, GUILayout.Height(50));
        }
    }

    private void DrawActionsList(List<ActionData> actions)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(actions[i].actionType);
            if (GUILayout.Button("Up") && i > 0)
            {
                ActionData temp = actions[i];
                actions[i] = actions[i - 1];
                actions[i - 1] = temp;
            }
            if (GUILayout.Button("Down") && i < actions.Count - 1)
            {
                ActionData temp = actions[i];
                actions[i] = actions[i + 1];
                actions[i + 1] = temp;
            }
            if (GUILayout.Button("Remove"))
            {
                actions.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        selectedActionIndex = EditorGUILayout.Popup("Choose action", selectedActionIndex, availableActions);
        if (GUILayout.Button("Add"))
        {
            actions.Add(new ActionData { actionType = availableActions[selectedActionIndex], codeSnippet = $"// {availableActions[selectedActionIndex]} code snippet" });
        }
    }

    private void CreateStateListeningItem()
    {
        if (string.IsNullOrEmpty(newItemName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a name for the new item.", "OK");
            return;
        }

        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
        newObject.name = newItemName;
        Undo.RegisterCreatedObjectUndo(newObject, "Create State Listening Item");

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string scriptFolderPath = string.Format(scriptFolderPathFormat, sceneName);

        if (!AssetDatabase.IsValidFolder(scriptFolderPath))
        {
            Directory.CreateDirectory(scriptFolderPath);
            AssetDatabase.Refresh();
        }

        string newScriptPath = $"{scriptFolderPath}/{newItemName}.js";
        AssetDatabase.CopyAsset(stateListeningItemScriptTemplatePath, newScriptPath);
        AssetDatabase.Refresh();

        GameObject scriptCombinerObject = newObject.GetComponent<ScriptableClusterScriptCombiner>().gameObject;
        ScriptableClusterScriptCombiner combiner = scriptCombinerObject.GetComponent<ScriptableClusterScriptCombiner>();
        var newScriptAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(newScriptPath);
        combiner.ReplaceScript(newScriptAsset, 1, null, 0, true);
        EditorUtility.SetDirty(combiner);
        EditorUtility.SetDirty(newScriptAsset);
        AssetDatabase.SaveAssets();

        RefreshStateListeningItems();

        // Automatically select the newly created item
        selectedStateListeningItem = newObject;
        selectedStateListeningItemSerialized = new SerializedObject(selectedStateListeningItem);
        selectedStateListeningItemScript = newScriptAsset;
        selectedStateListeningItemScriptIndex = 1;
        LoadStateListeners();

        newItemName = "";
        showCreateItemForm = false;
    }

    private JavaScriptAsset GetClusterScriptFromItem(GameObject item, out int scriptIndex)
    {
        scriptIndex = -1;
        ScriptableClusterScriptCombiner combiner = item.GetComponent<ScriptableClusterScriptCombiner>();
        var clusterScripts = combiner.GetClusterScripts();
        if (combiner != null && clusterScripts != null && clusterScripts.Count > 1)
        {
            scriptIndex = 1;
            return clusterScripts[1] as JavaScriptAsset;
        }
        return null;
    }

    private void AddStateListener(int stateIndex)
    {
        if (!stateListeners.ContainsKey(stateIndex))
        {
            stateListeners[stateIndex] = new StateListenerData();
            stateListenerFoldout[stateIndex] = true;
        }
    }

    private void LoadStateListeners()
    {
        stateListeners.Clear();
        stateListenerFoldout.Clear();

        if (selectedStateListeningItemScript == null) return;

        string scriptContent = GetScriptContent(selectedStateListeningItemScript);

        if (stateList != null)
        {
            for (int i = 0; i < stateList.States.Length; i++)
            {
                string stateName = stateList.States[i].StateName;
                StateListenerData listenerData = new StateListenerData();

                // Load On World Initialized actions and custom action
                string onStartPattern = @"\$\.onStart\(\(\) => \{([\s\S]*?)\}\)";
                var onStartMatch = System.Text.RegularExpressions.Regex.Match(scriptContent, onStartPattern);
                if (onStartMatch.Success)
                {
                    string onStartContent = onStartMatch.Groups[1].Value;
                    listenerData.onWorldInitializedActions = ExtractActionsFromCode(onStartContent);
                    listenerData.onWorldInitializedCustomAction = ExtractCustomActionFromCode(onStartContent);
                }

                // Load On State Started actions and custom action
                string onStateEnterPattern = $@"function OnStateEnter\(\) \{{[\s\S]*?if \(STATE_ID === {i}\) \{{([\s\S]*?)\}}[\s\S]*?\}}";
                var onStateEnterMatch = System.Text.RegularExpressions.Regex.Match(scriptContent, onStateEnterPattern);
                if (onStateEnterMatch.Success)
                {
                    string onStateEnterContent = onStateEnterMatch.Groups[1].Value;
                    listenerData.onStateStartedActions = ExtractActionsFromCode(onStateEnterContent);
                    listenerData.onStateStartedCustomAction = ExtractCustomActionFromCode(onStateEnterContent);
                }

                // Load During State actions and custom action
                string duringStatePattern = $@"function DuringState\(deltaTime\) \{{[\s\S]*?if \(STATE_ID === {i}\) \{{([\s\S]*?)\}}[\s\S]*?\}}";
                var duringStateMatch = System.Text.RegularExpressions.Regex.Match(scriptContent, duringStatePattern);
                if (duringStateMatch.Success)
                {
                    string duringStateContent = duringStateMatch.Groups[1].Value;
                    listenerData.duringStateActions = ExtractActionsFromCode(duringStateContent);
                    listenerData.duringStateCustomAction = ExtractCustomActionFromCode(duringStateContent);
                }

                // Load On State Exited actions and custom action
                string onStateExitPattern = $@"function OnStateExit\(\) \{{[\s\S]*?if \(STATE_ID === {i}\) \{{([\s\S]*?)\}}[\s\S]*?\}}";
                var onStateExitMatch = System.Text.RegularExpressions.Regex.Match(scriptContent, onStateExitPattern);
                if (onStateExitMatch.Success)
                {
                    string onStateExitContent = onStateExitMatch.Groups[1].Value;
                    listenerData.onStateExitedActions = ExtractActionsFromCode(onStateExitContent);
                    listenerData.onStateExitedCustomAction = ExtractCustomActionFromCode(onStateExitContent);
                }

                stateListeners[i] = listenerData;
                stateListenerFoldout[i] = false;
            }
        }
    }

    private List<ActionData> ExtractActionsFromCode(string code)
    {
        List<ActionData> actions = new List<ActionData>();
        foreach (string actionType in availableActions)
        {
            string pattern = $@"// {actionType} code snippet";
            if (code.Contains(pattern))
            {
                actions.Add(new ActionData { actionType = actionType, codeSnippet = pattern });
            }
        }
        return actions;
    }

    private string ExtractCustomActionFromCode(string code)
    {
        string customAction = "";
        foreach (string actionType in availableActions)
        {
            string pattern = $@"// {actionType} code snippet";
            code = code.Replace(pattern, "");
        }
        customAction = code.Trim();

        return customAction;
    }

    private void ApplyChangesToScript()
    {
        if (selectedStateListeningItem == null || selectedStateListeningItemScript == null) return;

        ScriptableClusterScriptCombiner combiner = selectedStateListeningItem.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner == null) return;

        string scriptContent = GetScriptContent(selectedStateListeningItemScript);

        // Replace OnStart content
        string onStartPattern = @"\$\.onStart\(\(\) => \{([\s\S]*?)\}\)";
        string newOnStartContent = GenerateOnStartContent();
        scriptContent = System.Text.RegularExpressions.Regex.Replace(scriptContent, onStartPattern, $"$.onStart(() => {{\n      {newOnStartContent}\n}})");

        // Replace OnStateEnter content
        string onStateEnterPattern = @"function OnStateEnter\(\) \{([\s\S]*?)\}";
        string newOnStateEnterContent = GenerateOnStateEnterContent();
        scriptContent = System.Text.RegularExpressions.Regex.Replace(scriptContent, onStateEnterPattern, $"function OnStateEnter() {{\n    {newOnStateEnterContent}\n}}");

        // Replace DuringState content
        string duringStatePattern = @"function DuringState\(deltaTime\) \{([\s\S]*?)\}";
        string newDuringStateContent = GenerateDuringStateContent();
        scriptContent = System.Text.RegularExpressions.Regex.Replace(scriptContent, duringStatePattern, $"function DuringState(deltaTime) {{\n    {newDuringStateContent}\n}}");

        // Replace OnStateExit content
        string onStateExitPattern = @"function OnStateExit\(\) \{([\s\S]*?)\}";
        string newOnStateExitContent = GenerateOnStateExitContent();
        scriptContent = System.Text.RegularExpressions.Regex.Replace(scriptContent, onStateExitPattern, $"function OnStateExit() {{\n    {newOnStateExitContent}\n}}");

        // Update the script content using ReplaceScript
        // combiner.ReplaceScript(selectedStateListeningItemScript, selectedStateListeningItemScriptIndex, scriptContent, 0, false);
        // combiner.CombineScripts();
        // EditorUtility.SetDirty(selectedStateListeningItemScript);
        // EditorUtility.SetDirty(combiner);
        // AssetDatabase.SaveAssets();
        // AssetDatabase.Refresh();

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var variablesAssetPath = string.Format(scriptFolderPathFormat, sceneName) + "/" + selectedStateListeningItemScript.name + ".js";

        // Write the changes to the actual file
        File.WriteAllText(variablesAssetPath, scriptContent);

        // Update the ScriptableObject
        SerializedObject serializedObject = new SerializedObject(selectedStateListeningItemScript);
        SerializedProperty textProperty = serializedObject.FindProperty("text");
        textProperty.stringValue = scriptContent;
        serializedObject.ApplyModifiedProperties();

        // Mark the asset as dirty and save
        EditorUtility.SetDirty(selectedStateListeningItemScript);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(variablesAssetPath);
    }

    private string GetScriptContent(JavaScriptAsset scriptAsset)
    {
        // Assuming the script content is stored in a field named 'source'
        var serializedScript = new SerializedObject(scriptAsset);
        var sourceProperty = serializedScript.FindProperty("source");
        return sourceProperty != null ? sourceProperty.stringValue : string.Empty;
    }

    private string GenerateOnStartContent()
    {
        string content = "";
        if (stateListeners.ContainsKey(0) && stateListeners[0].onWorldInitializedActions.Count > 0)
        {
            foreach (ActionData action in stateListeners[0].onWorldInitializedActions)
            {
                content += $"      {action.codeSnippet}\n";
            }
            content += $"      {stateListeners[0].onWorldInitializedCustomAction}\n";
        }
        return content;
    }

    private string GenerateOnStateEnterContent()
    {
        string content = "const STATE_ID = $.getStateCompat(\"global\", \"state_currentID\", \"integer\");\n";
        content += "    const CONDITION = $.groupState.currentCondition;\n";
        foreach (var listener in stateListeners)
        {
            int stateId = listener.Key;
            StateListenerData listenerData = listener.Value;
            if (listenerData.onStateStartedActions.Count > 0 || !string.IsNullOrEmpty(listenerData.onStateStartedCustomAction))
            {
                content += $"    if (STATE_ID === {stateId}) {{\n";
                foreach (ActionData action in listenerData.onStateStartedActions)
                {
                    content += $"      {action.codeSnippet}\n";
                }
                content += $"      {listenerData.onStateStartedCustomAction}\n";
                content += "    }\n";
            }
        }
        return content;
    }

    private string GenerateDuringStateContent()
    {
        string content = "const STATE_ID = $.getStateCompat(\"global\", \"state_currentID\", \"integer\");\n";
        content += "    const CONDITION = $.groupState.currentCondition;\n";
        foreach (var listener in stateListeners)
        {
            int stateId = listener.Key;
            StateListenerData listenerData = listener.Value;
            if (listenerData.duringStateActions.Count > 0 || !string.IsNullOrEmpty(listenerData.duringStateCustomAction))
            {
                content += $"    if (STATE_ID === {stateId}) {{\n";
                foreach (ActionData action in listenerData.duringStateActions)
                {
                    content += $"      {action.codeSnippet}\n";
                }
                content += $"      {listenerData.duringStateCustomAction}\n";
                content += "    }\n";
            }
        }
        return content;
    }

    private string GenerateOnStateExitContent()
    {
        string content = "const STATE_ID = $.getStateCompat(\"global\", \"state_currentID\", \"integer\");\n";
        content += "    const CONDITION = $.groupState.currentCondition;\n";
        foreach (var listener in stateListeners)
        {
            int stateId = listener.Key;
            StateListenerData listenerData = listener.Value;
            if (listenerData.onStateExitedActions.Count > 0 || !string.IsNullOrEmpty(listenerData.onStateExitedCustomAction))
            {
                content += $"    if (STATE_ID === {stateId}) {{\n";
                foreach (ActionData action in listenerData.onStateExitedActions)
                {
                    content += $"      {action.codeSnippet}\n";
                }
                content += $"      {listenerData.onStateExitedCustomAction}\n";
                content += "    }\n";
            }
        }
        return content;
    }
}