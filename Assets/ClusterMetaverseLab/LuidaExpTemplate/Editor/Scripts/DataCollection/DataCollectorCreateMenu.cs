using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using ClusterVR.CreatorKit.Item.Implements;

public class DataCollectorCreateMenu
{
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

        GameObject dataCollectorPrefab = LuidaPaths.Load<GameObject>(LuidaPaths.DataCollectorPrefab);
        if (dataCollectorPrefab == null)
        {
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
        string newCalculatorPath = LuidaPaths.DataCollectorJs(sceneName);

        LuidaPaths.EnsureFolder(LuidaPaths.DataCollectorsFolder);

        if (File.Exists(newCalculatorPath))
        {
            return AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(newCalculatorPath);
        }

        if (!AssetDatabase.CopyAsset(LuidaPaths.CalculatorTemplateJs, newCalculatorPath))
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
        var scriptCombiner = LuidaCombiner.Get(collectorInstance);
        if (scriptCombiner.IsAvailable)
        {
            var identifiersAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(LuidaPaths.ExpIdentifiersJs);
            if (identifiersAsset == null)
            {
                Debug.LogError("Failed to load Identifiers asset at: " + LuidaPaths.ExpIdentifiersJs);
                return;
            }
            scriptCombiner.ReplaceScript(identifiersAsset, 0, null, 0, false);
            scriptCombiner.ReplaceScript(scriptAsset, 2, null, 0, true);
            LuidaCombiner.MarkDirty(scriptCombiner);
            AssetDatabase.SaveAssets();
        }
    }

    private static void EnsureAccessToExpConditions(GameObject dataCollector)
    {
        var itemGroupMember = dataCollector.GetComponent<ItemGroupMember>();
        if(itemGroupMember == null) return;

        foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) != LuidaPaths.ExpManagersPrefab) continue;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == LuidaPaths.ConditionManagerPrefab)
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

        string configPath = LuidaPaths.DataCollectorConfigAsset(sceneName);
        var existing = AssetDatabase.LoadAssetAtPath<LuidaDataCollectorConfig>(configPath);
        if (existing != null) return existing;

        LuidaPaths.EnsureFolder(LuidaPaths.DataCollectorConfigFolder);

        var fresh = ScriptableObject.CreateInstance<LuidaDataCollectorConfig>();

        // Seed rawJs from the calculator template as a fallback for users who
        // later opt into Code Mode. Builder mode is the default (schema default
        // useCustomCodeMode = false) — do NOT override here.
        var templateText = LuidaPaths.Load<TextAsset>(LuidaPaths.CalculatorTemplateJs);
        if (templateText != null) fresh.rawJs = templateText.text;

        // Fresh asset is already on the latest schema; mark it migrated.
        fresh.schemaVersion = LuidaDataCollectorConfigMigrator.CurrentSchemaVersion;

        AssetDatabase.CreateAsset(fresh, configPath);
        AssetDatabase.SaveAssets();
        return fresh;
    }
}
