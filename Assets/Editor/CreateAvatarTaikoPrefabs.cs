using UnityEngine;
using UnityEditor;
using System.IO;

public static class CreateAvatarTaikoPrefabs
{
    private const string AvatarsRoot = "Assets/_Experiment_/Prefabs/Taiko/Avatars/Adults";
    private const string TaikoSetPath = "Assets/_Experiment_/Prefabs/Taiko/dummy_taiko_set.prefab";

    [MenuItem("Tools/Create Avatar Taiko Prefabs (Adults)")]
    public static void Create()
    {
        var taikoSetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TaikoSetPath);
        if (taikoSetPrefab == null)
        {
            Debug.LogError($"Could not load taiko set prefab at {TaikoSetPath}");
            return;
        }

        string fullRoot = Path.GetFullPath(AvatarsRoot);
        var subDirs = Directory.GetDirectories(fullRoot);
        int created = 0;

        foreach (string subDir in subDirs)
        {
            string folderName = Path.GetFileName(subDir);
            string fbxPath = $"{AvatarsRoot}/{folderName}/Export/{folderName}.fbx";
            string prefabSavePath = $"{AvatarsRoot}/{folderName}.prefab";

            var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxAsset == null)
            {
                Debug.LogWarning($"Skipping {folderName}: no FBX found at {fbxPath}");
                continue;
            }

            // Create root GameObject
            var root = new GameObject(folderName);

            // Instantiate FBX as child
            var avatarInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            avatarInstance.transform.SetParent(root.transform, false);

            // Instantiate taiko set as child with offset transform
            var taikoInstance = (GameObject)PrefabUtility.InstantiatePrefab(taikoSetPrefab);
            taikoInstance.transform.SetParent(root.transform, false);
            taikoInstance.transform.localPosition = new Vector3(0f, 0f, 1f);
            taikoInstance.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabSavePath);
            Object.DestroyImmediate(root);
            created++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"Created {created} avatar+taiko prefabs in {AvatarsRoot}");
    }
}
