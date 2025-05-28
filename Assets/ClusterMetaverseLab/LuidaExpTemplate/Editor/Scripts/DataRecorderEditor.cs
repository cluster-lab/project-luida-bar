using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class DataRecorderEditor : EditorWindow
{
    private const string DataRecorderPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/CustomDataRecording/CustomDataRecorder.prefab";
    private const string RequiredObjectsWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";
    private const string WorldItemRefListObjectName = "WorldItemRefList";

    private const string IdentifiersAssetPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string CalculatorTemplateAssetPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/CustomDataRecording/CustomDataCalculatorTemplate.js";
    private const string DataRecorderScriptFolderPath = "Assets/_Experiment_/Scripts/DataRecorder/";

    private GameObject customDataRecorder; // Only one instance allowed
    private ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset calculatorAsset;
    private Vector2 scrollPosition;

    // Store custom data list names and their corresponding calculation scripts
    private List<string> customDataListNames = new List<string>();
    private List<string> customDataCalculationScripts = new List<string>();
    private bool isSubscribed = false;

    public void OnEnable()
    {
        // Find or create the Custom Data Recorder on window enable
        FindOrCreateCustomDataRecorder();
        LoadCustomDataScripts();

        if (!isSubscribed)
        {
            TabbedEditor.OnEditorClosed += TrySaveChangesToScript;
            TabbedEditor.OnEditorClosed += OnDisable;
            TabbedEditor.OnItemsManagerTabLostFocus += TrySaveChangesToScript;
            isSubscribed = true;
        }
    }

    public void OnDisable()
    {
        if (isSubscribed)
        {
            TabbedEditor.OnEditorClosed -= TrySaveChangesToScript;
            TabbedEditor.OnEditorClosed -= OnDisable;
            TabbedEditor.OnItemsManagerTabLostFocus -= TrySaveChangesToScript;
            isSubscribed = false;
        }
    }

    public void OnGUI()
    {
        GUILayout.Label("Custom Data Recorder Editor", EditorStyles.largeLabel);
        
        if (customDataRecorder == null)
        {
            FindOrCreateCustomDataRecorder();
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
                if (GUILayout.Button("Remove this recorder", GUILayout.Width(150)))
                {
                    // Show warning before removing
                    if (EditorUtility.DisplayDialog("Remove Custom Data Entry",
                            "Are you sure you want to remove this custom data entry?",
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

    private void FindOrCreateCustomDataRecorder()
    {
        FindCustomDataRecorder();

        if (customDataRecorder == null)
        {
            CreateCustomDataRecorder();
        }
        EnsureCalculatorScriptExists();
    }

    private void FindCustomDataRecorder()
    {
        customDataRecorder = null;
        calculatorAsset = null;

        GameObject expRequiredObjectsWrapper = FindRequiredObjectsWrapperInstance();
        if (expRequiredObjectsWrapper != null)
        {
            Transform dataRecorderTransform = expRequiredObjectsWrapper.transform.Find("CustomDataRecorder");
            if (dataRecorderTransform != null)
            {
                customDataRecorder = dataRecorderTransform.gameObject;
                LinkCalculatorScript(customDataRecorder);
                return;
            }
        }
    }

    private void CreateCustomDataRecorder()
    {
        GameObject customDataRecorderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DataRecorderPrefabPath);
        if (customDataRecorderPrefab == null)
        {
            Debug.LogError("CustomDataRecorder prefab not found at path: " + DataRecorderPrefabPath);
            return;
        }

        GameObject newRecorderInstance = (GameObject)PrefabUtility.InstantiatePrefab(customDataRecorderPrefab);
        newRecorderInstance.name = "CustomDataRecorder";

        GameObject expRequiredObjectsWrapper = FindRequiredObjectsWrapperInstance();
        if (expRequiredObjectsWrapper != null)
        {
            GameObject worldItemRefList = expRequiredObjectsWrapper.transform.Find(WorldItemRefListObjectName)?.gameObject;
            if (worldItemRefList != null)
            {
                var worldItemReferenceList = worldItemRefList.GetComponent<ClusterVR.CreatorKit.Item.Implements.WorldItemReferenceList>();
                if (worldItemReferenceList != null)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(worldItemReferenceList);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(newRecorderInstance);
                }
                else
                {
                    Debug.LogError("WorldItemReferenceList component not found in WorldItemRefList.");
                }
            }
            else
            {
                Debug.LogError($"WorldItemRefList GameObject not found in {expRequiredObjectsWrapper.name}.");
            }
            customDataRecorder.transform.SetParent(expRequiredObjectsWrapper.transform);
        }
        else
        {
            Debug.LogError("ExpTemplateRequiredObjects prefab instance not found in the scene.");
        }

        DuplicateAndSetupCalculatorScript(newRecorderInstance);
        customDataRecorder = newRecorderInstance;
    }

    private GameObject FindRequiredObjectsWrapperInstance()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == RequiredObjectsWrapperPrefabPath)
            {
                return obj;
            }
        }
        return null;
    }

    private void DuplicateAndSetupCalculatorScript(GameObject newRecorderInstance)
    {
        var identifiersAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(IdentifiersAssetPath);
        var calculatorTemplateAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(CalculatorTemplateAssetPath);

        if (identifiersAsset == null || calculatorTemplateAsset == null)
        {
            Debug.LogError("Failed to load Identifiers or Calculator Template assets.");
            return;
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string newCalculatorPath = $"{DataRecorderScriptFolderPath}{sceneName}.js";

        if (!Directory.Exists(DataRecorderScriptFolderPath))
        {
            Directory.CreateDirectory(DataRecorderScriptFolderPath);
        }

        AssetDatabase.CopyAsset(CalculatorTemplateAssetPath, newCalculatorPath);
        AssetDatabase.Refresh();

        var newCalculatorAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(newCalculatorPath);
        if (newCalculatorAsset == null)
        {
            Debug.LogError("Failed to duplicate the Calculator template asset.");
            return;
        }

        AssignScriptToCombiner(newRecorderInstance, newCalculatorAsset);
        calculatorAsset = newCalculatorAsset;
    }

    private void EnsureCalculatorScriptExists()
    {
        if (calculatorAsset == null)
        {
            DuplicateAndSetupCalculatorScript(customDataRecorder);
        }
    }

    private void LinkCalculatorScript(GameObject recorder)
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string calculatorPath = $"{DataRecorderScriptFolderPath}{sceneName}.js";
        calculatorAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(calculatorPath);

        if (calculatorAsset != null)
        {
            AssignScriptToCombiner(recorder, calculatorAsset);
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

        if (customDataRecorder == null)
        {
            Debug.LogError("Custom data recorder Gameobject is null.");
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
        var scriptCombiner = customDataRecorder.GetComponent<ScriptableClusterScriptCombiner>();
        if (scriptCombiner != null)
        {
            scriptCombiner.CombineScripts();
        }
        else
        {
            Debug.LogError("ScriptableClusterScriptCombiner component not found on: " + customDataRecorder.name);
        }

        LoadCustomDataScripts();

        Debug.Log($"Custom data recorder's script saved to {path}");
    }

    private void AssignScriptToCombiner(GameObject recorderInstance, ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset scriptAsset)
    {
        var scriptCombiner = recorderInstance.GetComponent<ScriptableClusterScriptCombiner>();
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
            Debug.LogError("ScriptableClusterScriptCombiner component not found on the CustomDataRecorder instance.");
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
