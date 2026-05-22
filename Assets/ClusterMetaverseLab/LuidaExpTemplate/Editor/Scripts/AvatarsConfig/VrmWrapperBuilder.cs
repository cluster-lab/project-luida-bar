using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ClusterVR.CreatorKit.Item.Implements;

/// <summary>
/// Generates a CCK wrapper prefab from a humanoid VRM/avatar source prefab.
/// Walks the Animator bone hierarchy to build a per-avatar BoneMap.js header,
/// then creates a prefab with Item + MovableItem + ScriptableItem + CSCombiner.
/// </summary>
public static class VrmWrapperBuilder
{
    // Output folders are derived per-call from the registry's scene folder.
    // See AvatarsConfigAssetUtil.GetSceneFolderFromRegistry.
    private const string SyncCloneScriptPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/AvatarManagement/AvatarSyncClone.js";

    // Core 17 bones (always synced)
    private static readonly HumanBodyBones[] CoreBones = {
        HumanBodyBones.Hips,
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.Neck,
        HumanBodyBones.Head,
        HumanBodyBones.LeftShoulder,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.LeftHand,
        HumanBodyBones.RightShoulder,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.RightHand,
        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.RightUpperLeg,
        HumanBodyBones.RightLowerLeg,
    };

    private static readonly HumanBodyBones[] FeetBones = {
        HumanBodyBones.LeftFoot,
        HumanBodyBones.RightFoot,
        HumanBodyBones.LeftToes,
        HumanBodyBones.RightToes,
    };

    private static readonly HumanBodyBones[] FingerBones = {
        HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal,
        HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal,
        HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal,
        HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal,
        HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal,
        HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal,
        HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal,
        HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
        HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal,
        HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal,
    };

    private static readonly HumanBodyBones[] JawBones = {
        HumanBodyBones.Jaw,
    };

    // Maps each HumanBodyBones to its parent in the humanoid hierarchy.
    // null means parent is the item root (uses the item's world rotation).
    private static readonly Dictionary<HumanBodyBones, HumanBodyBones?> BoneParentMap = new Dictionary<HumanBodyBones, HumanBodyBones?>
    {
        // Core
        { HumanBodyBones.Hips, null },
        { HumanBodyBones.Spine, HumanBodyBones.Hips },
        { HumanBodyBones.Chest, HumanBodyBones.Spine },
        { HumanBodyBones.Neck, HumanBodyBones.Chest },
        { HumanBodyBones.Head, HumanBodyBones.Neck },
        { HumanBodyBones.LeftShoulder, HumanBodyBones.Chest },
        { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftShoulder },
        { HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftUpperArm },
        { HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm },
        { HumanBodyBones.RightShoulder, HumanBodyBones.Chest },
        { HumanBodyBones.RightUpperArm, HumanBodyBones.RightShoulder },
        { HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm },
        { HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm },
        { HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips },
        { HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftUpperLeg },
        { HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips },
        { HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg },
        // Feet
        { HumanBodyBones.LeftFoot, HumanBodyBones.LeftLowerLeg },
        { HumanBodyBones.RightFoot, HumanBodyBones.RightLowerLeg },
        { HumanBodyBones.LeftToes, HumanBodyBones.LeftFoot },
        { HumanBodyBones.RightToes, HumanBodyBones.RightFoot },
        // Jaw
        { HumanBodyBones.Jaw, HumanBodyBones.Head },
        // Fingers — Left
        { HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftHand },
        { HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbProximal },
        { HumanBodyBones.LeftThumbDistal, HumanBodyBones.LeftThumbIntermediate },
        { HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftHand },
        { HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexProximal },
        { HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexIntermediate },
        { HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftHand },
        { HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleProximal },
        { HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LeftMiddleIntermediate },
        { HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftHand },
        { HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingProximal },
        { HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingIntermediate },
        { HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftHand },
        { HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleProximal },
        { HumanBodyBones.LeftLittleDistal, HumanBodyBones.LeftLittleIntermediate },
        // Fingers — Right
        { HumanBodyBones.RightThumbProximal, HumanBodyBones.RightHand },
        { HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbProximal },
        { HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbIntermediate },
        { HumanBodyBones.RightIndexProximal, HumanBodyBones.RightHand },
        { HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexProximal },
        { HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate },
        { HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightHand },
        { HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleProximal },
        { HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate },
        { HumanBodyBones.RightRingProximal, HumanBodyBones.RightHand },
        { HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingProximal },
        { HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate },
        { HumanBodyBones.RightLittleProximal, HumanBodyBones.RightHand },
        { HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleProximal },
        { HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleIntermediate },
    };

    /// <summary>
    /// Build a CCK wrapper prefab from a humanoid source prefab. Wrappers and
    /// bone-map JS land under the registry's scene folder
    /// (Assets/_Experiment_/Avatars/&lt;scene&gt;/{Wrappers,Generated}/) so each
    /// scene's avatar set stays isolated.
    /// Returns the path to the saved wrapper prefab, or null on failure.
    /// </summary>
    public static string Build(GameObject sourceVrmPrefab, AvatarEntry entry, AvatarRegistry registry)
    {
        if (sourceVrmPrefab == null || entry == null || string.IsNullOrEmpty(entry.avatarID))
        {
            Debug.LogError("[VrmWrapperBuilder] Invalid arguments.");
            return null;
        }

        string sceneFolder = AvatarsConfigAssetUtil.GetSceneFolderFromRegistry(registry);
        if (sceneFolder == null)
        {
            Debug.LogError("[VrmWrapperBuilder] Could not derive scene folder from registry. Is the registry under Assets/_Experiment_/Avatars/<scene>/ ?");
            return null;
        }

        string wrapperFolder = AvatarsConfigAssetUtil.GetWrapperFolder(sceneFolder);
        string generatedFolder = AvatarsConfigAssetUtil.GetGeneratedFolder(sceneFolder);

        // Ensure output folders
        Directory.CreateDirectory(wrapperFolder);
        Directory.CreateDirectory(generatedFolder);

        // --- Step 1: Discover bone names from the Animator ---
        var boneNameMap = DiscoverBoneNames(sourceVrmPrefab, entry);
        if (boneNameMap == null || boneNameMap.Count == 0)
        {
            Debug.LogError("[VrmWrapperBuilder] No humanoid bones found. Is the source prefab configured as Humanoid?");
            return null;
        }

        // --- Step 2: Generate BoneMap.js ---
        string boneMapJsPath = GenerateBoneMapJs(entry, boneNameMap, generatedFolder);
        if (boneMapJsPath == null) return null;

        // --- Step 3: Build the wrapper prefab ---
        string wrapperPath = BuildWrapperPrefab(sourceVrmPrefab, entry, boneMapJsPath, wrapperFolder);
        return wrapperPath;
    }

    /// <summary>
    /// Instantiate the source prefab temporarily to read bone Transform names via Animator.
    /// </summary>
    private static Dictionary<HumanBodyBones, string> DiscoverBoneNames(GameObject sourceVrmPrefab, AvatarEntry entry)
    {
        // Instantiate in a preview scene so the Animator binds properly
        var previewScene = EditorSceneManager.NewPreviewScene();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceVrmPrefab, previewScene);

        var animator = instance.GetComponentInChildren<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogError("[VrmWrapperBuilder] Source prefab has no humanoid Animator.");
            Object.DestroyImmediate(instance);
            EditorSceneManager.ClosePreviewScene(previewScene);
            return null;
        }

        // Collect the bones we want based on checkboxes
        var bonesToSync = new List<HumanBodyBones>(CoreBones);
        if (entry.syncFeetToes) bonesToSync.AddRange(FeetBones);
        if (entry.syncFingers) bonesToSync.AddRange(FingerBones);
        if (entry.syncJaw) bonesToSync.AddRange(JawBones);

        var result = new Dictionary<HumanBodyBones, string>();
        foreach (var bone in bonesToSync)
        {
            Transform boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform != null)
            {
                result[bone] = boneTransform.name;
            }
        }

        Object.DestroyImmediate(instance);
        EditorSceneManager.ClosePreviewScene(previewScene);
        return result;
    }

    /// <summary>
    /// Generate a JS file containing const BONE_MAP and BONE_PARENT literals.
    /// </summary>
    private static string GenerateBoneMapJs(AvatarEntry entry, Dictionary<HumanBodyBones, string> boneNameMap, string generatedFolder)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"// Auto-generated bone map for avatar: {entry.avatarID}");
        sb.AppendLine("// Do not edit manually — regenerate via LUIDA Avatars window.");
        sb.AppendLine();

        // Sync options
        sb.AppendLine($"const AVATAR_SYNC_HIPS_Y = {(entry.syncHipsY ? "true" : "false")};");
        sb.AppendLine($"const AVATAR_HIPS_Y_OFFSET = {entry.hipsYOffset.ToString(System.Globalization.CultureInfo.InvariantCulture)};");
        sb.AppendLine();

        // BONE_MAP array
        sb.AppendLine("const BONE_MAP = [");
        foreach (var kvp in boneNameMap)
        {
            string csEnumName = kvp.Key.ToString(); // e.g. "Hips", "LeftUpperArm"
            string transformName = kvp.Value.Replace("\"", "\\\"");
            sb.AppendLine($"  {{ bone: HumanoidBone.{csEnumName}, name: \"{transformName}\" }},");
        }
        sb.AppendLine("];");
        sb.AppendLine();

        // BONE_PARENT map
        sb.AppendLine("const BONE_PARENT = {");
        foreach (var kvp in boneNameMap)
        {
            if (!BoneParentMap.TryGetValue(kvp.Key, out var parentBone)) continue;
            string csEnumName = kvp.Key.ToString();
            if (parentBone == null)
            {
                sb.AppendLine($"  [HumanoidBone.{csEnumName}]: null,");
            }
            else
            {
                // Only include parent if the parent bone is also in the map
                if (boneNameMap.ContainsKey(parentBone.Value))
                {
                    sb.AppendLine($"  [HumanoidBone.{csEnumName}]: HumanoidBone.{parentBone.Value},");
                }
                else
                {
                    // Parent not synced — fall back to root
                    sb.AppendLine($"  [HumanoidBone.{csEnumName}]: null,");
                }
            }
        }
        sb.AppendLine("};");

        string jsPath = Path.Combine(generatedFolder, $"{entry.avatarID}_BoneMap.js");
        File.WriteAllText(jsPath, sb.ToString());
        AssetDatabase.ImportAsset(jsPath, ImportAssetOptions.ForceUpdate);
        return jsPath;
    }

    /// <summary>
    /// Create the wrapper prefab with CCK components and CSCombiner pointing to BoneMap + SyncClone JS.
    /// </summary>
    private static string BuildWrapperPrefab(GameObject sourceVrmPrefab, AvatarEntry entry, string boneMapJsPath, string wrapperFolder)
    {
        // Create root GameObject
        GameObject root = new GameObject($"LUIDA-Avatar-{entry.avatarID}");

        // Instantiate source VRM as child
        GameObject body = (GameObject)PrefabUtility.InstantiatePrefab(sourceVrmPrefab);
        body.transform.SetParent(root.transform, false);

        // Add Rigidbody (required by MovableItem) — kinematic, no gravity
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Add CCK components
        root.AddComponent<Item>();
        root.AddComponent<MovableItem>();
        root.AddComponent<ScriptableItem>();

        // Add CSCombiner and wire up JS files
        var combiner = root.AddComponent<ScriptableClusterScriptCombiner>();

        var boneMapAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(boneMapJsPath);
        var syncCloneAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(SyncCloneScriptPath);

        if (boneMapAsset != null)
            combiner.AppendScript(boneMapAsset, null);
        else
            Debug.LogWarning($"[VrmWrapperBuilder] Could not load BoneMap JS at {boneMapJsPath}");

        if (syncCloneAsset != null)
            combiner.AppendScript(syncCloneAsset, null);
        else
            Debug.LogWarning($"[VrmWrapperBuilder] Could not load AvatarSyncClone.js at {SyncCloneScriptPath}");

        combiner.CombineScripts();

        // Save as prefab
        string prefabPath = Path.Combine(wrapperFolder, $"{entry.avatarID}.prefab");
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        Debug.Log($"[VrmWrapperBuilder] Created wrapper prefab at {prefabPath}");
        return prefabPath;
    }

    /// <summary>
    /// Rebuild an existing wrapper prefab (e.g. after changing bone sync checkboxes).
    /// </summary>
    public static string Rebuild(AvatarEntry entry, AvatarRegistry registry)
    {
        if (entry == null || entry.sourceVrmPrefab == null)
        {
            Debug.LogError("[VrmWrapperBuilder] Cannot rebuild: missing source prefab.");
            return null;
        }

        string sceneFolder = AvatarsConfigAssetUtil.GetSceneFolderFromRegistry(registry);
        string generatedFolder = sceneFolder != null ? AvatarsConfigAssetUtil.GetGeneratedFolder(sceneFolder) : null;

        // Delete old wrapper if it exists
        if (entry.wrapperItemPrefab != null)
        {
            string oldPath = AssetDatabase.GetAssetPath(entry.wrapperItemPrefab);
            if (!string.IsNullOrEmpty(oldPath))
                AssetDatabase.DeleteAsset(oldPath);
        }

        // Delete old BoneMap.js (in the registry's scene folder)
        if (generatedFolder != null)
        {
            string oldBoneMapPath = Path.Combine(generatedFolder, $"{entry.avatarID}_BoneMap.js");
            if (File.Exists(oldBoneMapPath))
                AssetDatabase.DeleteAsset(oldBoneMapPath);
        }

        return Build(entry.sourceVrmPrefab, entry, registry);
    }
}
