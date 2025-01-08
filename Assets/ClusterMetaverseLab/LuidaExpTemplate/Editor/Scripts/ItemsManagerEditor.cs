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

    // Changed: Dictionary to store StateListener lists per GameObject (StateListeningItem)
    private Dictionary<GameObject, List<StateListener>> stateListenersByItem = new Dictionary<GameObject, List<StateListener>>();
    private Dictionary<int, bool> stateListenerFoldout = new Dictionary<int, bool>();

    private Vector2 scrollPosition;

    private string[] availableActions = new string[]
    {
        "Action 1",
        "Action 2",
        "Action 3"
    };

    private int selectedActionIndex = 0;

    public void OnEnable()
    {
        RefreshStateListeningItems();
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

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string listenersFolderPath = string.Format(scriptFolderPathFormat, sceneName) + "/StateListeners";
            string listenersAssetPath = listenersFolderPath + "/" + selectedStateListeningItem.name + ".asset";
            StateListenersList selectedStateListenersList = AssetDatabase.LoadAssetAtPath<StateListenersList>(listenersAssetPath);
            if (selectedStateListenersList != null) {
                stateListenersByItem[selectedStateListeningItem] = selectedStateListenersList.listeners.ToList();
            }

            // Check if the selected item has any listeners
            if (stateListenersByItem.ContainsKey(selectedStateListeningItem) && stateListenersByItem[selectedStateListeningItem].Count > 0)
            {
                // Draw each state listener
                foreach (var listenerData in stateListenersByItem[selectedStateListeningItem])
                {
                    int stateId = listenerData.stateID;

                    // Get the state name using stateId
                    string stateName = "";
                    if (stateList != null && stateId >= 0 && stateId < stateList.States.Length)
                    {
                        stateName = stateList.States[stateId].StateName;
                    }

                    // Use stateId for foldout dictionary
                    if (!stateListenerFoldout.ContainsKey(stateId))
                    {
                        stateListenerFoldout[stateId] = false;
                    }

                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    stateListenerFoldout[stateId] = EditorGUILayout.Foldout(stateListenerFoldout[stateId], stateName);

                    if (stateListenerFoldout[stateId])
                    {
                        DrawStateListener(listenerData);
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No state listeners added yet.", EditorStyles.helpBox);
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

    // Changed: Accepts StateListener instead of stateIndex
    private void DrawStateListener(StateListener listenerData)
    {
        EditorGUILayout.BeginVertical("box");

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

        EditorGUILayout.EndVertical();
    }

    private void DrawActionsList(List<StateListeningAction> actions)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(actions[i].actionType);
            if (GUILayout.Button("Up") && i > 0)
            {
                StateListeningAction temp = actions[i];
                actions[i] = actions[i - 1];
                actions[i - 1] = temp;
            }
            if (GUILayout.Button("Down") && i < actions.Count - 1)
            {
                StateListeningAction temp = actions[i];
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
            actions.Add(new StateListeningAction { actionType = availableActions[selectedActionIndex], codeSnippet = $"// {availableActions[selectedActionIndex]} code snippet" });
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

    // Changed: Adds a new StateListener to the selected StateListeningItem
    private void AddStateListener(int stateIndex)
    {
        if (selectedStateListeningItem == null) return;

        // Ensure there's a list for the selected item
        if (!stateListenersByItem.ContainsKey(selectedStateListeningItem))
        {
            stateListenersByItem[selectedStateListeningItem] = new List<StateListener>();
        }

        // Create and add the new listener data
        StateListener newListener = new StateListener { stateID = stateIndex };
        stateListenersByItem[selectedStateListeningItem].Add(newListener);

        // Initialize foldout state for this listener
        stateListenerFoldout[stateIndex] = true;

        // Log
        Debug.Log($"Added state listener for state {stateIndex} to item {selectedStateListeningItem.name}");
    }

    // Changed: Loads StateListener for the selected StateListeningItem
    private void LoadStateListeners()
    {
        if (selectedStateListeningItem == null) return;

        // Ensure there's a list for the selected item, even if it's empty
        if (!stateListenersByItem.ContainsKey(selectedStateListeningItem))
        {
            stateListenersByItem[selectedStateListeningItem] = new List<StateListener>();
        }

        // Clear the foldout states because we're reloading
        stateListenerFoldout.Clear();

        if (selectedStateListeningItemScript == null)
        {
            Debug.LogWarning("Selected State Listening Item does not have a ClusterScript assigned.");
            return;
        }
        
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string scriptPath = string.Format(scriptFolderPathFormat, sceneName) + "/" + selectedStateListeningItemScript.name + ".js";
        string scriptContent = File.ReadAllText(scriptPath);

        // Iterate through each StateListener associated with the selected item
        foreach (StateListener listenerData in stateListenersByItem[selectedStateListeningItem])
        {
            int stateId = listenerData.stateID;
            
            // Load On State Started actions and custom action
            string onStateEnterPattern = $@"function OnStateEnter\(\) \{{[\s\S]*?if \(STATE_ID === {stateId}\) \{{([\s\S]*?)\}}[\s\S]*?\}}";
            var onStateEnterMatch = System.Text.RegularExpressions.Regex.Match(scriptContent, onStateEnterPattern);
            if (onStateEnterMatch.Success)
            {
                string onStateEnterContent = onStateEnterMatch.Groups[1].Value;
                listenerData.onStateStartedActions = ExtractActionsFromCode(onStateEnterContent);
                listenerData.onStateStartedCustomAction = ExtractCustomActionFromCode(onStateEnterContent);
            }

            // Load During State actions and custom action
            string duringStatePattern = $@"function DuringState\(deltaTime\) \{{[\s\S]*?if \(STATE_ID === {stateId}\) \{{([\s\S]*?)\}}[\s\S]*?\}}";
            var duringStateMatch = System.Text.RegularExpressions.Regex.Match(scriptContent, duringStatePattern);
            if (duringStateMatch.Success)
            {
                string duringStateContent = duringStateMatch.Groups[1].Value;
                listenerData.duringStateActions = ExtractActionsFromCode(duringStateContent);
                listenerData.duringStateCustomAction = ExtractCustomActionFromCode(duringStateContent);
            }

            // Load On State Exited actions and custom action
            string onStateExitPattern = $@"function OnStateExit\(\) \{{[\s\S]*?if \(STATE_ID === {stateId}\) \{{([\s\S]*?)\}}[\s\S]*?\}}";
            var onStateExitMatch = System.Text.RegularExpressions.Regex.Match(scriptContent, onStateExitPattern);
            if (onStateExitMatch.Success)
            {
                string onStateExitContent = onStateExitMatch.Groups[1].Value;
                listenerData.onStateExitedActions = ExtractActionsFromCode(onStateExitContent);
                listenerData.onStateExitedCustomAction = ExtractCustomActionFromCode(onStateExitContent);
            }

            // Initialize foldout states for each section
            listenerData.onStateStartedFoldout = false;
            listenerData.duringStateFoldout = false;
            listenerData.onStateExitedFoldout = false;

            // Log
            Debug.Log($"Loaded state listener data for state {stateId} from item {selectedStateListeningItem.name}");
        }
    }

    private List<StateListeningAction> ExtractActionsFromCode(string code)
    {
        List<StateListeningAction> actions = new List<StateListeningAction>();
        foreach (string actionType in availableActions)
        {
            string pattern = $@"// {actionType} code snippet";
            if (code.Contains(pattern))
            {
                actions.Add(new StateListeningAction { actionType = actionType, codeSnippet = pattern });
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

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string scriptPath = string.Format(scriptFolderPathFormat, sceneName) + "/" + selectedStateListeningItemScript.name + ".js";
        string scriptContent = File.ReadAllText(scriptPath);

        // Replace OnStateEnter content
        string onStateEnterPattern = @"function OnStateEnter\(\) \{([\s\S]*?)\}";
        string newOnStateEnterContent = GenerateOnStateEnterContent();
        scriptContent = System.Text.RegularExpressions.Regex.Replace(scriptContent, onStateEnterPattern, $"function OnStateEnter() {{\n    {newOnStateEnterContent}\n");

        // Replace DuringState content
        string duringStatePattern = @"function DuringState\(deltaTime\) \{([\s\S]*?)\}";
        string newDuringStateContent = GenerateDuringStateContent();
        scriptContent = System.Text.RegularExpressions.Regex.Replace(scriptContent, duringStatePattern, $"function DuringState(deltaTime) {{\n    {newDuringStateContent}\n");

        // Replace OnStateExit content
        string onStateExitPattern = @"function OnStateExit\(\) \{([\s\S]*?)\}";
        string newOnStateExitContent = GenerateOnStateExitContent();
        scriptContent = System.Text.RegularExpressions.Regex.Replace(scriptContent, onStateExitPattern, $"function OnStateExit() {{\n    {newOnStateExitContent}\n");

        var variablesAssetPath = string.Format(scriptFolderPathFormat, sceneName) + "/" + selectedStateListeningItemScript.name + ".js";

        // Write the changes to the actual file
        File.WriteAllText(variablesAssetPath, scriptContent);

        // Update the ScriptableObject
        SerializedObject serializedObject = new SerializedObject(selectedStateListeningItemScript);
        SerializedProperty textProperty = serializedObject.FindProperty("text");
        textProperty.stringValue = scriptContent;
        serializedObject.ApplyModifiedProperties();

        if (stateListenersByItem.ContainsKey(selectedStateListeningItem)) {
            StateListenersList asset = ScriptableObject.CreateInstance<StateListenersList>();
            asset.listeners = stateListenersByItem[selectedStateListeningItem].ToArray();
            string listenersFolderPath = string.Format(scriptFolderPathFormat, sceneName) + "/StateListeners";
            if (!Directory.Exists(listenersFolderPath))
            {
                Directory.CreateDirectory(listenersFolderPath);
            }
            string listenersAssetPath = listenersFolderPath + "/" + selectedStateListeningItem.name + ".asset";
            Debug.Log(asset);
            Debug.Log(listenersAssetPath);
            AssetDatabase.CreateAsset(asset, listenersAssetPath);
            EditorUtility.SetDirty(combiner);
        }

        // Mark the asset as dirty and save
        EditorUtility.SetDirty(selectedStateListeningItemScript);
        EditorUtility.SetDirty(combiner);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(variablesAssetPath);
    }

    private string GenerateOnStateEnterContent()
    {
        string content = "const STATE_ID = $.getStateCompat(\"global\", \"state_currentID\", \"integer\");\n";
        content += "    const CONDITION = $.groupState.currentCondition;\n";

        // Check if the selected item has any listeners
        if (stateListenersByItem.ContainsKey(selectedStateListeningItem))
        {
            // Aggregate On State Enter content from all listeners of the selected item
            foreach (StateListener listenerData in stateListenersByItem[selectedStateListeningItem])
            {
                int stateId = listenerData.stateID;
                if (listenerData.onStateStartedActions.Count > 0 || !string.IsNullOrEmpty(listenerData.onStateStartedCustomAction))
                {
                    content += $"    if (STATE_ID === {stateId}) {{\n";
                    foreach (StateListeningAction action in listenerData.onStateStartedActions)
                    {
                        content += $"      {action.codeSnippet}\n";
                    }
                    content += $"      {listenerData.onStateStartedCustomAction}\n";
                    content += "    }\n";
                }
            }
        }
        return content;
    }

    private string GenerateDuringStateContent()
    {
        string content = "const STATE_ID = $.getStateCompat(\"global\", \"state_currentID\", \"integer\");\n";
        content += "    const CONDITION = $.groupState.currentCondition;\n";

        // Check if the selected item has any listeners
        if (stateListenersByItem.ContainsKey(selectedStateListeningItem))
        {
            // Aggregate During State content from all listeners of the selected item
            foreach (StateListener listenerData in stateListenersByItem[selectedStateListeningItem])
            {
                int stateId = listenerData.stateID;
                if (listenerData.duringStateActions.Count > 0 || !string.IsNullOrEmpty(listenerData.duringStateCustomAction))
                {
                    content += $"    if (STATE_ID === {stateId}) {{\n";
                    foreach (StateListeningAction action in listenerData.duringStateActions)
                    {
                        content += $"      {action.codeSnippet}\n";
                    }
                    content += $"      {listenerData.duringStateCustomAction}\n";
                    content += "    }\n";
                }
            }
        }
        return content;
    }

    private string GenerateOnStateExitContent()
    {
        string content = "const STATE_ID = $.getStateCompat(\"global\", \"state_currentID\", \"integer\");\n";
        content += "    const CONDITION = $.groupState.currentCondition;\n";

        // Check if the selected item has any listeners
        if (stateListenersByItem.ContainsKey(selectedStateListeningItem))
        {
            // Aggregate On State Exit content from all listeners of the selected item
            foreach (StateListener listenerData in stateListenersByItem[selectedStateListeningItem])
            {
                int stateId = listenerData.stateID;
                if (listenerData.onStateExitedActions.Count > 0 || !string.IsNullOrEmpty(listenerData.onStateExitedCustomAction))
                {
                    content += $"    if (STATE_ID === {stateId}) {{\n";
                    foreach (StateListeningAction action in listenerData.onStateExitedActions)
                    {
                        content += $"      {action.codeSnippet}\n";
                    }
                    content += $"      {listenerData.onStateExitedCustomAction}\n";
                    content += "    }\n";
                }
            }
        }
        return content;
    }
}
