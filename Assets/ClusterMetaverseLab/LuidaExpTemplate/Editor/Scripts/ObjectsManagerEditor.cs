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
    private string conditionDependentObjectPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionDependentObject.prefab";
    private string formPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/Questionnaire/Questionnaire.prefab";
    private string jsStateTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/StateManagement/StateListeningObjectTemplate.js";
    private string jsConditionTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/ConditionManagement/ConditionDependentObjectTemplate.js";
    private string identifiersAssetPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string RequiredObjectsWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";
    private const string ConditionManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionManager.prefab";
    private const string WorldItemRefListObjectName = "WorldItemRefList";
    
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
    private Dictionary<string, bool> conditionObjectsFoldout = new Dictionary<string, bool>();

    private Dictionary<string, bool> showNewObjectRow = new Dictionary<string, bool>(); // Track visibility of the new object row for each state

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
        EditorGUILayout.HelpBox("Manage state-dependent, condition-dependent objects, and questionnaires for each state.", MessageType.Info);
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
            if (!conditionObjectsFoldout.ContainsKey(stateName)) conditionObjectsFoldout[stateName] = true;

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Bold state name with buttons to show 'Add Object' and 'Add Condition-dependent Object' rows
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(stateName, EditorStyles.boldLabel);
            if (GUILayout.Button("Add object listening to this state", GUILayout.Width(250)))
            {
                showNewObjectRow[stateName] = !showNewObjectRow[stateName]; // Toggle the new object row visibility
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
                List<GameObject> stateListeningObjects = createdObjects.Where(o => o.transform.parent != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o) == stateListeningObjectPrefabPath && (o.transform.parent.parent.name == stateName || o.transform.parent.name == stateName)).ToList();
                foreach (var obj in stateListeningObjects)
                {
                    DisplayObjectRow(obj, stateName, i);
                }
            }

            // Collapsible list for condition-dependent objects
            conditionObjectsFoldout[stateName] = EditorGUILayout.Foldout(conditionObjectsFoldout[stateName], "Condition-Dependent Objects", true, EditorStyles.foldout);
            if (conditionObjectsFoldout[stateName])
            {
                List<GameObject> conditionDependentObjects = createdObjects.Where(o => o.transform.parent != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o) == conditionDependentObjectPrefabPath && (o.transform.parent.parent.name == stateName || o.transform.parent.name == stateName)).ToList();
                foreach (var obj in conditionDependentObjects)
                {
                    DisplayObjectRow(obj, stateName, i);
                }
            }

            // Show the new object row only if the corresponding button has been clicked
            if (showNewObjectRow[stateName])
            {
                DisplayNewObjectRow(stateName, i, stateListeningObjectPrefabPath, jsStateTemplatePath, "New State Listening Object");
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
                        EditorUtility.SetDirty(combiner);
                        EditorUtility.SetDirty(identifiersAsset);
                        AssetDatabase.SaveAssets();

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
                        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == stateListeningObjectPrefabPath ||
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

    private void CopyWorldItemReferenceListToFormController(GameObject formController)
    {
        if (formController == null)
        {
            Debug.LogError("FormController not found in the new questionnaire object.");
            return;
        }

        GameObject expTemplateInstance = FindRequiredObjectsWrapperInstance();
        if (expTemplateInstance != null)
        {
            // Find the WorldItemRefList in the scene
            GameObject worldItemRefList = expTemplateInstance.transform.Find(WorldItemRefListObjectName).gameObject;

            if (worldItemRefList != null)
            {
                // Get the WorldItemReferenceList component
                var worldItemRefComponent = worldItemRefList.GetComponent<ClusterVR.CreatorKit.Item.Implements.WorldItemReferenceList>();

                if (worldItemRefComponent != null)
                {
                    // Copy the WorldItemReferenceList component to the new object
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
        else
        {
            Debug.LogError("ExpTemplateRequiredObjects prefab instance not found in the scene.");
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
}
