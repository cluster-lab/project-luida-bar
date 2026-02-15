using UnityEngine;
using UnityEditor;
using System.IO;

public static class AttachSticksToAvatarHands
{
    private const string AvatarsRoot = "Assets/_Experiment_/Prefabs/Taiko/Avatars/Rocketbox";
    private const string StickPath = "Assets/_Experiment_/Prefabs/Taiko/Japanese_drum_set/Models/stick.fbx";

    [MenuItem("Tools/Attach Sticks To Avatar Hands")]
    public static void Execute()
    {
        var stickPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StickPath);
        if (stickPrefab == null)
        {
            Debug.LogError($"Could not load stick FBX at {StickPath}");
            return;
        }

        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { AvatarsRoot });
        int processed = 0;

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);

            // Skip prefabs in subdirectories (e.g. Export folders)
            string relativePath = prefabPath.Substring(AvatarsRoot.Length + 1);
            if (relativePath.Contains("/"))
                continue;

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"Skipping: could not load prefab at {prefabPath}");
                continue;
            }

            Transform leftHand = FindChildRecursive(prefabRoot.transform, "L Hand");
            Transform rightHand = FindChildRecursive(prefabRoot.transform, "R Hand");

            if (leftHand == null || rightHand == null)
            {
                Debug.LogWarning($"Skipping {prefabRoot.name}: L Hand found={leftHand != null}, R Hand found={rightHand != null}");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                continue;
            }

            // Attach or adjust stick on left hand
            var leftStick = FindExistingStick(leftHand, stickPrefab);
            if (leftStick == null)
            {
                leftStick = (GameObject)PrefabUtility.InstantiatePrefab(stickPrefab);
                leftStick.transform.SetParent(leftHand, false);
            }
            leftStick.transform.localPosition = new Vector3(-0.16f, 0.02f, -0.06f);
            leftStick.transform.localRotation = Quaternion.Euler(0f, -30f, 0f);
            leftStick.transform.localScale = new Vector3(60f, 48f, 48f);

            // Attach or adjust stick on right hand
            var rightStick = FindExistingStick(rightHand, stickPrefab);
            if (rightStick == null)
            {
                rightStick = (GameObject)PrefabUtility.InstantiatePrefab(stickPrefab);
                rightStick.transform.SetParent(rightHand, false);
            }
            rightStick.transform.localPosition = new Vector3(-0.16f, 0.02f, 0.06f);
            rightStick.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            rightStick.transform.localScale = new Vector3(60f, 48f, 48f);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            processed++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"Attached sticks to {processed} avatar prefabs in {AvatarsRoot}");
    }

    private static GameObject FindExistingStick(Transform hand, GameObject stickPrefab)
    {
        foreach (Transform child in hand)
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == stickPrefab)
                return child.gameObject;
            if (child.name == stickPrefab.name)
                return child.gameObject;
        }
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string nameContains)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(nameContains))
                return child;

            Transform found = FindChildRecursive(child, nameContains);
            if (found != null)
                return found;
        }
        return null;
    }
}
