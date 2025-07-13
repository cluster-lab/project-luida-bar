using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ClusterVR.CreatorKit.Item.Implements;

public class DataCollectorConfigTab : EditorWindow
{
    private const string DataCollectorPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/CustomDataCollection/LUIDA-DataCollector.prefab";
    private const string ExpManagersWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/LUIDA-ExpManagers.prefab";
    private const string ConditionManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionManager.prefab";

    private const string IdentifiersAssetPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string CalculatorTemplateAssetPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/CustomDataCollection/CustomDataCalculatorTemplate.js";
    private const string DataCollectorScriptFolderPath = "Assets/_Experiment_/Scripts/DataCollectors/";

    private GameObject dataCollector; // Only one instance allowed
    private JavaScriptAsset calculatorAsset;
    private Vector2 scrollPosition;

    // Store custom data list names and their corresponding calculation scripts
    private string customDataCalculationScript = "return { foo: 'bar' };";
    private bool isSubscribed = false;

    public void OnEnable()
    {
        // Find or create the Custom Data Collector on window enable
        FindOrCreateCustomDataCollector();
        LoadCustomDataScript();

        if (!isSubscribed)
        {
            LuidaConfigWindow.OnEditorClosed += SaveChangesToScript;
            LuidaConfigWindow.OnEditorClosed += OnDisable;
            isSubscribed = true;
        }
    }

    public void OnDisable()
    {
        if (isSubscribed)
        {
            LuidaConfigWindow.OnEditorClosed -= SaveChangesToScript;
            LuidaConfigWindow.OnEditorClosed -= OnDisable;
            isSubscribed = false;
        }
    }

    public void OnGUI()
    {
        GUILayout.Label("Here you can edit the script to define what and how to save custom data", EditorStyles.largeLabel);

        if (dataCollector == null)
        {
            FindOrCreateCustomDataCollector();
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Double click this field to edit the script:");
            GUI.enabled = false; // Disable GUI interaction for the next control
            EditorGUILayout.ObjectField(calculatorAsset, typeof(ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset), false);
            GUI.enabled = true; // Re-enable GUI interaction
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Or edit it directly in the textarea below:");
            customDataCalculationScript = EditorGUILayout.TextArea(customDataCalculationScript, GUILayout.Height(300));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Available variables within the script: ", GUILayout.Width(200));
            EditorGUILayout.LabelField("CONDITION", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.HelpBox("⋅ Values are determined by your configured experimental variables and vary across trials.\n⋅ Only available during the trial states if you have enabled the LUIDA experiment progress automation feature.\n⋅ Use CONDITION[\"variable_name\"] to reference a specific condition within the current trial.", MessageType.Info);

            EditorGUILayout.LabelField("PARTICIPANTS", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("⋅ An array of PlayerHandle of the participants joining this experiment.\n⋅ Use `PARTICIPANTS[0]` to retrieve the first participant, `PARTICIPANTS[1]` to retrieve the second participant, and so on.", MessageType.Info);

            EditorGUILayout.LabelField("COLLECTED_DATA", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("⋅ The collected data you send to the LUIDA data collector using the SendDataToCollector action/function.\n⋅ Use `COLLECTED_DATA[your_data_label]` to retrieve the value.", MessageType.Info);

            EditorGUILayout.Space(30);
            EditorGUILayout.HelpBox("Ensure returning something in the end of the code block.\ne.g., `return { score: 100 };", MessageType.Warning);
        }
    }

    private void FindOrCreateCustomDataCollector()
    {
        FindCustomDataCollector();
        if (dataCollector == null) CreateCustomDataCollector();
        if (calculatorAsset == null) DuplicateAndSetupCalculatorScript();
        EnsureAccessToExpConditions();
    }

    private void FindCustomDataCollector()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == DataCollectorPrefabPath)
            {
                dataCollector = obj;
                calculatorAsset = FindExistingCalculatorScript();
                return;
            }
        }

        dataCollector = null;
        calculatorAsset = null;
    }

    private void CreateCustomDataCollector()
    {
        GameObject dataCollectorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DataCollectorPrefabPath);
        if (dataCollectorPrefab == null)
        {
            Debug.LogError("DataCollector prefab not found at path: " + DataCollectorPrefabPath);
            return;
        }
        GameObject newCollectorInstance = (GameObject)PrefabUtility.InstantiatePrefab(dataCollectorPrefab);
        newCollectorInstance.name = "LUIDA-DataCollector";
        dataCollector = newCollectorInstance;
    }

    private ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset FindExistingCalculatorScript()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string calculatorPath = $"{DataCollectorScriptFolderPath}{sceneName}.js";
        var asset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(calculatorPath);
        return asset;
    }

    private void DuplicateAndSetupCalculatorScript()
    {
        var identifiersAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(IdentifiersAssetPath);
        var calculatorTemplateAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(CalculatorTemplateAssetPath);

        if (identifiersAsset == null || calculatorTemplateAsset == null)
        {
            Debug.LogError("Failed to load Identifiers or Calculator Template assets.");
            return;
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string newCalculatorPath = $"{DataCollectorScriptFolderPath}{sceneName}.js";

        if (!Directory.Exists(DataCollectorScriptFolderPath))
        {
            Directory.CreateDirectory(DataCollectorScriptFolderPath);
        }

        AssetDatabase.CopyAsset(CalculatorTemplateAssetPath, newCalculatorPath);
        AssetDatabase.Refresh();

        var newCalculatorAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(newCalculatorPath);
        if (newCalculatorAsset == null)
        {
            Debug.LogError("Failed to duplicate the Calculator template asset.");
            return;
        }

        AssignScriptToCombiner(dataCollector, newCalculatorAsset);
        calculatorAsset = newCalculatorAsset;
    }

    private void EnsureAccessToExpConditions()
    {
        var itemGroupMember = dataCollector.GetComponent<ItemGroupMember>()
            ?? (dataCollector.AddComponent(typeof(ItemGroupMember)) as ItemGroupMember);

        foreach (GameObject obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
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
                        SerializedObject serializedItemGroupMember = new SerializedObject(itemGroupMember);
                        serializedItemGroupMember.FindProperty("host").objectReferenceValue = host;
                        serializedItemGroupMember.ApplyModifiedProperties();
                    }
                }
            }
        }
    }

    private void SaveChangesToScript()
    {
        if (calculatorAsset == null)
        {
            Debug.LogError("Calculator asset is null.");
            return;
        }

        if (dataCollector == null)
        {
            Debug.LogError("Custom data collector Gameobject is null.");
            return;
        }

        // Write the script content to the file
        string path = AssetDatabase.GetAssetPath(calculatorAsset);
        File.WriteAllText(path, customDataCalculationScript);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var scriptCombiner = dataCollector.GetComponent<ScriptableClusterScriptCombiner>();
        if (scriptCombiner != null)
        {
            scriptCombiner.CombineScripts();
        }
        else
        {
            Debug.LogError("ScriptableClusterScriptCombiner component not found on: " + dataCollector.name);
        }

        Debug.Log($"Custom data collector's script saved to {path}");
    }

    private void AssignScriptToCombiner(GameObject collectorInstance, ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset scriptAsset)
    {
        var scriptCombiner = collectorInstance.GetComponent<ScriptableClusterScriptCombiner>();
        if (scriptCombiner != null)
        {
            var identifiersAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(IdentifiersAssetPath);
            if (identifiersAsset == null)
            {
                Debug.LogError("Failed to load Identifiers asset.");
                return;
            }

            scriptCombiner.ReplaceScript(identifiersAsset, 0, null, 0, false);
            scriptCombiner.ReplaceScript(scriptAsset, 2, null, 0, true);

            EditorUtility.SetDirty(scriptCombiner);
            EditorUtility.SetDirty(identifiersAsset);
            EditorUtility.SetDirty(scriptAsset);

            AssetDatabase.SaveAssets();
        }
        else
        {
            Debug.LogError("ScriptableClusterScriptCombiner component not found on the DataCollector instance.");
        }
    }

    private void LoadCustomDataScript()
    {
        if (calculatorAsset == null)
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(calculatorAsset);
        if (File.Exists(path))
        {
            customDataCalculationScript = File.ReadAllText(path);
        }
        else
        {
            Debug.LogError("Script file not found at path: " + path);
        }
    }
}
