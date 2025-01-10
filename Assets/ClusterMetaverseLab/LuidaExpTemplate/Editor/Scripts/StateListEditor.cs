using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClusterVR.CreatorKit.Item;
using ClusterVR.CreatorKit.Item.Implements;

public class StateListEditor : EditorWindow
{
    private StateList stateList;
    private StateList.State[] previousStates;
    private SerializedObject serializedStateList;
    private SerializedProperty statesProperty;
    private string prefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/State.prefab";
    private const string RequiredObjectsWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";
    private const string stateManagementScriptFolderPathFormat = "Assets/_Experiment_/Scripts/StateManagement/{0}";
    private const string stateListeningItemPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/StateListeningItem.prefab";

    private readonly string[] FixedStateNames = new string[] {"Preparation", "Trial - Task", "Trial - Rest", "Trial - Questionnaire", "End"};
    private Vector2 scrollPos;

    public void OnEnable()
    {
        LoadStateList();
        previousStates = new StateList.State[stateList.States.Length];
        Array.Copy(stateList.States, previousStates, stateList.States.Length);
    }

    private void LoadStateList()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string stateListPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";

        stateList = AssetDatabase.LoadAssetAtPath<StateList>(stateListPath);
        if (stateList != null)
        {
            serializedStateList = new SerializedObject(stateList);
            statesProperty = serializedStateList.FindProperty("States");
        }
    }

    public void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (stateList == null)
        {
            LoadStateList();
        }

        if (stateList == null)
        {
            string templatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/StateList/Template.asset";
            StateList template = AssetDatabase.LoadAssetAtPath<StateList>(templatePath);

            if (template != null)
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                string newAssetPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
                AssetDatabase.CopyAsset(templatePath, newAssetPath);
                AssetDatabase.Refresh();
                stateList = AssetDatabase.LoadAssetAtPath<StateList>(newAssetPath);
                if (stateList != null)
                {
                    EditorGUILayout.HelpBox($"StateList created at {newAssetPath}.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Failed to create StateList at {newAssetPath}.", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"StateList template not found at {templatePath}. Please ensure it exists.", MessageType.Error);
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        if (serializedStateList == null || serializedStateList.targetObject != stateList)
        {
            serializedStateList = new SerializedObject(stateList);
            statesProperty = serializedStateList.FindProperty("States");
        }

        EditorGUILayout.LabelField("Edit States", EditorStyles.boldLabel);

        serializedStateList.Update();

        // Find special states indexes
        int preparationIndex = Array.FindIndex(stateList.States, s => s.StateName == "Preparation");
        int trialRestIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Rest");
        int endIndex = Array.FindIndex(stateList.States, s => s.StateName == "End");

        bool stateOrderChanged = false;

        // Check if any state (except 'End') transitions to 'End' before drawing GUI
        bool endTransitionFound = false;
        if (endIndex >= 0)
        {
            for (int idx = 0; idx < stateList.States.Length; idx++)
            {
                if (idx != endIndex && stateList.States[idx].DestStateName == "End")
                {
                    endTransitionFound = true;
                    break;
                }
            }
        }

        // Check if any state (except 'Preparation') transitions to 'Preparation'
        bool preparationTransitionFound = false;
        if (preparationIndex >= 0)
        {
            for (int idx = 0; idx < stateList.States.Length; idx++)
            {
                if (idx != preparationIndex && stateList.States[idx].DestStateName == "Preparation")
                {
                    preparationTransitionFound = true;
                    break;
                }
            }
        }

        for (int i = 0; i < statesProperty.arraySize; i++)
        {
            // Above 'Preparation' state:
            if (i == preparationIndex && preparationIndex >= 0)
            {
                GUILayout.Space(10);
                if (!preparationTransitionFound)
                {
                    EditorGUILayout.HelpBox("No state (except 'Preparation' itself) is transitioning to the 'Preparation' state!", MessageType.Warning);
                }

                if (GUILayout.Button("Add State"))
                {
                    GUI.FocusControl(null);
                    statesProperty.InsertArrayElementAtIndex(preparationIndex);

                    int sourceIndex = preparationIndex - 1;
                    if (sourceIndex >= 0)
                    {
                        CopyStateFields(sourceIndex, preparationIndex);
                    }
                    else
                    {
                        InitializeStateDefaults(preparationIndex);
                    }

                    stateOrderChanged = true;
                    serializedStateList.ApplyModifiedProperties();
                    break;
                }
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("These states are for trials in this experiment. Their order is fixed, and the repetition is controlled based on the variables you set.");
            }

            // Under 'Trial - Rest' state means line after 'Trial - Rest':
            if (trialRestIndex >= 0 && i == trialRestIndex + 1)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            }

            // Above 'End' state:
            if (i == endIndex && endIndex >= 0)
            {
                GUILayout.Space(10);
                if (GUILayout.Button("Add State"))
                {
                    GUI.FocusControl(null);
                    statesProperty.InsertArrayElementAtIndex(endIndex);

                    int sourceIndex = endIndex - 1;
                    if (sourceIndex >= 0)
                    {
                        CopyStateFields(sourceIndex, endIndex);
                    }
                    else
                    {
                        InitializeStateDefaults(endIndex);
                    }

                    stateOrderChanged = true;
                    serializedStateList.ApplyModifiedProperties();
                    break;
                }

                // Display the warning message here if needed
                if (!endTransitionFound)
                {
                    EditorGUILayout.HelpBox("No state (except 'End' itself) is transitioning to the 'End' state!", MessageType.Warning);
                }

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("The 'End' state should always be in the end of the state transitions.");
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("State ID", GUILayout.Width(60));
            EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(60));

            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();

            SerializedProperty state = statesProperty.GetArrayElementAtIndex(i);
            SerializedProperty stateName = state.FindPropertyRelative("StateName");
            SerializedProperty destStateName = state.FindPropertyRelative("DestStateName");
            SerializedProperty hasExitTime = state.FindPropertyRelative("HasExitTime");
            SerializedProperty exitTime = state.FindPropertyRelative("ExitTime");
            SerializedProperty isRepeated = state.FindPropertyRelative("IsRepeated");
            SerializedProperty repeatDestStateName = state.FindPropertyRelative("RepeatDestStateName");
            SerializedProperty repeatCount = state.FindPropertyRelative("RepeatCount");

            EditorGUILayout.LabelField("State name:");
            bool isFixedState = Array.IndexOf(FixedStateNames, stateName.stringValue) > -1;
            EditorGUI.BeginDisabledGroup(isFixedState);
            EditorGUILayout.PropertyField(stateName, GUIContent.none, GUILayout.Width(150));
            if (string.IsNullOrEmpty(stateName.stringValue))
            {
                stateName.stringValue = "State";
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();

            string[] allStateNames = Array.ConvertAll(stateList.States, s => s.StateName);

            bool isEndState = (endIndex >= 0 && i == endIndex);

            // Determine allowed moves:
            bool canMoveUp = true;
            bool canMoveDown = true;

            // Existing constraints
            if (preparationIndex >= 0)
            {
                if (i < preparationIndex)
                {
                    if (i + 1 >= preparationIndex) canMoveDown = false;
                }
            }

            if (trialRestIndex >= 0)
            {
                if (i > trialRestIndex)
                {
                    if (i - 1 <= trialRestIndex) canMoveUp = false;
                }
            }

            // New constraint: disable states before 'End' state to move down below 'End' state
            if (endIndex >= 0)
            {
                // Previously: if (i + 1 > endIndex) canMoveDown = false;
                // Now disallow even equal to endIndex, meaning they can't place themselves at or below End.
                if (i + 1 >= endIndex) canMoveDown = false;

                if (i == endIndex)
                {
                    canMoveUp = false;
                    canMoveDown = false;
                }
            }

            // Filter destinations:
            string[] allowedDestinations = allStateNames;
            if (preparationIndex >= 0 && i < preparationIndex)
            {
                int length = Math.Min(preparationIndex + 1, stateList.States.Length);
                allowedDestinations = new string[length];
                Array.Copy(allStateNames, allowedDestinations, length);
            }

            if (trialRestIndex >= 0 && i > trialRestIndex)
            {
                if (trialRestIndex + 1 < stateList.States.Length)
                {
                    int length = stateList.States.Length - (trialRestIndex + 1);
                    allowedDestinations = new string[length];
                    Array.Copy(allStateNames, trialRestIndex + 1, allowedDestinations, 0, length);
                }
                else
                {
                    allowedDestinations = new string[0];
                }
            }

            int destStateIndex = -1;
            if (allowedDestinations.Length > 0)
            {
                destStateIndex = Array.IndexOf(allowedDestinations, destStateName.stringValue);
            }

            EditorGUI.BeginDisabledGroup(isEndState);

            #region Transition Destination State
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Transit destination state");
            EditorGUI.BeginDisabledGroup(isFixedState);
            destStateIndex = EditorGUILayout.Popup(destStateIndex, allowedDestinations, GUILayout.Width(150));
            EditorGUI.EndDisabledGroup();

            if (destStateIndex >= 0 && allowedDestinations.Length > 0)
            {
                destStateName.stringValue = allowedDestinations[destStateIndex];
            }
            else if (allowedDestinations.Length == 0)
            {
                destStateName.stringValue = "";
            }

            EditorGUILayout.EndVertical();
            #endregion

            #region Move state
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Move state to:");

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(isFixedState || !canMoveUp);
            if (GUILayout.Button("Up", GUILayout.Width(50)))
            {
                GUI.FocusControl(null);
                if (i > 0)
                {
                    statesProperty.MoveArrayElement(i, i - 1);
                    stateOrderChanged = true;
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(isFixedState || !canMoveDown);
            if (GUILayout.Button("Down", GUILayout.Width(50)))
            {
                GUI.FocusControl(null);
                if (i < statesProperty.arraySize - 1)
                {
                    statesProperty.MoveArrayElement(i, i + 1);
                    stateOrderChanged = true;
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(isFixedState || isEndState);
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                GUI.FocusControl(null);
                statesProperty.DeleteArrayElementAtIndex(i);
                stateOrderChanged = true;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            #endregion

            #region Exit time
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Has Exit Time", GUILayout.Width(100));
            hasExitTime.boolValue = EditorGUILayout.Toggle(hasExitTime.boolValue, GUILayout.Width(100));

            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();

            if (hasExitTime.boolValue)
            {
                EditorGUILayout.LabelField("Exit Time", GUILayout.Width(100));
                exitTime.floatValue = EditorGUILayout.FloatField(exitTime.floatValue, GUILayout.Width(60));
            }

            EditorGUILayout.EndVertical();
            #endregion

            #region Repeating
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Is Repeated", GUILayout.Width(80));

            EditorGUI.BeginDisabledGroup(isFixedState || isEndState);
            isRepeated.boolValue = EditorGUILayout.Toggle(isRepeated.boolValue, GUILayout.Width(100));

            if (isRepeated.boolValue)
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();

                EditorGUILayout.LabelField("Repeat destination state");
                string[] repeatAllowed = allStateNames;
                int repeatStateIndex = Array.FindIndex(stateList.States, s => s.StateName == repeatDestStateName.stringValue);
                repeatStateIndex = EditorGUILayout.Popup(repeatStateIndex, repeatAllowed, GUILayout.Width(150));
                if (repeatStateIndex >= 0)
                {
                    repeatDestStateName.stringValue = repeatAllowed[repeatStateIndex];
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();

                EditorGUILayout.LabelField("Repeat Count", GUILayout.Width(80));
                repeatCount.intValue = EditorGUILayout.IntField(Math.Max(repeatCount.intValue, 1), GUILayout.Width(60));
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            #endregion

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup(); // End disabling if End state

            if (GUI.changed)
            {
                if (!EditorGUIUtility.editingTextField) GUI.FocusControl(null);
                GameObject transition = GameObject.Find(stateName.stringValue)?.transform.Find("Transition")?.gameObject;
                if (transition != null)
                {
                    int dIndex = Array.FindIndex(stateList.States, s => s.StateName == destStateName.stringValue);
                    UpdateTransitionDestStateId(transition, dIndex, stateName.stringValue == "Trial - Rest");

                    UpdateTransitionExitTime(transition, hasExitTime.boolValue, exitTime.floatValue);

                    int repeatDestId = Array.FindIndex(stateList.States, s => s.StateName == repeatDestStateName.stringValue);
                    UpdateRepeatedTransition(transition, Math.Max(0, repeatDestId), isRepeated.boolValue ? repeatCount.intValue : 1);

                    UpdateTransitionCurrentStateId(transition, i);
                }
            }
        }

        serializedStateList.ApplyModifiedProperties();

        UpdateSceneObjects();
        if (stateOrderChanged) {
            UpdateStateListeningItemsAfterReorder();
        }

        EditorGUILayout.EndScrollView();
    }

    private void CopyStateFields(int sourceIndex, int targetIndex)
    {
        SerializedProperty source = statesProperty.GetArrayElementAtIndex(sourceIndex);
        SerializedProperty target = statesProperty.GetArrayElementAtIndex(targetIndex);

        // Copy all relevant fields
        target.FindPropertyRelative("StateName").stringValue = source.FindPropertyRelative("StateName").stringValue;
        target.FindPropertyRelative("DestStateName").stringValue = source.FindPropertyRelative("DestStateName").stringValue;
        target.FindPropertyRelative("HasExitTime").boolValue = source.FindPropertyRelative("HasExitTime").boolValue;
        target.FindPropertyRelative("ExitTime").floatValue = source.FindPropertyRelative("ExitTime").floatValue;
        target.FindPropertyRelative("IsRepeated").boolValue = source.FindPropertyRelative("IsRepeated").boolValue;
        target.FindPropertyRelative("RepeatDestStateName").stringValue = source.FindPropertyRelative("RepeatDestStateName").stringValue;
        target.FindPropertyRelative("RepeatCount").intValue = source.FindPropertyRelative("RepeatCount").intValue;
    }

    private void InitializeStateDefaults(int index)
    {
        SerializedProperty target = statesProperty.GetArrayElementAtIndex(index);
        target.FindPropertyRelative("StateName").stringValue = "State";
        target.FindPropertyRelative("DestStateName").stringValue = "";
        target.FindPropertyRelative("HasExitTime").boolValue = false;
        target.FindPropertyRelative("ExitTime").floatValue = 0f;
        target.FindPropertyRelative("IsRepeated").boolValue = false;
        target.FindPropertyRelative("RepeatDestStateName").stringValue = "";
        target.FindPropertyRelative("RepeatCount").intValue = 1;
    }

    private void UpdateSceneObjects()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        GameObject statesObject = null;
        GameObject requiredObjectsWrapper = null;

        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == RequiredObjectsWrapperPrefabPath)
            {
                requiredObjectsWrapper = obj;
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    Transform child = obj.transform.GetChild(i);
                    if (child.gameObject.name == "States")
                    {
                        statesObject = child.gameObject;
                        break;
                    }
                }
            }
        }
        
        if (statesObject == null && requiredObjectsWrapper != null)
        {
            statesObject = new GameObject("States");
            statesObject.transform.SetParent(requiredObjectsWrapper.transform, false);
        }

        for (int i = 0; i < stateList.States.Length; i++)
        {
            string stateName = stateList.States[i].StateName;
            if (string.IsNullOrEmpty(stateName))
            {
                stateName = "State";
            }
            Transform child = statesObject.transform.Find(stateName);

            if (child == null)
            {
                child = FindChildByStateID(statesObject.transform, i);
                if (child != null)
                {
                    child.name = stateName;
                }
                else
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
            }
            else if (child.name != stateName)
            {
                child.name = stateName;
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
                UpdateTransitionDestStateId(transition, destStateId, stateList.States[i].StateName == "Trial - Rest");
            }
        }

        for (int i = statesObject.transform.childCount - 1; i >= stateList.States.Length; i--)
        {
            DestroyImmediate(statesObject.transform.GetChild(i).gameObject);
        }
    }

    private Transform FindChildByStateID(Transform parent, int stateID)
    {
        foreach (Transform child in parent)
        {
            GameObject transition = child.Find("Transition")?.gameObject;
            if (transition != null)
            {
                Component stateIdSettingComp = transition.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
                if (stateIdSettingComp != null)
                {
                    SerializedObject serializedComp = new SerializedObject(stateIdSettingComp);
                    SerializedProperty specificProperty = serializedComp.FindProperty("logic.statements");

                    if (specificProperty != null && specificProperty.isArray && specificProperty.arraySize > 0)
                    {
                        for (int i = 0; i < specificProperty.arraySize; i++)
                        {
                            SerializedProperty targetKey = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                            if (targetKey != null && targetKey.stringValue == "state_id")
                            {
                                SerializedProperty stateIdProp = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                                if (stateIdProp != null && stateIdProp.intValue == stateID)
                                {
                                    return child;
                                }
                            }
                        }
                    }
                }
            }
        }
        return null;
    }

    private void UpdateTransitionCurrentStateId(GameObject transition, int stateId)
    {
        Component stateIdSettingComp = transition.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
        if (stateIdSettingComp != null)
        {
            SerializedObject serializedComp = new SerializedObject(stateIdSettingComp);
            SerializedProperty specificProperty = serializedComp.FindProperty("logic.statements");

            if (specificProperty != null && specificProperty.isArray && specificProperty.arraySize > 0)
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
                        else
                        {
                            Debug.LogWarning("Property not found: logic.statements.singleStatement.expression.value.constant.integerValue");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Property not found: logic.statements");
            }
        }
    }

    private void UpdateTransitionDestStateId(GameObject transition, int destStateId, bool isTrialRestState = false)
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
                for (int i = 0; i < specificProperty.arraySize; i++)
                {
                    SerializedProperty targetKey = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "state_currentID")
                    {
                        SerializedProperty transitDestStateIdProp = isTrialRestState
                            ? specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.operatorExpression.operands.Array.data[1].value.constant.integerValue")
                            : specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");

                        if (transitDestStateIdProp != null)
                        {
                            transitDestStateIdProp.intValue = destStateId;
                            serializedTransitionSettingLogic.ApplyModifiedProperties();
                        }
                        else
                        {
                            Debug.LogWarning("Property not found for setting transition dest id");
                        }

                        if (isTrialRestState)
                        {
                            SerializedProperty trialTaskStateIdProp = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.operatorExpression.operands.Array.data[2].value.constant.integerValue");
                            if (trialTaskStateIdProp != null)
                            {
                                trialTaskStateIdProp.intValue = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Task");
                                serializedTransitionSettingLogic.ApplyModifiedProperties();
                            }
                            else
                            {
                                Debug.LogWarning("Property not found for setting Trial-Task in trial rest transition");
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Property not found: logic.statements");
            }
        }
    }

    private void UpdateTransitionExitTime(GameObject transition, bool hasExitTime, float exitTime)
    {
        var itemTimers = transition.GetComponents<ClusterVR.CreatorKit.Operation.Implements.ItemTimer>();
        foreach (var itemTimer in itemTimers)
        {
            SerializedObject serializedComp = new SerializedObject(itemTimer);
            var keyProp = serializedComp.FindProperty("key.key");
            if (keyProp != null && (keyProp.stringValue == "state_enter" || keyProp.stringValue == "state_enter(disabled)"))
            {
                keyProp.stringValue = hasExitTime ? "state_enter" : "state_enter(disabled)";
                var delayTimeProp = serializedComp.FindProperty("delayTimeSeconds");
                delayTimeProp.floatValue = exitTime;
                serializedComp.ApplyModifiedProperties();
                break;
            }
        }
    }

    private void UpdateRepeatedTransition(GameObject transition, int repeatDestStateId = 0, int repeatCount = 1)
    {
        var globalLogics = transition.GetComponents<ClusterVR.CreatorKit.Operation.Implements.GlobalLogic>();
        Component transitionSettingLogic = null;
        foreach (var globalLogic in globalLogics)
        {
            SerializedObject serializedComp = new SerializedObject(globalLogic);
            var keyProp = serializedComp.FindProperty("globalGimmickKey.key.key");
            if (keyProp != null && keyProp.stringValue == "state_triggerTransitionToRepeat")
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
                for (int i = 0; i < specificProperty.arraySize; i++)
                {
                    SerializedProperty targetKey = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "state_currentID")
                    {
                        SerializedProperty transitDestStateIdProp = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (transitDestStateIdProp != null)
                        {
                            transitDestStateIdProp.intValue = repeatDestStateId;
                            serializedTransitionSettingLogic.ApplyModifiedProperties();
                            break;
                        }
                        else
                        {
                            Debug.LogWarning("Property not found: logic.statements.singleStatement.expression.value.constant.integerValue");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Property not found: logic.statements");
            }
        }

        var stateIdSettingComp = transition.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
        if (stateIdSettingComp != null)
        {
            SerializedObject serializedComp = new SerializedObject(stateIdSettingComp);
            SerializedProperty specificProperty = serializedComp.FindProperty("logic.statements");

            if (specificProperty != null && specificProperty.isArray && specificProperty.arraySize > 0)
            {
                for (int i = 0; i < specificProperty.arraySize; i++)
                {
                    SerializedProperty targetKey = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "state_repeatCountMax")
                    {
                        SerializedProperty repeatCountMaxProp = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (repeatCountMaxProp != null)
                        {
                            repeatCountMaxProp.intValue = repeatCount;
                            serializedComp.ApplyModifiedProperties();
                            break;
                        }
                        else
                        {
                            Debug.LogWarning("Property not found: logic.statements.singleStatement.expression.value.constant.integerValue");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Property not found: logic.statements");
            }
        }
    }

    private void UpdateStateListeningItemsAfterReorder()
    {
        if (stateList == null || previousStates == null) return;

        // Create a dictionary to map old state IDs to new state IDs
        Dictionary<int, int> stateIdMap = new Dictionary<int, int>();
        for (int i = 0; i < previousStates.Length; i++)
        {
            string stateName = previousStates[i].StateName;
            int newStateIndex = Array.FindIndex(stateList.States, s => s.StateName == stateName);
            stateIdMap.Add(i, newStateIndex);
        }

        // Update state IDs in StateListeningItemData assets and corresponding ClusterScripts
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string listenersFolderPath = string.Format(stateManagementScriptFolderPathFormat, sceneName) + "/StateListeners";
        if (Directory.Exists(listenersFolderPath))
        {
            string[] assetFiles = Directory.GetFiles(listenersFolderPath, "*.asset");

            foreach (string assetFile in assetFiles)
            {
                StateListeningItemData stateListeningItemData = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetFile);
                if (stateListeningItemData != null)
                {
                    bool updated = false;
                    
                    var updatedListeners = new List<StateListener>();
                    foreach (StateListener listener in stateListeningItemData.stateListeners)
                    {
                        if (stateIdMap.ContainsKey(listener.stateID))
                        {
                            listener.stateID = stateIdMap[listener.stateID];
                            updated = true;
                            if (listener.stateID >= 0) updatedListeners.Add(listener);
                        }
                        else
                        {
                            updatedListeners.Add(listener);
                        }
                    }
                    stateListeningItemData.stateListeners = updatedListeners.ToArray();

                    if (updated)
                    {
                        EditorUtility.SetDirty(stateListeningItemData);
                    }
                }
                
                var itemName = assetFile.Replace(listenersFolderPath, "").Replace("\\", "").Replace(".asset", "");
                string scriptPath = string.Format("Assets/_Experiment_/Scripts/StateManagement/{0}/{1}.js", sceneName, itemName);
            
                string newScriptContent = "";
                newScriptContent += GenerateOnStateEnterFunction(stateListeningItemData.stateListeners);
                newScriptContent += "\n";
                newScriptContent += GenerateDuringStateFunction(stateListeningItemData.stateListeners);
                newScriptContent += "\n";
                newScriptContent += GenerateOnStateExitFunction(stateListeningItemData.stateListeners);

                File.WriteAllText(scriptPath, newScriptContent);
            }
        }

        // Save changes and refresh the AssetDatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Array.Copy(stateList.States, previousStates, stateList.States.Length);
    }
    
    private List<GameObject> RetrieveStateListeningItems()
    {
        List<GameObject> stateListeningItems = new List<GameObject>();
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(obj) != null)
            {
                string sourcePrefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(obj));
                if (sourcePrefabPath == stateListeningItemPrefabPath)
                {
                    stateListeningItems.Add(obj);
                }
            }
        }
        return stateListeningItems;
    }
    
    private string GenerateStateFunction(StateListener[] listeners, string functionName, Func<StateListener, List<StateListenerAction>> actionSelector, string extraParameters = "")
    {
        var content = $"function {functionName}({extraParameters}) {{\n";
        content += "  const STATE_ID = $.getStateCompat(\"global\", \"state_currentID\", \"integer\");\n";
        content += "  const CONDITION = $.groupState.currentCondition;\n\n";

        foreach (var listenerData in listeners)
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

        content += "}\n\n";
        return content;
    }

    private string GenerateOnStateEnterFunction(StateListener[] listeners)
    {
        return GenerateStateFunction(
            listeners,
            "OnStateEnter",
            listener => listener.onStateStartedActions
        );
    }

    private string GenerateDuringStateFunction(StateListener[] listeners)
    {
        return GenerateStateFunction(
            listeners,
            "DuringState",
            listener => listener.duringStateActions,
            "deltaTime"
        );
    }

    private string GenerateOnStateExitFunction(StateListener[] listeners)
    {
        return GenerateStateFunction(
            listeners,
            "OnStateExit",
            listener => listener.onStateExitedActions
        );
    }
}
