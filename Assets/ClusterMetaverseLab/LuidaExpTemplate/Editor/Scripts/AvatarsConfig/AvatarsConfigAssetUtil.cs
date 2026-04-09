using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClusterVR.CreatorKit.Item.Implements;

/// <summary>
/// Editor utilities for the LUIDA Avatars system:
/// registry lifecycle, drag-drop handling, spawner installation.
/// </summary>
public static class AvatarsConfigAssetUtil
{
    public const string RegistryPath = "Assets/_Experiment_/Avatars/AvatarRegistry.asset";
    private const string SourceFolder = "Assets/_Experiment_/Avatars/Source";
    private const string WrapperFolder = "Assets/_Experiment_/Avatars/Wrappers";
    private const string GeneratedFolder = "Assets/_Experiment_/Avatars/Generated";
    private const string SpawnerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/LUIDA-AvatarSpawner.prefab";
    private const string AvatarManagerJsPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/AvatarManagement/AvatarManager.js";
    private const string SpawnerObjectName = "LUIDA-AvatarSpawner";

    #region Folder & Registry Lifecycle

    public static void EnsureFolderLayout()
    {
        Directory.CreateDirectory(SourceFolder);
        Directory.CreateDirectory(WrapperFolder);
        Directory.CreateDirectory(GeneratedFolder);
    }

    public static AvatarRegistry EnsureRegistryAsset()
    {
        var registry = AssetDatabase.LoadAssetAtPath<AvatarRegistry>(RegistryPath);
        if (registry != null) return registry;

        EnsureFolderLayout();
        registry = ScriptableObject.CreateInstance<AvatarRegistry>();
        AssetDatabase.CreateAsset(registry, RegistryPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[LuidaAvatars] Created AvatarRegistry at {RegistryPath}");
        return registry;
    }

    #endregion

    #region Drag-Drop Handling

    /// <summary>
    /// Handle files/objects dropped onto the Avatars window drop zone.
    /// Accepts .vrm files (copied + postprocessed) and humanoid .prefab files (wrapped directly).
    /// </summary>
    public static void HandleDrop(Object[] droppedObjects, string[] droppedPaths, AvatarRegistry registry)
    {
        // Handle drag from project (Object references)
        if (droppedObjects != null)
        {
            foreach (var obj in droppedObjects)
            {
                if (obj is GameObject go)
                {
                    string path = AssetDatabase.GetAssetPath(go);
                    if (path.EndsWith(".prefab"))
                    {
                        HandlePrefabDrop(go, registry);
                    }
                }
            }
        }

        // Handle drag from file system (paths)
        if (droppedPaths != null)
        {
            foreach (var path in droppedPaths)
            {
                if (path.EndsWith(".vrm", System.StringComparison.OrdinalIgnoreCase))
                {
                    HandleVrmDrop(path, registry);
                }
                else if (path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null)
                        HandlePrefabDrop(go, registry);
                }
            }
        }
    }

    private static void HandleVrmDrop(string vrmPath, AvatarRegistry registry)
    {
        EnsureFolderLayout();

        string fileName = Path.GetFileName(vrmPath);
        string destPath = Path.Combine(SourceFolder, fileName);

        // Copy VRM into Source folder if not already there
        if (!vrmPath.StartsWith("Assets/"))
        {
            // External file — copy it in
            File.Copy(vrmPath, destPath.Replace("/", "\\"), overwrite: true);
            AssetDatabase.Refresh();
        }
        else if (vrmPath != destPath)
        {
            AssetDatabase.CopyAsset(vrmPath, destPath);
        }

        // UniVRM's vrmAssetPostprocessor will auto-import .vrm → .prefab
        // Wait for it via delayCall polling
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string expectedPrefabPath = Path.Combine(SourceFolder, baseName + ".prefab");

        // Poll for the prefab to appear (postprocessor runs asynchronously)
        int attempts = 0;
        EditorApplication.CallbackFunction pollCallback = null;
        pollCallback = () =>
        {
            attempts++;
            AssetDatabase.Refresh();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expectedPrefabPath);
            if (prefab != null)
            {
                EditorApplication.delayCall -= pollCallback;
                HandlePrefabDrop(prefab, registry);
                return;
            }
            if (attempts > 30) // ~30 frames, give up
            {
                EditorApplication.delayCall -= pollCallback;
                // Try alternate path patterns UniVRM might use
                var guids = AssetDatabase.FindAssets($"t:Prefab {baseName}", new[] { SourceFolder });
                if (guids.Length > 0)
                {
                    var foundPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var foundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(foundPath);
                    if (foundPrefab != null)
                    {
                        HandlePrefabDrop(foundPrefab, registry);
                        return;
                    }
                }
                Debug.LogWarning($"[LuidaAvatars] VRM postprocessor did not produce a prefab for {fileName}. Try importing it manually first.");
            }
            else
            {
                EditorApplication.delayCall += pollCallback;
            }
        };
        EditorApplication.delayCall += pollCallback;
    }

    private static void HandlePrefabDrop(GameObject prefab, AvatarRegistry registry)
    {
        // Validate that it's a humanoid
        var animator = prefab.GetComponentInChildren<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            EditorUtility.DisplayDialog("Not Humanoid",
                $"'{prefab.name}' does not have a humanoid Animator.\nOnly humanoid avatars can be registered.",
                "OK");
            return;
        }

        // Derive avatarID from prefab name
        string avatarID = SanitizeAvatarID(prefab.name);

        // Check for duplicates
        if (registry.FindByID(avatarID) != null)
        {
            if (!EditorUtility.DisplayDialog("Duplicate Avatar",
                $"An avatar with ID '{avatarID}' already exists.\nReplace it?",
                "Replace", "Cancel"))
                return;
            RemoveEntry(avatarID, registry);
        }

        // Create entry
        var entry = new AvatarEntry
        {
            avatarID = avatarID,
            displayName = prefab.name,
            sourceVrmPrefab = prefab,
            syncFingers = false,
            syncFeetToes = false,
            syncJaw = false,
        };

        // Build wrapper
        string wrapperPath = VrmWrapperBuilder.Build(prefab, entry);
        if (wrapperPath == null)
        {
            Debug.LogError("[LuidaAvatars] Wrapper build failed.");
            return;
        }
        entry.wrapperItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wrapperPath);

        // Add to registry
        Undo.RecordObject(registry, "Add Avatar Entry");
        registry.entries.Add(entry);
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();

        // Update spawner WorldItemTemplateList if present in scene
        UpdateSpawnerTemplateList(registry);

        Debug.Log($"[LuidaAvatars] Registered avatar '{avatarID}' from {prefab.name}");
    }

    #endregion

    #region Entry Management

    public static void RemoveEntry(string avatarID, AvatarRegistry registry)
    {
        var entry = registry.FindByID(avatarID);
        if (entry == null) return;

        // Delete generated files
        string boneMapPath = Path.Combine(GeneratedFolder, $"{avatarID}_BoneMap.js");
        if (File.Exists(boneMapPath))
            AssetDatabase.DeleteAsset(boneMapPath);

        if (entry.wrapperItemPrefab != null)
        {
            string wrapperPath = AssetDatabase.GetAssetPath(entry.wrapperItemPrefab);
            if (!string.IsNullOrEmpty(wrapperPath))
                AssetDatabase.DeleteAsset(wrapperPath);
        }

        Undo.RecordObject(registry, "Remove Avatar Entry");
        registry.entries.RemoveAll(e => e.avatarID == avatarID);
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();

        UpdateSpawnerTemplateList(registry);
    }

    public static void RebuildEntry(AvatarEntry entry, AvatarRegistry registry)
    {
        string newPath = VrmWrapperBuilder.Rebuild(entry);
        if (newPath != null)
        {
            entry.wrapperItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            UpdateSpawnerTemplateList(registry);
        }
    }

    public static string SanitizeAvatarID(string raw)
    {
        string sanitized = Regex.Replace(raw, @"[^A-Za-z0-9_]", "");
        if (string.IsNullOrEmpty(sanitized)) sanitized = "avatar";
        return sanitized;
    }

    #endregion

    #region Spawner Management

    /// <summary>
    /// Find the LUIDA-AvatarSpawner in the active scene, or null if not present.
    /// </summary>
    public static GameObject FindSpawnerInScene()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == SpawnerObjectName) return root;
            var found = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.gameObject.name == SpawnerObjectName);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    /// <summary>
    /// Install the AvatarSpawner prefab into the active scene.
    /// Bakes the spawner mode and default avatar into a constants JS header.
    /// </summary>
    public static void InstallSpawnerInActiveScene(string mode, string defaultAvatarID, AvatarRegistry registry)
    {
        if (FindSpawnerInScene() != null)
        {
            Debug.LogWarning("[LuidaAvatars] AvatarSpawner already exists in the scene.");
            return;
        }

        // Try to load from template prefab first; create fresh if not found
        GameObject spawner;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpawnerPrefabPath);
        if (prefab != null)
        {
            spawner = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        else
        {
            // Create from scratch
            spawner = new GameObject(SpawnerObjectName);
            spawner.AddComponent<Item>();
            spawner.AddComponent<ScriptableItem>();
            spawner.AddComponent<ScriptableClusterScriptCombiner>();
        }
        spawner.name = SpawnerObjectName;
        Undo.RegisterCreatedObjectUndo(spawner, "Add Avatar Spawner");

        // Generate constants header JS
        string headerContent = GenerateSpawnerHeader(mode, defaultAvatarID);
        string headerJsPath = Path.Combine(GeneratedFolder, "AvatarSpawnerConfig.js");
        Directory.CreateDirectory(GeneratedFolder);
        File.WriteAllText(headerJsPath, headerContent);
        AssetDatabase.ImportAsset(headerJsPath, ImportAssetOptions.ForceUpdate);

        // Wire up CSCombiner with header + AvatarManager.js
        var combiner = spawner.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner != null)
        {
            combiner.ClearScripts();

            var headerAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(headerJsPath);
            var managerAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(AvatarManagerJsPath);

            if (headerAsset != null) combiner.AppendScript(headerAsset, null);
            if (managerAsset != null) combiner.AppendScript(managerAsset, null);
            combiner.CombineScripts();
            EditorUtility.SetDirty(combiner);
        }

        // Add WorldItemTemplateList with all registered avatars
        var templateList = spawner.GetComponent<WorldItemTemplateList>();
        if (templateList == null)
            templateList = spawner.AddComponent<WorldItemTemplateList>();
        PopulateTemplateList(templateList, registry);

        // Add spawner reference to all existing state-listening items
        ItemsManagerAssetUtil.AddAvatarSpawnerReferenceToAllItems();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[LuidaAvatars] Installed AvatarSpawner in scene (mode: {mode})");
    }

    /// <summary>
    /// Update the spawner mode and default avatar on an existing spawner in the scene.
    /// </summary>
    public static void UpdateSpawnerConfig(string mode, string defaultAvatarID)
    {
        string headerContent = GenerateSpawnerHeader(mode, defaultAvatarID);
        string headerJsPath = Path.Combine(GeneratedFolder, "AvatarSpawnerConfig.js");
        Directory.CreateDirectory(GeneratedFolder);
        File.WriteAllText(headerJsPath, headerContent);
        AssetDatabase.ImportAsset(headerJsPath, ImportAssetOptions.ForceUpdate);

        var spawner = FindSpawnerInScene();
        if (spawner == null) return;

        var combiner = spawner.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner != null)
        {
            var headerAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(headerJsPath);
            if (headerAsset != null)
            {
                combiner.ReplaceScript(headerAsset, 0, null, 0);
                combiner.CombineScripts();
                EditorUtility.SetDirty(combiner);
            }
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    /// <summary>
    /// Sync the spawner's WorldItemTemplateList with the current registry entries.
    /// </summary>
    public static void UpdateSpawnerTemplateList(AvatarRegistry registry)
    {
        var spawner = FindSpawnerInScene();
        if (spawner == null) return;

        var templateList = spawner.GetComponent<WorldItemTemplateList>();
        if (templateList == null)
            templateList = spawner.AddComponent<WorldItemTemplateList>();
        PopulateTemplateList(templateList, registry);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void PopulateTemplateList(WorldItemTemplateList templateList, AvatarRegistry registry)
    {
        var so = new SerializedObject(templateList);
        var prop = so.FindProperty("worldItemTemplates");
        prop.ClearArray();

        for (int i = 0; i < registry.entries.Count; i++)
        {
            var entry = registry.entries[i];
            if (entry.wrapperItemPrefab == null) continue;

            var itemComp = entry.wrapperItemPrefab.GetComponent<Item>();
            if (itemComp == null) continue;

            prop.InsertArrayElementAtIndex(prop.arraySize);
            var element = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            element.FindPropertyRelative("id").stringValue = entry.avatarID;
            element.FindPropertyRelative("worldItemTemplate").objectReferenceValue = itemComp;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(templateList);
    }

    private static string GenerateSpawnerHeader(string mode, string defaultAvatarID)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("// Auto-generated spawner configuration");
        sb.AppendLine($"const SPAWNER_MODE = \"{mode}\";");
        if (!string.IsNullOrEmpty(defaultAvatarID))
            sb.AppendLine($"const DEFAULT_AVATAR_ID = \"{defaultAvatarID}\";");
        else
            sb.AppendLine("const DEFAULT_AVATAR_ID = null;");
        return sb.ToString();
    }

    /// <summary>
    /// Read the current spawner config from the generated JS file.
    /// Returns (mode, defaultAvatarID). Returns ("messageDriven", null) if file not found.
    /// </summary>
    public static (string mode, string defaultAvatarID) ReadCurrentSpawnerConfig()
    {
        string headerJsPath = Path.Combine(GeneratedFolder, "AvatarSpawnerConfig.js");
        if (!File.Exists(headerJsPath))
            return ("messageDriven", null);

        string content = File.ReadAllText(headerJsPath);

        string mode = "messageDriven";
        var modeMatch = System.Text.RegularExpressions.Regex.Match(content, @"SPAWNER_MODE\s*=\s*""(\w+)""");
        if (modeMatch.Success)
            mode = modeMatch.Groups[1].Value;

        string defaultAvatarID = null;
        var avatarMatch = System.Text.RegularExpressions.Regex.Match(content, @"DEFAULT_AVATAR_ID\s*=\s*""(\w+)""");
        if (avatarMatch.Success)
            defaultAvatarID = avatarMatch.Groups[1].Value;

        return (mode, defaultAvatarID);
    }

    #endregion
}
