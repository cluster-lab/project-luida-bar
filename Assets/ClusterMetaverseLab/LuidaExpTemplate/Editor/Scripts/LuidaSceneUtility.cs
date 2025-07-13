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
    private const string scenePath = "Assets/_Experiment_/Scenes/";
    private const string templateScenePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scenes/Template.unity";
    private const string CalculatorTemplateAssetPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/CustomDataCollection/CustomDataCalculatorTemplate.js";
    private const string DataCollectorScriptFolderPath = "Assets/_Experiment_/Scripts/DataCollectors/";

    /// <summary>
    /// Creates a new, "inactive" experiment scene from the template.
    /// </summary>
    public static void CreateNewSceneFromTemplate(string newSceneName)
    {
        string newScenePath = Path.Combine(scenePath, newSceneName + ".unity");

        if (File.Exists(newScenePath))
        {
            EditorUtility.DisplayDialog("Error", "A scene with that name already exists!", "OK");
            return;
        }

        Directory.CreateDirectory(scenePath);
        File.Copy(templateScenePath, newScenePath);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(newScenePath);
        
        // After opening the new scene, update the script references within it.
        UpdateDataCollectorScriptCombiner(newSceneName);
    }

    /// <summary>
    /// Duplicates the current experiment scene and all its associated LUIDA assets.
    /// </summary>
    public static void DuplicateCurrentScene(string newSceneName)
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        string newScenePath = Path.Combine(scenePath, newSceneName + ".unity");
        
        if (File.Exists(newScenePath))
        {
            EditorUtility.DisplayDialog("Error", "A scene with that name already exists!", "OK");
            return;
        }

        DuplicateSceneAndAssets(currentScenePath, newSceneName);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(newScenePath);
        
        // After opening the new scene, update the script references within it.
        string newStateListenerScriptsFolder = $"Assets/_Experiment_/Scripts/StateManagement/{newSceneName}";
        UpdateScriptableClusterScriptCombiners(newSceneName, newStateListenerScriptsFolder);
        UpdateDataCollectorScriptCombiner(newSceneName);
    }
    
    private static void DuplicateSceneAndAssets(string currentScenePath, string newSceneName)
    {
        string newScenePath = Path.Combine(scenePath, newSceneName + ".unity");
        File.Copy(currentScenePath, newScenePath, true);

        string currentSceneName = Path.GetFileNameWithoutExtension(currentScenePath);

        // Duplicate StateList asset
        string stateListPath = $"Assets/_Experiment_/Settings/StateList/{currentSceneName}.asset";
        if (File.Exists(stateListPath))
        {
            File.Copy(stateListPath, $"Assets/_Experiment_/Settings/StateList/{newSceneName}.asset", true);
        }

        // Duplicate ExperimentVariables asset
        string experimentVariablesPath = $"Assets/_Experiment_/Settings/ExperimentVariables/{currentSceneName}.js";
        if (File.Exists(experimentVariablesPath))
        {
            File.Copy(experimentVariablesPath, $"Assets/_Experiment_/Settings/ExperimentVariables/{newSceneName}.js", true);
        }

        // Duplicate StateListenersItemData assets
        string stateListenersFolder = $"Assets/_Experiment_/Scripts/StateManagement/{currentSceneName}/StateListeners";
        string newStateListenersFolder = $"Assets/_Experiment_/Scripts/StateManagement/{newSceneName}/StateListeners";
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
        string stateListenerScriptsFolder = $"Assets/_Experiment_/Scripts/StateManagement/{currentSceneName}";
        string newStateListenerScriptsFolder = $"Assets/_Experiment_/Scripts/StateManagement/{newSceneName}";
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
        string dataCollectorScriptPath = $"Assets/_Experiment_/Scripts/DataCollectors/{currentSceneName}.js";
        if (File.Exists(dataCollectorScriptPath))
        {
            File.Copy(dataCollectorScriptPath, $"Assets/_Experiment_/Scripts/DataCollectors/{newSceneName}.js", true);
        }
    }
    
    private static void UpdateScriptableClusterScriptCombiners(string newSceneName, string newStateListenerScriptsFolder)
    {
        var stateListeningItems = GameObject.FindObjectsOfType<LuidaStateListeningItem>();

        foreach (var item in stateListeningItems)
        {
            var scriptCombiner = item.GetComponent<ScriptableClusterScriptCombiner>();
            if (scriptCombiner == null) continue;

            string itemName = item.name;
            string newScriptPath = Path.Combine(newStateListenerScriptsFolder, $"{itemName}.js").Replace("\\", "/");

            var newScriptAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(newScriptPath);
            if (newScriptAsset == null) continue;

            scriptCombiner.ReplaceScript(newScriptAsset, 1, null, 0, false);
            scriptCombiner.CombineScripts();
            EditorUtility.SetDirty(scriptCombiner);
        }
        AssetDatabase.SaveAssets();
    }
    
    private static void UpdateDataCollectorScriptCombiner(string newSceneName)
    {
        var dataCollector = GameObject.FindObjectOfType<LuidaDataCollector>();
        if (dataCollector == null) return;

        var scriptCombiner = dataCollector.GetComponent<ScriptableClusterScriptCombiner>();
        if (scriptCombiner == null) return;

        var newScriptPath = $"{DataCollectorScriptFolderPath}{newSceneName}.js";
        var newScriptAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(newScriptPath);
        if (newScriptAsset == null)
        {
            var calculatorTemplateAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(CalculatorTemplateAssetPath);
            if (calculatorTemplateAsset == null)
            {
                Debug.LogError("Failed to load Identifiers or Calculator Template assets.");
                return;
            }

            if (!Directory.Exists(DataCollectorScriptFolderPath))
            {
                Directory.CreateDirectory(DataCollectorScriptFolderPath);
            }

            AssetDatabase.CopyAsset(CalculatorTemplateAssetPath, newScriptPath);
            AssetDatabase.Refresh();
        }

        dataCollector.calculationScript = newScriptAsset;
        scriptCombiner.ReplaceScript(newScriptAsset, 2, null, 0, false);
        scriptCombiner.CombineScripts();
        EditorUtility.SetDirty(scriptCombiner);
        AssetDatabase.SaveAssets();
    }
}
