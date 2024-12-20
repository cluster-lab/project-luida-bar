using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class ObjectsManagerEditor : EditorWindow
{
    private StateList stateList;
    private SerializedObject serializedStateList;
    private SerializedProperty statesProperty;
    private string stateListeningObjectPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/StateListeningObject.prefab";
    private string formPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/Questionnaire/Questionnaire.prefab";
    private string jsStateTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/StateManagement/StateListeningObjectTemplate.js";
    private string identifiersAssetPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string RequiredObjectsWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";
    private const string ConditionManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionManager.prefab";
    
    // Dictionaries to store text field input for each state
    private Dictionary<string, string> objectNames = new Dictionary<string, string>();
    private Dictionary<string, string> conditionObjectNames = new Dictionary<string, string>();
    private Dictionary<string, int> questionnaireQIDs = new Dictionary<string, int>();
    private Dictionary<string, bool> showQuestionnaireForm = new Dictionary<string, bool>();
    private Dictionary<string, bool> isScriptableState = new Dictionary<string, bool>(); // Track if the object is scriptable per state
    private Dictionary<string, bool> isAccessibleToConditionsState = new Dictionary<string, bool>(); // Track if the object is accessible to experimental conditions

    private List<GameObject> createdObjects = new List<GameObject>(); // Store created objects for display

    // Dictionaries to track foldout states for collapsible sections
    private Dictionary<string, bool> stateObjectsFoldout = new Dictionary<string, bool>();
    private Dictionary<string, bool> stateQuestionnairesFoldout = new Dictionary<string, bool>();

    private Dictionary<string, bool> showNewObjectRow = new Dictionary<string, bool>(); // Track visibility of the new object row for each state
    private Dictionary<string, int> selectedRoleIndices = new Dictionary<string, int>();
    private List<string> roleNames = new List<string>();
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
        
        LoadRolesFromParticipantRoles();
        RetrieveCreatedObjects();
    }

    public void OnGUI()
    {
        LoadRolesFromParticipantRoles();
        
        if (stateList == null)
        {
            string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            string stateListPath = scenePath.Replace("Scenes", "Settings/StateList").Replace(".unity", ".asset");
            stateList = AssetDatabase.LoadAssetAtPath<StateList>(stateListPath);
            if (stateList != null)
            {
                serializedStateList = new SerializedObject(stateList);
                statesProperty = serializedStateList.FindProperty("States");
            }
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition); // Start scrollable area

        // Instruction text
        EditorGUILayout.HelpBox("Manage state-listening objects and questionnaires for each state.", MessageType.Info);
        EditorGUILayout.HelpBox("Click 'Add object dependent to this state' to add a new object dependent to this state.", MessageType.Info);
        EditorGUILayout.HelpBox("Click 'Add condition-dependent object in this state' to add a condition-dependent object inside this state.", MessageType.Info);
        EditorGUILayout.HelpBox("Remember to click 'Update script' button after editing the script for an object.", MessageType.Info);
        EditorGUILayout.HelpBox("Click 'Add questionnaire' to add a questionnaire inside this state.", MessageType.Info);

        if (stateList == null)
        {
            EditorGUILayout.HelpBox("StateList not found. Please ensure you're in the correct scene with the StateList asset.", MessageType.Error);
            EditorGUILayout.EndScrollView();
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
            if (!showQuestionnaireForm.ContainsKey(stateName)) showQuestionnaireForm[stateName] = false;

            if (!stateObjectsFoldout.ContainsKey(stateName)) stateObjectsFoldout[stateName] = true;
            if (!stateQuestionnairesFoldout.ContainsKey(stateName)) stateQuestionnairesFoldout[stateName] = true;

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Bold state name with buttons to show 'Add Object' and 'Add Condition-dependent Object' rows
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(stateName, EditorStyles.boldLabel);
            if (GUILayout.Button("Add object listening to this state", GUILayout.Width(250)))
            {
                showNewObjectRow[stateName] = !showNewObjectRow[stateName]; // Toggle the new object row visibility
            }

            if (GUILayout.Button("Add questionnaire", GUILayout.Width(150)))
            {
                showQuestionnaireForm[stateName] = !showQuestionnaireForm[stateName]; // Toggle the add questionnaire form visibility
            }

            EditorGUILayout.EndHorizontal();

            // Collapsible list for state-listening objects
            stateObjectsFoldout[stateName] = EditorGUILayout.Foldout(stateObjectsFoldout[stateName], "State-Listening Objects", true, EditorStyles.foldout);
            if (stateObjectsFoldout[stateName])
            {
                List<GameObject> stateListeningObjects = createdObjects.Where(o => o.transform.parent != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o) == stateListeningObjectPrefabPath && (o.transform.parent.parent.name == stateName || o.transform.parent.name == stateName)).ToList();
                foreach (var obj in stateListeningObjects)
                {
                    DisplayObjectRow(obj, stateName, i);
                }
            }

            // Show the new object row only if the corresponding button has been clicked
            if (showNewObjectRow[stateName])
            {
                DisplayNewObjectRow(stateName, i, stateListeningObjectPrefabPath, jsStateTemplatePath, "New State Listening Object");
            }

            // Display existing questionnaires
            stateQuestionnairesFoldout[stateName] = EditorGUILayout.Foldout(stateQuestionnairesFoldout[stateName], "Questionnaires in this State", true, EditorStyles.foldout);
            if (stateQuestionnairesFoldout[stateName])
            {
                DisplayExistingQuestionnaires(stateName);
            }

            // Show the questionnaire form if the "Add questionnaire" button was clicked
            if (showQuestionnaireForm[stateName])
            {
                DisplayQuestionnaireForm(stateName, i);
            }

            // Add space after each state section
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
        serializedStateList.ApplyModifiedProperties();
    }

    private Dictionary<string, GameObject> objectReferences = new Dictionary<string, GameObject>(); // Store GameObject references

    private void DisplayNewObjectRow(string stateName, int stateId, string prefabPath, string jsTemplatePath, string label)
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Name", GUILayout.Width(35));
        if (!objectNames.ContainsKey(stateName)) objectNames[stateName] = string.Empty;
        objectNames[stateName] = EditorGUILayout.TextField(objectNames[stateName], GUILayout.Width(150));

        GUILayout.Space(20);

        if (!isScriptableState.ContainsKey(stateName)) isScriptableState[stateName] = false;
        EditorGUILayout.LabelField("Is Scriptable", GUILayout.Width(70));
        isScriptableState[stateName] = EditorGUILayout.Toggle("", isScriptableState[stateName], GUILayout.Width(20));
        
        GUILayout.Space(20);

        if (!isAccessibleToConditionsState.ContainsKey(stateName)) isAccessibleToConditionsState[stateName] = false;
        EditorGUILayout.LabelField("Is Accessible to Conditions", GUILayout.Width(155));
        isAccessibleToConditionsState[stateName] = EditorGUILayout.Toggle("", isAccessibleToConditionsState[stateName], GUILayout.Width(20));
        
        GUILayout.Space(20);

        // Add the GameObject reference field
        EditorGUILayout.LabelField("Create with existing GameObject", GUILayout.Width(190));
        if (!objectReferences.ContainsKey(stateName)) objectReferences[stateName] = null;
        objectReferences[stateName] = (GameObject)EditorGUILayout.ObjectField(objectReferences[stateName], typeof(GameObject), true, GUILayout.Width(150));

        GUILayout.Space(20);

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(objectNames[stateName]));
        if (GUILayout.Button("Add Object", GUILayout.Width(100)))
        {
            AddObject(stateName, stateId, objectNames[stateName], prefabPath, jsTemplatePath, isScriptableState[stateName], isAccessibleToConditionsState[stateName], objectReferences[stateName]);
            objectNames[stateName] = string.Empty;
            objectReferences[stateName] = null;  // Clear after adding
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
    
    private void DisplayExistingQuestionnaires(string stateName)
    {
        Transform stateObjectsParent = GameObject.Find(stateName)?.transform.Find("Objects");
        if (stateObjectsParent == null) return;

        foreach (Transform roleGroup in stateObjectsParent)
        {
            if (!roleGroup.name.StartsWith("Questionnaires_")) continue;

            string role = roleGroup.name.Replace("Questionnaires_", "");
            int qID = GetQIDFromRoleGroup(roleGroup);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"Role: {role}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"qID: {qID}", GUILayout.Width(100));
            EditorGUILayout.LabelField("Wrapper Object:", GUILayout.Width(90));
            EditorGUILayout.ObjectField(roleGroup.gameObject, typeof(GameObject), true, GUILayout.Width(200));


            if (GUILayout.Button("Remove All", GUILayout.Width(120)))
            {
                RemoveRoleQuestionnaires(roleGroup);
                Debug.Log($"Removed all questionnaires for role: {role}");
            }

            EditorGUILayout.EndHorizontal();
        }
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
        EditorGUILayout.LabelField("Select Role", GUILayout.Width(80));
        if (!selectedRoleIndices.ContainsKey(stateName))
            selectedRoleIndices[stateName] = 0;

        selectedRoleIndices[stateName] = EditorGUILayout.Popup(selectedRoleIndices[stateName], roleNames.ToArray(), GUILayout.Width(150));

        GUILayout.Space(30);
        if (GUILayout.Button("Add Questionnaire", GUILayout.Width(150)))
        {
            string selectedRole = roleNames[selectedRoleIndices[stateName]];
            int roleCount = GetRoleCount(selectedRole);

            CreateRoleQuestionnaires(stateName, selectedRole, roleCount, questionnaireQIDs[stateName]);
            Debug.Log($"Created {roleCount} questionnaires for role '{selectedRole}' with qID {questionnaireQIDs[stateName]}.");
            showQuestionnaireForm[stateName] = false; // Hide the form after adding
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void CreateRoleQuestionnaires(string stateName, string role, int count, int qID)
    {
        Transform stateObjectsParent = GameObject.Find(stateName)?.transform.Find("Objects");
        if (stateObjectsParent == null) return;

        // Create a parent object for this role
        GameObject roleGroup = new GameObject($"Questionnaires_{role}");
        roleGroup.transform.SetParent(stateObjectsParent);

        for (int i = 0; i < count; i++)
        {
            GameObject formPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(formPrefabPath);
            GameObject newQuestionnaire = (GameObject)PrefabUtility.InstantiatePrefab(formPrefab);
            newQuestionnaire.transform.SetParent(roleGroup.transform);
            newQuestionnaire.name = $"Questionnaire_{role}_{i + 1}";

            GameObject formController = newQuestionnaire.transform.Find("FormController")?.gameObject;
            UpdateFormIdSettings(formController, qID, role, i);
            AttachItemGroupMemberToFormController(formController);

            Debug.Log($"Created questionnaire {i + 1} for role '{role}' under state '{stateName}'.");
        }
    }
    
    private void RemoveRoleQuestionnaires(Transform roleGroup)
    {
        if (roleGroup != null)
            DestroyImmediate(roleGroup.gameObject);
    }
    
    private int GetQIDFromRoleGroup(Transform roleGroup)
    {
        Transform firstChild = roleGroup.childCount > 0 ? roleGroup.GetChild(0) : null;
        GameObject formController = firstChild?.Find("FormController")?.gameObject;

        return formController != null ? GetCurrentQID(formController) : -1;
    }

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

    private void UpdateFormIdSettings(GameObject formController, int qID, string roleName, int orderInRole)
    {
        var itemLogic = formController.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
        if (itemLogic == null) return;

        SerializedObject serializedComp = new SerializedObject(itemLogic);
        SerializedProperty statements = serializedComp.FindProperty("logic.statements");

        if (statements == null || !statements.isArray) return;

        int roleIndex = roleNames.IndexOf(roleName);

        for (int i = 0; i < statements.arraySize; i++)
        {
            SerializedProperty statement = statements.GetArrayElementAtIndex(i);
            SerializedProperty targetKey = statement.FindPropertyRelative("singleStatement.targetState.key");

            if (targetKey == null) continue;

            SerializedProperty valueProp = statement.FindPropertyRelative("singleStatement.expression.value.constant.integerValue");

            switch (targetKey.stringValue)
            {
                case "qID":
                    if (valueProp != null) valueProp.intValue = qID;
                    break;
                case "pRoleID":
                    if (valueProp != null) valueProp.intValue = roleIndex;
                    break;
                case "pIdInRole":
                    if (valueProp != null) valueProp.intValue = orderInRole;
                    break;
            }
        }

        serializedComp.ApplyModifiedProperties();
    }

    // Adds the new state-listening object and its corresponding JS script
    private void AddObject(string stateName, int stateId, string objectName, string prefabPath, string jsTemplatePath, bool isScriptable, bool isAccessibleToConditionsState, GameObject referenceObject)
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
        newObject.transform.SetParent(stateObject.transform);

        // Handle the case where the referenceObject is provided
        if (referenceObject != null)
        {
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

        // Set the state_id property
        SetStateIdForObject(newObject, stateId);

        // Rest of the process (replacing script, etc.)
        if (isScriptable)
        {
            // Ensure the folder for the current scene exists
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string scriptFolderPath = $"Assets/_Experiment_/Scripts/StateManagement/{sceneName}" ;

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
            EditorUtility.SetDirty(combiner);
            EditorUtility.SetDirty(newScriptAsset);
            AssetDatabase.SaveAssets();
        } else {
            // Remove ScriptableClusterScriptCombiner and ScriptableItem if they exist on the instance
            var scriptableClusterScriptCombiner = newObject.GetComponent<ScriptableClusterScriptCombiner>();
            if (scriptableClusterScriptCombiner != null)
            {
                DestroyImmediate(scriptableClusterScriptCombiner); // Remove the ScriptableClusterScriptCombiner component
            }

            var scriptableItem = newObject.GetComponent<ClusterVR.CreatorKit.Item.Implements.ScriptableItem>();
            if (scriptableItem != null)
            {
                DestroyImmediate(scriptableItem); // Remove the ScriptableItem component
            }
        }

        if (isAccessibleToConditionsState)
        {
            // Attach ItemGroupMember component to this object
            var itemGroupMember = newObject.AddComponent<ClusterVR.CreatorKit.Item.Implements.ItemGroupMember>();

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

        // Add the created object to the list for display
        createdObjects.Add(newObject);

        // Refresh the UI
        Repaint();
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
                        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == stateListeningObjectPrefabPath)
                        {
                            createdObjects.Add(obj);
                        }
                    }
                }
            }
        }
    }

    // Displays a row for each existing state-listening object
    private void DisplayObjectRow(GameObject obj, string stateName, int stateId)
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
            // Reference to the JS script asset (Only show if scriptable)
            EditorGUILayout.LabelField("JS Script", GUILayout.Width(60));
            var clusterScripts = combiner.GetClusterScripts();
            EditorGUILayout.ObjectField(clusterScripts[1], typeof(ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset), true, GUILayout.Width(200));

            GUILayout.Space(20); // Add space between columns

            // Update Script button
            if (GUILayout.Button("Update Script", GUILayout.Width(150)))
            {
                combiner?.CombineScripts();
            }
        }

        // Remove button
        if (GUILayout.Button("Remove", GUILayout.Width(100)))
        {
            RemoveObject(obj);
        }

        EditorGUILayout.EndHorizontal();

        // Now, check for state_id mismatch
        Component itemLogic = obj.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
        if (itemLogic != null)
        {
            SerializedObject serializedComp = new SerializedObject(itemLogic);
            SerializedProperty statementsProperty = serializedComp.FindProperty("logic.statements");

            if (statementsProperty != null && statementsProperty.isArray)
            {
                for (int i = 0; i < statementsProperty.arraySize; i++)
                {
                    SerializedProperty statement = statementsProperty.GetArrayElementAtIndex(i);
                    SerializedProperty targetKeyProp = statement.FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKeyProp != null && targetKeyProp.stringValue == "state_id")
                    {
                        SerializedProperty stateIdProp = statement.FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (stateIdProp != null)
                        {
                            int objStateId = stateIdProp.intValue;
                            if (objStateId != stateId)
                            {
                                // Display hint and fix button
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(20); // Indent
                                EditorGUILayout.HelpBox("State ID mismatch: object's state_id does not match this state's id.", MessageType.Warning);
                                if (GUILayout.Button("Fix state_id", GUILayout.Width(100)))
                                {
                                    // Fix the state_id
                                    stateIdProp.intValue = stateId;
                                    serializedComp.ApplyModifiedProperties();
                                    Debug.Log($"state_id of object {obj.name} updated to {stateId}");
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            break; // We found state_id, no need to check further
                        }
                    }
                }
            }
        }
    }

    // Removes the GameObject and the corresponding row from the editor
    private void RemoveObject(GameObject obj)
    {
        createdObjects.Remove(obj);
        DestroyImmediate(obj);
        Debug.Log("GameObject removed.");
        Repaint();
    }

    private void AttachItemGroupMemberToFormController(GameObject formController)
    {
        // Attach ItemGroupMember component to this object
        var itemGroupMember = formController.AddComponent<ClusterVR.CreatorKit.Item.Implements.ItemGroupMember>();

        // Find the ParticipantRoles GameObject in the scene
        GameObject participantRolesObject = FindParticipantRolesGameObject();
        if (participantRolesObject != null)
        {
            // Get the ItemGroupHost component from ParticipantRoles
            var participantRolesHost = participantRolesObject.GetComponent<ClusterVR.CreatorKit.Item.Implements.ItemGroupHost>();
            if (participantRolesHost != null)
            {
                // Use reflection or internal accessors to assign the host
                var serializedItemGroupMember = new UnityEditor.SerializedObject(itemGroupMember);
                var hostProperty = serializedItemGroupMember.FindProperty("host");

                if (hostProperty != null)
                {
                    hostProperty.objectReferenceValue = participantRolesHost;
                    serializedItemGroupMember.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogError("Unable to find 'host' property in ItemGroupMember.");
                }
            }
            else
            {
                Debug.LogError("ParticipantRoles does not have an ItemGroupHost component.");
            }
        }
        else
        {
            Debug.LogError("ParticipantRoles GameObject not found in the scene.");
        }
    }

    private GameObject FindRequiredObjectsWrapperInstance()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == RequiredObjectsWrapperPrefabPath)
            {
                Debug.Log(obj.name);
                return obj;
            }
        }
        return null;
    }

    private GameObject FindConditionManagerPrefabInstance()
    {
        GameObject requiredObjectsWrapper = FindRequiredObjectsWrapperInstance();
        if (!requiredObjectsWrapper) return null;

        for (int i = 0; i < requiredObjectsWrapper.transform.childCount; i++)
        {
            Transform child = requiredObjectsWrapper.transform.GetChild(i);
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == ConditionManagerPrefabPath)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private GameObject FindParticipantRolesGameObject()
    {
        GameObject requiredObjectsWrapper = FindRequiredObjectsWrapperInstance();
        if (!requiredObjectsWrapper) return null;

        for (int i = 0; i < requiredObjectsWrapper.transform.childCount; i++)
        {
            Transform child = requiredObjectsWrapper.transform.GetChild(i);
            if (child.gameObject.name == "ParticipantRoles")
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private void LoadRolesFromParticipantRoles()
    {
        roleNames.Clear();
        string sceneName = SceneManager.GetActiveScene().name;
        string rolesScriptPath = $"Assets/_Experiment_/Settings/ParticipantRoles/{sceneName}.js";

        if (File.Exists(rolesScriptPath))
        {
            string scriptContent = File.ReadAllText(rolesScriptPath);

            var matches = System.Text.RegularExpressions.Regex.Matches(scriptContent, @"role:\s*""(.*?)""");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    roleNames.Add(match.Groups[1].Value);
                }
            }
        }
        else
        {
            Debug.LogError($"ParticipantRoles script not found at {rolesScriptPath}");
        }
    }
    
    private int GetRoleCount(string roleName)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string rolesScriptPath = $"Assets/_Experiment_/Settings/ParticipantRoles/{sceneName}.js";

        if (File.Exists(rolesScriptPath))
        {
            string scriptContent = File.ReadAllText(rolesScriptPath);

            // Regex to extract role count for the specified role
            var match = System.Text.RegularExpressions.Regex.Match(scriptContent, $@"role:\s*""{roleName}"",\s*number:\s*(\d+)");
            if (match.Success && match.Groups.Count > 1)
            {
                return int.Parse(match.Groups[1].Value);
            }
        }

        Debug.LogWarning($"Role {roleName} not found or invalid count. Defaulting to 1.");
        return 1; // Default if role not found
    }
}
