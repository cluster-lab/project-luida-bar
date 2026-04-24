using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClusterVR.CreatorKit.Item.Implements;
using ClusterVR.CreatorKit.Operation.Implements;
using ClusterVR.CreatorKit.Gimmick.Implements;

public static class QuestionnaireEditorManager
{
    private const string formPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/Questionnaire/Questionnaire.prefab";
    private const string identifiersAssetPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string ExpManagersWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/LUIDA-ExpManagers.prefab";
    private const string ParticipantManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ParticipantManager.prefab";
    private const string ConditionManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionManager.prefab";
    private const string WorldItemRefListObjectName = "WorldItemRefList";
    private const string StateQuestionnaireRootName = "LUIDA-QuestionnaireByState";

    [MenuItem("GameObject/LUIDA/Questionnaire", false, 10)]
    public static void ShowCreateQuestionnaireDialog()
    {
        QuestionnaireDialog.ShowWindow();
    }

    public static void CreateQuestionnaireDirectly(int qIDToSet, int pNum = -1)
    {
        GameObject stateContainer = new GameObject($"Questionnaire_{qIDToSet}");
        Undo.RegisterCreatedObjectUndo(stateContainer, "Create Questionnaire Object");

        LuidaQuestionnaire idSync = stateContainer.AddComponent<LuidaQuestionnaire>();
        idSync.qId = qIDToSet;
        
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(formPrefabPath);
        if (prefab == null) return;
        
        if (pNum <= 0) pNum = GetPNum();

        float horizontalSpacing = 3f;
        float startX = -((pNum - 1) * horizontalSpacing) / 2f;

        for (int i = 1; i <= pNum; i++)
        {
            GameObject newFormInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stateContainer.transform);
            newFormInstance.name = $"{prefab.name}_p{i}";
            float x = startX + (i - 1) * horizontalSpacing;
            newFormInstance.transform.localPosition = new Vector3(x, newFormInstance.transform.localPosition.y, newFormInstance.transform.localPosition.z);
            GameObject formController = newFormInstance.transform.Find("FormController")?.gameObject;
            if (formController != null)
            {
                EnableAccessToParticipantManager(formController);
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
                RemoveStateTransitionTriggers(formController);
                UpdateID(formController, "qID", qIDToSet);
                UpdateID(formController, "pID", i);
                AddControllerManagerToWorldItemReferenceList(formController);
            }
        }
        Selection.activeObject = stateContainer;
        Debug.Log($"Successfully created a new questionnaire object named '{stateContainer.name}' with qID {qIDToSet} at the scene root.");
    }

    public static void AddOrEnableQuestionnaireForm(StateList stateList, int stateId, string stateNameInAsset, int pNum = -1)
    {
        GameObject questionnairesContainer = FindOrCreateContainer(StateQuestionnaireRootName);
        GameObject stateContainer = FindOrCreateStateContainer(questionnairesContainer, stateNameInAsset);

        GameObject expManagersWrapper = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root) == ExpManagersWrapperPrefabPath)
            {
                expManagersWrapper = root;
                break;
            }
        }

        if (expManagersWrapper != null)
        {
            Transform statesObject = expManagersWrapper.transform.Find("States");
            if (statesObject != null)
            {
                Transform specificStateObject = statesObject.transform.Find(stateList.States[stateId].StateName);
                if (specificStateObject != null)
                {
                    Transform objectsContainer = specificStateObject.transform.Find("Objects");
                    if (objectsContainer != null)
                    {
                        var sourceGimmick = objectsContainer.GetComponent<SetGameObjectActiveGimmick>();
                        if (sourceGimmick != null)
                        {
                            if (UnityEditorInternal.ComponentUtility.CopyComponent(sourceGimmick))
                            {
                                var existingGimmick = stateContainer.GetComponent<SetGameObjectActiveGimmick>();
                                if (existingGimmick != null)
                                {
                                    Undo.DestroyObjectImmediate(existingGimmick);
                                }

                                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(stateContainer);
                            }
                        }
                    }
                }
            }
        }
        
        for (int i = stateContainer.transform.childCount - 1; i >= 0; i--)
        {
            var child = stateContainer.transform.GetChild(i).gameObject;
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child) == formPrefabPath)
            {
                Undo.DestroyObjectImmediate(child);
            }
        }

        int qIDToSet = stateList.States[stateId].qID > 0 ? stateList.States[stateId].qID : stateId + 1;
        stateList.States[stateId].qID = qIDToSet;
        EditorUtility.SetDirty(stateList);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(formPrefabPath);
        if (prefab == null) return;
        
        if (pNum <= 0) pNum = GetPNum();

        float horizontalSpacing = 3f;
        float startX = -((pNum - 1) * horizontalSpacing) / 2f;
        for (int i = 1; i <= pNum; i++)
        {
            GameObject newFormInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stateContainer.transform);
            newFormInstance.name = $"{prefab.name}_p{i}";
            float x = startX + (i - 1) * horizontalSpacing;
            newFormInstance.transform.localPosition = new Vector3(x, newFormInstance.transform.localPosition.y, newFormInstance.transform.localPosition.z);
            GameObject formController = newFormInstance.transform.Find("FormController")?.gameObject;
            if (formController != null)
            {
                EnableAccessToParticipantManager(formController);
                var identifiersAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(identifiersAssetPath);
                if (identifiersAsset != null)
                {
                    var combiner = formController.GetComponent<ScriptableClusterScriptCombiner>();
                    if (combiner != null)
                    {
                        combiner.ReplaceScript(identifiersAsset, 0, null, 0, true);
                        EditorUtility.SetDirty(combiner);
                    }
                }
                UpdateID(formController, "qID", qIDToSet);
                UpdateID(formController, "pID", i);
                AddControllerManagerToWorldItemReferenceList(formController);
            }
        }
    }

    public static void UpdateID(GameObject formController, string idLabel, int id)
    {
        if (formController == null) return;
        var itemLogic = formController.GetComponent<ItemLogic>();
        if (itemLogic != null)
        {
            SerializedObject serializedComp = new SerializedObject(itemLogic);
            SerializedProperty statementsProp = serializedComp.FindProperty("logic.statements");
            if (statementsProp != null && statementsProp.isArray)
            {
                for (int i = 0; i < statementsProp.arraySize; i++)
                {
                    SerializedProperty targetKey = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.targetState.key");
                    if (targetKey != null && targetKey.stringValue == idLabel)
                    {
                        SerializedProperty idValueProp = statementsProp.GetArrayElementAtIndex(i).FindPropertyRelative("singleStatement.expression.value.constant.integerValue");
                        if (idValueProp != null && idValueProp.intValue != id)
                        {
                            idValueProp.intValue = id;
                            serializedComp.ApplyModifiedProperties();
                        }
                        break;
                    }
                }
            }
        }
    }

    public static void DisplayQuestionnaireRow(StateList stateList, SerializedObject serializedStateList, string stateName, int stateIdInAsset)
    {
        int assetQID = (stateList != null && stateIdInAsset >= 0 && stateIdInAsset < stateList.States.Length) ? stateList.States[stateIdInAsset].qID : -1;
        Rect qBoxRect = EditorGUILayout.BeginVertical("box");
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(qBoxRect, new Color(0.95f, 0.85f, 0.40f, 0.22f));
        }
        EditorGUILayout.LabelField("Questionnaires", GUILayout.Width(100));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("qID", GUILayout.Width(20));
        int newQID = EditorGUILayout.IntField(assetQID, GUILayout.Width(30));
        if (newQID != assetQID)
        {
            stateList.States[stateIdInAsset].qID = newQID;
            EditorUtility.SetDirty(stateList);
            serializedStateList.ApplyModifiedProperties();

            var root = GameObject.Find(StateQuestionnaireRootName);
            var stateContainer = root?.transform.Find(stateName)?.gameObject;
            if (stateContainer != null)
            {
                // REMOVED: No longer need to update the sync component here.
                // Update all child forms with the new ID.
                foreach (Transform childForm in stateContainer.transform)
                {
                    var fc = childForm.Find("FormController");
                    if (fc != null) UpdateID(fc.gameObject, "qID", newQID);
                }
            }
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Remove", GUILayout.Width(70)))
        {
            stateList.States[stateIdInAsset].qID = 0;
            EditorUtility.SetDirty(stateList);
            serializedStateList.ApplyModifiedProperties();
            RemoveFormInstance(stateName);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("GameObjects can be found here:");
        EditorGUILayout.BeginHorizontal();
        EditorGUI.EndDisabledGroup();
        var rootObj = GameObject.Find(StateQuestionnaireRootName);
        var containerObj = rootObj?.transform.Find(stateName)?.gameObject;
        EditorGUILayout.ObjectField(containerObj, typeof(GameObject), true, GUILayout.Width(100));
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    public static bool HasEnabledFormInstance(string stateName)
    {
        var root = GameObject.Find(StateQuestionnaireRootName);
        if (root == null) return false;
        var stateContainer = root.transform.Find(stateName);
        if (stateContainer == null) return false;
        foreach (Transform child in stateContainer)
        {
            if (!child.gameObject.activeSelf) continue;
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject);
            if (path == formPrefabPath) return true;
        }
        return false;
    }

    public static void SyncQuestionnaireContainers(StateList.State[] previous, StateList.State[] current)
    {
        var root = GameObject.Find(StateQuestionnaireRootName);
        if (root == null) return;
        var currentNames = new HashSet<string>(current.Select(s => s.StateName));
        var previousNames = new HashSet<string>(previous.Select(s => s.StateName));
        for (int i = 0; i < Mathf.Min(previous.Length, current.Length); i++)
        {
            string oldName = previous[i].StateName;
            string newName = current[i].StateName;
            if (oldName != newName && !string.IsNullOrEmpty(oldName) && !string.IsNullOrEmpty(newName))
            {
                var oldTrans = root.transform.Find(oldName);
                if (oldTrans != null)
                {
                    Undo.RecordObject(oldTrans.gameObject, "Rename Questionnaire Container");
                    oldTrans.name = newName;
                }
            }
        }
        foreach (var prevName in previousNames)
        {
            if (!string.IsNullOrEmpty(prevName) && !currentNames.Contains(prevName))
            {
                var t = root.transform.Find(prevName);
                if (t) Undo.DestroyObjectImmediate(t.gameObject);
            }
        }
    }
    
    private static void RemoveStateTransitionTriggers(GameObject formController)
    {
        var globalLogics = formController.GetComponents<GlobalLogic>();
        if (globalLogics.Length == 0) return;

        var keysToRemove = new HashSet<string> { "state_isTransitionTriggered", "state_triggerTransition" };

        foreach (var logic in globalLogics)
        {
            SerializedObject serializedLogic = new SerializedObject(logic);
            SerializedProperty triggersProperty = serializedLogic.FindProperty("logic.statements");
            
            if (triggersProperty == null || !triggersProperty.isArray) continue;

            // Iterate backwards when removing from a list to avoid index shifting issues.
            for (int i = triggersProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty triggerElement = triggersProperty.GetArrayElementAtIndex(i);
                SerializedProperty keyProperty = triggerElement.FindPropertyRelative("singleStatement.targetState.key");

                if (keyProperty != null && keysToRemove.Contains(keyProperty.stringValue))
                {
                    triggersProperty.DeleteArrayElementAtIndex(i);
                }
            }
            serializedLogic.ApplyModifiedProperties();
        }
    }

    private static void RemoveFormInstance(string stateName)
    {
        var root = GameObject.Find(StateQuestionnaireRootName);
        if (!root) return;
        var stateContainer = root.transform.Find(stateName);
        if (!stateContainer) return;
        Undo.DestroyObjectImmediate(stateContainer.gameObject);
        Debug.Log($"Removed questionnaire instances and container for '{stateName}'.");
    }

    private static void AddControllerManagerToWorldItemReferenceList(GameObject formController)
    {
        if (formController == null) return;
        var existingRefList = formController.GetComponent<WorldItemReferenceList>() ?? formController.AddComponent<WorldItemReferenceList>();
        var conditionManagerObj = FindConditionManagerGameObject();
        if (conditionManagerObj != null)
        {
            SerializedObject serializedRefList = new SerializedObject(existingRefList);
            var prop = serializedRefList.FindProperty("worldItemReferences");
            bool alreadyExists = false;
            for (int i = 0; i < prop.arraySize; i++)
            {
                var item = prop.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("id").stringValue == "ConditionManager")
                {
                    alreadyExists = true;
                    break;
                }
            }
            if (!alreadyExists)
            {
                prop.InsertArrayElementAtIndex(0);
                var refItem = prop.GetArrayElementAtIndex(0);
                refItem.FindPropertyRelative("id").stringValue = "ConditionManager";
                refItem.FindPropertyRelative("item").objectReferenceValue = conditionManagerObj.GetComponent<Item>();
                serializedRefList.ApplyModifiedProperties();
            }
        }
        else
        {
            Debug.LogError("ConditionManager not found in the scene.");
        }
    }

    public static GameObject FindConditionManagerGameObject()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root) != ExpManagersWrapperPrefabPath)
                continue;
            foreach (Transform child in root.transform)
            {
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == ConditionManagerPrefabPath)
                {
                    return child.gameObject;
                }
            }
        }
        return null;
    }

    private static int GetPNum()
    {
        if (!File.Exists(identifiersAssetPath)) return 1;
        var text = File.ReadAllText(identifiersAssetPath);
        var m = System.Text.RegularExpressions.Regex.Match(text, @"pNum\s*=\s*(\d+)\s*;");
        return m.Success ? int.Parse(m.Groups[1].Value) : 1;
    }

    private static void EnableAccessToParticipantManager(GameObject item)
    {
        var itemGroupMember = item.GetComponent<ItemGroupMember>() ?? (item.AddComponent(typeof(ItemGroupMember)) as ItemGroupMember);
        foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) != ExpManagersWrapperPrefabPath) continue;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == ParticipantManagerPrefabPath)
                {
                    ItemGroupHost host = child.GetComponent<ItemGroupHost>();
                    if (host != null)
                    {
                        SerializedObject serializedItemGroupMember = new SerializedObject(itemGroupMember);
                        serializedItemGroupMember.FindProperty("host").objectReferenceValue = host;
                        serializedItemGroupMember.ApplyModifiedProperties();
                    }
                }
            }
        }
    }

    private static GameObject FindOrCreateContainer(string name)
    {
        GameObject container = GameObject.Find(name);
        if (container == null)
        {
            container = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(container, $"Create {name}");
        }
        return container;
    }

    private static GameObject FindOrCreateStateContainer(GameObject questionnairesContainer, string stateName)
    {
        Transform stateContainerTransform = questionnairesContainer.transform.Find(stateName);
        if (stateContainerTransform == null)
        {
            GameObject stateContainer = new GameObject(stateName);
            stateContainer.transform.SetParent(questionnairesContainer.transform, false);
            Undo.RegisterCreatedObjectUndo(stateContainer, $"Create State Container for {stateName}");
            return stateContainer;
        }
        return stateContainerTransform.gameObject;
    }
}