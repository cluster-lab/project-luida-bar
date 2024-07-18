using UnityEngine;
using UnityEditor;

public class StateListEditor : EditorWindow
{
    private StateList stateList;
    private SerializedObject serializedStateList;
    private SerializedProperty statesProperty;
    private string prefabPath = "Assets/_ExpWorld_/Prefabs/State/State/State.prefab";

    [MenuItem("Window/State List Editor")]
    public static void ShowWindow()
    {
        GetWindow<StateListEditor>("State List Editor");
    }

    private void OnGUI()
    {
        string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        string stateListPath = scenePath.Replace("Scenes", "ExpSettings/StateList").Replace(".unity", ".asset");

        stateList = AssetDatabase.LoadAssetAtPath<StateList>(stateListPath);

        if (stateList == null)
        {
            EditorGUILayout.HelpBox($"StateList not found at {stateListPath}. Please ensure it exists.", MessageType.Warning);
            return;
        }

        if (serializedStateList == null || serializedStateList.targetObject != stateList)
        {
            serializedStateList = new SerializedObject(stateList);
            statesProperty = serializedStateList.FindProperty("States");
        }

        EditorGUILayout.LabelField("Edit States", EditorStyles.boldLabel);

        serializedStateList.Update();

        for (int i = 0; i < statesProperty.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();

            SerializedProperty state = statesProperty.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(state, GUIContent.none);

            if (GUILayout.Button("Up", GUILayout.Width(50)))
            {
                GUI.FocusControl(null); // Unfocus any field
                if (i > 0)
                {
                    statesProperty.MoveArrayElement(i, i - 1);
                }
            }

            if (GUILayout.Button("Down", GUILayout.Width(50)))
            {
                GUI.FocusControl(null); // Unfocus any field
                if (i < statesProperty.arraySize - 1)
                {
                    statesProperty.MoveArrayElement(i, i + 1);
                }
            }

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                GUI.FocusControl(null); // Unfocus any field
                statesProperty.DeleteArrayElementAtIndex(i);
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add State"))
        {
            GUI.FocusControl(null); // Unfocus any field
            statesProperty.InsertArrayElementAtIndex(statesProperty.arraySize);
        }

        serializedStateList.ApplyModifiedProperties();

        UpdateSceneObjects();
    }

    private void UpdateSceneObjects()
    {
        GameObject statesObject = GameObject.Find("States");
        if (statesObject == null)
        {
            statesObject = new GameObject("States");
        }

        for (int i = 0; i < stateList.States.Length; i++)
        {
            string stateName = stateList.States[i];
            Transform child = statesObject.transform.Find(stateName);

            if (child == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    GameObject newChild = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    newChild.name = stateName;
                    newChild.transform.SetParent(statesObject.transform);
                    child = newChild.transform;
                }
            }

            if (child.GetSiblingIndex() != i)
            {
                child.SetSiblingIndex(i);
            }

            GameObject transition = child.Find("Transition")?.gameObject;
            if (transition != null)
            {
                UpdateTransitionComponent(transition, i);
            }
        }

        for (int i = statesObject.transform.childCount - 1; i >= stateList.States.Length; i--)
        {
            DestroyImmediate(statesObject.transform.GetChild(i).gameObject);
        }
    }

    private void UpdateTransitionComponent(GameObject transition, int stateId)
    {
        Component logicComponent = GetComponentByIndex(transition, 2); // transition.GetComponent<ItemLogic>(); // Replace with the actual component type
        if (logicComponent != null)
        {
            SerializedObject serializedLogicComponent = new SerializedObject(logicComponent);

            SerializedProperty specificProperty = serializedLogicComponent.FindProperty("logic.statements");

            if (specificProperty != null && specificProperty.isArray && specificProperty.arraySize > 0)
            {
                SerializedProperty firstElement = specificProperty.GetArrayElementAtIndex(0).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                if (firstElement != null)
                {
                    firstElement.intValue = stateId;
                    serializedLogicComponent.ApplyModifiedProperties();
                }
            }
            else
            {
                Debug.LogWarning("Property not found: logic.statements");
            }
        }
    }

    private Component GetComponentByIndex(GameObject gameObject, int index)
    {
        Component[] components = gameObject.GetComponents<Component>();
        if (index >= 0 && index < components.Length)
        {
            return components[index];
        }
        return null;
    }
}
