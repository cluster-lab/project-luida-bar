using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using ClusterVR.CreatorKit.Item.Implements;

public class DataCollectorCreateMenu
{
    // Define the necessary asset paths
    private const string DataCollectorPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/CustomDataCollection/LUIDA-DataCollector.prefab";
    private const string ExpManagersWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/LUIDA-ExpManagers.prefab";
    private const string ConditionManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionManager.prefab";
    private const string IdentifiersAssetPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string CalculatorTemplateAssetPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/CustomDataCollection/CustomDataCalculatorTemplate.js";
    private const string DataCollectorScriptFolderPath = "Assets/_Experiment_/Scripts/DataCollectors/";
    public const string DataCollectorConfigFolderPath = "Assets/_Experiment_/Settings/DataCollectorConfig/";

    /// <summary>
    /// Creates a LUIDA Data Collector instance in the scene from the GameObject menu.
    /// </summary>
    [MenuItem("GameObject/LUIDA/Data Collector", false, 10)]
    public static void CreateDataCollector(MenuCommand menuCommand)
    {
        if (Object.FindObjectOfType<LuidaDataCollector>() != null)
        {
            EditorUtility.DisplayDialog("Error", "A LUIDA Data Collector already exists in this scene. Only one instance is allowed.", "OK");
            return;
        }

        CreateDataCollectorInScene(registerUndo: true, selectObject: true);
    }

    /// <summary>
    /// Creates a LUIDA Data Collector instance in the scene programmatically.
    /// </summary>
    /// <param name="registerUndo">Whether to register undo for the creation.</param>
    /// <param name="selectObject">Whether to select the created object in the hierarchy.</param>
    /// <returns>The created GameObject, or null if creation failed or a collector already exists.</returns>
    public static GameObject CreateDataCollectorInScene(bool registerUndo = true, bool selectObject = false)
    {
        if (Object.FindObjectOfType<LuidaDataCollector>() != null)
        {
            Debug.LogWarning("A LUIDA Data Collector already exists in this scene. Skipping creation.");
            return null;
        }

        GameObject dataCollectorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DataCollectorPrefabPath);
        if (dataCollectorPrefab == null)
        {
            Debug.LogError("LUIDA Data Collector prefab not found at: " + DataCollectorPrefabPath);
            return null;
        }

        GameObject collectorInstance = (GameObject)PrefabUtility.InstantiatePrefab(dataCollectorPrefab);
        collectorInstance.name = "LUIDA-DataCollector";
        var luidaComponent = collectorInstance.GetComponent<LuidaDataCollector>();
        if (!luidaComponent) luidaComponent = collectorInstance.AddComponent<LuidaDataCollector>();

        JavaScriptAsset calculatorAsset = FindOrCreateCalculatorScript();
        if (calculatorAsset != null)
        {
            luidaComponent.calculationScript = calculatorAsset;
            AssignScriptToCombiner(collectorInstance, calculatorAsset);
        }

        EnsureAccessToExpConditions(collectorInstance);
        FindOrCreateBuilderConfig();

        if (registerUndo)
        {
            Undo.RegisterCreatedObjectUndo(collectorInstance, "Create " + collectorInstance.name);
        }

        if (selectObject)
        {
            Selection.activeObject = collectorInstance;
        }

        return collectorInstance;
    }

    private static JavaScriptAsset FindOrCreateCalculatorScript()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string newCalculatorPath = $"{DataCollectorScriptFolderPath}{sceneName}.js";

        if (!Directory.Exists(DataCollectorScriptFolderPath))
        {
            Directory.CreateDirectory(DataCollectorScriptFolderPath);
        }

        if (File.Exists(newCalculatorPath))
        {
            return AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(newCalculatorPath);
        }

        if (!AssetDatabase.CopyAsset(CalculatorTemplateAssetPath, newCalculatorPath))
        {
            Debug.LogError("Failed to copy the Calculator template asset.");
            return null;
        }

        AssetDatabase.Refresh();
        var newCalculatorAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(newCalculatorPath);
        if (newCalculatorAsset == null)
        {
            Debug.LogError("Failed to load the newly created Calculator asset.");
        }
        return newCalculatorAsset;
    }

    private static void AssignScriptToCombiner(GameObject collectorInstance, JavaScriptAsset scriptAsset)
    {
        var scriptCombiner = collectorInstance.GetComponent<ScriptableClusterScriptCombiner>();
        if (scriptCombiner != null)
        {
            var identifiersAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(IdentifiersAssetPath);
            if (identifiersAsset == null)
            {
                Debug.LogError("Failed to load Identifiers asset at: " + IdentifiersAssetPath);
                return;
            }
            scriptCombiner.ReplaceScript(identifiersAsset, 0, null, 0, false);
            scriptCombiner.ReplaceScript(scriptAsset, 2, null, 0, true);
            EditorUtility.SetDirty(scriptCombiner);
            AssetDatabase.SaveAssets();
        }
        else
        {
            Debug.LogError("ScriptableClusterScriptCombiner component not found on the DataCollector instance.");
        }
    }

    private static void EnsureAccessToExpConditions(GameObject dataCollector)
    {
        var itemGroupMember = dataCollector.GetComponent<ItemGroupMember>();
        if(itemGroupMember == null) return;

        foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) != ExpManagersWrapperPrefabPath) continue;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == ConditionManagerPrefabPath)
                {
                    ItemGroupHost host = child.GetComponent<ItemGroupHost>();
                    if (host != null)
                    {
                        var serializedItemGroupMember = new SerializedObject(itemGroupMember);
                        serializedItemGroupMember.FindProperty("host").objectReferenceValue = host;
                        serializedItemGroupMember.ApplyModifiedProperties();
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Idempotently ensures the per-scene LuidaDataCollectorConfig asset exists.
    /// Returns the asset (existing or freshly created). Safe to call from any
    /// editor flow that touches the DataCollector — also surfaced as the public
    /// API for the Inspector and the Config tab.
    /// </summary>
    public static LuidaDataCollectorConfig FindOrCreateBuilderConfig()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName)) return null;

        string configPath = $"{DataCollectorConfigFolderPath}{sceneName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<LuidaDataCollectorConfig>(configPath);
        if (existing != null) return existing;

        if (!Directory.Exists(DataCollectorConfigFolderPath))
        {
            Directory.CreateDirectory(DataCollectorConfigFolderPath);
        }

        var fresh = ScriptableObject.CreateInstance<LuidaDataCollectorConfig>();

        // Seed rawJs from the calculator template as a fallback for users who
        // later opt into Code Mode. Builder mode is the default (schema default
        // useCustomCodeMode = false) — do NOT override here.
        var templateText = AssetDatabase.LoadAssetAtPath<TextAsset>(CalculatorTemplateAssetPath);
        if (templateText != null) fresh.rawJs = templateText.text;

        // Fresh asset is already on the latest schema; mark it migrated.
        fresh.schemaVersion = LuidaDataCollectorConfigMigrator.CurrentSchemaVersion;

        AssetDatabase.CreateAsset(fresh, configPath);
        AssetDatabase.SaveAssets();
        return fresh;
    }
}
