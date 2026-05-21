using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ClusterVR.CreatorKit.Item.Implements;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Gimmick.Implements;
using ClusterVR.CreatorKit.Operation.Implements;
using ClusterVR.CreatorKit.World.Implements.WorldRuntimeSetting;

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
        // Track processed asset paths to avoid handling the same prefab twice
        // (Unity populates both objectReferences and paths when dragging from
        // the Project window).
        var processedPaths = new System.Collections.Generic.HashSet<string>();

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
                        processedPaths.Add(path);
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
                if (processedPaths.Contains(path))
                    continue;

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
        // Also check for spawner renamed by previous bug ("LuidaResetGlobalBool")
        string[] names = { SpawnerObjectName, "LuidaResetGlobalBool" };
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var name in names)
            {
                if (root.name == name)
                {
                    if (root.name != SpawnerObjectName) root.name = SpawnerObjectName; // Auto-fix
                    return root;
                }
                var found = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.gameObject.name == name);
                if (found != null)
                {
                    if (found.gameObject.name != SpawnerObjectName) found.gameObject.name = SpawnerObjectName;
                    return found.gameObject;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Install the AvatarSpawner into the active scene.
    /// Message-driven only — responds to gimmick integer commands and direct messages.
    /// </summary>
    public static void InstallSpawnerInActiveScene(AvatarRegistry registry)
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

        // Ensure WorldRuntimeSetting: custom clipping planes with near=0.1
        var wrs = spawner.GetComponent<WorldRuntimeSetting>();
        if (wrs == null)
        {
            wrs = spawner.AddComponent<WorldRuntimeSetting>();
            var so = new SerializedObject(wrs);
            so.FindProperty("useCustomClippingPlanes").boolValue = true;
            so.FindProperty("nearPlane").floatValue = 0.1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Wire up CSCombiner with AvatarManager.js (config will be added by GenerateAvatarGimmickTriggerConfig)
        var combiner = spawner.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner != null)
        {
            combiner.ClearScripts();
            var managerAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(AvatarManagerJsPath);
            if (managerAsset != null) combiner.AppendScript(managerAsset, null);
            combiner.CombineScripts();
            EditorUtility.SetDirty(combiner);
        }

        // Add WorldItemTemplateList with all registered avatars
        var templateList = spawner.GetComponent<WorldItemTemplateList>();
        if (templateList == null)
            templateList = spawner.AddComponent<WorldItemTemplateList>();
        PopulateTemplateList(templateList, registry);

        // Add ItemGroupMember so the spawner can access $.groupState
        AddItemGroupMemberToSpawner(spawner);

        // Add spawner reference to all existing state-listening items
        ItemsManagerAssetUtil.AddAvatarSpawnerReferenceToAllItems();

        // Generate gimmick trigger config for any avatar gimmick instances in the scene
        GenerateAvatarGimmickTriggerConfig();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[LuidaAvatars] Installed AvatarSpawner in scene");
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

        // Repair any state-listener worldItemReferences whose item slot went null
        // after a previous spawner GameObject was deleted/recreated.
        ItemsManagerAssetUtil.AddAvatarSpawnerReferenceToAllItems();

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

    /// <summary>
    /// Add an ItemGroupMember to the spawner and link it to the ConditionManager's ItemGroupHost,
    /// so that the spawner can access $.groupState (needed for participant resolution).
    /// </summary>
    private static void AddItemGroupMemberToSpawner(GameObject spawner)
    {
        var itemGroupMember = spawner.GetComponent<ItemGroupMember>()
            ?? (spawner.AddComponent(typeof(ItemGroupMember)) as ItemGroupMember);

        foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            string prefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(obj));
            if (prefabPath != ExpManagersWrapperPrefabPath) continue;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == ConditionManagerPrefabPath)
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

    private const string ExpManagersWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/LUIDA-ExpManagers.prefab";
    private const string ConditionManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionManager.prefab";

    #endregion

    #region Avatar Gimmick Trigger Config

    private const string CommandConfigFileName = "AvatarCommandConfig.js";

    /// <summary>
    /// Generates AVATAR_INDEX_MAP (avatar ID array ordered by registry index) and
    /// installs fixed reset GlobalLogic components on the spawner for the integer command system.
    /// </summary>
    public static void GenerateAvatarGimmickTriggerConfig()
    {
        Directory.CreateDirectory(GeneratedFolder);
        string configPath = Path.Combine(GeneratedFolder, CommandConfigFileName);

        // Generate AVATAR_INDEX_MAP from registry
        var registry = AssetDatabase.LoadAssetAtPath<AvatarRegistry>(RegistryPath);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("// Auto-generated avatar command config");
        sb.Append("const AVATAR_INDEX_MAP = [");
        if (registry != null && registry.entries.Count > 0)
        {
            for (int i = 0; i < registry.entries.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append($"\"{EscapeJs(registry.entries[i].avatarID)}\"");
            }
        }
        sb.AppendLine("];");

        File.WriteAllText(configPath, sb.ToString());
        AssetDatabase.ImportAsset(configPath, ImportAssetOptions.ForceUpdate);

        // Also delete old AvatarGimmickTriggers.js if present
        string oldConfigPath = Path.Combine(GeneratedFolder, "AvatarGimmickTriggers.js");
        if (File.Exists(oldConfigPath))
            AssetDatabase.DeleteAsset(oldConfigPath);

        // Ensure the config file is in the AvatarSpawner's CSCombiner
        WireGimmickConfigIntoSpawner(configPath);

        // Add fixed reset GlobalLogic components to the spawner
        var spawner = FindSpawnerInScene();
        if (spawner != null)
        {
            AddResetGlobalLogicToSpawner(spawner);
        }
    }



    private static string EscapeJs(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

    private static void WireGimmickConfigIntoSpawner(string configPath)
    {
        var spawner = FindSpawnerInScene();
        if (spawner == null) return;

        var combiner = spawner.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner == null) return;

        var configAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(configPath);
        if (configAsset == null) return;

        var scripts = combiner.GetClusterScripts();
        if (scripts == null) scripts = new List<JavaScriptAsset>();

        // Check if already present — if so, replace in-place to pick up changes
        int existingIdx = scripts.IndexOf(configAsset);
        if (existingIdx >= 0)
        {
            combiner.ReplaceScript(configAsset, existingIdx, null, 0);
            combiner.CombineScripts();
            EditorUtility.SetDirty(combiner);
            return;
        }

        // Rebuild the full script list: [commandConfig, manager]
        var managerAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(AvatarManagerJsPath);

        combiner.ClearScripts();
        combiner.AppendScript(configAsset, null);
        if (managerAsset != null) combiner.AppendScript(managerAsset, null);
        combiner.CombineScripts();
        EditorUtility.SetDirty(combiner);
    }

    private const string ResetTemplatePath = "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/ResetGlobalBool";

    /// <summary>
    /// Adds exactly 2 hidden reset GlobalLogic components to the spawner:
    /// one for "luida_avatar_cmd" and one for "luida_avatar_participant".
    /// Both listen for the same "luida_avatar_cmd_reset" signal on Item scope
    /// and set their respective global integer state to 0.
    /// </summary>
    private static void AddResetGlobalLogicToSpawner(GameObject spawner)
    {
        RemoveOldResetComponents(spawner);

        GameObject templatePrefab = (GameObject)Resources.Load(ResetTemplatePath);
        if (templatePrefab == null)
        {
            Debug.LogWarning("[LuidaAvatars] ResetGlobalBool prefab not found. Gimmick re-triggering will not work.");
            return;
        }

        GlobalLogic templateComponent = templatePrefab.GetComponent<GlobalLogic>();
        if (templateComponent == null)
        {
            Debug.LogWarning("[LuidaAvatars] ResetGlobalBool prefab has no GlobalLogic component.");
            return;
        }

        var spawnerItem = spawner.GetComponent<Item>();

        string[] stateKeys = { "luida_avatar_cmd", "luida_avatar_participant" };
        foreach (string stateKey in stateKeys)
        {
            // Deep copy from template using EditorUtility.CopySerialized
            GlobalLogic resetLogic = spawner.AddComponent<GlobalLogic>();
            EditorUtility.CopySerialized(templateComponent, resetLogic);

            // Patch globalGimmickKey: listen for "luida_avatar_cmd_reset" signal on Item scope
            var gimmickKey = System.Activator.CreateInstance(typeof(GlobalGimmickKey));
            var keyField = typeof(GlobalGimmickKey).GetField("key", BindingFlags.NonPublic | BindingFlags.Instance);
            var itemField = typeof(GlobalGimmickKey).GetField("item", BindingFlags.NonPublic | BindingFlags.Instance);

            if (keyField != null)
                keyField.SetValue(gimmickKey, new GimmickKey(GimmickTarget.Item, "luida_avatar_cmd_reset"));
            if (itemField != null)
                itemField.SetValue(gimmickKey, spawnerItem);

            resetLogic.GetType().GetField("globalGimmickKey", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(resetLogic, gimmickKey);

            // Patch to set the global integer state to 0
            LuidaFakeGimmick.PatchStatementToInteger(resetLogic, stateKey, 0);

            resetLogic.hideFlags = HideFlags.HideInInspector;
            EditorUtility.SetDirty(resetLogic);
        }
    }

    private static void RemoveOldResetComponents(GameObject spawner)
    {
        var allGlobalLogics = spawner.GetComponents<GlobalLogic>();
        foreach (var gl in allGlobalLogics)
        {
            if (gl.hideFlags == HideFlags.HideInInspector)
            {
                Object.DestroyImmediate(gl);
            }
        }
    }

    #endregion
}
