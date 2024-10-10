#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class StateDependentObjectEditor : EditorWindow
{
    private StateList stateList;
    private SerializedObject serializedStateList;
    private SerializedProperty statesProperty;
    private string stateDependentObjectPrefabPath = "Assets/_ExpWorld_/Prefabs/StateManagement/StateDependentObject.prefab";
    private string jsTemplatePath = "Assets/_ExpWorld_/Scripts/StateManagement/StateDependentObjectTemplate.js";

    // A dictionary to store the text field input for each state
    private Dictionary<string, string> objectNames = new Dictionary<string, string>();
    private List<GameObject> createdObjects = new List<GameObject>(); // Store created objects for display
    private Dictionary<GameObject, bool> alwaysVisibleStates = new Dictionary<GameObject, bool>(); // Store 'Always keep visible' state

    private Dictionary<string, bool> showNewObjectRow = new Dictionary<string, bool>(); // Track visibility of the new object row for each state

    private Vector2 scrollPosition; // Scroll position for the window

    public static void ShowWindow()
    {
        GetWindow<StateDependentObjectEditor>("State Dependent Object Editor");
    }

    public void OnEnable()
    {
        string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        string stateListPath = scenePath.Replace("Scenes", "ExpSettings/StateList").Replace(".unity", ".asset");

        stateList = AssetDatabase.LoadAssetAtPath<StateList>(stateListPath);
        if (stateList != null)
        {
            serializedStateList = new SerializedObject(stateList);
            statesProperty = serializedStateList.FindProperty("States");
        }

        // Retrieve existing state-dependent objects
        RetrieveCreatedObjects();
    }

    public void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition); // Start scrollable area

        // Instruction text
        EditorGUILayout.HelpBox("Click 'Add Object for this state' to add a new state-dependent object.", MessageType.Info);
        EditorGUILayout.HelpBox("Remember to click 'Update script' button after editting the cluster script for a state-dependent object.", MessageType.Info);

        if (stateList == null)
        {
            EditorGUILayout.HelpBox("StateList not found. Please ensure you're in the correct scene with the StateList asset.", MessageType.Error);
            return;
        }

        serializedStateList.Update();

        // Display fields for each state
        for (int i = 0; i < statesProperty.arraySize; i++)
        {
            SerializedProperty state = statesProperty.GetArrayElementAtIndex(i);
            string stateName = state.FindPropertyRelative("StateName").stringValue;

            // Initialize visibility dictionary for the new object row
            if (!showNewObjectRow.ContainsKey(stateName)) showNewObjectRow[stateName] = false;

            // Bold state name with a button to show the 'Add Object' row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(stateName, EditorStyles.boldLabel);
            if (GUILayout.Button("Add Object for this state", GUILayout.Width(200)))
            {
                showNewObjectRow[stateName] = !showNewObjectRow[stateName]; // Toggle the new object row visibility
            }
            EditorGUILayout.EndHorizontal();

            // Display list of existing objects for this state
            List<GameObject> stateObjects = createdObjects.Where(o => o.transform.parent != null && (o.transform.parent.parent.name == stateName || o.transform.parent.name == stateName)).ToList();
            foreach (var obj in stateObjects)
            {
                DisplayObjectRow(obj, stateName);
            }

            // Show the new object row only if the corresponding button has been clicked
            if (showNewObjectRow[stateName])
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("New Object", GUILayout.Width(100));

                // Initialize text field storage for the state
                if (!objectNames.ContainsKey(stateName))
                {
                    objectNames[stateName] = string.Empty;
                }

                // Text field for new object name
                objectNames[stateName] = EditorGUILayout.TextField(objectNames[stateName], GUILayout.Width(200));

                // Add Object button
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(objectNames[stateName]));
                if (GUILayout.Button("Add Object", GUILayout.Width(150)))
                {
                    AddStateDependentObject(stateName, i, objectNames[stateName]);
                    objectNames[stateName] = string.Empty;  // Clear the text field after adding an object
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }

            // Add space after each state section
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView(); // End scrollable area

        serializedStateList.ApplyModifiedProperties();
    }

    // Adds the new state-dependent object and its corresponding JS script
    private void AddStateDependentObject(string stateName, int stateId, string objectName)
    {
        GameObject stateObject = GameObject.Find("States")?.transform.Find(stateName)?.Find("EnabledObjects")?.gameObject;
        if (stateObject == null)
        {
            Debug.LogError($"State {stateName} not found or missing EnabledObjects.");
            return;
        }

        // Instantiate the prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(stateDependentObjectPrefabPath);
        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        newObject.name = objectName;
        newObject.transform.SetParent(stateObject.transform);  // Place under EnabledObjects

        // Set the state_id property (similar to qID setting)
        SetStateIdForObject(newObject, stateId);

        // Ensure the folder for the current scene exists
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string scriptFolderPath = $"Assets/_ExpWorld_/Scripts/StateManagement/{sceneName}";
        if (!Directory.Exists(scriptFolderPath))
        {
            Directory.CreateDirectory(scriptFolderPath);
            AssetDatabase.Refresh();
        }

        // Duplicate the JS script
        string newScriptPath = $"{scriptFolderPath}/{objectName}.js";
        AssetDatabase.CopyAsset(jsTemplatePath, newScriptPath);
        AssetDatabase.Refresh();

        // Retrieve the ScriptableClusterScriptCombiner and run ReplaceScript
        GameObject scriptCombinerObject = newObject.GetComponent<ScriptableClusterScriptCombiner>().gameObject;
        ScriptableClusterScriptCombiner combiner = scriptCombinerObject.GetComponent<ScriptableClusterScriptCombiner>();
        var newScriptAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(newScriptPath);
        combiner.ReplaceScript(newScriptAsset, 1, null, 0, false);

        // Add the created object to the list for display and initialize its 'Always visible' state
        createdObjects.Add(newObject);
        alwaysVisibleStates[newObject] = false; // Default to false

        // Refresh the UI
        Repaint();
    }

    // Sets the state_id for the newly instantiated object
    private void SetStateIdForObject(GameObject obj, int stateId)
    {
        Component itemLogic = obj.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
        if (itemLogic != null)
        {
            SerializedObject serializedComp = new SerializedObject(itemLogic);
            SerializedProperty specificProperty = serializedComp.FindProperty("logic.statements");

            if (specificProperty != null && specificProperty.isArray)
            {
                for (int i = 0; i < specificProperty.arraySize; i++)
                {
                    SerializedProperty targetKey = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "state_id")
                    {
                        SerializedProperty stateIdProp = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (stateIdProp != null)
                        {
                            stateIdProp.intValue = stateId;
                            serializedComp.ApplyModifiedProperties();
                            break;
                        }
                    }
                }
            }
        }
    }

    // Retrieves all existing StateDependentObject instances from the scene and stores them for display
    private void RetrieveCreatedObjects()
    {
        createdObjects.Clear();
        alwaysVisibleStates.Clear();

        GameObject statesObject = GameObject.Find("States");
        if (statesObject != null)
        {
            foreach (Transform stateTransform in statesObject.transform)
            {
                // Retrieve from EnabledObjects
                Transform enabledObjects = stateTransform.Find("EnabledObjects");
                if (enabledObjects != null)
                {
                    foreach (Transform objTransform in enabledObjects)
                    {
                        GameObject obj = objTransform.gameObject;
                        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == stateDependentObjectPrefabPath)
                        {
                            createdObjects.Add(obj);
                            alwaysVisibleStates[obj] = false; // Default to false
                        }
                    }
                }

                // Also retrieve objects directly under the stateName
                foreach (Transform objTransform in stateTransform)
                {
                    GameObject obj = objTransform.gameObject;
                    if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == stateDependentObjectPrefabPath && objTransform.name != "EnabledObjects")
                    {
                        createdObjects.Add(obj);
                        alwaysVisibleStates[obj] = true; // Default to true for objects directly under the state
                    }
                }
            }
        }
    }

    // Displays a row for each existing state-dependent object
    private void DisplayObjectRow(GameObject obj, string stateName)
    {
        EditorGUILayout.BeginHorizontal();

        // Reference to the GameObject
        EditorGUILayout.ObjectField("GameObject", obj, typeof(GameObject), true);

        GUILayout.Space(10); // Add space between columns

        // Retrieve the ScriptableClusterScriptCombiner component
        ScriptableClusterScriptCombiner combiner = obj.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner != null)
        {
            // Reference to the JS script asset
            EditorGUILayout.ObjectField("JS Script", combiner.ClusterScripts[1], typeof(ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset), true);
        }

        GUILayout.Space(10); // Add space between columns

        // Checkbox for 'Always keep visible'
        bool alwaysVisible = alwaysVisibleStates.ContainsKey(obj) && alwaysVisibleStates[obj];
        alwaysVisibleStates[obj] = EditorGUILayout.Toggle("Always keep visible", alwaysVisible);

        GUILayout.Space(10); // Add space between columns

        // Update Script button
        if (GUILayout.Button("Update Script", GUILayout.Width(150)))
        {
            combiner?.CombineScripts();
        }

        // Remove button
        if (GUILayout.Button("Remove", GUILayout.Width(100)))
        {
            RemoveObject(obj);
        }

        EditorGUILayout.EndHorizontal();

        // Move the GameObject based on the checkbox value
        if (alwaysVisible)
        {
            GameObject stateParent = GameObject.Find("States")?.transform.Find(stateName)?.gameObject;
            obj.transform.SetParent(stateParent != null ? stateParent.transform : null); // Move under the stateName object
        }
        else
        {
            GameObject stateObject = GameObject.Find("States")?.transform.Find(stateName)?.Find("EnabledObjects")?.gameObject;
            if (stateObject != null)
            {
                obj.transform.SetParent(stateObject.transform); // Move back to EnabledObjects
            }
        }
    }

    // Removes the GameObject and the corresponding row from the editor
    private void RemoveObject(GameObject obj)
    {
        createdObjects.Remove(obj);
        alwaysVisibleStates.Remove(obj);
        DestroyImmediate(obj);
        Debug.Log("GameObject removed.");
        Repaint();
    }
}
#endif
