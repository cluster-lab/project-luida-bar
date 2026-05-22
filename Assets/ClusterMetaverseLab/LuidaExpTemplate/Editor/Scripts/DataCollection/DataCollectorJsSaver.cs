#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using ClusterVR.CreatorKit.Item.Implements;

/// <summary>
/// Generates calculator JS from the config, writes it to the scene's
/// DataCollector script asset, and triggers CSCombiner. Shared by the
/// gimmick inspector (one-off Register button) and the Data Collector tab.
/// </summary>
public static class DataCollectorJsSaver
{
    private const string DataCollectorScriptFolderPath = "Assets/_Experiment_/Scripts/DataCollectors/";

    /// <summary>
    /// Regenerate calculator JS from the config and combine. Idempotent;
    /// no-op (with a logged warning) if there is no DataCollector in scene.
    /// </summary>
    public static void WriteAndCombine(LuidaDataCollectorConfig config)
    {
        if (config == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[LUIDA] Scene name unavailable — calculator JS not written.");
            return;
        }

        string scriptPath = $"{DataCollectorScriptFolderPath}{sceneName}.js";
        if (!Directory.Exists(DataCollectorScriptFolderPath))
            Directory.CreateDirectory(DataCollectorScriptFolderPath);

        string js = LuidaDataCollectorJsGenerator.Generate(config);

        // Normalize line endings to LF for clean git diffs across platforms.
        js = js.Replace("\r\n", "\n").Replace("\r", "\n");

        File.WriteAllText(scriptPath, js);
        AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceUpdate);

        var dataCollector = Object.FindObjectOfType<LuidaDataCollector>();
        if (dataCollector == null)
        {
            Debug.LogWarning("[LUIDA] No LUIDA-DataCollector in scene — calculator JS written but not combined.");
            return;
        }

        var combiner = dataCollector.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner == null)
        {
            Debug.LogError("[LUIDA] LUIDA-DataCollector is missing ScriptableClusterScriptCombiner — cannot combine.");
            return;
        }

        var calculatorAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(scriptPath);
        if (calculatorAsset != null && dataCollector.calculationScript != calculatorAsset)
        {
            dataCollector.calculationScript = calculatorAsset;
            EditorUtility.SetDirty(dataCollector);
        }

        combiner.CombineScripts();
    }
}
#endif
