using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using ClusterVR.CreatorKit.Item.Implements;

public class CreateStateListeningItemMenu
{
    // [MenuItem("GameObject/LUIDA State-Listening Item", false, 10)]
    static void CreateNewStateListeningItem()
    {
        InputNameWindow.ShowWindow(CreateItemWithName);
    }

    static void CreateItemWithName(string newItemName)
    {
        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(LuidaPaths.Load<GameObject>(LuidaPaths.StateListeningItemPrefab));
        newObject.name = newItemName;
        EnableAccessToConditions(newObject);
        Undo.RegisterCreatedObjectUndo(newObject, "Create LUIDA State-Listening Item");

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string scriptFolderPath = LuidaPaths.SceneStateManagementFolder(sceneName);

        if (!AssetDatabase.IsValidFolder(scriptFolderPath))
        {
            Directory.CreateDirectory(scriptFolderPath);
            AssetDatabase.Refresh();
        }

        string newScriptPath = $"{scriptFolderPath}/{newItemName}.js";
        AssetDatabase.CopyAsset(LuidaPaths.StateListeningItemTemplateJs, newScriptPath);
        AssetDatabase.Refresh();

        var combiner = LuidaCombiner.Get(newObject);
        var newScriptAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(newScriptPath);
        combiner.ReplaceScript(newScriptAsset, 1, null, 0, true);
        LuidaCombiner.MarkDirty(combiner);
        EditorUtility.SetDirty(newScriptAsset);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = newObject;
    }

    private static GameObject FindConditionManagerPrefabInstance()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == LuidaPaths.ExpManagersPrefab)
            {
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    Transform child = obj.transform.GetChild(i);
                    if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == LuidaPaths.ConditionManagerPrefab)
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
