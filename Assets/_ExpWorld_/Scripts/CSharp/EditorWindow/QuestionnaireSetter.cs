#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

public class QuestionnaireSetter : EditorWindow
{
    private StateList stateList;
    private SerializedObject serializedStateList;
    private SerializedProperty statesProperty;
    private string formPrefabPath = "Assets/_ExpWorld_/Prefabs/Questionnaire/Questionnaire.prefab";
    private string newStateToAdd;
    private int newQIDToAdd;

    public static void ShowWindow()
    {
        GetWindow<QuestionnaireSetter>("Questionnaire Setter");
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
    }

    public void OnGUI()
    {
        EditorGUILayout.LabelField("Questionnaire Setter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assign questionnaires you registered on the web console to any states by setting their qID, or remove them.", MessageType.Info);

        if (stateList == null)
        {
            EditorGUILayout.HelpBox("StateList not found. Please ensure you're in the correct scene with the StateList asset.", MessageType.Error);
            return;
        }

        serializedStateList.Update();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("State Name", EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField("qID", EditorStyles.boldLabel, GUILayout.Width(50));
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        // Display existing states with a valid and enabled Questionnaire instance
        for (int i = 0; i < statesProperty.arraySize; i++)
        {
            SerializedProperty state = statesProperty.GetArrayElementAtIndex(i);
            string stateName = state.FindPropertyRelative("StateName").stringValue;
            GameObject stateObject = GameObject.Find(stateName)?.transform.Find("EnabledObjects")?.gameObject;

            if (stateObject != null && HasEnabledFormInstance(stateObject))
            {
                GameObject formController = GetFormController(stateObject);
                int currentQID = GetCurrentQID(formController);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(stateName, GUILayout.Width(150));

                int newQID = EditorGUILayout.IntField(currentQID, GUILayout.Width(50));

                if (newQID != currentQID)
                {
                    UpdateQID(formController, newQID);
                }

                // Remove button
                if (GUILayout.Button("Remove", GUILayout.Width(100)))
                {
                    RemoveFormInstance(stateObject, formController);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        serializedStateList.ApplyModifiedProperties();
        EditorGUILayout.Space();

        // Add new state with Questionnaire prefab and qID
        EditorGUILayout.LabelField("Add New Questionnaire", EditorStyles.boldLabel);
        newStateToAdd = StateDropdown();
        newQIDToAdd = EditorGUILayout.IntField("qID", newQIDToAdd, GUILayout.Width(200));

        if (GUILayout.Button("Add", GUILayout.Width(200)))
        {
            if (!string.IsNullOrEmpty(newStateToAdd))
            {
                AddOrEnableFormInstance(newStateToAdd, newQIDToAdd);
            }
        }
    }

    // Dropdown list for states that don't have enabled Questionnaire prefab instances
    private string StateDropdown()
    {
        string[] stateNames = stateList.States.Select(s => s.StateName).ToArray();
        string[] statesWithoutForm = stateNames.Where(stateName =>
        {
            GameObject stateObject = GameObject.Find(stateName)?.transform.Find("EnabledObjects")?.gameObject;
            return stateObject == null || !HasEnabledFormInstance(stateObject);
        }).ToArray();

        int selectedIndex = Array.IndexOf(statesWithoutForm, newStateToAdd);
        selectedIndex = EditorGUILayout.Popup("Select State", selectedIndex, statesWithoutForm, GUILayout.Width(200));

        if (selectedIndex >= 0 && selectedIndex < statesWithoutForm.Length)
        {
            return statesWithoutForm[selectedIndex];
        }

        return null;
    }

    // Check if a valid and enabled Questionnaire prefab instance exists in the state's EnabledObjects
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
        GameObject stateObject = GameObject.Find(stateName)?.transform.Find("EnabledObjects")?.gameObject;

        if (stateObject != null)
        {
            GameObject existingInstance = stateObject.transform.Cast<Transform>()
                .FirstOrDefault(child => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath)?.gameObject;

            if (existingInstance != null && !existingInstance.activeSelf)
            {
                existingInstance.SetActive(true); // Re-enable the existing instance
                GameObject formController = existingInstance.transform.Find("FormController")?.gameObject;
                UpdateQID(formController, qID);
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
                        UpdateQID(formController, qID);
                        Debug.Log($"Questionnaire instance added to {stateName} with qID {qID}");
                    }
                }
            }
        }
    }
}
#endif
