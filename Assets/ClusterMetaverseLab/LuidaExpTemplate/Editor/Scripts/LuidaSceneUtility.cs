using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;
using ClusterVR.CreatorKit.Item.Implements;

// These using statements assume the custom types are available in the project.
// You might need to adjust them based on your project's namespace structure.
using ClusterVR.CreatorKit.Gimmick.Implements; 

public static class LuidaSceneUtility
{
    /// <summary>
    /// Creates a new, "inactive" experiment scene from the template.
    /// </summary>
    public static void CreateNewSceneFromTemplate(string newSceneName)
    {
        string newScenePath = $"{LuidaPaths.ExperimentScenesFolder}/{newSceneName}.unity";

        if (File.Exists(newScenePath))
        {
            EditorUtility.DisplayDialog("Error", "A scene with that name already exists!", "OK");
            return;
        }

        Directory.CreateDirectory(LuidaPaths.ExperimentScenesFolder);
        File.Copy(LuidaPaths.SceneTemplateUnity, newScenePath);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(newScenePath);

        // Create a DataCollector in the new scene (Template doesn't include one)
        DataCollectorCreateMenu.CreateDataCollectorInScene(registerUndo: false, selectObject: false);

        // Mark scene as dirty so changes are saved
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        // After opening the new scene, update the script references within it (fallback if collector exists).
        UpdateDataCollectorScriptCombiner(newSceneName);
    }

    /// <summary>
    /// Duplicates the current experiment scene and all its associated LUIDA assets.
    /// </summary>
    public static void DuplicateCurrentScene(string newSceneName)
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        string newScenePath = $"{LuidaPaths.ExperimentScenesFolder}/{newSceneName}.unity";

        if (File.Exists(newScenePath))
        {
            EditorUtility.DisplayDialog("Error", "A scene with that name already exists!", "OK");
            return;
        }

        DuplicateSceneAndAssets(currentScenePath, newSceneName);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(newScenePath);

        // After opening the new scene, update the script references within it.
        string newStateListenerScriptsFolder = LuidaPaths.SceneStateManagementFolder(newSceneName);
        UpdateScriptableClusterScriptCombiners(newSceneName, newStateListenerScriptsFolder);
        UpdateDataCollectorScriptCombiner(newSceneName);
        RewireAvatarSpawnerForDuplicatedScene(newSceneName);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
    
    private static void DuplicateSceneAndAssets(string currentScenePath, string newSceneName)
    {
        string newScenePath = $"{LuidaPaths.ExperimentScenesFolder}/{newSceneName}.unity";
        File.Copy(currentScenePath, newScenePath, true);

        string currentSceneName = Path.GetFileNameWithoutExtension(currentScenePath);

        // Duplicate StateList asset
        string stateListPath = LuidaPaths.StateListAsset(currentSceneName);
        if (File.Exists(stateListPath))
        {
            string newStateListPath = LuidaPaths.StateListAsset(newSceneName);
            File.Copy(stateListPath, newStateListPath, true);
            RenameAssetObjectToMatchFile(newStateListPath, newSceneName);
        }

        // Duplicate ExperimentVariables asset
        string experimentVariablesPath = LuidaPaths.ExperimentVariablesJs(currentSceneName);
        if (File.Exists(experimentVariablesPath))
        {
            File.Copy(experimentVariablesPath, LuidaPaths.ExperimentVariablesJs(newSceneName), true);
        }

        // Duplicate StateListenersItemData assets
        string stateListenersFolder = $"{LuidaPaths.SceneStateManagementFolder(currentSceneName)}/StateListeners";
        string newStateListenersFolder = $"{LuidaPaths.SceneStateManagementFolder(newSceneName)}/StateListeners";
        if (Directory.Exists(stateListenersFolder))
        {
            Directory.CreateDirectory(newStateListenersFolder);
            foreach (string file in Directory.GetFiles(stateListenersFolder, "*.*", SearchOption.AllDirectories))
            {
                string newFilePath = file.Replace(stateListenersFolder, newStateListenersFolder);
                Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                File.Copy(file, newFilePath, true);
            }
        }

        // Duplicate StateListenersItemData scripts
        string stateListenerScriptsFolder = LuidaPaths.SceneStateManagementFolder(currentSceneName);
        string newStateListenerScriptsFolder = LuidaPaths.SceneStateManagementFolder(newSceneName);
        if (Directory.Exists(stateListenerScriptsFolder))
        {
            Directory.CreateDirectory(newStateListenerScriptsFolder);
            foreach (string file in Directory.GetFiles(stateListenerScriptsFolder, "*.*", SearchOption.AllDirectories))
            {
                string newFilePath = file.Replace(stateListenerScriptsFolder, newStateListenerScriptsFolder);
                Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                File.Copy(file, newFilePath, true);
            }
        }

        // Duplicate DataCollector script
        string dataCollectorScriptPath = LuidaPaths.DataCollectorJs(currentSceneName);
        if (File.Exists(dataCollectorScriptPath))
        {
            File.Copy(dataCollectorScriptPath, LuidaPaths.DataCollectorJs(newSceneName), true);
        }

        // Duplicate DataCollector config asset. Gimmicks resolve this by active
        // scene name (DataCollectorGimmickShared.FindBuilderConfig), so we just
        // need a file present at the new scene's path — no reference rewiring.
        string dcConfigPath = LuidaPaths.DataCollectorConfigAsset(currentSceneName);
        if (File.Exists(dcConfigPath))
        {
            Directory.CreateDirectory(LuidaPaths.DataCollectorConfigFolder);
            string newDcConfigPath = LuidaPaths.DataCollectorConfigAsset(newSceneName);
            File.Copy(dcConfigPath, newDcConfigPath, true);
            RenameAssetObjectToMatchFile(newDcConfigPath, newSceneName);
        }

        // Duplicate the scene-scoped avatar folder by rebuilding wrappers from
        // each entry's source prefab. A plain folder copy can't be used because
        // the spawner's WorldItemTemplateList references wrappers by GUID and a
        // byte-level copy would leave new wrappers pointing at the source scene's
        // BoneMap.js files. Rebuilding produces an internally consistent new set.
        DuplicateAvatarRegistryByRebuild(currentSceneName, newSceneName);
    }

    /// <summary>
    /// File.Copy preserves the source asset's serialized m_Name, which then
    /// disagrees with the new file name and trips Unity's "Object name does not
    /// match file name" inspector warning. Re-import the file so it's tracked,
    /// then update the live Object's name to match.
    /// </summary>
    private static void RenameAssetObjectToMatchFile(string assetPath, string newName)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (obj == null || obj.name == newName) return;
        obj.name = newName;
        EditorUtility.SetDirty(obj);
        AssetDatabase.SaveAssetIfDirty(obj);
    }

    /// <summary>
    /// Rebuilds the source scene's avatar set in the new scene's folder. Source
    /// VRM/humanoid prefabs (registry.entries[i].sourceVrmPrefab) are reused —
    /// they're immutable inputs and sharing them keeps the disk layout clean.
    /// </summary>
    private static void DuplicateAvatarRegistryByRebuild(string currentSceneName, string newSceneName)
    {
        string oldSceneFolder = AvatarsConfigAssetUtil.SanitizeSceneFolderName(currentSceneName);
        string newSceneFolder = AvatarsConfigAssetUtil.SanitizeSceneFolderName(newSceneName);
        if (oldSceneFolder == null || newSceneFolder == null) return;

        var oldRegistry = AssetDatabase.LoadAssetAtPath<AvatarRegistry>(
            AvatarsConfigAssetUtil.GetRegistryPath(oldSceneFolder));
        if (oldRegistry == null || oldRegistry.entries.Count == 0) return;

        AvatarsConfigAssetUtil.EnsureFolderLayout(newSceneFolder);

        string newRegistryPath = AvatarsConfigAssetUtil.GetRegistryPath(newSceneFolder);
        var newRegistry = ScriptableObject.CreateInstance<AvatarRegistry>();
        AssetDatabase.CreateAsset(newRegistry, newRegistryPath);

        foreach (var oldEntry in oldRegistry.entries)
        {
            if (oldEntry == null || oldEntry.sourceVrmPrefab == null) continue;

            var newEntry = new AvatarEntry
            {
                avatarID      = oldEntry.avatarID,
                displayName   = oldEntry.displayName,
                sourceVrmPrefab = oldEntry.sourceVrmPrefab,
                syncFingers   = oldEntry.syncFingers,
                syncFeetToes  = oldEntry.syncFeetToes,
                syncJaw       = oldEntry.syncJaw,
                scaleMode     = oldEntry.scaleMode,
                syncHipsY     = oldEntry.syncHipsY,
                hipsYOffset   = oldEntry.hipsYOffset,
                needsRebuild  = false,
            };

            string wrapperPath = VrmWrapperBuilder.Build(oldEntry.sourceVrmPrefab, newEntry, newRegistry);
            if (wrapperPath == null) continue;
            newEntry.wrapperItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wrapperPath);
            newRegistry.entries.Add(newEntry);
        }

        EditorUtility.SetDirty(newRegistry);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Run after the duplicated scene is opened. Repoints the in-scene spawner's
    /// WorldItemTemplateList at the freshly rebuilt wrappers, and regenerates
    /// AvatarCommandConfig.js into the new scene's Generated folder so the
    /// spawner's CSCombiner references the new scene's config.
    /// </summary>
    private static void RewireAvatarSpawnerForDuplicatedScene(string newSceneName)
    {
        string sceneFolder = AvatarsConfigAssetUtil.SanitizeSceneFolderName(newSceneName);
        if (sceneFolder == null) return;

        var newRegistry = AssetDatabase.LoadAssetAtPath<AvatarRegistry>(
            AvatarsConfigAssetUtil.GetRegistryPath(sceneFolder));
        if (newRegistry == null) return; // source scene had no avatars

        // Rebuilds the template list from registry entries (now pointing at the
        // new scene's wrappers) and also repairs state-listener spawner refs via
        // AddAvatarSpawnerReferenceToAllItems — the dangling-rebind fix added
        // earlier covers the file-copy case where item slots survived but point
        // at the source scene's spawner Item.
        AvatarsConfigAssetUtil.UpdateSpawnerTemplateList(newRegistry);

        // Regenerates AvatarCommandConfig.js into <newScene>/Generated/ and
        // rewires it into the spawner's CSCombiner.
        AvatarsConfigAssetUtil.GenerateAvatarGimmickTriggerConfig();
    }
    
    private static void UpdateScriptableClusterScriptCombiners(string newSceneName, string newStateListenerScriptsFolder)
    {
        var stateListeningItems = GameObject.FindObjectsOfType<LuidaStateListeningItem>();

        foreach (var item in stateListeningItems)
        {
            var scriptCombiner = LuidaCombiner.Get(item.gameObject);
            if (!scriptCombiner.IsAvailable) continue;

            string itemName = item.name;
            string newScriptPath = Path.Combine(newStateListenerScriptsFolder, $"{itemName}.js").Replace("\\", "/");

            var newScriptAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(newScriptPath);
            if (newScriptAsset == null) continue;

            scriptCombiner.ReplaceScript(newScriptAsset, 1, null, 0, false);
            scriptCombiner.Combine();
            LuidaCombiner.MarkDirty(scriptCombiner);
        }
        AssetDatabase.SaveAssets();
    }
    
    private static void UpdateDataCollectorScriptCombiner(string newSceneName)
    {
        var dataCollector = GameObject.FindObjectOfType<LuidaDataCollector>();
        if (dataCollector == null) return;

        var scriptCombiner = LuidaCombiner.Get(dataCollector.gameObject);
        if (!scriptCombiner.IsAvailable) return;

        var newScriptPath = LuidaPaths.DataCollectorJs(newSceneName);
        var newScriptAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(newScriptPath);
        if (newScriptAsset == null)
        {
            var calculatorTemplateAsset = LuidaPaths.Load<JavaScriptAsset>(LuidaPaths.CalculatorTemplateJs);
            if (calculatorTemplateAsset == null)
            {
                return;
            }

            LuidaPaths.EnsureFolder(LuidaPaths.DataCollectorsFolder);

            AssetDatabase.CopyAsset(LuidaPaths.CalculatorTemplateJs, newScriptPath);
            AssetDatabase.Refresh();
            newScriptAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(newScriptPath);
        }

        dataCollector.calculationScript = newScriptAsset;
        EditorUtility.SetDirty(dataCollector);
        scriptCombiner.ReplaceScript(newScriptAsset, 2, null, 0, false);
        scriptCombiner.Combine();
        LuidaCombiner.MarkDirty(scriptCombiner);
        AssetDatabase.SaveAssets();
    }
}
