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
    private ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset calculatorAsset;
    private Vector2 scrollPosition;

    // Store custom data list names and their corresponding calculation scripts
    private List<string> customDataListNames = new List<string>();
    private List<string> customDataCalculationScripts = new List<string>();
    private bool isSubscribed = false;

    public void OnEnable()
    {
        // Find or create the Custom Data Collector on window enable
        FindOrCreateCustomDataCollector();
        LoadCustomDataScripts();

        if (!isSubscribed)
        {
            LuidaConfigWindow.OnEditorClosed += TrySaveChangesToScript;
            LuidaConfigWindow.OnEditorClosed += OnDisable;
            LuidaConfigWindow.OnItemsManagerTabLostFocus += TrySaveChangesToScript;
            isSubscribed = true;
        }
    }

    public void OnDisable()
    {
        if (isSubscribed)
        {
            LuidaConfigWindow.OnEditorClosed -= TrySaveChangesToScript;
            LuidaConfigWindow.OnEditorClosed -= OnDisable;
            LuidaConfigWindow.OnItemsManagerTabLostFocus -= TrySaveChangesToScript;
            isSubscribed = false;
        }
    }

    public void OnGUI()
    {
        GUILayout.Label("Data Collector Config", EditorStyles.largeLabel);
        
        if (dataCollector == null)
        {
            FindOrCreateCustomDataCollector();
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Display existing custom data entries
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (customDataListNames.Count == 0 && customDataCalculationScripts.Count == 0)
            {
                customDataListNames.Add("");
                customDataCalculationScripts.Add("");
            }
            for (int i = 0; i < customDataListNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("List Name to save custom data", GUILayout.Width(180));
                customDataListNames[i] = EditorGUILayout.TextField(customDataListNames[i], GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove this Colected Data Entry", GUILayout.Width(150)))
                {
                    // Show warning before removing
                    if (EditorUtility.DisplayDialog("Remove Colected Data Entry",
                            "Are you sure you want to remove this collected data entry?",
                            "Remove", "Cancel"))
                    {
                        customDataListNames.RemoveAt(i);
                        customDataCalculationScripts.RemoveAt(i);
                        break; // Exit the loop after removing
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField("Script to define what and how to save custom data:");
                customDataCalculationScripts[i] = EditorGUILayout.TextArea(customDataCalculationScripts[i], GUILayout.Height(150));
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.Width(250));
                EditorGUILayout.LabelField("Available variables in code blocks: ", GUILayout.Width(200));
                EditorGUILayout.LabelField("CONDITION", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.HelpBox("⋅ Values are determined by your configured experimental variables and vary across trials.\n⋅ Use CONDITION[\"condition_name\"] to reference a specific condition within the current trial.", MessageType.Info);
                EditorGUILayout.Space(30);
                EditorGUILayout.HelpBox("Ensure returning something in the end of the code block.\ne.g., `return { score: 100 };", MessageType.Warning);
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            }

            // Button to add a new custom data entry
            if (GUILayout.Button("Add Custom Data Entry"))
            {
                int entryNumber = customDataListNames.Count + 1;
                string defaultListName = $"customData{entryNumber}";
                string defaultScript = "// Return an object with your custom data fields\nreturn {\n//  cond: CONDITION['sampleVariable'],\n//  ans: $.getStateCompat('global', 'sampleAnswer', 'boolean'),\n  value: 0\n};";
                customDataListNames.Add(defaultListName);
                customDataCalculationScripts.Add(defaultScript);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Save Changes button
            // if (GUILayout.Button("SAVE CHANGES"))
            // {
            //     TrySaveChangesToScript();
            // }
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

    private void TrySaveChangesToScript()
    {
        // Check for empty list names
        bool hasEmptyName = false;
        foreach (string name in customDataListNames)
        {
            if (string.IsNullOrEmpty(name))
            {
                hasEmptyName = true;
                break;
            }
        }

        if (hasEmptyName)
        {
            EditorUtility.DisplayDialog("Error", "List Name cannot be empty.", "OK");
        }
        else
        {
            SaveChangesToScript();
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

        // Construct the JavaScript script content
        StringBuilder scriptBuilder = new StringBuilder();
        scriptBuilder.Append("function calculateData () {\n");
        scriptBuilder.Append("  let returnData = $.state.customData;\n");
        scriptBuilder.Append("  const CONDITION = $.groupState.currentCondition;\n\n");

        for (int i = 0; i < customDataListNames.Count; i++)
        {
            string listName = customDataListNames[i];
            string calculationScript = customDataCalculationScripts[i];

            // Sanitize the list name to be a valid JavaScript function name
            string functionName = "saveData_" + Regex.Replace(listName, "[^a-zA-Z0-9_]", "");

            scriptBuilder.Append($"  function {functionName}() {{\n");
            scriptBuilder.Append(calculationScript);
            scriptBuilder.Append($"\n    return {{}};\n  }}\n");
            scriptBuilder.Append($"  const newRecord_{listName} = {functionName}();\n");

            scriptBuilder.Append($"  if (\"{listName}\" in returnData && Array.isArray(returnData[\"{listName}\"])) {{\n");
            scriptBuilder.Append($"    returnData[\"{listName}\"].push(newRecord_{listName});\n");
            scriptBuilder.Append($"  }} else {{\n");
            scriptBuilder.Append($"    returnData[\"{listName}\"] = [newRecord_{listName}];\n");
            scriptBuilder.Append($"  }}\n\n");
        }

        scriptBuilder.Append("  return returnData;\n");
        scriptBuilder.Append("}\n");

        string scriptContent = scriptBuilder.ToString();

        // Write the script content to the file
        string path = AssetDatabase.GetAssetPath(calculatorAsset);
        File.WriteAllText(path, scriptContent);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        // Update the script
        var scriptCombiner = dataCollector.GetComponent<ScriptableClusterScriptCombiner>();
        if (scriptCombiner != null)
        {
            scriptCombiner.CombineScripts();
        }
        else
        {
            Debug.LogError("ScriptableClusterScriptCombiner component not found on: " + dataCollector.name);
        }

        LoadCustomDataScripts();

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

    private void LoadCustomDataScripts()
    {
        if (calculatorAsset == null)
        {
            return;
        }

        customDataListNames.Clear();
        customDataCalculationScripts.Clear();

        string path = AssetDatabase.GetAssetPath(calculatorAsset);
        if (File.Exists(path))
        {
            string scriptContent = File.ReadAllText(path);

            // Regular expression to find function definitions and their names
            var functionRegex = new Regex(@"function\s+saveData_(\w+)\s*\(\s*\)\s*\{([\s\S]*?)return\s*\{\s*\};\s*\n\s*\ }", RegexOptions.Multiline);
            var matches = functionRegex.Matches(scriptContent);

            foreach (Match match in matches)
            {
                string functionName = match.Groups[1].Value;
                string functionBody = match.Groups[2].Value;

                // Remove 'saveData_' prefix for display
                string listName = Regex.Replace(functionName, "^saveData_", "");

                customDataListNames.Add(listName);
                customDataCalculationScripts.Add(functionBody.Trim('\n').Trim());
            }
        }
        else
        {
            Debug.LogError("Script file not found at path: " + path);
        }
    }
}
