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
    private static StateListeningAction[] AvailableStateListeningActions = {
        new StateListeningAction("Show item", "$.setStateCompat('this', 'exp_showItem', true);"),
        new StateListeningAction("Hide item", "$.setStateCompat('this', 'exp_showItem', false);"),
        new StateListeningAction("To next state", "$.sendSignalCompat('this', 'state_triggerTransition');"),
        new StateListeningAction("Record custom data", "$.sendSignalCompat('this', 'exp_recordCustomData');"),
        new StateListeningAction("Upload recorded data", "$.sendSignalCompat('this', 'exp_uploadCustomData');"),
    };

    private string newItemName = "";
    private GameObject referenceObject = null;

    private const string prefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/StateListeningItem.prefab";
    private const string scriptFolderPathFormat = "Assets/_Experiment_/Scripts/StateManagement/{0}";
    private const string stateListeningItemScriptTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/StateManagement/StateListeningItemTemplate.js";
    private const string RequiredObjectsWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";
    private const string ConditionManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionManager.prefab";

    private List<GameObject> stateListeningItems = new List<GameObject>();
    private GameObject selectedStateListeningItem;
    private SerializedObject selectedStateListeningItemSerialized;
    private JavaScriptAsset selectedStateListeningItemScript;
    private int selectedStateListeningItemScriptIndex;
    private bool isStateListeningItemAssetLoaded;

    private StateList stateList;
    private SerializedObject serializedStateList;
    private SerializedProperty statesProperty;

    private int selectedStateIndex = 0;

    // Dictionary to store StateListener lists per GameObject (StateListeningItem)
    private Dictionary<GameObject, List<StateListener>> stateListenersByItem = new Dictionary<GameObject, List<StateListener>>();
    private Dictionary<GameObject, string> otherImplementationByItem = new Dictionary<GameObject, string>();
    private Dictionary<int, bool> stateListenerFoldout = new Dictionary<int, bool>();

    private Vector2 scrollPosition;

    private int selectedActionIndex = 0;
    private Dictionary<List<StateListenerAction>, bool> isAddingActionState = new Dictionary<List<StateListenerAction>, bool>();

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
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 4));
        DrawLeftColumn();
        EditorGUILayout.EndVertical();

        // Middle Column
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width));
        DrawMiddleColumn();
        EditorGUILayout.EndVertical();

        // Right Column
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 4));
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
        EditorGUILayout.LabelField("State Listening Items", EditorStyles.largeLabel);

        var items = stateListeningItems.ToArray();
        foreach (GameObject item in items)
        {
            if (item)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(item.name, EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.ObjectField(item, typeof(GameObject), true, GUILayout.Width(100));
                EditorGUILayout.ObjectField(GetClusterScriptFromItem(item, out int scriptIndex), typeof(JavaScriptAsset), true, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Destroy"))
                {
                    DestroyStateListeningItem(item);
                }
                if (GUILayout.Button("Confirm/Edit Actions"))
                {
                    selectedStateListeningItem = item;
                    selectedStateListeningItemSerialized = new SerializedObject(selectedStateListeningItem);
                    selectedStateListeningItemScript = GetClusterScriptFromItem(selectedStateListeningItem, out selectedStateListeningItemScriptIndex);
                    isStateListeningItemAssetLoaded = false;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("Create State-Listening Item", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Item Name", GUILayout.Width(70));
        newItemName = EditorGUILayout.TextField(newItemName, GUILayout.Width(80));
        
        EditorGUILayout.LabelField("(Optional) Create with existing object", GUILayout.Width(210));
        referenceObject = (GameObject)EditorGUILayout.ObjectField(referenceObject, typeof(GameObject), true, GUILayout.Width(150));

        if (GUILayout.Button("Create", GUILayout.Width(100)))
        {
            CreateStateListeningItem(referenceObject);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawMiddleColumn()
    {
        if (selectedStateListeningItem != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Edit Item {selectedStateListeningItem.name}", EditorStyles.largeLabel);
            if (GUILayout.Button("CLICK TO APPLY CHANGES"))
            {
                ApplyChangesToScript();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.LabelField($"Actions listening to states", EditorStyles.boldLabel);
            // Load item asset and import its content
            if (!isStateListeningItemAssetLoaded)
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                string listenersFolderPath = string.Format(scriptFolderPathFormat, sceneName) + "/StateListeners";
                string listenersAssetPath = listenersFolderPath + "/" + selectedStateListeningItem.name + ".asset";
                StateListeningItemData selectedStateListeningItemData = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(listenersAssetPath);
                if (selectedStateListeningItemData != null)
                {
                    if (stateListenersByItem.ContainsKey(selectedStateListeningItem))
                    {
                        stateListenersByItem[selectedStateListeningItem] = selectedStateListeningItemData.stateListeners.ToList();
                    }
                    else
                    {
                        stateListenersByItem.Add(selectedStateListeningItem, selectedStateListeningItemData.stateListeners.ToList());
                    }

                    if (otherImplementationByItem.ContainsKey(selectedStateListeningItem))
                    {
                        otherImplementationByItem[selectedStateListeningItem] = selectedStateListeningItemData.otherImplementation;
                    }
                    else
                    {
                        otherImplementationByItem.Add(selectedStateListeningItem, selectedStateListeningItemData.otherImplementation);
                    }
                }

                isStateListeningItemAssetLoaded = true;
            }

            // Check if the selected item has any listeners
            if (stateListenersByItem.ContainsKey(selectedStateListeningItem) && stateListenersByItem[selectedStateListeningItem].Count > 0)
            {
                // Draw each state listener
                List<int> listenerIndicesToRemove = new List<int>();
                for (var i = 0; i < stateListenersByItem[selectedStateListeningItem].Count; i++)
                {
                    var listenerData = stateListenersByItem[selectedStateListeningItem][i];
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

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Listen to State:", GUILayout.Width(90));
                    stateListenerFoldout[stateId] = EditorGUILayout.Foldout(stateListenerFoldout[stateId], stateName, EditorStyles.foldoutHeader);
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        listenerIndicesToRemove.Add(i);
                    }
                    EditorGUILayout.EndHorizontal();

                    if (stateListenerFoldout[stateId])
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space(20);
                        DrawStateListener(listenerData);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.Space();
                }

                // Remove listeners marked for removal
                foreach (int index in listenerIndicesToRemove.OrderByDescending(i => i))
                {
                    stateListenersByItem[selectedStateListeningItem].RemoveAt(index);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No state listeners added yet.", EditorStyles.helpBox);
            }
            
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Add State Listener", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (stateList != null && statesProperty != null)
            {
                List<string> stateNames = new List<string>();
                for (int i = 0; i < statesProperty.arraySize; i++)
                {
                    SerializedProperty state = statesProperty.GetArrayElementAtIndex(i);
                    stateNames.Add(state.FindPropertyRelative("StateName").stringValue);
                }

                selectedStateIndex = EditorGUILayout.Popup("Select State", selectedStateIndex, stateNames.ToArray());

                if (GUILayout.Button("Add"))
                {
                    AddStateListener(selectedStateIndex);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("Other implementation not listening to any state", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Implement other Cluster Script callbacks (e.g., $.onInteract, $.onGrab, ...) or your custom functions here.", MessageType.Info);
            EditorGUILayout.HelpBox("Don't use $.onUpdate here!\nImplement `function Update(deltaTime) {...}` instead.", MessageType.Warning);

            if (!otherImplementationByItem.ContainsKey(selectedStateListeningItem) || otherImplementationByItem[selectedStateListeningItem] == null)
            {
                otherImplementationByItem.Add(selectedStateListeningItem, "");
            }
            otherImplementationByItem[selectedStateListeningItem] = EditorGUILayout.TextArea(otherImplementationByItem[selectedStateListeningItem], GUILayout.ExpandHeight(true));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (GUILayout.Button("CLICK TO APPLY CHANGES"))
            {
                ApplyChangesToScript();
            }
        }
        else
        {
            EditorGUILayout.LabelField("Select a State Listening Item to view its listeners.", EditorStyles.helpBox);
        }
    }

    private void DrawRightColumn()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(350));
        EditorGUILayout.LabelField("Available variables and functions inside code blocks", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox("Check this column when you are implementing inside any code block on this panel.", MessageType.Info);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("--------------- Variables ---------------", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("CONDITION", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("⋅ Accessible within 'Trial' states.\n⋅ Values are determined by your configured experimental variables and vary across trials.\n⋅ Use CONDITION[\"condition_name\"] to reference a specific condition within the current trial.", MessageType.Info);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("--------------- Functions ---------------", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("ShowItem()", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Make the item visible for users", MessageType.Info);
        EditorGUILayout.LabelField("HideItem()", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Make the item invisible for users", MessageType.Info);
        EditorGUILayout.LabelField("ToNextState()", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Transit to the next state", MessageType.Info);
        EditorGUILayout.LabelField("RecordCustomData()", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Record custom data as how you defined in the DataRecorder object", MessageType.Info);
        EditorGUILayout.LabelField("UploadRecordedData()", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Upload the recorded custom data", MessageType.Info);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawStateListener(StateListener listenerData)
    {
        EditorGUILayout.BeginVertical("box");

        // On State Started
        listenerData.onStateStartedFoldout = EditorGUILayout.Foldout(listenerData.onStateStartedFoldout, "On State Started", EditorStyles.foldoutHeader);
        if (listenerData.onStateStartedFoldout)
        {
            DrawActionsList(listenerData.onStateStartedActions, ref listenerData.onStateStartedFoldout);
        }
        
        EditorGUILayout.Space();

        // During State
        listenerData.duringStateFoldout = EditorGUILayout.Foldout(listenerData.duringStateFoldout, "During State", EditorStyles.foldoutHeader);
        if (listenerData.duringStateFoldout)
        {
            DrawActionsList(listenerData.duringStateActions, ref listenerData.duringStateFoldout);
        }
        
        EditorGUILayout.Space();

        // On State Exited
        listenerData.onStateExitedFoldout = EditorGUILayout.Foldout(listenerData.onStateExitedFoldout, "On State Exited", EditorStyles.foldoutHeader);
        if (listenerData.onStateExitedFoldout)
        {
            DrawActionsList(listenerData.onStateExitedActions, ref listenerData.onStateExitedFoldout);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActionsList(List<StateListenerAction> actions, ref bool isFoldoutOpen)
    {
        List<int> actionsIndicesToRemove = new List<int>();
        if (actions.Count == 0)
        {
            EditorGUILayout.LabelField("No actions added yet.", EditorStyles.helpBox);
        }
        else
        {
            EditorGUILayout.BeginVertical("box");
            for (int i = 0; i < actions.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(20);

                // Display action label as text
                EditorGUILayout.LabelField("Action " + (i + 1) + ":", GUILayout.Width(70));
                EditorGUILayout.LabelField(actions[i].GetActionLabel(), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                
                // Edit Custom Action (if applicable)
                if (actions[i].predefinedAction.actionType == null || actions[i].predefinedAction.actionType.Length == 0)
                {
                    if (GUILayout.Button(actions[i].showCustomActionFoldout ? "Hide Code Block" : "Edit Script", GUILayout.Width(120)))
                    {
                        actions[i].showCustomActionFoldout = !actions[i].showCustomActionFoldout;
                    }
                }

                if (GUILayout.Button("▲", GUILayout.Width(30)) && i > 0)
                {
                    StateListenerAction temp = actions[i];
                    actions[i] = actions[i - 1];
                    actions[i - 1] = temp;
                }
                if (GUILayout.Button("▼", GUILayout.Width(30)) && i < actions.Count - 1)
                {
                    StateListenerAction temp = actions[i];
                    actions[i] = actions[i + 1];
                    actions[i + 1] = temp;
                }
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    actionsIndicesToRemove.Add(i);
                }
                EditorGUILayout.EndHorizontal();

                // Custom Action Text Area (if applicable)
                if (actions[i].showCustomActionFoldout)
                {
                    if (actions[i].customAction != null)
                    {
                        actions[i].customAction = EditorGUILayout.TextArea(actions[i].customAction, GUILayout.ExpandHeight(true));
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        // Remove actions marked for removal
        foreach (int index in actionsIndicesToRemove.OrderByDescending(i => i))
        {
            actions.RemoveAt(index);
        }

        // Ensure the dictionary has an entry for this actions list
        if (!isAddingActionState.ContainsKey(actions))
        {
            isAddingActionState[actions] = false;
        }

        // Add new actions UI
        if (!isAddingActionState[actions])
        {
            if (GUILayout.Button("+", GUILayout.Width(100))) // The '+' button
            {
                isAddingActionState[actions] = true;
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal("box", GUILayout.MaxWidth(200));
            if (GUILayout.Button("Hide", GUILayout.Width(40))) // Hide button
            {
                isAddingActionState[actions] = false;
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Add Predefined action");
            EditorGUILayout.BeginHorizontal();
            selectedActionIndex = EditorGUILayout.Popup(selectedActionIndex, AvailableStateListeningActions.Select(action => action.actionType).ToArray());
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                actions.Add(new StateListenerAction(AvailableStateListeningActions[selectedActionIndex]));
                isAddingActionState[actions] = false; // Hide the UI after adding
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box", GUILayout.Width(10));
            EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(10));
            EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(10));
            EditorGUILayout.EndVertical();
        
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Add Custom action");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                actions.Add(new StateListenerAction { customAction = "" });
                isAddingActionState[actions] = false; // Hide the UI after adding
            }
            EditorGUILayout.LabelField("* You can edit its script later", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void CreateStateListeningItem(GameObject referenceObject = null)
    {
        if (string.IsNullOrEmpty(newItemName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a name for the new item.", "OK");
            return;
        }

        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
        newObject.name = newItemName;
        EnableAccessToConditions(newObject);
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

        CopyFromReferenceObject(newObject, referenceObject);

        RefreshStateListeningItems();

        // Automatically select the newly created item
        selectedStateListeningItem = newObject;
        selectedStateListeningItemSerialized = new SerializedObject(selectedStateListeningItem);
        selectedStateListeningItemScript = newScriptAsset;
        selectedStateListeningItemScriptIndex = 1;

        newItemName = "";
        referenceObject = null;
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

        // Check if a listener for this state already exists
        if (stateListenersByItem[selectedStateListeningItem].Any(listener => listener.stateID == stateIndex))
        {
            EditorUtility.DisplayDialog("Error", $"A listener for state {stateIndex} already exists in this item.", "OK");
            return;
        }

        // Create and add the new listener data
        StateListener newListener = new StateListener { stateID = stateIndex };
        stateListenersByItem[selectedStateListeningItem].Add(newListener);

        // Initialize foldout state for this listener
        stateListenerFoldout[stateIndex] = true;

        // Log
        Debug.Log($"Added state listener for state {stateIndex} to item {selectedStateListeningItem.name}");
    }

    private void DestroyStateListeningItem(GameObject item)
    {
        if (item == null) return;

        if (EditorUtility.DisplayDialog("Destroy State Listening Item",
                $"Are you sure you want to destroy {item.name}?\n\n" +
                "This will also delete its associated ClusterScript and State Listeners.",
                "Destroy", "Keep it"))
        {
            // Remove from the list of items
            stateListeningItems.Remove(item);

            // Delete the associated ClusterScript
            JavaScriptAsset script = GetClusterScriptFromItem(item, out int scriptIndex);
            if (script != null)
            {
                string scriptPath = AssetDatabase.GetAssetPath(script);
                AssetDatabase.DeleteAsset(scriptPath);
            }

            // Delete the associated StateListeningItemData asset
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string listenersFolderPath = string.Format(scriptFolderPathFormat, sceneName) + "/StateListeners";
            string listenersAssetPath = listenersFolderPath + "/" + item.name + ".asset";
            if (File.Exists(listenersAssetPath))
            {
                AssetDatabase.DeleteAsset(listenersAssetPath);
            }

            // Destroy the item in the scene
            Undo.DestroyObjectImmediate(item);

            // Deselect the item
            if (selectedStateListeningItem == item)
            {
                selectedStateListeningItem = null;
                selectedStateListeningItemSerialized = null;
                selectedStateListeningItemScript = null;
                selectedStateListeningItemScriptIndex = -1;
            }

            // Refresh the list of items
            RefreshStateListeningItems();

            // Refresh the AssetDatabase
            AssetDatabase.Refresh();
        }
    }

    private void ApplyChangesToScript()
    {
        if (selectedStateListeningItem == null || selectedStateListeningItemScript == null) return;

        ScriptableClusterScriptCombiner combiner = selectedStateListeningItem.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner == null) return;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string scriptPath = string.Format(scriptFolderPathFormat, sceneName) + "/" + selectedStateListeningItemScript.name + ".js";
        string scriptContent = File.ReadAllText(scriptPath);
        
        string newScriptContent = "";
        newScriptContent += GenerateOnStateEnterFunction();
        newScriptContent += "\n";
        newScriptContent += GenerateDuringStateFunction();
        newScriptContent += "\n";
        newScriptContent += GenerateOnStateExitFunction();
        if (otherImplementationByItem.ContainsKey(selectedStateListeningItem))
        {
            newScriptContent += "\n";
            newScriptContent += otherImplementationByItem[selectedStateListeningItem];
        }
        newScriptContent += "\n";
        

        // Write the changes to the actual file
        File.WriteAllText(scriptPath, newScriptContent);

        if (stateListenersByItem.ContainsKey(selectedStateListeningItem) && otherImplementationByItem.ContainsKey(selectedStateListeningItem))
        {
            StateListeningItemData asset = ScriptableObject.CreateInstance<StateListeningItemData>();
            asset.stateListeners = stateListenersByItem[selectedStateListeningItem].ToArray();
            asset.otherImplementation = otherImplementationByItem[selectedStateListeningItem];
            string listenersFolderPath = string.Format(scriptFolderPathFormat, sceneName) + "/StateListeners";
            if (!Directory.Exists(listenersFolderPath))
            {
                Directory.CreateDirectory(listenersFolderPath);
            }
            string listenersAssetPath = listenersFolderPath + "/" + selectedStateListeningItem.name + ".asset";
            AssetDatabase.CreateAsset(asset, listenersAssetPath);
            EditorUtility.SetDirty(combiner);
        }

        // Mark the asset as dirty and save
        EditorUtility.SetDirty(selectedStateListeningItemScript);
        EditorUtility.SetDirty(combiner);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(scriptPath);

        Assets.KaomoLab.CSCombiner.CSCombiner.CombineAll();
    }
    
    private string GenerateStateFunction(
        string functionName,
        Func<StateListener, List<StateListenerAction>> actionSelector,
        string extraParameters = "")
    {
        // Build the function signature, optionally adding extra parameters.
        var content = $"function {functionName}({extraParameters}) {{\n";
        content += "  const STATE_ID = $.state.state_id;\n";
        content += "  const CONDITION = $.groupState.currentCondition;\n\n";

        // Check if the selected item has any listeners
        if (stateListenersByItem.ContainsKey(selectedStateListeningItem))
        {
            // Aggregate action content from all listeners of the selected item
            foreach (var listenerData in stateListenersByItem[selectedStateListeningItem])
            {
                var actions = actionSelector(listenerData);
                if (actions.Count > 0)
                {
                    content += $"  if (STATE_ID === {listenerData.stateID}) {{\n";
                    foreach (var action in actions)
                    {
                        content += $"    {action.GetActionContent()}\n";
                    }
                    content += "  }\n";
                }
            }
        }

        content += "}\n\n";
        return content;
    }

    private string GenerateOnStateEnterFunction()
    {
        return GenerateStateFunction(
            "OnStateEnter",
            listener => listener.onStateStartedActions
        );
    }

    private string GenerateDuringStateFunction()
    {
        return GenerateStateFunction(
            "DuringState",
            listener => listener.duringStateActions,
            "deltaTime"
        );
    }

    private string GenerateOnStateExitFunction()
    {
        return GenerateStateFunction(
            "OnStateExit",
            listener => listener.onStateExitedActions
        );
    }
    
    private GameObject FindConditionManagerPrefabInstance()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == RequiredObjectsWrapperPrefabPath)
            {
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    Transform child = obj.transform.GetChild(i);
                    if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == ConditionManagerPrefabPath)
                    {
                        return child.gameObject;
                    }
                }
            }
        }
        return null;
    }

    private void EnableAccessToConditions(GameObject item)
    {
        // Attach ItemGroupMember component to this object
        var itemGroupMember = item.GetComponent<ClusterVR.CreatorKit.Item.Implements.ItemGroupMember>();

        // Find the ConditionManager GameObject in the scene
        GameObject conditionManagerObject = FindConditionManagerPrefabInstance();
        if (conditionManagerObject != null)
        {
            // Get the ItemGroupHost component from ConditionManager
            var conditionManagerHost = conditionManagerObject.GetComponent<ClusterVR.CreatorKit.Item.Implements.ItemGroupHost>();
            if (conditionManagerHost != null)
            {
                // Use reflection or internal accessors to assign the host
                var serializedItemGroupMember = new UnityEditor.SerializedObject(itemGroupMember);
                var hostProperty = serializedItemGroupMember.FindProperty("host");

                if (hostProperty != null)
                {
                    hostProperty.objectReferenceValue = conditionManagerHost;
                    serializedItemGroupMember.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogError("Unable to find 'host' property in ItemGroupMember.");
                }
            }
            else
            {
                Debug.LogError("ConditionManager does not have an ItemGroupHost component.");
            }
        }
        else
        {
            Debug.LogError("ConditionManager GameObject not found in the scene.");
        }
    }

    private void CopyFromReferenceObject(GameObject newObject, GameObject referenceObject)
    {
        if (!referenceObject) return;
        
        // Copy values from the reference object's Item component to the new object's Item component (without removing it)
        var referenceItem = referenceObject.GetComponent<ClusterVR.CreatorKit.Item.Implements.Item>();
        var newItem = newObject.GetComponent<ClusterVR.CreatorKit.Item.Implements.Item>();

        if (referenceItem != null && newItem != null)
        {
            CopyItemComponentValues(referenceItem, newItem);
        }

        // Copy the Transform component values from the reference object to the new object
        newObject.transform.position = referenceObject.transform.position;
        newObject.transform.rotation = referenceObject.transform.rotation;
        newObject.transform.localScale = referenceObject.transform.localScale;

        // Copy all other components (excluding ScriptableItem and ScriptableClusterScriptCombiner)
        var components = referenceObject.GetComponents<Component>().Where(c => !(c is ClusterVR.CreatorKit.Item.Implements.ScriptableItem) && !(c is ScriptableClusterScriptCombiner) && !(c is Transform));
        foreach (var component in components)
        {
            if (component is ClusterVR.CreatorKit.Item.Implements.Item) continue;
            UnityEditorInternal.ComponentUtility.CopyComponent(component);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(newObject);
        }

        // Copy all child GameObjects
        foreach (Transform child in referenceObject.transform)
        {
            GameObject newChild = GameObject.Instantiate(child.gameObject, newObject.transform);
            newChild.name = child.name;
        }
    }
    
    private void CopyItemComponentValues(ClusterVR.CreatorKit.Item.Implements.Item sourceItem, ClusterVR.CreatorKit.Item.Implements.Item targetItem)
    {
        // Use SerializedObject to copy field values between components
        SerializedObject sourceSerializedItem = new SerializedObject(sourceItem);
        SerializedObject targetSerializedItem = new SerializedObject(targetItem);

        // Iterate over all properties of the Item component and copy values from source to target
        SerializedProperty property = sourceSerializedItem.GetIterator();
        while (property.NextVisible(true))
        {
            targetSerializedItem.CopyFromSerializedProperty(property);
        }

        targetSerializedItem.ApplyModifiedProperties(); // Apply changes to the target Item component
    }
}
