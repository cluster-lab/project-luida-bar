using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClusterVR.CreatorKit.Gimmick.Implements;
using ClusterVR.CreatorKit.Item.Implements;

public class StateMachineConfigTab : LuidaAutomationConfigTab
{
    protected override LuidaConfigWindow.TabIndex TabIndex => LuidaConfigWindow.TabIndex.StateMachine;
    
    public StateList stateList;
    private StateList.State[] previousStates;
    private SerializedObject serializedStateList;
    private SerializedProperty statesProperty;
    private string statePrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/State.prefab";
    private string trialRestStatePrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/Trial - Rest State.prefab";
    private const string ExpManagersWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/LUIDA-ExpManagers.prefab";
    private const string stateListTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/StateList/Template.asset";
    private const string stateManagementScriptFolderPathFormat = "Assets/_Experiment_/Scripts/StateManagement/{0}";

    // Fixed states that must not be moved.
    private readonly string[] FixedStateNames = new string[] { "Trial - Start", "Trial - Rest", "End" };
    private Vector2 scrollPos;
    private string sceneName;

    public void OnEnable()
    {
        sceneName = SceneManager.GetActiveScene().name;
        LoadStateList();
        if (stateList != null && stateList.States != null)
        {
            previousStates = new StateList.State[stateList.States.Length];
            Array.Copy(stateList.States, previousStates, stateList.States.Length);
        }
        else if (stateList != null)
        {
            previousStates = new StateList.State[0];
        }

        FindOrCreateStatesContainer();
    }

    private void LoadStateList()
    {
        sceneName = SceneManager.GetActiveScene().name;
        string stateListPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";

        stateList = AssetDatabase.LoadAssetAtPath<StateList>(stateListPath);
        if (stateList == null)
        {
            string newAssetPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(newAssetPath));
            AssetDatabase.CopyAsset(stateListTemplatePath, newAssetPath);
            AssetDatabase.Refresh();
            stateList = AssetDatabase.LoadAssetAtPath<StateList>(newAssetPath);
        }

        if (stateList != null)
        {
            serializedStateList = new SerializedObject(stateList);
            statesProperty = serializedStateList.FindProperty("States");
            if (stateList.States == null)
            {
                stateList.States = new StateList.State[0];
                EditorUtility.SetDirty(stateList);
                serializedStateList.Update();
            }
        }
    }

    public void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (stateList == null || sceneName != UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
        {
            LoadStateList();
            if (stateList != null && stateList.States != null && (previousStates == null || previousStates.Length != stateList.States.Length))
            {
                previousStates = new StateList.State[stateList.States.Length];
                Array.Copy(stateList.States, previousStates, stateList.States.Length);
            }
        }

        if (stateList == null)
        {
            StateList template = AssetDatabase.LoadAssetAtPath<StateList>(stateListTemplatePath);
            if (template != null)
            {
                string newAssetPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
                Directory.CreateDirectory(Path.GetDirectoryName(newAssetPath));
                AssetDatabase.CopyAsset(stateListTemplatePath, newAssetPath);
                AssetDatabase.Refresh();
                stateList = AssetDatabase.LoadAssetAtPath<StateList>(newAssetPath);
                if (stateList != null)
                {
                    EditorGUILayout.HelpBox($"StateList created at {newAssetPath}.", MessageType.Info);
                    serializedStateList = new SerializedObject(stateList);
                    statesProperty = serializedStateList.FindProperty("States");
                    if (stateList.States == null) stateList.States = new StateList.State[0];
                    previousStates = new StateList.State[stateList.States.Length];
                    Array.Copy(stateList.States, previousStates, stateList.States.Length);
                }
                else
                    EditorGUILayout.HelpBox($"Failed to create StateList at {newAssetPath}.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox($"StateList template not found at {stateListTemplatePath}. Please ensure it exists.", MessageType.Error);
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        if (serializedStateList == null || serializedStateList.targetObject != stateList)
        {
            serializedStateList = new SerializedObject(stateList);
            statesProperty = serializedStateList.FindProperty("States");
        }

        serializedStateList.Update();

        int trialStartIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Start");
        int trialRestIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Rest");
        int endIndex = Array.FindIndex(stateList.States, s => s.StateName == "End");

        bool stateOrderChanged = false;

        bool endTransitionFound = false;
        if (endIndex >= 0)
        {
            for (int k = 0; k < stateList.States.Length; k++)
            {
                if (stateList.States[k].StateName == "End") continue;

                if (k < stateList.States.Length - 1)
                {
                    if (stateList.States[k + 1].StateName == "End")
                    {
                        endTransitionFound = true;
                        break;
                    }
                }
                else if (k == stateList.States.Length - 1)
                {
                    endTransitionFound = true;
                    break;
                }
            }
        }

        for (int i = 0; i < statesProperty.arraySize; i++)
        {
            if (i == 0)
            {
                var boldLargeLabel = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                EditorGUILayout.LabelField("States Before Trials", boldLargeLabel);
            }

            if (i == trialStartIndex && trialStartIndex >= 0)
            {
                if (GUILayout.Button("Add State Before Trials", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.3f - 10f)))
                {
                    GUI.FocusControl(null);
                    int newStateIndex = trialStartIndex;
                    statesProperty.InsertArrayElementAtIndex(newStateIndex);
                    InitializeStateDefaults(newStateIndex);
                    InsertStateGameObjectAtIndex(newStateIndex);
                    UpdateStateIDsFromIndex(newStateIndex + 1);
                    stateOrderChanged = true;
                    serializedStateList.ApplyModifiedProperties();
                    stateList = (StateList)serializedStateList.targetObject;
                    trialStartIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Start");
                    trialRestIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Rest");
                    endIndex = Array.FindIndex(stateList.States, s => s.StateName == "End");
                    break;
                }
                GUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                DrawDarkLabel("Trial-related States", true);
                DrawDarkLabel("Automatically repeat " + CalculateTrialCountForCurrentScene().ToString() + " times from 'Trial - Start' to 'Trial - Rest' (repetition time calculated from your configuration for within-subject variables)");
            }

            if (trialRestIndex >= 0 && i == trialRestIndex + 1)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUILayout.Space(20);
                var boldLargeLabel = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                EditorGUILayout.LabelField("States After Trials", boldLargeLabel);
            }

            if (trialStartIndex != -1 && trialRestIndex != -1 && i == trialRestIndex)
            {
                if (GUILayout.Button("Add State During Trials", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.3f - 10f)))
                {
                    GUI.FocusControl(null);
                    int newTrialStateIndex = trialRestIndex;
                    statesProperty.InsertArrayElementAtIndex(newTrialStateIndex);
                    InitializeTrialStateDefaults(newTrialStateIndex);
                    InsertTrialStateGameObjectAtIndex(newTrialStateIndex);
                    UpdateStateIDsFromIndex(newTrialStateIndex + 1);
                    stateOrderChanged = true;
                    serializedStateList.ApplyModifiedProperties();
                    stateList = (StateList)serializedStateList.targetObject;
                    trialStartIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Start");
                    trialRestIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Rest");
                    endIndex = Array.FindIndex(stateList.States, s => s.StateName == "End");
                    break;
                }
            }

            if (i == endIndex && endIndex >= 0)
            {
                if (GUILayout.Button("Add State After Trials", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.3f - 10f)))
                {
                    GUI.FocusControl(null);
                    int newStateIndex = endIndex;
                    statesProperty.InsertArrayElementAtIndex(newStateIndex);
                    InitializeStateDefaults(newStateIndex);
                    InsertStateGameObjectAtIndex(newStateIndex);
                    UpdateStateIDsFromIndex(newStateIndex + 1);
                    stateOrderChanged = true;
                    serializedStateList.ApplyModifiedProperties();
                    stateList = (StateList)serializedStateList.targetObject;
                    trialStartIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Start");
                    trialRestIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Rest");
                    endIndex = Array.FindIndex(stateList.States, s => s.StateName == "End");
                    break;
                }

                if (!endTransitionFound && stateList.States.Length > 1)
                    EditorGUILayout.HelpBox("No state appears to lead to the 'End' state. Ensure the experiment can conclude.", MessageType.Warning);

                GUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                DrawDarkLabel("The 'End' state should always be the last state.");
            }

            bool isHighlight = (trialStartIndex >= 0 && i >= trialStartIndex && trialRestIndex >= 0 && i <= trialRestIndex) || (endIndex >= 0 && i == endIndex);

            Color originalBackgroundColor = GUI.backgroundColor;
            Color originalContentColor = GUI.contentColor;

            if (isHighlight)
            {
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(20);
            EditorGUILayout.BeginVertical(GUILayout.Width(60));
            EditorGUILayout.LabelField("State ID", GUILayout.Width(60));
            EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(60));
            EditorGUILayout.EndVertical();

            SerializedProperty stateProp = statesProperty.GetArrayElementAtIndex(i);
            SerializedProperty stateNameProp = stateProp.FindPropertyRelative("StateName");
            SerializedProperty destStateNameProp = stateProp.FindPropertyRelative("DestStateName");
            string currentActualStateName = stateNameProp.stringValue;
            bool isCurrentFixedState = Array.IndexOf(FixedStateNames, currentActualStateName) > -1;
            bool isCurrentEndState = (currentActualStateName == "End");

            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            EditorGUILayout.LabelField("State name:");
            EditorGUI.BeginDisabledGroup(isCurrentFixedState);
            EditorGUILayout.PropertyField(stateNameProp, GUIContent.none, GUILayout.Width(150));
            if (string.IsNullOrEmpty(stateNameProp.stringValue))
                stateNameProp.stringValue = "State" + i;
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            string autoCalculatedDestName = "";
            if (!isCurrentEndState)
            {
                if (i < statesProperty.arraySize - 1)
                {
                    SerializedProperty nextStateInList = statesProperty.GetArrayElementAtIndex(i + 1);
                    autoCalculatedDestName = nextStateInList.FindPropertyRelative("StateName").stringValue;
                }
                else
                {
                    string endStateNameInList = "";
                    int endStateIndexInProperty = -1;
                    for (int j = 0; j < statesProperty.arraySize; ++j)
                    {
                        if (statesProperty.GetArrayElementAtIndex(j).FindPropertyRelative("StateName").stringValue == "End")
                        {
                            endStateNameInList = "End";
                            endStateIndexInProperty = j;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(endStateNameInList) && i != endStateIndexInProperty)
                        autoCalculatedDestName = endStateNameInList;
                    else
                        autoCalculatedDestName = string.Empty;
                }
            }

            if (destStateNameProp.stringValue != autoCalculatedDestName)
                destStateNameProp.stringValue = autoCalculatedDestName;

            EditorGUILayout.BeginVertical(GUILayout.MinWidth(250), GUILayout.MaxWidth(250));
            EditorGUILayout.LabelField("Move state to:");
            EditorGUILayout.BeginHorizontal();
            bool canMoveUp = i > 0 && !isCurrentFixedState;
            bool canMoveDown = i < statesProperty.arraySize - 1 && !isCurrentFixedState;

            EditorGUI.BeginDisabledGroup(!canMoveUp);
            if (GUILayout.Button("Up", GUILayout.Width(50))) { statesProperty.MoveArrayElement(i, i - 1); stateOrderChanged = true; break; }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(!canMoveDown);
            if (GUILayout.Button("Down", GUILayout.Width(50))) { statesProperty.MoveArrayElement(i, i + 1); stateOrderChanged = true; break; }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(isCurrentFixedState);
            if (GUILayout.Button("Remove", GUILayout.Width(60))) { statesProperty.DeleteArrayElementAtIndex(i); stateOrderChanged = true; break; }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.MinWidth(180), GUILayout.MaxWidth(250));
            SerializedProperty hasExitTime = stateProp.FindPropertyRelative("HasExitTime");
            SerializedProperty exitTime = stateProp.FindPropertyRelative("ExitTime");

            hasExitTime.boolValue = EditorGUILayout.ToggleLeft("Has Exit Time", hasExitTime.boolValue);
            if (hasExitTime.boolValue)
            {
                EditorGUI.indentLevel++;
                exitTime.floatValue = EditorGUILayout.FloatField("Exit Time", Mathf.Max(0, exitTime.floatValue));
                EditorGUI.indentLevel--;
            }

            SerializedProperty isRepeated = stateProp.FindPropertyRelative("IsRepeated");
            SerializedProperty repeatDestName = stateProp.FindPropertyRelative("RepeatDestStateName");
            SerializedProperty repeatCount = stateProp.FindPropertyRelative("RepeatCount");

            bool isTrialRelated = (trialStartIndex >= 0 && trialRestIndex >= 0 && i >= trialStartIndex && i <= trialRestIndex);

            if (!isTrialRelated)
            {
                EditorGUI.BeginDisabledGroup(isCurrentFixedState || isCurrentEndState);
                isRepeated.boolValue = EditorGUILayout.ToggleLeft("Is Repeated", isRepeated.boolValue);
                EditorGUI.EndDisabledGroup();

                if (isRepeated.boolValue)
                {
                    EditorGUI.indentLevel++;
                    string[] allStateNamesForRepeat = new string[statesProperty.arraySize];
                    for (int k = 0; k < statesProperty.arraySize; ++k)
                        allStateNamesForRepeat[k] = statesProperty.GetArrayElementAtIndex(k).FindPropertyRelative("StateName").stringValue;

                    int repIndex = Array.IndexOf(allStateNamesForRepeat, repeatDestName.stringValue);
                    repIndex = EditorGUILayout.Popup("Repeat Destination", repIndex, allStateNamesForRepeat);
                    repeatDestName.stringValue = (repIndex >= 0) ? allStateNamesForRepeat[repIndex] : string.Empty;
                    repeatCount.intValue = EditorGUILayout.IntField("Repeat Count", Math.Max(1, repeatCount.intValue));
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(50);
            
            // ### MODIFICATION START ###
            // The questionnaire UI logic is now handled by the QuestionnaireEditorManager class.
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(180));
            string stateNameValFromProp = stateNameProp.stringValue;
            if (QuestionnaireEditorManager.HasEnabledFormInstance(stateNameValFromProp))
            {
                QuestionnaireEditorManager.DisplayQuestionnaireRow(stateList, serializedStateList, stateNameValFromProp, i);
            }
            else
            {
                EditorGUILayout.LabelField("Questionnaires", GUILayout.Width(100));
                if (GUILayout.Button("Add Questionnaires", GUILayout.Width(150)))
                {
                    QuestionnaireEditorManager.AddOrEnableQuestionnaireForm(stateList, i, stateNameValFromProp);
                    serializedStateList.Update(); // Re-sync after external modification
                    Repaint(); // Refresh the editor window
                }
            }
            EditorGUILayout.EndVertical();
            // ### MODIFICATION END ###

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = originalBackgroundColor;
            GUI.contentColor = originalContentColor;

            if (GUI.changed && !EditorGUIUtility.editingTextField)
                GUI.FocusControl(null);
        }

        serializedStateList.ApplyModifiedProperties();

        bool contentChanged = stateOrderChanged;
        if (!contentChanged && previousStates != null && stateList.States.Length == previousStates.Length)
        {
            for (int k = 0; k < stateList.States.Length; k++)
            {
                if (!stateList.States[k].Equals(previousStates[k]))
                {
                    contentChanged = true;
                    break;
                }
            }
        }
        else if (previousStates == null && stateList.States != null && stateList.States.Length > 0)
        {
            contentChanged = true;
        }
        else if (previousStates != null && stateList.States != null && stateList.States.Length != previousStates.Length)
        {
            contentChanged = true;
        }

        if (contentChanged)
        {
            UpdateSceneObjects();
            UpdateStateListeningItemsAfterReorder();
            
            // ### MODIFICATION START ###
            // Synchronize questionnaire containers using the new manager
            QuestionnaireEditorManager.SyncQuestionnaireContainers(previousStates, stateList.States);
            // ### MODIFICATION END ###

            // Now update previousStates for the next frame
            previousStates = new StateList.State[stateList.States.Length];
            Array.Copy(stateList.States, previousStates, stateList.States.Length);
        }

        EditorGUILayout.EndScrollView();
    }
    
    // ... All other methods from StateMachineConfigTab (excluding those moved to QuestionnaireEditorManager) remain here ...
    // e.g., InitializeStateDefaults, UpdateSceneObjects, UpdateTransitionCurrentStateId, etc.
    
    private void InitializeStateDefaults(int index)
    {
        SerializedProperty target = statesProperty.GetArrayElementAtIndex(index);
        target.FindPropertyRelative("StateName").stringValue = "NewState" + index;
        target.FindPropertyRelative("DestStateName").stringValue = ""; // Will be auto-set by GUI logic
        target.FindPropertyRelative("HasExitTime").boolValue = false;
        target.FindPropertyRelative("ExitTime").floatValue = 0f;
        target.FindPropertyRelative("IsRepeated").boolValue = false;
        target.FindPropertyRelative("RepeatDestStateName").stringValue = "";
        target.FindPropertyRelative("RepeatCount").intValue = 1;
        target.FindPropertyRelative("qID").intValue = 0;
    }

    private void InitializeTrialStateDefaults(int index)
    {
        SerializedProperty target = statesProperty.GetArrayElementAtIndex(index);
        target.FindPropertyRelative("StateName").stringValue = "NewTrialState" + index;
        target.FindPropertyRelative("DestStateName").stringValue = ""; // Will be auto-set by GUI logic
        target.FindPropertyRelative("HasExitTime").boolValue = false;
        target.FindPropertyRelative("ExitTime").floatValue = 0f;
        target.FindPropertyRelative("IsRepeated").boolValue = false;
        target.FindPropertyRelative("RepeatDestStateName").stringValue = "";
        target.FindPropertyRelative("RepeatCount").intValue = 1;
        target.FindPropertyRelative("qID").intValue = 0;
    }

    private void UpdateSceneObjects()
    {
        if (stateList == null || stateList.States == null) return;

        var statesObjectContainer = FindOrCreateStatesContainer();
        if (statesObjectContainer == null)
        {
            Debug.LogError("Could not find or create 'States' container object.");
            return;
        }

        for (int i = statesObjectContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(statesObjectContainer.transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < stateList.States.Length; i++)
        {
            StateList.State currentStateData = stateList.States[i];
            string expectedName = string.IsNullOrEmpty(currentStateData.StateName) ? $"State{i}" : currentStateData.StateName;

            string prefabPath = (currentStateData.StateName == "Trial - Rest")
                ? trialRestStatePrefabPath
                : statePrefabPath;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Prefab not found for state: {expectedName}");
                continue;
            }

            GameObject newChild = (GameObject)PrefabUtility.InstantiatePrefab(prefab, statesObjectContainer.transform);
            newChild.name = expectedName;
            newChild.transform.SetSiblingIndex(i);

            GameObject transitionObj = newChild.transform.Find("Transition")?.gameObject;
            if (transitionObj != null)
            {
                UpdateTransitionCurrentStateId(transitionObj, i);

                int destStateId = Array.FindIndex(
                    stateList.States,
                    s => s.StateName == currentStateData.DestStateName
                );
                UpdateTransitionDestStateId(
                    transitionObj,
                    destStateId,
                    currentStateData.StateName == "Trial - Rest"
                );

                UpdateTransitionExitTime(
                    transitionObj,
                    currentStateData.HasExitTime,
                    currentStateData.ExitTime
                );

                int repeatDestId = Array.FindIndex(
                    stateList.States,
                    s => s.StateName == currentStateData.RepeatDestStateName
                );
                UpdateRepeatedTransition(
                    transitionObj,
                    Mathf.Max(0, repeatDestId),
                    currentStateData.IsRepeated ? currentStateData.RepeatCount : 1
                );
            }

            if (currentStateData.qID > 0)
            {
                // ### MODIFICATION ###
                // Re-create the questionnaire form using the new manager.
                QuestionnaireEditorManager.AddOrEnableQuestionnaireForm(stateList, i, expectedName);
            }
        }
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
                        if (stateIdProp != null && stateIdProp.intValue != stateId)
                        {
                            stateIdProp.intValue = stateId;
                            serializedComp.ApplyModifiedProperties();
                        }
                        break;
                    }
                }
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

                        if (transitDestStateIdProp != null && transitDestStateIdProp.intValue != destStateId)
                        {
                            transitDestStateIdProp.intValue = destStateId;
                            serializedTransitionSettingLogic.ApplyModifiedPropertiesWithoutUndo();
                        }

                        if (isTrialRestState)
                        {
                            SerializedProperty trialTaskStateIdProp = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.operatorExpression.operands.Array.data[2].value.constant.integerValue");
                            int trialStartIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Start");
                            if (trialTaskStateIdProp != null && trialTaskStateIdProp.intValue != trialStartIndex)
                            {
                                trialTaskStateIdProp.intValue = trialStartIndex;
                                serializedTransitionSettingLogic.ApplyModifiedPropertiesWithoutUndo();
                            }
                        }
                        break;
                    }
                }
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
                string newKey = hasExitTime ? "state_enter" : "state_enter(disabled)";
                bool changed = false;
                if (keyProp.stringValue != newKey)
                {
                    keyProp.stringValue = newKey;
                    changed = true;
                }

                var delayTimeProp = serializedComp.FindProperty("delayTimeSeconds");
                if (delayTimeProp != null && delayTimeProp.floatValue != exitTime)
                {
                    delayTimeProp.floatValue = exitTime;
                    changed = true;
                }
                if (changed)
                {
                    serializedComp.ApplyModifiedProperties();
                }
                break;
            }
        }
    }

    private void UpdateRepeatedTransition(GameObject transition, int repeatDestStateId = 0, int repeatCount = 1)
    {
        var globalLogics = transition.GetComponents<ClusterVR.CreatorKit.Operation.Implements.GlobalLogic>();
        Component repeatTransitionLogic = null;
        foreach (var globalLogic in globalLogics)
        {
            SerializedObject serializedComp = new SerializedObject(globalLogic);
            var keyProp = serializedComp.FindProperty("globalGimmickKey.key.key");
            if (keyProp != null && keyProp.stringValue == "state_triggerTransitionToRepeat")
            {
                repeatTransitionLogic = globalLogic;
                break;
            }
        }

        if (repeatTransitionLogic != null)
        {
            SerializedObject serializedRepeatLogic = new SerializedObject(repeatTransitionLogic);
            SerializedProperty statementsProp = serializedRepeatLogic.FindProperty("logic.statements");
            if (statementsProp != null && statementsProp.isArray && statementsProp.arraySize > 0)
            {
                for (int i = 0; i < statementsProp.arraySize; i++)
                {
                    SerializedProperty targetKey = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "state_currentID")
                    {
                        SerializedProperty destIdProp = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (destIdProp != null && destIdProp.intValue != repeatDestStateId)
                        {
                            destIdProp.intValue = repeatDestStateId;
                            serializedRepeatLogic.ApplyModifiedProperties();
                        }
                        break;
                    }
                }
            }
        }

        var itemLogicComp = transition.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
        if (itemLogicComp != null)
        {
            SerializedObject serializedItemLogic = new SerializedObject(itemLogicComp);
            SerializedProperty statementsProp = serializedItemLogic.FindProperty("logic.statements");
            if (statementsProp != null && statementsProp.isArray && statementsProp.arraySize > 0)
            {
                for (int i = 0; i < statementsProp.arraySize; i++)
                {
                    SerializedProperty targetKey = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "state_repeatCountMax")
                    {
                        SerializedProperty countProp = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (countProp != null && countProp.intValue != repeatCount)
                        {
                            countProp.intValue = repeatCount;
                            serializedItemLogic.ApplyModifiedProperties();
                        }
                        break;
                    }
                }
            }
        }
    }

    private void UpdateStateListeningItemsAfterReorder()
    {
        if (stateList == null || stateList.States == null)
        {
            if (stateList != null && stateList.States == null) stateList.States = new StateList.State[0];
            else return;
        }
        if (previousStates == null)
        {
            previousStates = new StateList.State[stateList.States.Length];
            Array.Copy(stateList.States, previousStates, stateList.States.Length);
        }

        var nameToNewIndexMap = new Dictionary<string, int>();
        for (int i = 0; i < stateList.States.Length; i++)
        {
            if (!string.IsNullOrEmpty(stateList.States[i].StateName))
            {
                nameToNewIndexMap[stateList.States[i].StateName] = i;
            }
        }

        sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string listenersFolder = string.Format(stateManagementScriptFolderPathFormat, sceneName) + "/StateListeners";

        if (!Directory.Exists(listenersFolder)) return;

        string[] assetFiles = Directory.GetFiles(listenersFolder, "*.asset", SearchOption.AllDirectories);
        bool anyDataDirtied = false;

        foreach (var assetFile in assetFiles)
        {
            var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetFile);
            if (data == null || data.stateListeners == null) continue;

            bool currentDataDirty = false;

            for (int listenerIndex = 0; listenerIndex < data.stateListeners.Length; listenerIndex++)
            {
                StateListener listener = data.stateListeners[listenerIndex];
                string oldStateName = null;
                if (listener.stateID >= 0 && listener.stateID < previousStates.Length)
                {
                    if (listener.stateID < previousStates.Length && !string.IsNullOrEmpty(previousStates[listener.stateID].StateName))
                    {
                        oldStateName = previousStates[listener.stateID].StateName;
                    }
                }
                
                if (!string.IsNullOrEmpty(oldStateName))
                {
                    if (nameToNewIndexMap.TryGetValue(oldStateName, out var newIndex))
                    {
                        if (listener.stateID != newIndex)
                        {
                            listener.stateID = newIndex;
                            currentDataDirty = true;
                        }
                    }
                    else
                    {
                        if (listener.stateID != -1)
                        {
                            listener.stateID = -1;
                            currentDataDirty = true;
                        }
                    }
                }
                else if (listener.stateID != -1)
                {
                    listener.stateID = -1;
                    currentDataDirty = true;
                }
                
                data.stateListeners[listenerIndex] = listener;
            }

            if (currentDataDirty)
            {
                EditorUtility.SetDirty(data);
                anyDataDirtied = true;
                string itemName = Path.GetFileNameWithoutExtension(assetFile);
                string scriptDir = Path.GetDirectoryName(assetFile);
                if (!Directory.Exists(scriptDir)) Directory.CreateDirectory(scriptDir);
                string scriptPath = Path.Combine(scriptDir, itemName + ".js");


                var sb = new StringBuilder();
                sb.AppendLine(GenerateOnStateEnterFunction(data.stateListeners));
                sb.AppendLine(GenerateDuringStateFunction(data.stateListeners));
                sb.AppendLine(GenerateOnStateExitFunction(data.stateListeners));
                sb.AppendLine(data.otherImplementation ?? string.Empty);

                File.WriteAllText(scriptPath, sb.ToString());
            }
        }

        if (anyDataDirtied)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
    
    private GameObject FindExpManagersWrapperInstance()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.Regular) {
                string prefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(obj));
                if (prefabPath == ExpManagersWrapperPrefabPath)
                    return obj;
            }
        }
        return null;
    }

    private string GenerateStateFunction(StateListener[] listeners, string functionName, Func<StateListener, List<StateListenerAction>> actionSelector, string extraParameters = "")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"function {functionName}({extraParameters}) {{");
        sb.AppendLine("  const STATE_ID = $.state.state_id;");
        sb.AppendLine("  const CONDITION = $.groupState.currentCondition;");
        sb.AppendLine("  const PARTICIPANTS = $.groupState.participants;");
        sb.AppendLine("");

        var groupedListeners = listeners
            .Where(l => l != null && l.stateID >= 0)
            .GroupBy(l => l.stateID);

        foreach (var group in groupedListeners)
        {
            sb.AppendLine($"  if (STATE_ID === {group.Key}) {{");
            foreach (var listenerData in group)
            {
                if (listenerData == null) continue;
                var actions = actionSelector(listenerData);
                if (actions != null)
                {
                    foreach (var action in actions)
                    {
                        if (action != null)
                            sb.AppendLine($"    {action.GetActionContent()}");
                    }
                }
            }
            sb.AppendLine("  }");
        }
        sb.AppendLine("}");
        sb.AppendLine("");
        return sb.ToString();
    }

    private string GenerateOnStateEnterFunction(StateListener[] listeners)
    {
        return GenerateStateFunction(listeners, "OnStateEnter", listener => listener.onStateStartedActions);
    }

    private string GenerateDuringStateFunction(StateListener[] listeners)
    {
        return GenerateStateFunction(listeners, "DuringState", listener => listener.duringStateActions, "deltaTime");
    }

    private string GenerateOnStateExitFunction(StateListener[] listeners)
    {
        return GenerateStateFunction(listeners, "OnStateExit", listener => listener.onStateExitedActions);
    }

    private void InsertStateGameObjectAtIndex(int index)
    {
        serializedStateList.ApplyModifiedProperties();
        stateList = (StateList)serializedStateList.targetObject;

        GameObject statesContainer = FindOrCreateStatesContainer();
        if (statesContainer == null || stateList == null || stateList.States == null || index < 0 || index >= stateList.States.Length)
        {
            Debug.LogError("Cannot insert state GameObject due to invalid input or missing container.");
            return;
        }

        string stateName = stateList.States[index].StateName;
        string prefabToUsePath = (stateName == "Trial - Rest") ? trialRestStatePrefabPath : statePrefabPath;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabToUsePath);
        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at {prefabToUsePath} for state {stateName}");
            return;
        }

        GameObject newStateGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab, statesContainer.transform);
        newStateGO.name = stateName;
        newStateGO.transform.SetSiblingIndex(index);

        GameObject transition = newStateGO.transform.Find("Transition")?.gameObject;
        if (transition != null)
            UpdateTransitionCurrentStateId(transition, index);
    }

    private void UpdateStateIDsFromIndex(int startIndex)
    {
        GameObject statesContainer = FindOrCreateStatesContainer();
        if (statesContainer == null) return;

        for (int i = startIndex; i < statesContainer.transform.childCount; i++)
        {
            if (i < stateList.States.Length)
            {
                Transform child = statesContainer.transform.GetChild(i);
                GameObject transition = child.Find("Transition")?.gameObject;
                if (transition != null)
                {
                    UpdateTransitionCurrentStateId(transition, i);
                }
            }
        }
    }

    private GameObject FindOrCreateStatesContainer()
    {
        GameObject expManagersWrapper = FindExpManagersWrapperInstance();
        if (expManagersWrapper == null)
        {
            GameObject wrapperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExpManagersWrapperPrefabPath);
            if (wrapperPrefab != null)
            {
                expManagersWrapper = (GameObject)PrefabUtility.InstantiatePrefab(wrapperPrefab);
                expManagersWrapper.name = wrapperPrefab.name;
                Undo.RegisterCreatedObjectUndo(expManagersWrapper, "Create Required Objects Wrapper");
                Debug.Log("ExpManagersWrapper prefab instance created as it was not found.");
            }
            else
            {
                Debug.LogError($"ExpManagersWrapper prefab not found at {ExpManagersWrapperPrefabPath}. Cannot create 'States' container.");
                return null;
            }
        }

        Transform statesObjectTransform = expManagersWrapper.transform.Find("States");
        if (statesObjectTransform == null)
        {
            GameObject statesObject = new GameObject("States");
            Undo.RegisterCreatedObjectUndo(statesObject, "Create States Container");
            statesObject.transform.SetParent(expManagersWrapper.transform, false);
            UpdateSceneObjects();
            return statesObject;
        }
        return statesObjectTransform.gameObject;
    }

    private void InsertTrialStateGameObjectAtIndex(int index)
    {
        serializedStateList.ApplyModifiedProperties();
        stateList = (StateList)serializedStateList.targetObject;

        GameObject statesContainer = FindOrCreateStatesContainer();
        if (statesContainer == null || stateList == null || stateList.States == null || index < 0 || index >= stateList.States.Length)
        {
            Debug.LogError("Cannot insert trial state GameObject due to invalid input or missing container.");
            return;
        }

        string stateName = stateList.States[index].StateName;
        string prefabToUsePath = statePrefabPath;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabToUsePath);
        if (prefab == null)
        {
            Debug.LogError($"Trial State prefab not found at {prefabToUsePath} for state {stateName}");
            return;
        }

        GameObject newTrialStateGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab, statesContainer.transform);
        newTrialStateGO.name = stateName;
        newTrialStateGO.transform.SetSiblingIndex(index);

        GameObject transition = newTrialStateGO.transform.Find("Transition")?.gameObject;
        if (transition != null)
            UpdateTransitionCurrentStateId(transition, index);
    }

    private void DrawDarkLabel(string text, bool isLarge = false)
    {
        var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * (isLarge ? 1.2f : 1f));
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

        Color originalContent = GUI.contentColor;
        GUI.contentColor = Color.white;

        var style = isLarge ? new GUIStyle(EditorStyles.largeLabel) : new GUIStyle(EditorStyles.wordWrappedMiniLabel);
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;

        var labelRect = new Rect(rect.x + 4, rect.y, rect.width - 8, rect.height);
        EditorGUI.LabelField(labelRect, text, style);

        GUI.contentColor = originalContent;
    }

    private int CalculateTrialCountForCurrentScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string jsPath = $"Assets/_Experiment_/Settings/ExperimentVariables/{sceneName}.js";
        var jsAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(jsPath);
        int trialsCountForEachUniqueCondition = 1;
        int product = 1;

        if (jsAsset != null && !string.IsNullOrEmpty(jsAsset.text))
        {
            var match = System.Text.RegularExpressions.Regex.Match(jsAsset.text, @"const trialsCountForEachUniqueCondition = (\d+);");
            if (match.Success)
                trialsCountForEachUniqueCondition = int.Parse(match.Groups[1].Value);

            var pattern = @"const within_subjects_variables = \[(.*?)\];";
            var matchVars = System.Text.RegularExpressions.Regex.Match(jsAsset.text, pattern, System.Text.RegularExpressions.RegexOptions.Singleline);
            if (matchVars.Success)
            {
                var arrayContent = matchVars.Groups[1].Value;
                var variableMatches = System.Text.RegularExpressions.Regex.Matches(arrayContent, @"\{(.*?)\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                foreach (System.Text.RegularExpressions.Match variableMatch in variableMatches)
                {
                    string variableContent = variableMatch.Groups[1].Value;
                    string valuesString = System.Text.RegularExpressions.Regex.Match(variableContent, @"values: \[(.*?)\]").Groups[1].Value;
                    string[] values = valuesString.Split(',').Select(v => v.Trim().Trim('"')).Where(v => !string.IsNullOrEmpty(v)).ToArray();
                    if (values.Length > 0)
                        product *= values.Length;
                }
            }
        }
        return trialsCountForEachUniqueCondition * product;
    }
}
