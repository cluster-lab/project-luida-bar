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
            AssetDatabase.CopyAsset(stateListTemplatePath, newAssetPath);
            AssetDatabase.Refresh();
            stateList = AssetDatabase.LoadAssetAtPath<StateList>(newAssetPath);
        }
        
        // Ensure serializedObject and property are initialized if stateList is valid
        if (stateList != null)
        {
            serializedStateList = new SerializedObject(stateList);
            statesProperty = serializedStateList.FindProperty("States");
            if (stateList.States == null) // Handle case where States array might be null initially
            {
                stateList.States = new StateList.State[0];
                EditorUtility.SetDirty(stateList);
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
                AssetDatabase.CopyAsset(stateListTemplatePath, newAssetPath);
                AssetDatabase.Refresh();
                stateList = AssetDatabase.LoadAssetAtPath<StateList>(newAssetPath);
                if (stateList != null)
                {
                    EditorGUILayout.HelpBox($"StateList created at {newAssetPath}.", MessageType.Info);
                    // Initialize serialized object and property after creation
                    serializedStateList = new SerializedObject(stateList);
                    statesProperty = serializedStateList.FindProperty("States");
                    if (stateList.States == null) stateList.States = new StateList.State[0];
                    previousStates = new StateList.State[stateList.States.Length]; // Initialize previousStates
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

        EditorGUILayout.LabelField("Edit States", EditorStyles.largeLabel);
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
            if (i == 0)
            {
                EditorGUILayout.LabelField("States Before Trials", EditorStyles.largeLabel);
            }
            
            // --- Add State Button (before 'Preparation' state) ---
            if (i == preparationIndex && preparationIndex >= 0)
            {
                if (!preparationTransitionFound)
                    EditorGUILayout.HelpBox("No state (except 'Preparation' itself) is transitioning to the 'Preparation' state!", MessageType.Warning);

                if (GUILayout.Button("Add State Before Trials", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.3f - 10f)))
                {
                    GUI.FocusControl(null);
                    int newStateIndex = preparationIndex;
                    statesProperty.InsertArrayElementAtIndex(newStateIndex);
                    InitializeStateDefaults(newStateIndex);
                    serializedStateList.ApplyModifiedProperties();
                    InsertStateGameObjectAtIndex(newStateIndex);
                    UpdateStateIDsFromIndex(newStateIndex + 1);
                    stateOrderChanged = true;
                    break;
                }
                GUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                DrawDarkLabel("Trial-related States", true);
                DrawDarkLabel("These states are for trials. Their order is fixed, and the repetition is controlled by your configured variables.");
            }

            // --- (Optional) Horizontal separator after 'Trial - Rest' ---
            if (trialRestIndex >= 0 && i == trialRestIndex + 1)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUILayout.Space(20);
                EditorGUILayout.LabelField("States After Trials", EditorStyles.largeLabel);
            }

            // --- Add Button between 'Trial - Start' and 'Trial - Rest' ---
            if (trialTaskIndex != -1 && trialRestIndex != -1 && i == trialRestIndex)
            {
                if (GUILayout.Button("Add State During Trials", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.3f - 10f)))
                {
                    GUI.FocusControl(null);
                    int newTrialStateIndex = trialRestIndex;
                    statesProperty.InsertArrayElementAtIndex(newTrialStateIndex);
                    InitializeTrialStateDefaults(newTrialStateIndex);
                    serializedStateList.ApplyModifiedProperties();
                    InsertTrialStateGameObjectAtIndex(newTrialStateIndex);
                    UpdateStateIDsFromIndex(newTrialStateIndex + 1);
                    stateOrderChanged = true;
                    break;
                }
            }

            // --- Add State Button (before 'End' state) ---
            if (i == endIndex && endIndex >= 0)
            {
                if (GUILayout.Button("Add State After Trials", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.3f - 10f)))
                {
                    GUI.FocusControl(null);
                    int newStateIndex = endIndex;
                    statesProperty.InsertArrayElementAtIndex(newStateIndex);
                    InitializeStateDefaults(newStateIndex);
                    serializedStateList.ApplyModifiedProperties();
                    InsertStateGameObjectAtIndex(newStateIndex);
                    UpdateStateIDsFromIndex(newStateIndex + 1);
                    stateOrderChanged = true;
                    break;
                }

                if (!endTransitionFound)
                    EditorGUILayout.HelpBox("No state (except 'End' itself) is transitioning to the 'End' state!", MessageType.Warning);

                GUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                DrawDarkLabel("The 'End' state should always be the last state.");
            }

            // Determine highlight region for darker background
            bool isHighlight = (preparationIndex >= 0 && i >= preparationIndex && i <= trialRestIndex) || i == endIndex;
            
            Color originalBackgroundColor = GUI.backgroundColor;
            Color originalContentColor = GUI.contentColor;

            if (isHighlight)
            {
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Slightly lighter dark
                GUI.contentColor = Color.white; // Ensure text and checkmarks are visible
            }

            // Wrap state row in a box
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();

            // --- State ID ---
            EditorGUILayout.BeginVertical(GUILayout.Width(60));
            EditorGUILayout.LabelField("State ID", GUILayout.Width(60));
            EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(60));
            EditorGUILayout.EndVertical();

            // --- State Name and Dest ---
            SerializedProperty state = statesProperty.GetArrayElementAtIndex(i);
            SerializedProperty stateName = state.FindPropertyRelative("StateName");
            SerializedProperty destStateName = state.FindPropertyRelative("DestStateName");
            bool isFixedState = Array.IndexOf(FixedStateNames, stateName.stringValue) > -1;

            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            EditorGUILayout.LabelField("State name:");
            EditorGUI.BeginDisabledGroup(isFixedState);
            EditorGUILayout.PropertyField(stateName, GUIContent.none, GUILayout.Width(150));
            if (string.IsNullOrEmpty(stateName.stringValue))
                stateName.stringValue = "State" + i;
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            // --- Transition Destination ---
            string[] allStateNames = Array.ConvertAll(stateList.States, s => s.StateName);
            string[] allowedDestinations = allStateNames;
            if (preparationIndex >= 0 && i < preparationIndex)
            {
                allowedDestinations = allStateNames.Take(preparationIndex + 1).ToArray();
            }
            else if (trialRestIndex >= 0 && endIndex != -1 && i > trialRestIndex && i < endIndex)
            {
                allowedDestinations = allStateNames.Skip(trialRestIndex + 1).Take(endIndex - trialRestIndex).ToArray();
            }
            int destIndex = Array.IndexOf(allowedDestinations, destStateName.stringValue);
            bool isEndState = i == endIndex;

            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            EditorGUILayout.LabelField("Transit destination state");
            EditorGUI.BeginDisabledGroup(isEndState || (isFixedState && (stateName.stringValue == "Trial - Start" || stateName.stringValue == "Trial - Rest")));
            destIndex = EditorGUILayout.Popup(destIndex, allowedDestinations, GUILayout.Width(150));
            EditorGUI.EndDisabledGroup();
            if (destIndex >= 0)
                destStateName.stringValue = allowedDestinations[destIndex];
            else
                destStateName.stringValue = string.Empty;
            EditorGUILayout.EndVertical();

            // --- Move / Remove buttons ---
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Move state to:");
            EditorGUILayout.BeginHorizontal();
            bool canMoveUp = i > 0 && !isFixedState;
            bool canMoveDown = i < statesProperty.arraySize - 1 && !isFixedState;
            EditorGUI.BeginDisabledGroup(!canMoveUp);
            if (GUILayout.Button("Up", GUILayout.Width(50))) { statesProperty.MoveArrayElement(i, i - 1); stateOrderChanged = true; }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(!canMoveDown);
            if (GUILayout.Button("Down", GUILayout.Width(50))) { statesProperty.MoveArrayElement(i, i + 1); stateOrderChanged = true; }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(isFixedState || isEndState);
            if (GUILayout.Button("Remove", GUILayout.Width(60))) { statesProperty.DeleteArrayElementAtIndex(i); stateOrderChanged = true; break; }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // --- Time and Repeat Settings Column ---
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(180), GUILayout.MaxWidth(250)); // Flexible width column for these settings

            // --- Exit Time ---
            SerializedProperty hasExitTime = state.FindPropertyRelative("HasExitTime");
            SerializedProperty exitTime = state.FindPropertyRelative("ExitTime");
            
            hasExitTime.boolValue = EditorGUILayout.ToggleLeft("Has Exit Time", hasExitTime.boolValue);
            if (hasExitTime.boolValue)
            {
                EditorGUI.indentLevel++;
                exitTime.floatValue = EditorGUILayout.FloatField("Exit Time", Mathf.Max(0, exitTime.floatValue));
                EditorGUI.indentLevel--;
            }
            //GUILayout.Space(2); // Optional small vertical space if needed

            // --- Repeating ---
            SerializedProperty isRepeated = state.FindPropertyRelative("IsRepeated");
            SerializedProperty repeatDestName = state.FindPropertyRelative("RepeatDestStateName");
            SerializedProperty repeatCount = state.FindPropertyRelative("RepeatCount");

            EditorGUI.BeginDisabledGroup(isFixedState || isEndState);
            isRepeated.boolValue = EditorGUILayout.ToggleLeft("Is Repeated", isRepeated.boolValue);
            EditorGUI.EndDisabledGroup();

            if (isRepeated.boolValue)
            {
                EditorGUI.indentLevel++;
                int repIndex = Array.IndexOf(allStateNames, repeatDestName.stringValue);
                repIndex = EditorGUILayout.Popup("Repeat Destination", repIndex, allStateNames);
                if (repIndex >= 0) {
                    repeatDestName.stringValue = allStateNames[repIndex];
                } else {
                    repeatDestName.stringValue = string.Empty; // Clear if selection is invalid
                }

                repeatCount.intValue = EditorGUILayout.IntField("Repeat Count", Math.Max(1, repeatCount.intValue));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical(); // End of Time and Repeat settings column


            // --- Questionnaire ---
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(180)); // Give it some minimum width
            string stateNameVal = stateName.stringValue;
            GameObject sceneObj = GameObject.Find(stateNameVal); // This might be slow in a loop, consider optimizing if performance hit
            if (HasEnabledFormInstance(sceneObj))
                DisplayQuestionnaireRow(stateNameVal, i);
            else
            {
                EditorGUILayout.LabelField("Questionnaire", GUILayout.Width(100));
                if (GUILayout.Button("Add Questionnaire", GUILayout.Width(150)))
                    AddOrEnableQuestionnaireForm(i, stateNameVal);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.EndVertical(); // End of box for state row
            
            // Restore original GUI colors
            GUI.backgroundColor = originalBackgroundColor;
            GUI.contentColor = originalContentColor;


            if (GUI.changed && !EditorGUIUtility.editingTextField)
                GUI.FocusControl(null);
        }

        serializedStateList.ApplyModifiedProperties();

        // Auto-manage "Trial - Start" transition destination
        int finalTrialTaskIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Start");
        if (finalTrialTaskIndex != -1 && finalTrialTaskIndex + 1 < stateList.States.Length)
        {
            if (stateList.States[finalTrialTaskIndex].DestStateName != stateList.States[finalTrialTaskIndex + 1].StateName)
            {
                stateList.States[finalTrialTaskIndex].DestStateName = stateList.States[finalTrialTaskIndex + 1].StateName;
                EditorUtility.SetDirty(stateList);
            }
        }

        // Auto-manage "Trial - Rest" transition destination
        int finalTrialRestIndex = Array.FindIndex(stateList.States, s => s.StateName == "Trial - Rest");
        if (finalTrialRestIndex != -1 && finalTrialRestIndex + 1 < stateList.States.Length)
        {
            if (stateList.States[finalTrialRestIndex].DestStateName != stateList.States[finalTrialRestIndex + 1].StateName)
            {
                stateList.States[finalTrialRestIndex].DestStateName = stateList.States[finalTrialRestIndex + 1].StateName;
                EditorUtility.SetDirty(stateList);
            }
        }

        // Check for content changes and update scene/game objects
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
        else if (previousStates != null && stateList.States.Length != previousStates.Length)
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
        }

        EditorGUILayout.EndScrollView();
    }

    // REMOVED CopyStateFields as it's not used with the new Initialize...Defaults approach

    private void InitializeStateDefaults(int index)
    {
        SerializedProperty target = statesProperty.GetArrayElementAtIndex(index);
        target.FindPropertyRelative("StateName").stringValue = "NewState" + index; // MODIFIED
        target.FindPropertyRelative("DestStateName").stringValue = ""; // Default to no destination initially
        // If not the last state, maybe point to next? Or leave for user. For now, empty.
        // If (index + 1 < statesProperty.arraySize) {
        //    SerializedProperty nextState = statesProperty.GetArrayElementAtIndex(index + 1);
        //    target.FindPropertyRelative("DestStateName").stringValue = nextState.FindPropertyRelative("StateName").stringValue;
        // }
        target.FindPropertyRelative("HasExitTime").boolValue = false;
        target.FindPropertyRelative("ExitTime").floatValue = 0f;
        target.FindPropertyRelative("IsRepeated").boolValue = false;
        target.FindPropertyRelative("RepeatDestStateName").stringValue = "";
        target.FindPropertyRelative("RepeatCount").intValue = 1;
    }

    private void InitializeTrialStateDefaults(int index)
    {
        SerializedProperty target = statesProperty.GetArrayElementAtIndex(index);
        target.FindPropertyRelative("StateName").stringValue = "NewTrialState" + index; // MODIFIED
        target.FindPropertyRelative("DestStateName").stringValue = ""; // Trial destinations often auto-managed or specific
        // For a new trial state inserted before "Trial - Rest", it should probably point to the *next* state in sequence.
        // This will be handled by the auto-management logic for "Trial - Start" and "Trial - Rest" if it's one of them.
        // If it's an intermediate trial state, it should point to the next trial state or "Trial - Rest".
        if (index + 1 < statesProperty.arraySize)
        {
            SerializedProperty nextState = statesProperty.GetArrayElementAtIndex(index + 1);
            // Only set if next state is not "End" or another critical fixed state it shouldn't jump to by default
            string nextStateNameVal = nextState.FindPropertyRelative("StateName").stringValue;
            if (nextStateNameVal != "End") // Basic guard
            {
                 target.FindPropertyRelative("DestStateName").stringValue = nextStateNameVal;
            }
        }

        target.FindPropertyRelative("HasExitTime").boolValue = false;
        target.FindPropertyRelative("ExitTime").floatValue = 0f;
        target.FindPropertyRelative("IsRepeated").boolValue = false;
        target.FindPropertyRelative("RepeatDestStateName").stringValue = "";
        target.FindPropertyRelative("RepeatCount").intValue = 1;
    }

    private void UpdateSceneObjects()
    {
        if (stateList == null || stateList.States == null) return;

        GameObject statesObjectContainer = FindOrCreateStatesContainer();
        if (statesObjectContainer == null)
        {
            Debug.LogError("Could not find or create 'States' container object.");
            return;
        }

        // Synchronize GameObject children with stateList.States
        // First, rename/reorder/update existing, and add missing
        for (int i = 0; i < stateList.States.Length; i++)
        {
            StateList.State currentStateData = stateList.States[i];
            string expectedName = currentStateData.StateName;
            if (string.IsNullOrEmpty(expectedName)) expectedName = "State" + i; // Fallback name

            Transform stateTransform = null;
            // Try to find child by current state_id logic first
            stateTransform = FindChildByStateID(statesObjectContainer.transform, i);
            
            if (stateTransform == null) // If not found by ID (e.g. new state or ID mismatch)
            {
                 // Try to find by name, in case it exists but ID is wrong
                 Transform existingByName = statesObjectContainer.transform.Find(expectedName);
                 if (existingByName != null) {
                     // Check if this existingByName object is already matched to another state_id
                     bool isClaimed = false;
                     for(int j=0; j < i; j++) {
                         if (FindChildByStateID(statesObjectContainer.transform, j) == existingByName) {
                             isClaimed = true;
                             break;
                         }
                     }
                     if (!isClaimed) stateTransform = existingByName;
                 }


                if (stateTransform == null) // Still not found, instantiate new
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        currentStateData.StateName == "Preparation" ? prepareStatePrefabPath :
                        (currentStateData.StateName == "Trial - Rest" ? trialRestStatePrefabPath : statePrefabPath)
                    );
                    if (prefab != null)
                    {
                        GameObject newChild = (GameObject)PrefabUtility.InstantiatePrefab(prefab, statesObjectContainer.transform);
                        newChild.name = expectedName;
                        stateTransform = newChild.transform;
                    }
                    else
                    {
                        Debug.LogError($"Prefab not found for state: {expectedName}");
                        continue;
                    }
                }
            }

            // Ensure correct name and sibling index
            if (stateTransform.name != expectedName)
            {
                stateTransform.name = expectedName;
            }
            if (stateTransform.GetSiblingIndex() != i)
            {
                stateTransform.SetSiblingIndex(i);
            }

            // Update transition component on the GameObject
            GameObject transitionObj = stateTransform.Find("Transition")?.gameObject;
            if (transitionObj != null)
            {
                UpdateTransitionCurrentStateId(transitionObj, i);
                int destStateId = Array.FindIndex(stateList.States, s => s.StateName == currentStateData.DestStateName);
                UpdateTransitionDestStateId(transitionObj, destStateId, currentStateData.StateName == "Trial - Rest");
                UpdateTransitionExitTime(transitionObj, currentStateData.HasExitTime, currentStateData.ExitTime);
                int repeatDestId = Array.FindIndex(stateList.States, s => s.StateName == currentStateData.RepeatDestStateName);
                UpdateRepeatedTransition(transitionObj, Mathf.Max(0, repeatDestId), currentStateData.IsRepeated ? currentStateData.RepeatCount : 1);
            }
        }

        // Remove surplus GameObjects
        for (int i = statesObjectContainer.transform.childCount - 1; i >= stateList.States.Length; i--)
        {
            DestroyImmediate(statesObjectContainer.transform.GetChild(i).gameObject);
        }
    }

    private Transform FindChildByStateID(Transform parent, int stateID)
    {
        foreach (Transform child in parent)
        {
            GameObject transition = child.Find("Transition")?.gameObject;
            if (transition != null)
            {
                // Assuming ItemLogic component stores the state_id
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
                if (!found) Debug.LogWarning($"'state_id' key not found in ItemLogic on {transition.transform.parent.name}/Transition");
            }
            // else Debug.LogWarning($"Property 'logic.statements' not found or empty in ItemLogic on {transition.transform.parent.name}/Transition");
        }
        // else Debug.LogWarning($"ItemLogic component not found on {transition.transform.parent.name}/Transition");
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
                    if (targetKey != null && targetKey.stringValue == "state_currentID") // This is the variable that receives the destination ID
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
                                serializedTransitionSettingLogic.ApplyModifiedPropertiesWithoutUndo(); // Apply changes
                            }
                        } else {
                             Debug.LogWarning($"Transition destination ID property not found for {transition.transform.parent.name}, isTrialRestState: {isTrialRestState}");
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
                                     serializedTransitionSettingLogic.ApplyModifiedPropertiesWithoutUndo(); // Apply changes
                                }
                            } else {
                                Debug.LogWarning($"Trial Start ID property not found for Trial - Rest state: {transition.transform.parent.name}");
                            }
                        }
                         break; // Found and processed targetKey
                    }
                }
                 if (!foundTarget) Debug.LogWarning($"'state_currentID' target key not found in GlobalLogic (state_triggerTransition) on {transition.transform.parent.name}");
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
            var keyProp = serializedComp.FindProperty("key.key"); // This is the key for the timer itself (local key)
            // We need to check if this timer is the one responsible for triggering the exit time logic.
            // This usually means its "Gimmick Key" (if it triggers a global gimmick) or its "Statements" (if it sets a local state)
            // targets "state_enter".
            // Assuming the ItemTimer itself has a key like "state_enter" or "state_enter(disabled)" to denote its function.
            // This part of the logic was specific to a certain setup.

            // Let's assume the existing logic of checking keyProp.stringValue is correct for the project's setup.
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
        // if (!found) Debug.LogWarning($"ItemTimer with key 'state_enter' or 'state_enter(disabled)' not found on {transition.transform.parent.name}");
    }

    private void UpdateRepeatedTransition(GameObject transition, int repeatDestStateId = 0, int repeatCount = 1)
    {
        // Update logic for "state_triggerTransitionToRepeat"
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
                bool found = false;
                for (int i = 0; i < statementsProp.arraySize; i++)
                {
                    SerializedProperty targetKey = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "state_currentID") // Sets destination
                    {
                        found = true;
                        SerializedProperty destIdProp = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (destIdProp != null && destIdProp.intValue != repeatDestStateId)
                        {
                            destIdProp.intValue = repeatDestStateId;
                            serializedRepeatLogic.ApplyModifiedProperties();
                        }
                        break;
                    }
                }
                // if (!found) Debug.LogWarning($"'state_currentID' target key not found in GlobalLogic (state_triggerTransitionToRepeat) on {transition.transform.parent.name}");
            }
        }

        // Update "state_repeatCountMax" in ItemLogic
        var itemLogicComp = transition.GetComponent<ClusterVR.CreatorKit.Operation.Implements.ItemLogic>();
        if (itemLogicComp != null)
        {
            SerializedObject serializedItemLogic = new SerializedObject(itemLogicComp);
            SerializedProperty statementsProp = serializedItemLogic.FindProperty("logic.statements");
            if (statementsProp != null && statementsProp.isArray && statementsProp.arraySize > 0)
            {
                bool found = false;
                for (int i = 0; i < statementsProp.arraySize; i++)
                {
                    SerializedProperty targetKey = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == "state_repeatCountMax")
                    {
                        found = true;
                        SerializedProperty countProp = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (countProp != null && countProp.intValue != repeatCount)
                        {
                            countProp.intValue = repeatCount;
                            serializedItemLogic.ApplyModifiedProperties();
                        }
                        break;
                    }
                }
                // if (!found) Debug.LogWarning($"'state_repeatCountMax' target key not found in ItemLogic on {transition.transform.parent.name}");
            }
        }
    }

    private void UpdateStateListeningItemsAfterReorder()
    {
        if (stateList == null || stateList.States == null || previousStates == null)
        {
            // Debug.LogWarning("UpdateStateListeningItemsAfterReorder: stateList, States, or previousStates is null. Skipping.");
            // Ensure previousStates is initialized if stateList.States exists
            if (stateList != null && stateList.States != null && previousStates == null) {
                 previousStates = new StateList.State[stateList.States.Length];
                 Array.Copy(stateList.States, previousStates, stateList.States.Length);
            } else if (stateList != null && stateList.States != null && stateList.States.Length != previousStates.Length) {
                 previousStates = new StateList.State[stateList.States.Length];
                 Array.Copy(stateList.States, previousStates, stateList.States.Length);
            }
            else if (stateList == null || stateList.States == null) return;
        }


        // 1) Build map: old state name → new index
        var nameToNewIndexMap = new Dictionary<string, int>();
        for (int i = 0; i < stateList.States.Length; i++)
        {
            if (!string.IsNullOrEmpty(stateList.States[i].StateName))
            {
                nameToNewIndexMap[stateList.States[i].StateName] = i;
            }
        }

        // 2) Locate all StateListeningItemData assets
        sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string listenersFolder = string.Format(stateManagementScriptFolderPathFormat, sceneName) + "/StateListeners";
        
        if (!Directory.Exists(listenersFolder))
        {
            // Debug.Log($"Listeners folder not found: {listenersFolder}");
            return;
        }

        foreach (var assetFile in Directory.GetFiles(listenersFolder, "*.asset", SearchOption.AllDirectories))
        {
            var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetFile);
            if (data == null || data.stateListeners == null) continue;

            bool dirty = false;
            
            for(int listenerIndex = 0; listenerIndex < data.stateListeners.Length; listenerIndex++)
            {
                StateListener listener = data.stateListeners[listenerIndex];
                // Find the old state name using the listener's current (old) stateID from previousStates
                string oldStateName = null;
                if (listener.stateID >= 0 && listener.stateID < previousStates.Length)
                {
                    oldStateName = previousStates[listener.stateID].StateName;
                }

                if (!string.IsNullOrEmpty(oldStateName))
                {
                    // Find the new index of this state name
                    if (nameToNewIndexMap.TryGetValue(oldStateName, out var newIndex))
                    {
                        if (listener.stateID != newIndex)
                        {
                            listener.stateID = newIndex;
                            dirty = true;
                        }
                    }
                    else
                    {
                        // Old state name no longer exists, listener is orphaned. Mark as -1 or handle as error.
                        // For now, let's assume it might be an issue or the state was intentionally removed.
                        // To prevent errors, we could set it to an invalid ID or remove the listener.
                        // For safety, let's update its ID to -1 if its old name is gone.
                        Debug.LogWarning($"State '{oldStateName}' for listener in '{assetFile}' not found in new state list. Listener stateID may be invalid.");
                        if(listener.stateID != -1) { // Only change if not already -1
                            listener.stateID = -1; // Mark as invalid/orphaned
                            dirty = true;
                        }
                    }
                } else if (listener.stateID != -1) { // If oldStateName couldn't be determined but ID was valid
                    Debug.LogWarning($"Could not determine old state name for listener with ID {listener.stateID} in '{assetFile}'. Listener stateID may be invalid.");
                    // listener.stateID = -1; // Optionally mark as invalid
                    // dirty = true;
                }
                data.stateListeners[listenerIndex] = listener; // Write back modified listener struct
            }


            if (dirty)
            {
                EditorUtility.SetDirty(data);
                // Regenerate the .js for this item
                string itemName = Path.GetFileNameWithoutExtension(assetFile);
                string scriptPath = Path.Combine(Path.GetDirectoryName(assetFile), itemName + ".js"); // Assume .js is beside .asset

                var sb = new StringBuilder();
                sb.AppendLine(GenerateOnStateEnterFunction(data.stateListeners));
                sb.AppendLine(GenerateDuringStateFunction(data.stateListeners));
                sb.AppendLine(GenerateOnStateExitFunction(data.stateListeners));
                sb.AppendLine(data.otherImplementation);

                File.WriteAllText(scriptPath, sb.ToString());
            }
        }
        
        if (Directory.Exists(listenersFolder)) { // Only save/refresh if we actually did something
             AssetDatabase.SaveAssets();
             AssetDatabase.Refresh();
        }
       
        // previousStates snapshot is updated at the end of OnGUI if contentChanged is true
    }
    
    // Adds or enables a Questionnaire instance and sets its qID.
    private void AddOrEnableQuestionnaireForm(int stateId, string stateNameInAsset)
    {
        GameObject stateObjectInScene = GameObject.Find(stateNameInAsset); // Find by name from asset
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
            
            int qIDToSet = stateId; // Default qID to stateId

            if (existingInstance != null)
            {
                if (!existingInstance.activeSelf)
                {
                    existingInstance.SetActive(true);
                }
                GameObject formController = existingInstance.transform.Find("FormController")?.gameObject;
                UpdateQID(formController, qIDToSet); // Update qID, possibly redundant if already correct
                // CopyWorldItemReferenceListToFormController(formController); // This might be heavy to do every time
                Debug.Log($"Questionnaire in {stateNameInAsset} ensured active with qID {qIDToSet}");
            }
            else
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(formPrefabPath);
                if (prefab != null)
                {
                    GameObject newFormInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, objectsContainer);
                    newFormInstance.name = prefab.name; // Or a more specific name
                    GameObject formController = newFormInstance.transform.Find("FormController")?.gameObject;
                    if (formController != null)
                    {
                        var identifiersAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(identifiersAssetPath);
                        if (identifiersAsset != null) {
                            ScriptableClusterScriptCombiner combiner = formController.GetComponent<ScriptableClusterScriptCombiner>();
                            if (combiner != null) {
                                combiner.ReplaceScript(identifiersAsset, 0, null, 0, true); // Assuming script index 0
                                EditorUtility.SetDirty(combiner);
                            } else {
                                Debug.LogWarning($"ScriptableClusterScriptCombiner not found on FormController of {newFormInstance.name}");
                            }
                        } else {
                            Debug.LogWarning($"Identifiers asset not found at {identifiersAssetPath}");
                        }
                        
                        UpdateQID(formController, qIDToSet);
                        CopyWorldItemReferenceListToFormController(formController);
                        Debug.Log($"Questionnaire added to {stateNameInAsset} with qID {qIDToSet}");
                    }
                }
            }
        } else {
            Debug.LogWarning($"State GameObject '{stateNameInAsset}' not found in scene. Cannot add questionnaire.");
        }
    }

    private GameObject GetFormController(GameObject stateObjectInScene) // stateObjectInScene is the parent "StateX" GameObject
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
        return -1; // Default if not found
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
                            break; // Found qID, no need to continue loop
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

    private void DisplayQuestionnaireRow(string stateNameInAsset, int stateIdInAsset) // stateIdInAsset is the index from StateList
    {
        GameObject stateObjectInScene = GameObject.Find(stateNameInAsset);
        if (stateObjectInScene != null && HasEnabledFormInstance(stateObjectInScene))
        {
            GameObject formController = GetFormController(stateObjectInScene);
            int currentQID = GetCurrentQID(formController); // Get qID from the scene object

            EditorGUILayout.BeginVertical("box"); // Box for the questionnaire section
            EditorGUILayout.LabelField("Questionnaire", GUILayout.Width(100)); // Keep a consistent label width or use AutoLayout
            EditorGUILayout.BeginHorizontal();
            
            GameObject questionnaireObject = formController?.transform.parent.gameObject;
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true); // Object field is for display only
            EditorGUILayout.ObjectField(questionnaireObject, typeof(GameObject), true, GUILayout.Width(100)); // MODIFIED width
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("qID", GUILayout.Width(20));
            
            int displayedQID = currentQID;
            
            int newQID = EditorGUILayout.IntField(displayedQID, GUILayout.Width(30));
            if (newQID != displayedQID && formController != null)
            {
                UpdateQID(formController, newQID);
            } else if (formController != null && displayedQID != stateIdInAsset) {
                 // Optionally, add a button to sync qID with stateIdInAsset
                 // if (GUILayout.Button("Sync qID", GUILayout.Width(70))) { UpdateQID(formController, stateIdInAsset); }
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
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
            // Check if it's an instance of the form prefab and is active
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath && child.gameObject.activeSelf)
                return true;
        }
        return false;
    }

    private void RemoveFormInstance(GameObject stateObjectInScene, GameObject formController) // stateObjectInScene is parent e.g. "State0"
    {
        if (formController != null) // formController is "FormController" GameObject
        {
            GameObject formInstance = formController.transform.parent.gameObject; // This should be the "Questionnaire" prefab instance
            if (formInstance != null)
            {
                Undo.DestroyObjectImmediate(formInstance); // Use Undo for editor operations
                Debug.Log($"Questionnaire in {stateObjectInScene.name} removed.");
            }
        }
        else // Fallback if formController is null but we want to remove based on stateObject
        {
             Transform objects = stateObjectInScene.transform.Find("Objects");
             if (objects != null) {
                 Transform formToDestroy = null;
                 foreach (Transform child in objects) {
                     if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == formPrefabPath) {
                         formToDestroy = child;
                         break;
                     }
                 }
                 if (formToDestroy != null) {
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
                    // Remove existing WorldItemReferenceList from formController to prevent duplicates if re-adding
                    var existingRefList = formController.GetComponent<WorldItemReferenceList>();
                    if (existingRefList != null) {
                        DestroyImmediate(existingRefList, true); // Allow destroying asset component if it's part of prefab
                    }

                    if (UnityEditorInternal.ComponentUtility.CopyComponent(worldItemRefComponentSource))
                    {
                        if (!UnityEditorInternal.ComponentUtility.PasteComponentAsNew(formController))
                        {
                             Debug.LogError("Failed to paste WorldItemReferenceList component to FormController.");
                        }
                    } else {
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
            if (PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.Regular || PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.Variant) {
                 if (AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(obj)) == RequiredObjectsWrapperPrefabPath)
                    return obj;
            }
        }
        return null;
    }
    
    // UpdateQuestionnaireFormsAfterReorder seems redundant if qID is tied to stateID or manually set.
    // If qIDs were based on old indices and needed remapping, this would be useful.
    // Since AddOrEnableQuestionnaireForm uses the current stateId, and DisplayQuestionnaireRow allows manual edit,
    // direct reordering impact on qID is less of a concern unless qIDs were meant to be stable across reorders based on original position.
    // For now, this method might not be strictly needed with current qID logic.
    // private void UpdateQuestionnaireFormsAfterReorder(Dictionary<int, int> stateIdMap) { ... }
    
    private List<GameObject> RetrieveStateListeningItems() // This seems unused, but keeping it
    {
        List<GameObject> stateListeningItems = new List<GameObject>();
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            // Check if the GameObject is an instance of a prefab
            if (PrefabUtility.GetCorrespondingObjectFromSource(obj) != null)
            {
                // Get the asset path of the source prefab
                string sourcePrefabActualPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(obj));
                // Compare it with the defined path for state listening items
                if (sourcePrefabActualPath == stateListeningItemPrefabPath)
                {
                    stateListeningItems.Add(obj);
                }
            }
        }
        return stateListeningItems;
    }
    
    private string GenerateStateFunction(StateListener[] listeners, string functionName, Func<StateListener, List<StateListenerAction>> actionSelector, string extraParameters = "")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"function {functionName}({extraParameters}) {{");
        sb.AppendLine("  const STATE_ID = $.state.state_id;");
        sb.AppendLine("  const CONDITION = $.groupState.currentCondition;"); // Assuming this is available
        sb.AppendLine("");

        var groupedListeners = listeners
            .Where(l => l.stateID >= 0) // Only process valid listeners
            .GroupBy(l => l.stateID);

        foreach (var group in groupedListeners)
        {
            sb.AppendLine($"  if (STATE_ID === {group.Key}) {{");
            foreach (var listenerData in group)
            {
                var actions = actionSelector(listenerData);
                if (actions != null) {
                    foreach (var action in actions)
                    {
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
        GameObject statesContainer = FindOrCreateStatesContainer();
        if (statesContainer == null || stateList == null || index < 0 || index >= stateList.States.Length)
        {
            Debug.LogError("Cannot insert state GameObject due to invalid input or missing container.");
            return;
        }

        string stateName = stateList.States[index].StateName;
        string prefabPath = (stateName == "Preparation") ? prepareStatePrefabPath :
                            (stateName == "Trial - Rest") ? trialRestStatePrefabPath : statePrefabPath;
        
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at {prefabPath} for state {stateName}");
            return;
        }

        GameObject newStateGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab, statesContainer.transform);
        newStateGO.name = stateName;
        newStateGO.transform.SetSiblingIndex(index); // Ensure correct order
        
        GameObject transition = newStateGO.transform.Find("Transition")?.gameObject;
        if (transition != null)
            UpdateTransitionCurrentStateId(transition, index); // Set its state_id immediately
    }

    private void UpdateStateIDsFromIndex(int startIndex)
    {
        GameObject statesContainer = FindOrCreateStatesContainer();
        if (statesContainer == null) return;

        for (int i = startIndex; i < statesContainer.transform.childCount; i++)
        {
            // Only update if the index i is valid for stateList.States as well
            if (i < stateList.States.Length) 
            {
                Transform child = statesContainer.transform.GetChild(i);
                // It's possible the child was just deleted or order is off during rapid changes.
                // Safety check: Ensure child name matches expected state name, or rely on sibling index.
                // For now, assume sibling index is authoritative after reordering/insertions.
                GameObject transition = child.Find("Transition")?.gameObject;
                if (transition != null)
                {
                    UpdateTransitionCurrentStateId(transition, i); // Update state_id to current index 'i'
                }
            }
        }
    }

    private GameObject FindOrCreateStatesContainer()
    {
        GameObject requiredObjectsWrapper = FindRequiredObjectsWrapperInstance();
        if (requiredObjectsWrapper == null) {
            Debug.LogWarning("RequiredObjectsWrapper prefab instance not found. Cannot create/find 'States' container.");
            return null;
        }

        Transform statesObjectTransform = requiredObjectsWrapper.transform.Find("States");
        if (statesObjectTransform == null)
        {
            GameObject statesObject = new GameObject("States");
            statesObject.transform.SetParent(requiredObjectsWrapper.transform, false);
            return statesObject;
        }
        return statesObjectTransform.gameObject;
    }

    private void InsertTrialStateGameObjectAtIndex(int index)
    {
        GameObject statesContainer = FindOrCreateStatesContainer();
         if (statesContainer == null || stateList == null || index < 0 || index >= stateList.States.Length)
        {
            Debug.LogError("Cannot insert trial state GameObject due to invalid input or missing container.");
            return;
        }

        string stateName = stateList.States[index].StateName;
        // For trial states, typically use the default state prefab unless a specific one is defined
        string prefabPath = statePrefabPath; 
        
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Trial State prefab not found at {prefabPath} for state {stateName}");
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
        var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f)); // Background color
        
        Color originalContent = GUI.contentColor;
        GUI.contentColor = Color.white; // Text color

        var labelRect = new Rect(rect.x + 4, rect.y, rect.width - 4, rect.height);
        EditorGUI.LabelField(labelRect, text, isLarge ? EditorStyles.largeLabel : EditorStyles.wordWrappedMiniLabel);
        
        GUI.contentColor = originalContent; // Restore text color
    }
}
