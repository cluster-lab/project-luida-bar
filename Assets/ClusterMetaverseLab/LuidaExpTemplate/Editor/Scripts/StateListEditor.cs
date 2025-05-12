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
    private string prepareStatePrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/Preparation State.prefab";
    private const string RequiredObjectsWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";
    private const string stateListTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/StateList/Template.asset";
    private const string stateManagementScriptFolderPathFormat = "Assets/_Experiment_/Scripts/StateManagement/{0}";
    private const string stateListeningItemPrefabPath = "Assets/ClusterVR.CreatorKit.Item.Implements.StateListeningItem"; //Fixed this Path
    private const string formPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/Questionnaire/Questionnaire.prefab";
    private const string identifiersAssetPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string WorldItemRefListObjectName = "WorldItemRefList";

    // Fixed states that must not be moved.
    // Now, the fixed trial states are "Trial - Start" and "Trial - Rest".
    private readonly string[] FixedStateNames = new string[] { "Preparation", "Trial - Start", "Trial - Rest", "End" };
    private Vector2 scrollPos;
    private string sceneName;

    public void OnEnable()
    {
        sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        LoadStateList();
        previousStates = new StateList.State[stateList.States.Length];
        Array.Copy(stateList.States, previousStates, stateList.States.Length);
    }

    private void LoadStateList()
    {
        sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string stateListPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";

        stateList = AssetDatabase.LoadAssetAtPath<StateList>(stateListPath);
        if (stateList == null)
        {
            string newAssetPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
            AssetDatabase.CopyAsset(stateListTemplatePath, newAssetPath);
            AssetDatabase.Refresh();
            stateList = AssetDatabase.LoadAssetAtPath<StateList>(newAssetPath);
        }
        else
        {
            serializedStateList = new SerializedObject(stateList);
            statesProperty = serializedStateList.FindProperty("States");
        }
    }

    public void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (stateList == null || sceneName != UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
        {
            LoadStateList();
        }

        if (stateList == null)
        {
            StateList template = AssetDatabase.LoadAssetAtPath<StateList>(stateListTemplatePath);
            if (template != null)
            {
                string newAssetPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
                AssetDatabase.CopyAsset(stateListTemplatePath, newAssetPath);
                AssetDatabase.Refresh();
                stateList = AssetDatabase.LoadAssetAtPath<StateList>(newAssetPath);
                if (stateList != null)
                    EditorGUILayout.HelpBox($"StateList created at {newAssetPath}.", MessageType.Info);
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

        EditorGUILayout.LabelField("Edit States", EditorStyles.boldLabel);
        serializedStateList.Update();

        // Find special state indexes
        int preparationIndex = Array.FindIndex(stateList.States, s => s.StateName == "Preparation");
        int trialTaskIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Start");
        int trialRestIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Rest");
        int endIndex = Array.FindIndex(stateList.States, s => s.StateName == "End");

        bool stateOrderChanged = false;

        // Check transitions to 'End'
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

        // Check transitions to 'Preparation'
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

        // Loop over all states
        for (int i = 0; i < statesProperty.arraySize; i++)
        {
            // --- Above 'Preparation' state ---
            if (i == preparationIndex && preparationIndex >= 0)
            {
                GUILayout.Space(10);
                if (!preparationTransitionFound)
                    EditorGUILayout.HelpBox("No state (except 'Preparation' itself) is transitioning to the 'Preparation' state!", MessageType.Warning);

                if (GUILayout.Button("Add State"))
                {
                    GUI.FocusControl(null);
                    int newStateIndex = preparationIndex - 1;
                    statesProperty.InsertArrayElementAtIndex(newStateIndex);
                    // Initialize or copy defaults for the new state...
                    serializedStateList.ApplyModifiedProperties();
                    
                    // Now insert the new GameObject incrementally:
                    InsertStateGameObjectAtIndex(newStateIndex);
                    
                    // And update the state_id for all states that come after the inserted one:
                    UpdateStateIDsFromIndex(newStateIndex + 1);
                    
                    stateOrderChanged = true;
                    break;
                }
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("These states are for trials. Their order is fixed, and the repetition is controlled by set variables.");
            }

            // --- (Optional) Horizontal separator after 'Trial - Rest' ---
            if (trialRestIndex >= 0 && i == trialRestIndex + 1)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // --- Add Button between "Trial - Start" and "Trial - Rest" ---
            // When i reaches "Trial - Rest", insert an "Add Trial State" button.
            if (trialTaskIndex != -1 && trialRestIndex != -1 && i == trialRestIndex)
            {
                GUILayout.Space(10);
                if (GUILayout.Button("Add Trial State"))
                {
                    GUI.FocusControl(null);
                    int newTrialStateIndex = i;
                    // Insert the new trial state into the serialized state list
                    statesProperty.InsertArrayElementAtIndex(newTrialStateIndex);
                    InitializeTrialStateDefaults(newTrialStateIndex);
                    serializedStateList.ApplyModifiedProperties();

                    // Incrementally insert the new trial state's GameObject at the proper position:
                    InsertTrialStateGameObjectAtIndex(newTrialStateIndex);

                    // Update the state_id values for all subsequent states
                    UpdateStateIDsFromIndex(newTrialStateIndex + 1);

                    stateOrderChanged = true;
                    break;
                }
            }

            // --- Above 'End' state ---
            if (i == endIndex && endIndex >= 0)
            {
                GUILayout.Space(10);
                if (GUILayout.Button("Add State"))
                {
                    GUI.FocusControl(null);
                    int newStateIndex = endIndex - 1;
                    statesProperty.InsertArrayElementAtIndex(newStateIndex);
                    // Initialize or copy defaults for the new state...
                    serializedStateList.ApplyModifiedProperties();
                    
                    // Now insert the new GameObject incrementally:
                    InsertStateGameObjectAtIndex(newStateIndex);
                    
                    // And update the state_id for all states that come after the inserted one:
                    UpdateStateIDsFromIndex(newStateIndex + 1);
                    
                    stateOrderChanged = true;
                    break;
                }

                if (!endTransitionFound)
                    EditorGUILayout.HelpBox("No state (except 'End' itself) is transitioning to the 'End' state!", MessageType.Warning);

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("The 'End' state should always be the last state.");
            }

            // --- Display state fields ---
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
                stateName.stringValue = "State";
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            string[] allStateNames = Array.ConvertAll(stateList.States, s => s.StateName);
            bool isEndState = (endIndex >= 0 && i == endIndex);

            // --- Determine allowed moves ---
            bool canMoveUp = true;
            bool canMoveDown = true;
            // Constraint for states before 'Preparation'
            if (preparationIndex >= 0)
            {
                if (i < preparationIndex)
                {
                    if (i + 1 >= preparationIndex)
                        canMoveDown = false;
                }
            }
            // Constraint for the End state remains.
            if (endIndex >= 0)
            {
                if (i + 1 >= endIndex)
                    canMoveDown = false;
                if (i == endIndex)
                {
                    canMoveUp = false;
                    canMoveDown = false;
                }
            }
            // For extra trial states (those not fixed and whose names start with "Trial - "),
            // do not allow moving them beyond the fixed "Trial - Start" (up) or "Trial - Rest" (down)
            if (!isFixedState && stateName.stringValue.StartsWith("Trial - "))
            {
                if (trialTaskIndex != -1 && i - 1 == trialTaskIndex)
                    canMoveUp = false;
                if (trialRestIndex != -1 && i + 1 == trialRestIndex)
                    canMoveDown = false;
            }

            // --- Filter allowed destination states ---
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
                destStateIndex = Array.IndexOf(allowedDestinations, destStateName.stringValue);

            EditorGUI.BeginDisabledGroup(isEndState);
            #region Transition Destination State
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Transit destination state");
            EditorGUI.BeginDisabledGroup(isFixedState);
            destStateIndex = EditorGUILayout.Popup(destStateIndex, allowedDestinations, GUILayout.Width(150));
            EditorGUI.EndDisabledGroup();
            if (destStateIndex >= 0 && allowedDestinations.Length > 0)
                destStateName.stringValue = allowedDestinations[destStateIndex];
            else if (allowedDestinations.Length == 0)
                destStateName.stringValue = "";
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
                serializedStateList.ApplyModifiedProperties();

                // Check if the state being removed is immediately before a fixed state.
                // (For example, if it's right before "Preparation" or, in the trial states, right before "Trial - Rest".)
                bool removeGameObjectIncrementally = false;
                if (preparationIndex >= 0 && i == preparationIndex - 1)
                {
                    removeGameObjectIncrementally = true;
                }
                if (trialRestIndex >= 0 && i == trialRestIndex - 1)
                {
                    removeGameObjectIncrementally = true;
                }
                
                // If we are in one of those special cases, remove the corresponding GameObject from the scene.
                if (removeGameObjectIncrementally)
                {
                    GameObject statesContainer = FindOrCreateStatesContainer();
                    Transform childToRemove = FindChildByStateID(statesContainer.transform, i);
                    if (childToRemove != null)
                    {
                        DestroyImmediate(childToRemove.gameObject);
                    }
                }
                
                // Update the state_id values for all GameObjects after the removed one.
                UpdateStateIDsFromIndex(i);
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
                    repeatDestStateName.stringValue = repeatAllowed[repeatStateIndex];
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Repeat Count", GUILayout.Width(80));
                repeatCount.intValue = EditorGUILayout.IntField(Math.Max(repeatCount.intValue, 1), GUILayout.Width(60));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            #endregion

            #region Questionnaire Form
            EditorGUILayout.BeginVertical();
            string stateNameStringValue = stateName.stringValue;
            // Check if a questionnaire already exists for this state
            if (HasEnabledFormInstance(GameObject.Find(stateNameStringValue)))
            {
                // Display existing questionnaire row
                DisplayQuestionnaireRow(stateNameStringValue, i);
            }
            else
            {
                // Display "Add Questionnaire" button
                EditorGUILayout.LabelField("Questionnaire", GUILayout.Width(100));
                if (GUILayout.Button("Add Questionnaire", GUILayout.Width(150)))
                    AddOrEnableQuestionnaireForm(i, stateNameStringValue);
            }
            EditorGUILayout.EndVertical();
            #endregion

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            EditorGUI.EndDisabledGroup(); // End disabling for End state

            if (GUI.changed)
            {
                if (!EditorGUIUtility.editingTextField)
                    GUI.FocusControl(null);
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

        // --- NEW: Update "Trial - Start" transition destination ---
        // Every time trial states are edited, set the "Trial - Start" state's destination to the state immediately following it.
        int updatedTrialTaskIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Start");
        if (updatedTrialTaskIndex != -1 && updatedTrialTaskIndex + 1 < stateList.States.Length)
            stateList.States[updatedTrialTaskIndex].DestStateName = stateList.States[updatedTrialTaskIndex + 1].StateName;

        UpdateSceneObjects();
        if (stateOrderChanged)
            UpdateStateListeningItemsAfterReorder();

        EditorGUILayout.EndScrollView();
    }

    private void CopyStateFields(int sourceIndex, int targetIndex)
    {
        SerializedProperty source = statesProperty.GetArrayElementAtIndex(sourceIndex);
        SerializedProperty target = statesProperty.GetArrayElementAtIndex(targetIndex);
        target.FindPropertyRelative("StateName").stringValue = "State " + targetIndex;
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
        target.FindPropertyRelative("StateName").stringValue = "State " + index;
        target.FindPropertyRelative("DestStateName").stringValue = "";
        target.FindPropertyRelative("HasExitTime").boolValue = false;
        target.FindPropertyRelative("ExitTime").floatValue = 0f;
        target.FindPropertyRelative("IsRepeated").boolValue = false;
        target.FindPropertyRelative("RepeatDestStateName").stringValue = "";
        target.FindPropertyRelative("RepeatCount").intValue = 1;
    }

    // For extra trial states, initialize the default name as "Trial - "
    private void InitializeTrialStateDefaults(int index)
    {
        SerializedProperty target = statesProperty.GetArrayElementAtIndex(index);
        target.FindPropertyRelative("StateName").stringValue = "Trial - " + index;
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

        // Instead of using transform.Find(name) (which can return duplicates), always find the child by its state ID.
        for (int i = 0; i < stateList.States.Length; i++)
        {
            string stateName = stateList.States[i].StateName;
            if (string.IsNullOrEmpty(stateName))
                stateName = "State";
            Transform stateTransform = FindChildByStateID(statesObject.transform, i);
            if (stateTransform == null)
            {
                // Not found by stateID, so instantiate a new child.
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    stateName == "Preparation"
                      ? prepareStatePrefabPath
                      : ( stateName == "Trial - Rest"
                        ? trialRestStatePrefabPath
                        : statePrefabPath
                    ));
                if (prefab != null)
                {
                    GameObject newChild = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    newChild.name = stateName;
                    newChild.transform.SetParent(statesObject.transform);
                    stateTransform = newChild.transform;
                }
            }
            else
            {
                // Always update the name to the current state name.
                stateTransform.name = stateName;
            }
            if (stateTransform.GetSiblingIndex() != i)
                stateTransform.SetSiblingIndex(i);

            GameObject transition = stateTransform.Find("Transition")?.gameObject;
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
                                    return child;
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
                Debug.LogWarning("Property not found: logic.statements");
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

                        if (isTrialRestState)
                        {
                            SerializedProperty trialTaskStateIdProp = specificProperty.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.operatorExpression.operands.Array.data[2].value.constant.integerValue");
                            if (trialTaskStateIdProp != null)
                            {
                                trialTaskStateIdProp.intValue = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Start");
                                serializedTransitionSettingLogic.ApplyModifiedProperties();
                            }
                        }
                    }
                }
            }
            else
                Debug.LogWarning("Property not found: logic.statements");
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
                            Debug.LogWarning("Property not found: logic.statements.singleStatement.expression.value.constant.integerValue");
                    }
                }
            }
            else
                Debug.LogWarning("Property not found: logic.statements");
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
                            Debug.LogWarning("Property not found: logic.statements.singleStatement.expression.value.constant.integerValue");
                    }
                }
            }
            else
                Debug.LogWarning("Property not found: logic.statements");
        }
    }

    private void UpdateStateListeningItemsAfterReorder()
    {
        if (stateList == null || previousStates == null) return;

        // 1) Build map: old index → new index (by StateName)
        var stateIdMap = new Dictionary<int, int>();
        for (int i = 0; i < previousStates.Length; i++)
        {
            string name = previousStates[i].StateName;
            int newIndex = Array.FindIndex(stateList.States, s => s.StateName == name);
            stateIdMap.Add(i, newIndex);
        }

        // 2) Locate all StateListeningItemData assets in your scene's StateListeners folder
        sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string listenersFolder = string.Format(stateManagementScriptFolderPathFormat, sceneName) + "/StateListeners";
        if (Directory.Exists(listenersFolder))
        {
            foreach (var assetFile in Directory.GetFiles(listenersFolder, "*.asset"))
            {
                var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetFile);
                if (data == null) continue;

                bool dirty = false;
                var kept = new List<StateListener>();

                // 3) Remap each listener's stateID
                foreach (var listener in data.stateListeners)
                {
                    if (stateIdMap.TryGetValue(listener.stateID, out var mapped))
                    {
                        listener.stateID = mapped;
                        dirty = true;
                        // only keep valid mappings
                        if (mapped >= 0) kept.Add(listener);
                    }
                    else
                    {
                        // if the old ID wasn't found, just keep it untouched
                        kept.Add(listener);
                    }
                }

                // 4) Write back and mark dirty if anything changed
                data.stateListeners = kept.ToArray();
                if (dirty) EditorUtility.SetDirty(data);

                // 5) Regenerate the `.js` for this item
                string itemName = Path.GetFileNameWithoutExtension(assetFile);
                string scriptPath = 
                    $"Assets/_Experiment_/Scripts/StateManagement/{sceneName}/{itemName}.js";

                var sb = new StringBuilder();
                sb.AppendLine(GenerateOnStateEnterFunction(data.stateListeners));
                sb.AppendLine(GenerateDuringStateFunction(data.stateListeners));
                sb.AppendLine(GenerateOnStateExitFunction(data.stateListeners));
                sb.AppendLine(data.otherImplementation);

                File.WriteAllText(scriptPath, sb.ToString());
            }
        }

        // 6) Persist all changes
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 7) Refresh our "previousStates" snapshot so next re-order diff will work
        Array.Copy(stateList.States, previousStates, stateList.States.Length);
    }

    // Adds or enables a Questionnaire instance and sets its qID.
    // qID is updated when the user changes the text field.
    private void AddOrEnableQuestionnaireForm(int stateId, string stateName)
    {
        GameObject stateObject = GameObject.Find(stateName)?.gameObject;
        if (stateObject != null)
        {
            GameObject objects = stateObject.transform.Find("Objects")?.gameObject;
            if (!objects)
            {
                objects = new GameObject("Objects");
                objects.transform.SetParent(stateObject.transform, false);
            }
            GameObject existingInstance = objects.transform.Cast<Transform>()
                .FirstOrDefault(child => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath)?.gameObject;
            if (existingInstance != null && !existingInstance.activeSelf)
            {
                existingInstance.SetActive(true);
                GameObject formController = existingInstance.transform.Find("FormController")?.gameObject;
                UpdateQID(formController, -1);
                CopyWorldItemReferenceListToFormController(formController);
                Debug.Log($"Questionnaire in {stateName} re-enabled with qID {stateId}");
            }
            else if (!existingInstance)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(formPrefabPath);
                if (prefab != null)
                {
                    GameObject newFormInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    newFormInstance.transform.SetParent(objects.transform);
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
                        UpdateQID(formController, -1);
                        CopyWorldItemReferenceListToFormController(formController);
                        Debug.Log($"Questionnaire added to {stateName} with qID {stateId}");
                    }
                }
            }
        }
    }

    // Get the FormController game object inside the Questionnaire prefab.
    private GameObject GetFormController(GameObject stateObject)
    {
        foreach (Transform child in stateObject.transform)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath)
                return child.Find("FormController")?.gameObject;
        }
        return null;
    }

    // Get the current qID value from the FormController.
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
                            return qIdProp.intValue;
                    }
                }
            }
        }
        return -1;
    }

    // Update the qID value in the FormController.
    // This method is now invoked only when the user directly inputs a new value.
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

    // Displays a row for the existing questionnaire object if present.
    private void DisplayQuestionnaireRow(string stateName, int stateId)
    {
        GameObject stateObject = GameObject.Find(stateName);
        if (stateObject != null && HasEnabledFormInstance(stateObject))
        {
            GameObject formController = GetFormController(stateObject.transform.Find("Objects").gameObject);
            int currentQID = GetCurrentQID(formController);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Questionnaire", GUILayout.Width(100));
            EditorGUILayout.BeginHorizontal();
            GameObject questionnaireObject = formController.transform.parent.gameObject;
            EditorGUILayout.ObjectField(questionnaireObject, typeof(GameObject), true, GUILayout.Width(60));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("qID", GUILayout.Width(20));
            int newQID = EditorGUILayout.IntField(currentQID, GUILayout.Width(30));
            if (newQID != currentQID)
                UpdateQID(formController, newQID);
            GUILayout.Space(10);
            if (GUILayout.Button("x", GUILayout.Width(20)))
                RemoveFormInstance(stateObject, formController);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }

    // Check if a valid and enabled Questionnaire instance exists in the state's Objects.
    private bool HasEnabledFormInstance(GameObject stateObject)
    {
        if (!stateObject) return false;
        Transform objects = stateObject.transform.Find("Objects");
        if (!objects) return false;
        foreach (Transform child in objects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath && child.gameObject.activeSelf)
                return true;
        }
        return false;
    }

    // Remove the Questionnaire instance from the scene by disabling it.
    private void RemoveFormInstance(GameObject stateObject, GameObject formController)
    {
        if (formController != null)
        {
            GameObject formInstance = formController.transform.parent.gameObject;
            DestroyImmediate(formInstance);
            Debug.Log($"Questionnaire in {stateObject.name} removed.");
        }
    }

    private void CopyWorldItemReferenceListToFormController(GameObject formController)
    {
        if (formController == null)
        {
            Debug.LogError("FormController not found.");
            return;
        }
        GameObject expTemplateInstance = FindRequiredObjectsWrapperInstance();
        if (expTemplateInstance != null)
        {
            GameObject worldItemRefList = expTemplateInstance.transform.Find(WorldItemRefListObjectName).gameObject;
            if (worldItemRefList != null)
            {
                var worldItemRefComponent = worldItemRefList.GetComponent<ClusterVR.CreatorKit.Item.Implements.WorldItemReferenceList>();
                if (worldItemRefComponent != null)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(worldItemRefComponent);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(formController);
                }
                else
                    Debug.LogError("WorldItemReferenceList component not found.");
            }
            else
                Debug.LogError("WorldItemRefList object not found in the scene.");
        }
        else
            Debug.LogError("ExpTemplateRequiredObjects prefab not found.");
    }

    private GameObject FindRequiredObjectsWrapperInstance()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == RequiredObjectsWrapperPrefabPath)
                return obj;
        }
        return null;
    }
    
    private void UpdateQuestionnaireFormsAfterReorder(Dictionary<int, int> stateIdMap)
    {
        // qID is only updated via direct text field input.
        GameObject statesObject = GameObject.Find("States");
        if (statesObject != null)
        {
            foreach (Transform stateTransform in statesObject.transform)
            {
                GameObject stateObject = stateTransform.gameObject;
                if (HasEnabledFormInstance(stateObject))
                {
                    // Do nothing—qID is only updated via the text field input.
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Fix: Reallocate previousStates if necessary.
        if (previousStates.Length != stateList.States.Length)
        {
            previousStates = new StateList.State[stateList.States.Length];
        }
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
                    stateListeningItems.Add(obj);
            }
        }
        return stateListeningItems;
    }
    
    private string GenerateStateFunction(StateListener[] listeners, string functionName, Func<StateListener, List<StateListenerAction>> actionSelector, string extraParameters = "")
    {
        var content = $"function {functionName}({extraParameters}) {{\n";
        content += "  const STATE_ID = $.state.state_id;\n";
        content += "  const CONDITION = $.groupState.currentCondition;\n\n";
        foreach (var listenerData in listeners)
        {
            var actions = actionSelector(listenerData);
            if (actions.Count > 0)
            {
                content += $"  if (STATE_ID === {listenerData.stateID}) {{\n";
                foreach (var action in actions)
                    content += $"    {action.GetActionContent()}\n";
                content += "  }\n";
            }
        }
        content += "}\n\n";
        return content;
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
        // Find or create the "States" parent object
        GameObject statesContainer = FindOrCreateStatesContainer();
        // Determine the correct prefab for the new state
        string stateName = stateList.States[index].StateName;
        string prefabPath = (stateName == "Preparation")
                                ? prepareStatePrefabPath
                                : (stateName == "Trial - Rest")
                                    ? trialRestStatePrefabPath
                                    : statePrefabPath;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("Prefab not found at " + prefabPath);
            return;
        }
        // Instantiate the prefab
        GameObject newState = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        newState.name = stateName;
        newState.transform.SetParent(statesContainer.transform, false);
        // Immediately insert at the correct index:
        newState.transform.SetSiblingIndex(index);
        
        // Update its transition state_id immediately:
        GameObject transition = newState.transform.Find("Transition")?.gameObject;
        if (transition != null)
            UpdateTransitionCurrentStateId(transition, index);
    }

    private void UpdateStateIDsFromIndex(int startIndex)
    {
        // Assume the "States" container is already available
        GameObject statesContainer = FindOrCreateStatesContainer();
        for (int i = startIndex; i < statesContainer.transform.childCount; i++)
        {
            Transform child = statesContainer.transform.GetChild(i);
            GameObject transition = child.Find("Transition")?.gameObject;
            if (transition != null)
            {
                UpdateTransitionCurrentStateId(transition, i);
            }
        }
    }

    // Helper that finds (or creates) the "States" GameObject under the required wrapper
    private GameObject FindOrCreateStatesContainer()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        GameObject statesObject = null;
        GameObject requiredObjectsWrapper = null;
        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == RequiredObjectsWrapperPrefabPath)
            {
                requiredObjectsWrapper = obj;
                statesObject = obj.transform.Find("States")?.gameObject;
                break;
            }
        }
        if (statesObject == null && requiredObjectsWrapper != null)
        {
            statesObject = new GameObject("States");
            statesObject.transform.SetParent(requiredObjectsWrapper.transform, false);
        }
        return statesObject;
    }

    private void InsertTrialStateGameObjectAtIndex(int index)
    {
        // Find or create the "States" container
        GameObject statesContainer = FindOrCreateStatesContainer();
        // Get the state name (for a trial state, InitializeTrialStateDefaults will have set it to something like "Trial - X")
        string stateName = stateList.States[index].StateName;
        // For trial states, use the default state prefab (unless you have a separate one for trials)
        string prefabPath = statePrefabPath;  // Change this if you have a different prefab for trial states.
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("Trial State prefab not found at " + prefabPath);
            return;
        }
        // Instantiate the prefab and set its name
        GameObject newTrialState = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        newTrialState.name = stateName;
        // Set its parent immediately
        newTrialState.transform.SetParent(statesContainer.transform, false);
        // Insert at the desired index
        newTrialState.transform.SetSiblingIndex(index);
        
        // Update the state_id on its Transition component
        GameObject transition = newTrialState.transform.Find("Transition")?.gameObject;
        if (transition != null)
            UpdateTransitionCurrentStateId(transition, index);
    }
}