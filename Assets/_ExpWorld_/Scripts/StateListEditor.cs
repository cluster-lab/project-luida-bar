#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

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
            EditorGUILayout.BeginVertical();

            SerializedProperty state = statesProperty.GetArrayElementAtIndex(i);
            SerializedProperty stateName = state.FindPropertyRelative("StateName");
            SerializedProperty destStateName = state.FindPropertyRelative("DestStateName");

            EditorGUILayout.LabelField("State name:");
            EditorGUILayout.PropertyField(stateName, GUIContent.none);

            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();

            // Label and dropdown for Transition Destination State ID
            EditorGUILayout.LabelField("Transit destination state ID");
            int destStateIndex = Array.FindIndex(stateList.States, s => s.StateName == destStateName.stringValue);
            string[] stateNames = Array.ConvertAll(stateList.States, s => s.StateName);
            destStateIndex = EditorGUILayout.Popup(destStateIndex, stateNames);

            if (destStateIndex >= 0)
            {
                destStateName.stringValue = stateList.States[destStateIndex].StateName;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();

            // Label and buttons for moving state
            EditorGUILayout.LabelField("Move state to:");

            EditorGUILayout.BeginHorizontal();

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

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (GUI.changed && !EditorGUIUtility.editingTextField)
            {
                GUI.FocusControl(null); // Unfocus any field
                GameObject transition = GameObject.Find(stateName.stringValue)?.transform.Find("Transition")?.gameObject;
                if (transition != null)
                {
                    int destStateId = Array.FindIndex(stateList.States, s => s.StateName == destStateName.stringValue);
                    UpdateTransitionDestStateId(transition, destStateId);
                }
            }
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
            string stateName = stateList.States[i].StateName;
            Transform child = statesObject.transform.Find(stateName);

            if (child == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                // Debug.Log("?????");
                if (prefab != null)
                {
                    // Debug.Log("!!!!!!!!!");
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
                UpdateTransitionCurrentStateId(transition, i);
                int destStateId = Array.FindIndex(stateList.States, s => s.StateName == stateList.States[i].DestStateName);
                UpdateTransitionDestStateId(transition, destStateId);
            }
        }

        for (int i = statesObject.transform.childCount - 1; i >= stateList.States.Length; i--)
        {
            DestroyImmediate(statesObject.transform.GetChild(i).gameObject);
        }
    }

    private void UpdateTransitionCurrentStateId(GameObject transition, int stateId)
    {
        Component stateIdSettingComp = transition.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>(); // Replace with the actual component type
        if (stateIdSettingComp != null)
        {
            SerializedObject serializedComp = new SerializedObject(stateIdSettingComp);

            SerializedProperty specificProperty = serializedComp.FindProperty("logic.statements");

            if (specificProperty != null && specificProperty.isArray && specificProperty.arraySize > 0)
            {
                SerializedProperty firstElement = specificProperty.GetArrayElementAtIndex(0).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                if (firstElement != null)
                {
                    firstElement.intValue = stateId;
                    serializedComp.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogWarning("Property not found: logic.statements.singleStatement.expression.value.constant.integerValue");
                }
            }
            else
            {
                Debug.LogWarning("Property not found: logic.statements");
            }
        }
    }

    private void UpdateTransitionDestStateId(GameObject transition, int destStateId)
    {
        var globalLogics = transition.GetComponents<ClusterVR.CreatorKit.Operation.Implements.GlobalLogic>();
        Component transitionSettingLogic = null;
        foreach (var globalLogic in globalLogics)
        {
            SerializedObject serializedComp = new SerializedObject(globalLogic);
            var keyProp = serializedComp.FindProperty("globalGimmickKey.key.key");
            if (keyProp != null && keyProp.stringValue == "state_triggerTransition")
            {
                transitionSettingLogic = globalLogic;
                break;
            }
        }
        if (transitionSettingLogic != null)
        {
            SerializedObject serializedTransitionSettingLogic = new SerializedObject(transitionSettingLogic);

            SerializedProperty specificProperty = serializedTransitionSettingLogic.FindProperty("logic.statements");

            if (specificProperty != null && specificProperty.isArray && specificProperty.arraySize > 0)
            {
                var targetStateKey = specificProperty.GetArrayElementAtIndex(1).FindPropertyRelative("singleStatement.targetState.key");
                if (targetStateKey.stringValue == "state_currentID")
                {
                    var transitDestStateIdProp = specificProperty.GetArrayElementAtIndex(1).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                    if (transitDestStateIdProp != null)
                    {
                        transitDestStateIdProp.intValue = destStateId;
                        serializedTransitionSettingLogic.ApplyModifiedProperties();
                    }
                    else
                    {
                        Debug.LogWarning("Property not found: logic.statements.singleStatement.expression.value.constant.integerValue");
                    }
                }
                else
                {
                    Debug.LogWarning("Property not found: logic.statements.singleStatement.targetState.key");
                }
            }
            else
            {
                Debug.LogWarning("Property not found: logic.statements");
            }
        }
    }
}
#endif
