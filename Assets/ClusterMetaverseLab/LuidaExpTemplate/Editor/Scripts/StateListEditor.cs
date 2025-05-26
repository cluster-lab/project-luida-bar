using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClusterVR.CreatorKit.Item;
using ClusterVR.CreatorKit.Item.Implements;

public class StateListEditor : EditorWindow
{
    private StateList stateList;
    private StateList.State[] previousStates;
    private SerializedObject serializedStateList;
    private SerializedProperty statesProperty;
    private string statePrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/State.prefab";
    private string trialRestStatePrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/Trial - Rest State.prefab";
    private const string RequiredObjectsWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";
    private const string stateListTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/StateList/Template.asset";
    private const string stateManagementScriptFolderPathFormat = "Assets/_Experiment_/Scripts/StateManagement/{0}";
    private const string stateListeningItemPrefabPath = "Assets/ClusterVR.CreatorKit.Item.Implements.StateListeningItem"; //Fixed this Path
    private const string formPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/Questionnaire/Questionnaire.prefab";
    private const string identifiersAssetPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string WorldItemRefListObjectName = "WorldItemRefList";

    // Fixed states that must not be moved.
    private readonly string[] FixedStateNames = new string[] { "Trial - Start", "Trial - Rest", "End" };
    private Vector2 scrollPos;
    private string sceneName;

    public void OnEnable()
    {
        sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        LoadStateList();
        if (stateList != null && stateList.States != null) // Ensure stateList and States are not null
        {
            previousStates = new StateList.State[stateList.States.Length];
            Array.Copy(stateList.States, previousStates, stateList.States.Length);
        }
        else if (stateList != null) // stateList exists but States is null
        {
            previousStates = new StateList.State[0]; // Initialize to empty array
        }
        // If stateList is null, previousStates will remain null and handled by LoadStateList logic
    }

    private void LoadStateList()
    {
        sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string stateListPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";

        stateList = AssetDatabase.LoadAssetAtPath<StateList>(stateListPath);
        if (stateList == null)
        {
            string newAssetPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(newAssetPath)); // Ensure directory exists
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
                serializedStateList.Update(); // Update serialized object if we changed the underlying asset
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
                Directory.CreateDirectory(Path.GetDirectoryName(newAssetPath)); // Ensure directory exists
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

        // Check transitions to 'End' - REVISED
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
                    normal = { textColor = Color.white } // Set font color to white
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
                    // No need to apply here, will be applied after loop
                    InsertStateGameObjectAtIndex(newStateIndex); // This uses stateList.States, so apply needs to happen before or it needs serializedProp
                    UpdateStateIDsFromIndex(newStateIndex + 1);
                    stateOrderChanged = true;
                    // Applying immediately after insertion ensures subsequent logic in the loop (like auto-dest) uses updated data
                    serializedStateList.ApplyModifiedProperties();
                    stateList = (StateList)serializedStateList.targetObject; // Re-fetch if direct modifications were made to asset
                    // Recalculate indices as they might have shifted
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
                    normal = { textColor = Color.white } // Set font color to white
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

                if (!endTransitionFound && stateList.States.Length > 1) // Don't show if "End" is the only state or no states
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

            // --- Auto-set and Display Transition Destination ---
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
                    {
                        autoCalculatedDestName = endStateNameInList;
                    }
                    else
                    {
                        autoCalculatedDestName = string.Empty;
                    }
                }
            }

            if (destStateNameProp.stringValue != autoCalculatedDestName)
            {
                destStateNameProp.stringValue = autoCalculatedDestName;
            }

            /*
            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            EditorGUILayout.LabelField("Transit destination state:");
            EditorGUI.BeginDisabledGroup(true); 
            EditorGUILayout.LabelField(destStateNameProp.stringValue, GUILayout.Width(146));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            */

            EditorGUILayout.BeginVertical(GUILayout.MinWidth(250), GUILayout.MaxWidth(250));
            EditorGUILayout.LabelField("Move state to:");
            EditorGUILayout.BeginHorizontal();
            bool canMoveUp = i > 0 && !isCurrentFixedState;
            // Prevent moving a state past "End" if "End" is supposed to be last, or into fixed blocks.
            // More detailed logic might be needed if strict ordering around fixed blocks is enforced.
            bool canMoveDown = i < statesProperty.arraySize - 1 && !isCurrentFixedState;

            EditorGUI.BeginDisabledGroup(!canMoveUp);
            if (GUILayout.Button("Up", GUILayout.Width(50))) { statesProperty.MoveArrayElement(i, i - 1); stateOrderChanged = true; break; }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(!canMoveDown);
            if (GUILayout.Button("Down", GUILayout.Width(50))) { statesProperty.MoveArrayElement(i, i + 1); stateOrderChanged = true; break; }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(isCurrentFixedState); // FixedStateNames includes "End"
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
            bool isTrialRest = (currentActualStateName == "Trial - Rest");

            if (!isTrialRelated)
            {
                // --- Show repetition settings only for non-trial-related states ---
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
                    if (repIndex >= 0)
                    {
                        repeatDestName.stringValue = allStateNamesForRepeat[repIndex];
                    }
                    else
                    {
                        repeatDestName.stringValue = string.Empty;
                    }

                    repeatCount.intValue = EditorGUILayout.IntField("Repeat Count", Math.Max(1, repeatCount.intValue));
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(50);


            EditorGUILayout.BeginVertical(GUILayout.MinWidth(180));
            string stateNameValFromProp = stateNameProp.stringValue;
            GameObject sceneObj = GameObject.Find(stateNameValFromProp);
            if (HasEnabledFormInstance(sceneObj))
                DisplayQuestionnaireRow(stateNameValFromProp, i);
            else
            {
                EditorGUILayout.LabelField("Questionnaire", GUILayout.Width(100));
                if (GUILayout.Button("Add Questionnaire", GUILayout.Width(150)))
                    AddOrEnableQuestionnaireForm(i, stateNameValFromProp);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = originalBackgroundColor;
            GUI.contentColor = originalContentColor;

            if (GUI.changed && !EditorGUIUtility.editingTextField)
                GUI.FocusControl(null);
        }

        serializedStateList.ApplyModifiedProperties();

        // The specific auto-management for "Trial - Start" and "Trial - Rest" destinations
        // is now handled by the general auto-destination logic within the loop.
        // So, those specific blocks are removed.

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
        { // Initial creation
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
            if (stateList.States != null)
            {
                previousStates = new StateList.State[stateList.States.Length];
                Array.Copy(stateList.States, previousStates, stateList.States.Length);
            }
            else
            {
                previousStates = new StateList.State[0];
            }
        }

        EditorGUILayout.EndScrollView();
    }


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

        // 1) Find or create the "States" container
        var statesObjectContainer = FindOrCreateStatesContainer();
        if (statesObjectContainer == null)
        {
            Debug.LogError("Could not find or create 'States' container object.");
            return;
        }

        // 2) Remove all existing state GameObjects
        for (int i = statesObjectContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(statesObjectContainer.transform.GetChild(i).gameObject);
        }

        // 3) Regenerate each state and re‐add questionnaires as needed
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

            // ── Transition setup ──
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

            // ── Questionnaire re‐spawn ──
            if (currentStateData.qID > 0)
            {
                AddOrEnableQuestionnaireForm(i, expectedName);
            }
        }
    }

    private Transform FindChildByStateID(Transform parent, int stateID)
    {
        foreach (Transform child in parent)
        {
            GameObject transition = child.Find("Transition")?.gameObject;
            if (transition != null)
            {
                var itemLogic = transition.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
                if (itemLogic != null)
                {
                    SerializedObject serializedComp = new SerializedObject(itemLogic);
                    SerializedProperty statementsProp = serializedComp.FindProperty("logic.statements");
                    if (statementsProp != null && statementsProp.isArray)
                    {
                        for (int i = 0; i < statementsProp.arraySize; i++)
                        {
                            SerializedProperty statement = statementsProp.GetArrayElementAtIndex(i);
                            SerializedProperty targetKey = statement.FindPropertyRelative("singleStatement.targetState.key");
                            if (targetKey != null && targetKey.stringValue == "state_id")
                            {
                                SerializedProperty valueProp = statement.FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                                if (valueProp != null && valueProp.intValue == stateID)
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
                bool found = false;
                for (int i = 0; i < specificProperty.arraySize; i++)
                {
                    SerializedProperty targetKey = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "state_id")
                    {
                        SerializedProperty stateIdProp = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (stateIdProp != null)
                        {
                            if (stateIdProp.intValue != stateId)
                            {
                                stateIdProp.intValue = stateId;
                                serializedComp.ApplyModifiedProperties();
                            }
                            found = true;
                            break;
                        }
                    }
                }
                // if (!found) Debug.LogWarning($"'state_id' key not found in ItemLogic on {transition.transform.parent.name}/Transition");
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
                bool foundTarget = false;
                for (int i = 0; i < specificProperty.arraySize; i++)
                {
                    SerializedProperty targetKey = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "state_currentID")
                    {
                        foundTarget = true;
                        SerializedProperty transitDestStateIdProp = isTrialRestState
                            ? specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.operatorExpression.operands.Array.data[1].value.constant.integerValue")
                            : specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");

                        if (transitDestStateIdProp != null)
                        {
                            if (transitDestStateIdProp.intValue != destStateId)
                            {
                                transitDestStateIdProp.intValue = destStateId;
                                serializedTransitionSettingLogic.ApplyModifiedPropertiesWithoutUndo();
                            }
                        }
                        else
                        {
                            // Debug.LogWarning($"Transition destination ID property not found for {transition.transform.parent.name}, isTrialRestState: {isTrialRestState}");
                        }

                        if (isTrialRestState)
                        {
                            SerializedProperty trialTaskStateIdProp = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.operatorExpression.operands.Array.data[2].value.constant.integerValue");
                            int trialStartIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Start");
                            if (trialTaskStateIdProp != null)
                            {
                                if (trialTaskStateIdProp.intValue != trialStartIndex)
                                {
                                    trialTaskStateIdProp.intValue = trialStartIndex;
                                    serializedTransitionSettingLogic.ApplyModifiedPropertiesWithoutUndo();
                                }
                            }
                            else
                            {
                                // Debug.LogWarning($"Trial Start ID property not found for Trial - Rest state: {transition.transform.parent.name}");
                            }
                        }
                        break;
                    }
                }
                // if (!foundTarget) Debug.LogWarning($"'state_currentID' target key not found in GlobalLogic (state_triggerTransition) on {transition.transform.parent.name}");
            }
        }
    }

    private void UpdateTransitionExitTime(GameObject transition, bool hasExitTime, float exitTime)
    {
        var itemTimers = transition.GetComponents<ClusterVR.CreatorKit.Operation.Implements.ItemTimer>();
        bool found = false;
        foreach (var itemTimer in itemTimers)
        {
            SerializedObject serializedComp = new SerializedObject(itemTimer);
            var keyProp = serializedComp.FindProperty("key.key");

            if (keyProp != null && (keyProp.stringValue == "state_enter" || keyProp.stringValue == "state_enter(disabled)"))
            {
                found = true;
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
            if (stateList != null && stateList.States == null) stateList.States = new StateList.State[0]; // Ensure not null
            else return; // stateList itself is null
        }
        if (previousStates == null)
        { // Initialize previousStates if it's null but states exist
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

        if (!Directory.Exists(listenersFolder))
        {
            return;
        }

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
                if (listener.stateID >= 0 && listener.stateID < previousStates.Length) // Use previousStates length
                {
                    // Ensure previousStates[listener.stateID] itself is not null if it's an array of classes
                    if (listener.stateID < previousStates.Length && !string.IsNullOrEmpty(previousStates[listener.stateID].StateName))
                    {
                        oldStateName = previousStates[listener.stateID].StateName;
                    }
                    else if (listener.stateID != -1)
                    { // If stateID was valid but couldn't get name from previous
                        // This might happen if previousStates wasn't perfectly synced or had null entries.
                        // Try to find the state by ID in the *current* list and use its name as a fallback for "old name".
                        // This is a bit of a recovery attempt.
                        if (listener.stateID < stateList.States.Length)
                        {
                            // oldStateName = stateList.States[listener.stateID].StateName; // This is risky, might map to wrong new ID
                            // Let's rather log a warning or set to -1 if oldStateName is indeterminable from previousStates
                        }
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
                            // Debug.LogWarning($"State '{oldStateName}' for listener in '{assetFile}' no longer exists. Setting listener stateID to -1.");
                            listener.stateID = -1;
                            currentDataDirty = true;
                        }
                    }
                }
                else if (listener.stateID != -1 && listener.stateID < previousStates.Length)
                {
                    // Old state ID was valid but name was empty or state was null in previousStates.
                    // This indicates an issue with previousStates or the listener's old ID.
                    // Debug.LogWarning($"Could not determine old state name for listener (old ID: {listener.stateID}) in '{assetFile}'. State name might have been empty or state removed. Setting listener stateID to -1.");
                    listener.stateID = -1;
                    currentDataDirty = true;
                }
                else if (listener.stateID >= previousStates.Length && listener.stateID != -1)
                {
                    // Old state ID was out of bounds for previousStates, clearly an issue or a new listener for a state just added.
                    // If it's a new listener, it shouldn't have an oldStateName.
                    // If it's an old listener with an invalid ID, mark it.
                    // This case is complex; for simplicity, if it's out of bounds and not -1, assume it's problematic.
                    // Debug.LogWarning($"Listener stateID {listener.stateID} in '{assetFile}' was out of bounds for previous state list. Setting to -1.");
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
                // Ensure the directory for the .js script exists.
                string scriptDir = Path.GetDirectoryName(assetFile); // Assumes .js is beside .asset
                if (!Directory.Exists(scriptDir)) Directory.CreateDirectory(scriptDir);
                string scriptPath = Path.Combine(scriptDir, itemName + ".js");


                var sb = new StringBuilder();
                sb.AppendLine(GenerateOnStateEnterFunction(data.stateListeners));
                sb.AppendLine(GenerateDuringStateFunction(data.stateListeners));
                sb.AppendLine(GenerateOnStateExitFunction(data.stateListeners));
                sb.AppendLine(data.otherImplementation ?? string.Empty); // Ensure otherImplementation is not null

                File.WriteAllText(scriptPath, sb.ToString());
            }
        }

        if (anyDataDirtied)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void AddOrEnableQuestionnaireForm(int stateId, string stateNameInAsset)
    {
        GameObject stateObjectInScene = FindStateObject(stateNameInAsset);
        if (stateObjectInScene != null)
        {
            Transform objectsContainer = stateObjectInScene.transform.Find("Objects");
            if (objectsContainer == null)
            {
                GameObject newObjectsContainer = new GameObject("Objects");
                newObjectsContainer.transform.SetParent(stateObjectInScene.transform, false);
                objectsContainer = newObjectsContainer.transform;
            }

            GameObject existingInstance = objectsContainer.Cast<Transform>()
                .FirstOrDefault(child => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath)?.gameObject;

            // Get the qID value from the State scriptable object asset
            int qIDToSet = 0;
            if (stateId >= 0 && stateId < stateList.States.Length)
            {
                qIDToSet = stateList.States[stateId].qID > 0 ? stateList.States[stateId].qID : 0;
            }

            if (existingInstance != null)
            {
                if (!existingInstance.activeSelf)
                {
                    existingInstance.SetActive(true);
                }
                GameObject formController = existingInstance.transform.Find("FormController")?.gameObject;
                UpdateQID(formController, qIDToSet);
                // If the script needs to be re-applied or ensured on existing instances:
                if (formController != null)
                {
                    var identifiersAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(identifiersAssetPath);
                    if (identifiersAsset != null)
                    {
                        ScriptableClusterScriptCombiner combiner = formController.GetComponent<ScriptableClusterScriptCombiner>();
                        if (combiner != null)
                        {
                            combiner.ReplaceScript(identifiersAsset, 0, null, 0, true);
                            EditorUtility.SetDirty(combiner);
                        }
                    }
                }
                Debug.Log($"Questionnaire in {stateNameInAsset} ensured active with qID {qIDToSet}");
            }
            else
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(formPrefabPath);
                if (prefab != null)
                {
                    GameObject newFormInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, objectsContainer);
                    newFormInstance.name = prefab.name;
                    GameObject formController = newFormInstance.transform.Find("FormController")?.gameObject;
                    if (formController != null)
                    {
                        var identifiersAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(identifiersAssetPath);
                        if (identifiersAsset != null)
                        {
                            ScriptableClusterScriptCombiner combiner = formController.GetComponent<ScriptableClusterScriptCombiner>();
                            if (combiner != null)
                            {
                                combiner.ReplaceScript(identifiersAsset, 0, null, 0, true);
                                EditorUtility.SetDirty(combiner);
                            }
                            else
                            {
                                Debug.LogWarning($"ScriptableClusterScriptCombiner not found on FormController of {newFormInstance.name}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Identifiers asset not found at {identifiersAssetPath}");
                        }

                        UpdateQID(formController, qIDToSet);
                        CopyWorldItemReferenceListToFormController(formController);
                        Debug.Log($"Questionnaire added to {stateNameInAsset} with qID {qIDToSet}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"State GameObject '{stateNameInAsset}' not found in scene. Cannot add questionnaire.");
        }
    }

    private GameObject GetFormController(GameObject stateObjectInScene)
    {
        if (stateObjectInScene == null) return null;
        Transform objectsContainer = stateObjectInScene.transform.Find("Objects");
        if (objectsContainer == null) return null;

        foreach (Transform child in objectsContainer)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath)
                return child.Find("FormController")?.gameObject;
        }
        return null;
    }

    private int GetCurrentQID(GameObject formController)
    {
        if (formController == null) return -1;
        var itemLogic = formController.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
        if (itemLogic != null)
        {
            SerializedObject serializedComp = new SerializedObject(itemLogic);
            SerializedProperty statementsProp = serializedComp.FindProperty("logic.statements");
            if (statementsProp != null && statementsProp.isArray)
            {
                for (int i = 0; i < statementsProp.arraySize; i++)
                {
                    SerializedProperty targetKey = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "qID")
                    {
                        SerializedProperty qIdValueProp = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (qIdValueProp != null)
                            return qIdValueProp.intValue;
                    }
                }
            }
        }
        return -1;
    }

    private void UpdateQID(GameObject formController, int qID)
    {
        if (formController == null) return;
        var itemLogic = formController.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
        if (itemLogic != null)
        {
            SerializedObject serializedComp = new SerializedObject(itemLogic);
            SerializedProperty statementsProp = serializedComp.FindProperty("logic.statements");
            bool qIdUpdated = false;
            if (statementsProp != null && statementsProp.isArray)
            {
                for (int i = 0; i < statementsProp.arraySize; i++)
                {
                    SerializedProperty targetKey = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "qID")
                    {
                        SerializedProperty qIdValueProp = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (qIdValueProp != null)
                        {
                            if (qIdValueProp.intValue != qID)
                            {
                                qIdValueProp.intValue = qID;
                                qIdUpdated = true;
                            }
                            break;
                        }
                    }
                }
            }
            if (qIdUpdated)
            {
                serializedComp.ApplyModifiedProperties();
            }
        }
    }

    private void DisplayQuestionnaireRow(string stateNameInAsset, int stateIdInAsset)
    {
        GameObject stateObjectInScene = FindStateObject(stateNameInAsset);
        if (stateObjectInScene != null && HasEnabledFormInstance(stateObjectInScene))
        {
            GameObject formController = GetFormController(stateObjectInScene);

            // Always get qID from the asset, not from the formController
            int assetQID = (stateList != null && stateIdInAsset >= 0 && stateIdInAsset < stateList.States.Length)
                ? stateList.States[stateIdInAsset].qID
                : -1;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Questionnaire", GUILayout.Width(100));
            EditorGUILayout.BeginHorizontal();

            GameObject questionnaireObject = formController?.transform.parent.gameObject;

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(questionnaireObject, typeof(GameObject), true, GUILayout.Width(100));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("qID", GUILayout.Width(20));

            int newQID = EditorGUILayout.IntField(assetQID, GUILayout.Width(30));
            if (newQID != assetQID && formController != null)
            {
                // Update only this state's qID in the asset and the corresponding formController
                stateList.States[stateIdInAsset].qID = newQID;
                EditorUtility.SetDirty(stateList);
                if (serializedStateList != null)
                    serializedStateList.ApplyModifiedProperties();

                UpdateQID(formController, newQID);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                // Set qID to zero and update asset
                stateList.States[stateIdInAsset].qID = 0;
                EditorUtility.SetDirty(stateList);
                if (serializedStateList != null)
                    serializedStateList.ApplyModifiedProperties();

                // Remove the form instance GameObject
                RemoveFormInstance(stateObjectInScene, formController);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }

    private bool HasEnabledFormInstance(GameObject stateObjectInScene)
    {
        if (stateObjectInScene == null) return false;
        Transform objects = stateObjectInScene.transform.Find("Objects");
        if (objects == null) return false;
        foreach (Transform child in objects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath && child.gameObject.activeSelf)
                return true;
        }
        return false;
    }

    private void RemoveFormInstance(GameObject stateObjectInScene, GameObject formController)
    {
        if (formController != null)
        {
            GameObject formInstance = formController.transform.parent.gameObject;
            if (formInstance != null)
            {
                Undo.DestroyObjectImmediate(formInstance);
                Debug.Log($"Questionnaire in {stateObjectInScene.name} removed.");
            }
        }
        else
        {
            Transform objects = stateObjectInScene.transform.Find("Objects");
            if (objects != null)
            {
                Transform formToDestroy = null;
                foreach (Transform child in objects)
                {
                    if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath)
                    {
                        formToDestroy = child;
                        break;
                    }
                }
                if (formToDestroy != null)
                {
                    Undo.DestroyObjectImmediate(formToDestroy.gameObject);
                    Debug.Log($"Questionnaire in {stateObjectInScene.name} removed (fallback).");
                }
            }
        }
    }

    private void CopyWorldItemReferenceListToFormController(GameObject formController)
    {
        if (formController == null)
        {
            Debug.LogError("FormController not found for CopyWorldItemReferenceListToFormController.");
            return;
        }
        GameObject expTemplateInstance = FindRequiredObjectsWrapperInstance();
        if (expTemplateInstance != null)
        {
            Transform worldItemRefListTransform = expTemplateInstance.transform.Find(WorldItemRefListObjectName);
            if (worldItemRefListTransform != null)
            {
                var worldItemRefComponentSource = worldItemRefListTransform.GetComponent<WorldItemReferenceList>();
                if (worldItemRefComponentSource != null)
                {
                    var existingRefList = formController.GetComponent<WorldItemReferenceList>();
                    if (existingRefList != null)
                    {
                        DestroyImmediate(existingRefList, true);
                    }

                    if (UnityEditorInternal.ComponentUtility.CopyComponent(worldItemRefComponentSource))
                    {
                        if (!UnityEditorInternal.ComponentUtility.PasteComponentAsNew(formController))
                        {
                            Debug.LogError("Failed to paste WorldItemReferenceList component to FormController.");
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to copy WorldItemReferenceList component.");
                    }
                }
                else
                    Debug.LogError($"'{WorldItemRefListObjectName}' does not have a WorldItemReferenceList component.");
            }
            else
                Debug.LogError($"'{WorldItemRefListObjectName}' object not found under ExpTemplateRequiredObjects.");
        }
        else
            Debug.LogError("ExpTemplateRequiredObjects prefab instance not found in the scene.");
    }

    private GameObject FindRequiredObjectsWrapperInstance()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            string prefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(obj));
            if (prefabPath == RequiredObjectsWrapperPrefabPath)
                return obj;
        }
        return null;
    }

    private string GenerateStateFunction(StateListener[] listeners, string functionName, Func<StateListener, List<StateListenerAction>> actionSelector, string extraParameters = "")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"function {functionName}({extraParameters}) {{");
        sb.AppendLine("  const STATE_ID = $.state.state_id;");
        sb.AppendLine("  const CONDITION = $.groupState.currentCondition;");
        sb.AppendLine("");

        var groupedListeners = listeners
            .Where(l => l != null && l.stateID >= 0) // Added null check for listener itself
            .GroupBy(l => l.stateID);

        foreach (var group in groupedListeners)
        {
            sb.AppendLine($"  if (STATE_ID === {group.Key}) {{");
            foreach (var listenerData in group)
            {
                if (listenerData == null) continue; // Defensive check
                var actions = actionSelector(listenerData);
                if (actions != null)
                { // Ensure actions list is not null
                    foreach (var action in actions)
                    {
                        if (action != null) // Ensure action is not null
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
        // This function is called after statesProperty.InsertArrayElementAtIndex & InitializeStateDefaults
        // but *before* ApplyModifiedProperties in the button action.
        // To get the correct stateName, we should use the statesProperty.
        serializedStateList.ApplyModifiedProperties(); // Apply first to ensure stateList is up-to-date for name retrieval
        stateList = (StateList)serializedStateList.targetObject; // Refresh local stateList

        GameObject statesContainer = FindOrCreateStatesContainer();
        if (statesContainer == null || stateList == null || stateList.States == null || index < 0 || index >= stateList.States.Length)
        {
            Debug.LogError("Cannot insert state GameObject due to invalid input or missing container.");
            return;
        }

        string stateName = stateList.States[index].StateName; // Now this should be correct
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
        GameObject requiredObjectsWrapper = FindRequiredObjectsWrapperInstance();
        if (requiredObjectsWrapper == null)
        {
            // Attempt to create the wrapper if it's missing
            GameObject wrapperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RequiredObjectsWrapperPrefabPath);
            if (wrapperPrefab != null)
            {
                requiredObjectsWrapper = (GameObject)PrefabUtility.InstantiatePrefab(wrapperPrefab);
                requiredObjectsWrapper.name = wrapperPrefab.name; // Remove "(Clone)"
                Undo.RegisterCreatedObjectUndo(requiredObjectsWrapper, "Create Required Objects Wrapper");
                Debug.Log("RequiredObjectsWrapper prefab instance created as it was not found.");
            }
            else
            {
                Debug.LogError($"RequiredObjectsWrapper prefab not found at {RequiredObjectsWrapperPrefabPath}. Cannot create 'States' container.");
                return null;
            }
        }

        Transform statesObjectTransform = requiredObjectsWrapper.transform.Find("States");
        if (statesObjectTransform == null)
        {
            GameObject statesObject = new GameObject("States");
            Undo.RegisterCreatedObjectUndo(statesObject, "Create States Container");
            statesObject.transform.SetParent(requiredObjectsWrapper.transform, false);
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
        style.normal.textColor = Color.white; // Ensure text color is white for large labels too
        style.fontStyle = FontStyle.Bold; // Make it bold

        var labelRect = new Rect(rect.x + 4, rect.y, rect.width - 8, rect.height); // Added padding
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
            // Parse trialsCountForEachUniqueCondition
            var match = System.Text.RegularExpressions.Regex.Match(jsAsset.text, @"const trialsCountForEachUniqueCondition = (\d+);");
            if (match.Success)
                trialsCountForEachUniqueCondition = int.Parse(match.Groups[1].Value);

            // Parse within_subjects_variables
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
    
    private GameObject FindStateObject(string stateName)
    {
        var wrapper = FindRequiredObjectsWrapperInstance();
        if (wrapper == null) return null;
        var statesContainer = wrapper.transform.Find("States");
        if (statesContainer == null) return null;
        var stateTransform = statesContainer.Find(stateName);
        return stateTransform?.gameObject;
    }
}
