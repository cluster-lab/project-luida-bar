#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Single source of truth for every file path LUIDA's editor tooling touches.
///
/// Package-internal assets (prefabs, template scripts, docs, fonts) resolve
/// relative to a dynamically discovered package root, so they keep working
/// whether LUIDA lives under Assets/ during development or under Packages/ once
/// it ships as a Unity package. User output always lives in the consuming
/// project's writable Assets/_Experiment_/ tree.
/// </summary>
public static class LuidaPaths
{
    // Stable GUID of a package-shipped file (StateListeningItemBase.js) used to
    // locate the package root when PackageInfo can't (i.e. embedded under Assets/).
    private const string AnchorGuid = "695ee6946997a415aac1845484e25dee";
    private const string AnchorRelativePath = "Runtime/Scripts/StateManagement/StateListeningItemBase.js";

    private static string _packageRoot;
    private static bool _packageRootResolved;

    /// <summary>
    /// Folder containing this package's Runtime/ and Editor/ trees (e.g.
    /// "Assets/ClusterMetaverseLab/LuidaExpTemplate" or
    /// "Packages/com.cluster-lab.luida-exp-template"). Resolved once and cached.
    /// Returns null (with a logged error) if it can't be located.
    /// </summary>
    public static string PackageRoot
    {
        get
        {
            if (_packageRootResolved) return _packageRoot;
            _packageRootResolved = true;
            _packageRoot = ResolvePackageRoot();
            if (_packageRoot == null)
            {
                Debug.LogError("[LUIDA] Could not locate the LUIDA package root. " +
                               "The package may be incompletely imported or its anchor asset was removed.");
            }
            return _packageRoot;
        }
    }

    private static string ResolvePackageRoot()
    {
        // 1) Installed as a UPM package (resolves once the code is in its own asmdef).
        var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(LuidaPaths).Assembly);
        if (pkg != null && !string.IsNullOrEmpty(pkg.assetPath)) return pkg.assetPath;

        // 2) Embedded under Assets/ — derive the root from the anchor asset's path.
        var anchorPath = AssetDatabase.GUIDToAssetPath(AnchorGuid);
        if (!string.IsNullOrEmpty(anchorPath) &&
            anchorPath.EndsWith(AnchorRelativePath, StringComparison.Ordinal))
        {
            // Trim "/<AnchorRelativePath>" to leave the package root.
            return anchorPath.Substring(0, anchorPath.Length - AnchorRelativePath.Length - 1);
        }

        return null;
    }

    // --- Package-internal assets (ship with LUIDA) --------------------------

    /// <summary>Resolves a package-relative path to a full asset path, or null if the root is unknown.</summary>
    public static string Internal(string relativePath)
        => PackageRoot == null ? null : $"{PackageRoot}/{relativePath}";

    // Prefabs
    public static string ExpManagersPrefab          => Internal("Runtime/Prefabs/LUIDA-ExpManagers.prefab");
    // Optional: this prefab is not shipped. AvatarsConfigAssetUtil.InstallSpawnerInActiveScene
    // builds the spawner from scratch when it is absent, so it is validated as optional.
    public static string AvatarSpawnerPrefab        => Internal("Runtime/Prefabs/LUIDA-AvatarSpawner.prefab");
    public static string ParticipantManagerPrefab   => Internal("Runtime/Prefabs/ParticipantManager.prefab");
    public static string QuestionnairePrefab        => Internal("Runtime/Prefabs/Questionnaire/Questionnaire.prefab");
    public static string StatePrefab                => Internal("Runtime/Prefabs/StateManagement/State.prefab");
    public static string TrialRestStatePrefab       => Internal("Runtime/Prefabs/StateManagement/Trial - Rest State.prefab");
    public static string StateListeningItemPrefab   => Internal("Runtime/Prefabs/StateManagement/StateListeningItem.prefab");
    public static string DataCollectorPrefab        => Internal("Runtime/Prefabs/CustomDataCollection/LUIDA-DataCollector.prefab");
    public static string ConditionManagerPrefab     => Internal("Runtime/Prefabs/ConditionManagement/ConditionManager.prefab");

    // Templates & base scripts
    public static string ExpIdentifiersTemplateJs     => Internal("Runtime/ExpSettings/ExpIdentifiers.js");
    public static string VariablesTemplateJs          => Internal("Runtime/ExpSettings/VariablesTemplate.js");
    public static string StateListTemplateAsset       => Internal("Runtime/ExpSettings/StateList/Template.asset");
    public static string SceneTemplateUnity           => Internal("Runtime/Scenes/Template.unity");
    public static string CalculatorTemplateJs         => Internal("Runtime/Scripts/CustomDataCollection/CustomDataCalculatorTemplate.js");
    public static string StateListeningItemTemplateJs => Internal("Runtime/Scripts/StateManagement/StateListeningItemTemplate.js");
    public static string AvatarManagerJs              => Internal("Runtime/Scripts/AvatarManagement/AvatarManager.js");
    public static string AvatarSyncCloneJs            => Internal("Runtime/Scripts/AvatarManagement/AvatarSyncClone.js");
    public static string ConditionManagerSourceJs     => Internal("Runtime/Scripts/ConditionManagement/ConditionManager.js");

    // Docs & fonts (relocated into the package tree — see refactor plan Part 5).
    public static string StateListeningItemDocMd      => Internal("Doc/LUIDA-StateListeningItemScriptDoc.md");
    public static string DataCollectorDocMd           => Internal("Doc/LUIDA-DataCollectorScriptDoc.md");
    public static string CodeFontTtf                  => Internal("Fonts/FiraCode-Regular.ttf");

    // --- User output (consuming project's Assets/, always writable) ---------

    public const string ExperimentRoot = "Assets/_Experiment_";

    public static string ExperimentScenesFolder              => $"{ExperimentRoot}/Scenes";
    public static string SettingsFolder                      => $"{ExperimentRoot}/Settings";
    public static string ExpIdentifiersJs                    => $"{ExperimentRoot}/Settings/ExpIdentifiers.js";
    public static string StateListFolder                     => $"{ExperimentRoot}/Settings/StateList";
    public static string StateListAsset(string scene)        => $"{StateListFolder}/{scene}.asset";
    public static string ExperimentVariablesFolder           => $"{ExperimentRoot}/Settings/ExperimentVariables";
    public static string ExperimentVariablesJs(string scene) => $"{ExperimentVariablesFolder}/{scene}.js";
    public static string DataCollectorConfigFolder           => $"{ExperimentRoot}/Settings/DataCollectorConfig";
    public static string DataCollectorConfigAsset(string scene) => $"{DataCollectorConfigFolder}/{scene}.asset";
    public static string StateManagementFolder               => $"{ExperimentRoot}/Scripts/StateManagement";
    public static string SceneStateManagementFolder(string scene) => $"{StateManagementFolder}/{scene}";
    public static string DataCollectorsFolder                => $"{ExperimentRoot}/Scripts/DataCollectors";
    public static string DataCollectorJs(string scene)       => $"{DataCollectorsFolder}/{scene}.js";
    public static string AvatarsRoot                         => $"{ExperimentRoot}/Avatars";

    // --- Safe access + error handling ---------------------------------------

    /// <summary>
    /// Loads a package-internal asset (pass a path from the accessors above).
    /// Logs one actionable error and returns null if the root or asset is missing,
    /// instead of letting a NullReferenceException surface deep in a call site.
    /// </summary>
    public static T Load<T>(string resolvedPath) where T : Object
    {
        if (string.IsNullOrEmpty(resolvedPath)) return null; // PackageRoot error already logged
        var asset = AssetDatabase.LoadAssetAtPath<T>(resolvedPath);
        if (asset == null)
        {
            Debug.LogError($"[LUIDA] Required package asset not found: {resolvedPath}. " +
                           "Reimport or repair the LUIDA package.");
        }
        return asset;
    }

    /// <summary>True if an asset exists at the resolved path (used by the installation validator).</summary>
    public static bool Exists(string resolvedPath)
        => !string.IsNullOrEmpty(resolvedPath) && AssetDatabase.LoadAssetAtPath<Object>(resolvedPath) != null;

    /// <summary>Creates an Assets-relative folder if absent. Returns the path for chaining.</summary>
    public static string EnsureFolder(string assetFolderPath)
    {
        if (!string.IsNullOrEmpty(assetFolderPath) && !Directory.Exists(assetFolderPath))
        {
            Directory.CreateDirectory(assetFolderPath);
        }
        return assetFolderPath;
    }

    /// <summary>
    /// True if the path lives inside the (potentially read-only) package tree —
    /// i.e. it must not be written to. Used to guard in-place file rewrites.
    /// </summary>
    public static bool IsInsidePackage(string assetPath)
    {
        var root = PackageRoot;
        if (root == null || string.IsNullOrEmpty(assetPath)) return false;
        return assetPath.Replace('\\', '/').StartsWith(root + "/", StringComparison.Ordinal);
    }

    /// <summary>
    /// (name, path) pairs for every package-internal asset LUIDA needs at runtime.
    /// Drives "LUIDA &gt; Validate installation".
    /// </summary>
    public static IEnumerable<KeyValuePair<string, string>> RequiredInternalAssets()
    {
        yield return new KeyValuePair<string, string>("LUIDA-ExpManagers prefab", ExpManagersPrefab);
        yield return new KeyValuePair<string, string>("Participant manager prefab", ParticipantManagerPrefab);
        yield return new KeyValuePair<string, string>("Questionnaire prefab", QuestionnairePrefab);
        yield return new KeyValuePair<string, string>("State prefab", StatePrefab);
        yield return new KeyValuePair<string, string>("Trial - Rest State prefab", TrialRestStatePrefab);
        yield return new KeyValuePair<string, string>("State-listening item prefab", StateListeningItemPrefab);
        yield return new KeyValuePair<string, string>("Data collector prefab", DataCollectorPrefab);
        yield return new KeyValuePair<string, string>("Condition manager prefab", ConditionManagerPrefab);
        yield return new KeyValuePair<string, string>("ExpIdentifiers template", ExpIdentifiersTemplateJs);
        yield return new KeyValuePair<string, string>("Variables template", VariablesTemplateJs);
        yield return new KeyValuePair<string, string>("StateList template", StateListTemplateAsset);
        yield return new KeyValuePair<string, string>("Scene template", SceneTemplateUnity);
        yield return new KeyValuePair<string, string>("Calculator template", CalculatorTemplateJs);
        yield return new KeyValuePair<string, string>("State-listening item template", StateListeningItemTemplateJs);
        yield return new KeyValuePair<string, string>("Avatar manager JS", AvatarManagerJs);
        yield return new KeyValuePair<string, string>("Avatar sync clone JS", AvatarSyncCloneJs);
        yield return new KeyValuePair<string, string>("Condition manager JS", ConditionManagerSourceJs);
        yield return new KeyValuePair<string, string>("State-listening item doc", StateListeningItemDocMd);
        yield return new KeyValuePair<string, string>("Data collector doc", DataCollectorDocMd);
        yield return new KeyValuePair<string, string>("Code font", CodeFontTtf);
    }

    /// <summary>
    /// Package-internal assets that are OPTIONAL: LUIDA falls back to building them
    /// at runtime if absent, so a missing one is not an installation error.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, string>> OptionalInternalAssets()
    {
        // AvatarsConfigAssetUtil.InstallSpawnerInActiveScene builds the spawner from
        // scratch (Item + ScriptableItem + combiner) when this prefab is missing.
        yield return new KeyValuePair<string, string>("Avatar spawner prefab", AvatarSpawnerPrefab);
    }
}
#endif
