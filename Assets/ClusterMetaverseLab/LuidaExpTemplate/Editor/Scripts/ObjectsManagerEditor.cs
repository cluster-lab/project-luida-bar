using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class ObjectsManagerEditor : EditorWindow
{
    private StateList stateList;
    private SerializedObject serializedStateList;
    private SerializedProperty statesProperty;
    private string stateDependentObjectPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/StateDependentObject.prefab";
    private string conditionDependentObjectPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionDependentObject.prefab";
    private string formPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/Questionnaire/Questionnaire.prefab";
    private string jsStateTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/StateManagement/StateDependentObjectTemplate.js";
    private string jsConditionTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/ConditionManagement/ConditionDependentObjectTemplate.js";
    private string identifiersAssetPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    
    // Dictionaries to store text field input for each state
    private Dictionary<string, string> objectNames = new Dictionary<string, string>();
    private Dictionary<string, string> conditionObjectNames = new Dictionary<string, string>();
    private Dictionary<string, int> questionnaireQIDs = new Dictionary<string, int>();
    private Dictionary<string, bool> showQuestionnaireForm = new Dictionary<string, bool>();

    private List<GameObject> createdObjects = new List<GameObject>(); // Store created objects for display

    // Dictionaries to track foldout states for collapsible sections
    private Dictionary<string, bool> stateObjectsFoldout = new Dictionary<string, bool>();
    private Dictionary<string, bool> conditionObjectsFoldout = new Dictionary<string, bool>();

    private Dictionary<string, bool> showNewObjectRow = new Dictionary<string, bool>(); // Track visibility of the new object row for each state
    private Dictionary<string, bool> showNewConditionObjectRow = new Dictionary<string, bool>(); // Track visibility of the new condition object row for each state

    private Vector2 scrollPosition; // Scroll position for the window

    public static void ShowWindow()
    {
        GetWindow<ObjectsManagerEditor>("Objects Manager");
    }

    public void OnEnable()
    {
        string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        string stateListPath = scenePath.Replace("Scenes", "Settings/StateList").Replace(".unity", ".asset");

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
        EditorGUILayout.HelpBox("Manage state-dependent, condition-dependent objects, and questionnaires for each state.", MessageType.Info);
        EditorGUILayout.HelpBox("Click 'Add object dependent to this state' to add a new object dependent to this state.", MessageType.Info);
        EditorGUILayout.HelpBox("Click 'Add condition-dependent object in this state' to add a condition-dependent object inside this state.", MessageType.Info);
        EditorGUILayout.HelpBox("Remember to click 'Update script' button after editing the script for an object.", MessageType.Info);
        EditorGUILayout.HelpBox("Click 'Add questionnaire' to add a questionnaire inside this state.", MessageType.Info);

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

            // Initialize visibility dictionaries for new object and condition object rows
            if (!showNewObjectRow.ContainsKey(stateName)) showNewObjectRow[stateName] = false;
            if (!showNewConditionObjectRow.ContainsKey(stateName)) showNewConditionObjectRow[stateName] = false;
            if (!showQuestionnaireForm.ContainsKey(stateName)) showQuestionnaireForm[stateName] = false;

            if (!stateObjectsFoldout.ContainsKey(stateName)) stateObjectsFoldout[stateName] = true;
            if (!conditionObjectsFoldout.ContainsKey(stateName)) conditionObjectsFoldout[stateName] = true;

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Bold state name with buttons to show 'Add Object' and 'Add Condition-dependent Object' rows
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(stateName, EditorStyles.boldLabel);
            if (GUILayout.Button("Add object dependent to this state", GUILayout.Width(250)))
            {
                showNewObjectRow[stateName] = !showNewObjectRow[stateName]; // Toggle the new object row visibility
            }
            if (GUILayout.Button("Add condition-dependent object inside this state", GUILayout.Width(300)))
            {
                showNewConditionObjectRow[stateName] = !showNewConditionObjectRow[stateName]; // Toggle the new condition object row visibility
            }

            if (HasEnabledFormInstance(GameObject.Find(stateName)?.transform.Find("Objects")?.gameObject))
            {
                EditorGUI.BeginDisabledGroup(true); // Disable the button if a questionnaire already exists
                GUILayout.Button("Add questionnaire", GUILayout.Width(150));
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                if (GUILayout.Button("Add questionnaire", GUILayout.Width(150)))
                {
                    showQuestionnaireForm[stateName] = !showQuestionnaireForm[stateName]; // Toggle the add questionnaire form visibility
                }
            }

            EditorGUILayout.EndHorizontal();

            // Collapsible list for state-dependent objects
            stateObjectsFoldout[stateName] = EditorGUILayout.Foldout(stateObjectsFoldout[stateName], "State-Dependent Objects", true, EditorStyles.foldout);
            if (stateObjectsFoldout[stateName])
            {
                List<GameObject> stateDependentObjects = createdObjects.Where(o => o.transform.parent != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o) == stateDependentObjectPrefabPath && (o.transform.parent.parent.name == stateName || o.transform.parent.name == stateName)).ToList();
                foreach (var obj in stateDependentObjects)
                {
                    DisplayObjectRow(obj, stateName);
                }
            }

            // Collapsible list for condition-dependent objects
            conditionObjectsFoldout[stateName] = EditorGUILayout.Foldout(conditionObjectsFoldout[stateName], "Condition-Dependent Objects", true, EditorStyles.foldout);
            if (conditionObjectsFoldout[stateName])
            {
                List<GameObject> conditionDependentObjects = createdObjects.Where(o => o.transform.parent != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o) == conditionDependentObjectPrefabPath && (o.transform.parent.parent.name == stateName || o.transform.parent.name == stateName)).ToList();
                foreach (var obj in conditionDependentObjects)
                {
                    DisplayObjectRow(obj, stateName);
                }
            }

            // Show the new object row only if the corresponding button has been clicked
            if (showNewObjectRow[stateName])
            {
                DisplayNewObjectRow(stateName, i, stateDependentObjectPrefabPath, jsStateTemplatePath, "New State Object");
            }

            // Show the new condition-dependent object row only if the corresponding button has been clicked
            if (showNewConditionObjectRow[stateName])
            {
                DisplayNewObjectRow(stateName, i, conditionDependentObjectPrefabPath, jsConditionTemplatePath, "New Condition-dependent Object");
            }

            // Show the questionnaire form if the "Add questionnaire" button was clicked
            if (showQuestionnaireForm[stateName])
            {
                DisplayQuestionnaireForm(stateName, i);
            }

            // Display existing questionnaire if it exists
            DisplayQuestionnaireRow(stateName);

            // Add space after each state section
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView(); // End scrollable area

        serializedStateList.ApplyModifiedProperties();
    }

    // Displays a row for creating a new object
    private void DisplayNewObjectRow(string stateName, int stateId, string prefabPath, string jsTemplatePath, string label)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(200));

        EditorGUILayout.LabelField("Name", GUILayout.Width(35));
        // Initialize text field storage for the state
        if (!objectNames.ContainsKey(stateName))
        {
            objectNames[stateName] = string.Empty;
        }

        // Text field for new object name
        objectNames[stateName] = EditorGUILayout.TextField(objectNames[stateName], GUILayout.Width(200));

        GUILayout.Space(30);

        // Add Object button
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(objectNames[stateName]));
        if (GUILayout.Button("Add Object", GUILayout.Width(150)))
        {
            AddObject(stateName, stateId, objectNames[stateName], prefabPath, jsTemplatePath);
            objectNames[stateName] = string.Empty;  // Clear the text field after adding an object
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    // Displays the form to add a new questionnaire
    private void DisplayQuestionnaireForm(string stateName, int stateId)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Questionnaire qID", GUILayout.Width(120));
        if (!questionnaireQIDs.ContainsKey(stateName))
        {
            questionnaireQIDs[stateName] = 0;  // Initialize with default qID
        }

        questionnaireQIDs[stateName] = EditorGUILayout.IntField(questionnaireQIDs[stateName], GUILayout.Width(50));

        GUILayout.Space(30);
        if (GUILayout.Button("Add Questionnaire", GUILayout.Width(150)))
        {
            AddOrEnableFormInstance(stateName, questionnaireQIDs[stateName]);
            showQuestionnaireForm[stateName] = false; // Hide the form after adding
        }
        EditorGUILayout.EndHorizontal();
    }

    // Displays a row for the existing questionnaire object if present
    private void DisplayQuestionnaireRow(string stateName)
    {
        GameObject stateObject = GameObject.Find(stateName)?.transform.Find("Objects")?.gameObject;
        if (stateObject != null && HasEnabledFormInstance(stateObject))
        {
            GameObject formController = GetFormController(stateObject);
            int currentQID = GetCurrentQID(formController);

            EditorGUILayout.BeginHorizontal();
            
            // Reference field for the Questionnaire object
            GameObject questionnaireObject = formController.transform.parent.gameObject;
            EditorGUILayout.LabelField("Questionnaire Object", GUILayout.Width(120));
            EditorGUILayout.ObjectField(questionnaireObject, typeof(GameObject), true, GUILayout.Width(120));
            
            GUILayout.Space(20); // Add space between columns

            // qID text field
            EditorGUILayout.LabelField("qID", GUILayout.Width(20));
            int newQID = EditorGUILayout.IntField(currentQID, GUILayout.Width(50));
            if (newQID != currentQID)
            {
                UpdateQID(formController, newQID);
            }

            GUILayout.Space(20); // Add space between columns

            // Remove button for the questionnaire
            if (GUILayout.Button("Remove Questionnaire", GUILayout.Width(150)))
            {
                RemoveFormInstance(stateObject, formController);
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }

    // Check if a valid and enabled Questionnaire prefab instance exists in the state's Objects
    private bool HasEnabledFormInstance(GameObject stateObject)
    {
        foreach (Transform child in stateObject.transform)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath && child.gameObject.activeSelf)
            {
                return true;
            }
        }
        return false;
    }

    // Get the FormController game object inside the Questionnaire prefab
    private GameObject GetFormController(GameObject stateObject)
    {
        foreach (Transform child in stateObject.transform)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath)
            {
                return child.Find("FormController")?.gameObject;
            }
        }
        return null;
    }

    // Get the current qID value from the FormController
    private int GetCurrentQID(GameObject formController)
    {
        if (formController == null) return -1;

        Component itemLogic = formController.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
        if (itemLogic != null)
        {
            SerializedObject serializedComp = new SerializedObject(itemLogic);
            SerializedProperty specificProperty = serializedComp.FindProperty("logic.statements");

            if (specificProperty != null && specificProperty.isArray)
            {
                for (int i = 0; i < specificProperty.arraySize; i++)
                {
                    SerializedProperty targetKey = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "qID")
                    {
                        SerializedProperty qIdProp = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (qIdProp != null)
                        {
                            return qIdProp.intValue;
                        }
                    }
                }
            }
        }
        return -1;
    }

    // Update the qID value in the FormController
    private void UpdateQID(GameObject formController, int qID)
    {
        Component itemLogic = formController.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
        if (itemLogic != null)
        {
            SerializedObject serializedComp = new SerializedObject(itemLogic);
            SerializedProperty specificProperty = serializedComp.FindProperty("logic.statements");

            if (specificProperty != null && specificProperty.isArray)
            {
                for (int i = 0; i < specificProperty.arraySize; i++)
                {
                    SerializedProperty targetKey = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "qID")
                    {
                        SerializedProperty qIdProp = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (qIdProp != null)
                        {
                            qIdProp.intValue = qID;
                            serializedComp.ApplyModifiedProperties();
                            break;
                        }
                    }
                }
            }
        }
    }

    // Remove the Questionnaire instance from the scene by disabling it and setting qID to -1
    private void RemoveFormInstance(GameObject stateObject, GameObject formController)
    {
        if (formController != null)
        {
            GameObject formInstance = formController.transform.parent.gameObject;
            DestroyImmediate(formInstance);
            Debug.Log($"Questionnaire instance in {stateObject.name} removed.");
        }
    }

    // Add or re-enable a Questionnaire instance to the selected state and set its qID
    private void AddOrEnableFormInstance(string stateName, int qID)
    {
        GameObject stateObject = GameObject.Find(stateName)?.transform.Find("Objects")?.gameObject;

        if (stateObject != null)
        {
            GameObject existingInstance = stateObject.transform.Cast<Transform>()
                .FirstOrDefault(child => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath)?.gameObject;

            if (existingInstance != null && !existingInstance.activeSelf)
            {
                existingInstance.SetActive(true); // Re-enable the existing instance
                GameObject formController = existingInstance.transform.Find("FormController")?.gameObject;
                UpdateQID(formController, qID);

                // Paste WorldItemReferenceList component to FormController
                CopyWorldItemReferenceListToFormController(formController);

                Debug.Log($"Questionnaire instance in {stateName} re-enabled with qID {qID}");
            }
            else
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(formPrefabPath);
                if (prefab != null)
                {
                    GameObject newFormInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    newFormInstance.transform.SetParent(stateObject.transform);
                    newFormInstance.name = prefab.name;

                    GameObject formController = newFormInstance.transform.Find("FormController")?.gameObject;
                    if (formController != null)
                    {
                        var identifiersAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(identifiersAssetPath);
                        ScriptableClusterScriptCombiner combiner = formController.GetComponent<ScriptableClusterScriptCombiner>();
                        combiner.ReplaceScript(identifiersAsset, 0, null, 0, true);
                        
                        UpdateQID(formController, qID);

                        // Paste WorldItemReferenceList component to FormController
                        CopyWorldItemReferenceListToFormController(formController);

                        Debug.Log($"Questionnaire instance added to {stateName} with qID {qID}");
                    }
                }
            }
        }
    }

    // Adds the new state-dependent object or condition-dependent object and its corresponding JS script
    private void AddObject(string stateName, int stateId, string objectName, string prefabPath, string jsTemplatePath)
    {
        GameObject stateObject = GameObject.Find("States")?.transform.Find(stateName)?.Find("Objects")?.gameObject;
        if (stateObject == null)
        {
            Debug.LogError($"State {stateName} not found or missing Objects.");
            return;
        }

        // Instantiate the prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        newObject.name = objectName;
        newObject.transform.SetParent(stateObject.transform);  // Place under Objects

        // Set the state_id property (similar to qID setting)
        SetStateIdForObject(newObject, stateId);

        // Copy WorldItemReferenceList component from WorldItemRefList
        CopyWorldItemReferenceListToNewObject(newObject);

        // Ensure the folder for the current scene exists
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string scriptFolderPath = "";
        if (jsTemplatePath.Contains("StateManagement"))
        {
            scriptFolderPath = $"Assets/_Experiment_/Scripts/StateManagement/{sceneName}";
        }
        else if (jsTemplatePath.Contains("ConditionManagement"))
        {
            scriptFolderPath = $"Assets/_Experiment_/Scripts/ConditionManagement/{sceneName}";
        }

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
        combiner.ReplaceScript(newScriptAsset, 1, null, 0, true);

        // Add the created object to the list for display
        createdObjects.Add(newObject);

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

    // Retrieves all existing objects from the scene and stores them for display
    private void RetrieveCreatedObjects()
    {
        createdObjects.Clear();

        GameObject statesObject = GameObject.Find("States");
        if (statesObject != null)
        {
            foreach (Transform stateTransform in statesObject.transform)
            {
                // Retrieve from Objects
                Transform objects = stateTransform.Find("Objects");
                if (objects != null)
                {
                    foreach (Transform objTransform in objects)
                    {
                        GameObject obj = objTransform.gameObject;
                        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == stateDependentObjectPrefabPath ||
                            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == conditionDependentObjectPrefabPath)
                        {
                            createdObjects.Add(obj);
                        }
                    }
                }
            }
        }
    }

    // Displays a row for each existing state-dependent or condition-dependent object
    private void DisplayObjectRow(GameObject obj, string stateName)
    {
        EditorGUILayout.BeginHorizontal();

        // Reference to the GameObject
        EditorGUILayout.LabelField("GameObject", GUILayout.Width(75));
        EditorGUILayout.ObjectField(obj, typeof(GameObject), true, GUILayout.Width(200));

        GUILayout.Space(20); // Add space between columns

        // Retrieve the ScriptableClusterScriptCombiner component
        ScriptableClusterScriptCombiner combiner = obj.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner != null)
        {
            // Reference to the JS script asset
            EditorGUILayout.LabelField("JS Script", GUILayout.Width(60));
            EditorGUILayout.ObjectField(combiner.ClusterScripts[1], typeof(ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset), true, GUILayout.Width(200));
        }

        GUILayout.Space(20); // Add space between columns

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
    }

    // Removes the GameObject and the corresponding row from the editor
    private void RemoveObject(GameObject obj)
    {
        createdObjects.Remove(obj);
        DestroyImmediate(obj);
        Debug.Log("GameObject removed.");
        Repaint();
    }
    
    private void CopyWorldItemReferenceListToNewObject(GameObject newObject)
    {
        // Find the WorldItemRefList in the scene
        GameObject worldItemRefList = GameObject.Find("WorldItemRefList");

        if (worldItemRefList != null)
        {
            // Get the WorldItemReferenceList component
            var worldItemRefComponent = worldItemRefList.GetComponent<ClusterVR.CreatorKit.Item.Implements.WorldItemReferenceList>();

            if (worldItemRefComponent != null)
            {
                // Copy the WorldItemReferenceList component to the new object
                UnityEditorInternal.ComponentUtility.CopyComponent(worldItemRefComponent);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(newObject);
            }
            else
            {
                Debug.LogError("WorldItemReferenceList component not found on WorldItemRefList.");
            }
        }
        else
        {
            Debug.LogError("WorldItemRefList object not found in the scene.");
        }
    }
    
    private void CopyWorldItemReferenceListToFormController(GameObject formController)
    {
        if (formController == null)
        {
            Debug.LogError("FormController not found in the new questionnaire object.");
            return;
        }

        // Find the WorldItemRefList in the scene
        GameObject worldItemRefList = GameObject.Find("WorldItemRefList");

        if (worldItemRefList != null)
        {
            // Get the WorldItemReferenceList component
            var worldItemRefComponent = worldItemRefList.GetComponent<ClusterVR.CreatorKit.Item.Implements.WorldItemReferenceList>();

            if (worldItemRefComponent != null)
            {
                // Copy the WorldItemReferenceList component and paste it into the FormController
                UnityEditorInternal.ComponentUtility.CopyComponent(worldItemRefComponent);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(formController);
            }
            else
            {
                Debug.LogError("WorldItemReferenceList component not found on WorldItemRefList.");
            }
        }
        else
        {
            Debug.LogError("WorldItemRefList object not found in the scene.");
        }
    }
}
