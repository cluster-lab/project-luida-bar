using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using ClusterVR.CreatorKit.Item.Implements;

public class CreateStateListeningItemMenu
{
    private const string prefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/StateListeningItem.prefab";
    private const string scriptFolderPathFormat = "Assets/_Experiment_/Scripts/StateManagement/{0}";
    private const string stateListeningItemScriptTemplatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/StateManagement/StateListeningItemTemplate.js";
    private const string ExpManagersWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/LUIDA-ExpManagers.prefab";
    private const string ConditionManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionManager.prefab";

    // [MenuItem("GameObject/LUIDA State-Listening Item", false, 10)]
    static void CreateNewStateListeningItem()
    {
        InputNameWindow.ShowWindow(CreateItemWithName);
    }

    static void CreateItemWithName(string newItemName)
    {
        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
        newObject.name = newItemName;
        EnableAccessToConditions(newObject);
        Undo.RegisterCreatedObjectUndo(newObject, "Create LUIDA State-Listening Item");

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string scriptFolderPath = string.Format(scriptFolderPathFormat, sceneName);

        if (!AssetDatabase.IsValidFolder(scriptFolderPath))
        {
            Directory.CreateDirectory(scriptFolderPath);
            AssetDatabase.Refresh();
        }

        string newScriptPath = $"{scriptFolderPath}/{newItemName}.js";
        AssetDatabase.CopyAsset(stateListeningItemScriptTemplatePath, newScriptPath);
        AssetDatabase.Refresh();

        GameObject scriptCombinerObject = newObject.GetComponent<ScriptableClusterScriptCombiner>().gameObject;
        ScriptableClusterScriptCombiner combiner = scriptCombinerObject.GetComponent<ScriptableClusterScriptCombiner>();
        var newScriptAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(newScriptPath);
        combiner.ReplaceScript(newScriptAsset, 1, null, 0, true);
        EditorUtility.SetDirty(combiner);
        EditorUtility.SetDirty(newScriptAsset);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = newObject;
    }

    private static GameObject FindConditionManagerPrefabInstance()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == ExpManagersWrapperPrefabPath)
            {
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    Transform child = obj.transform.GetChild(i);
                    if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == ConditionManagerPrefabPath)
                    {
                        return child.gameObject;
                    }
                }
            }
        }
        return null;
    }

    private static void EnableAccessToConditions(GameObject item)
    {
        // Attach ItemGroupMember component to this object
        var itemGroupMember = item.GetComponent<ClusterVR.CreatorKit.Item.Implements.ItemGroupMember>();

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
}
