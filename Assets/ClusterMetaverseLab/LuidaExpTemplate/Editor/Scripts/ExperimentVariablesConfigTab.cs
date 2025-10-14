using ClusterVR.CreatorKit.Item.Implements;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal; // Required for ReorderableList

public class ExperimentVariablesConfigTab : LuidaAutomationConfigTab
{
    protected override LuidaConfigWindow.TabIndex TabIndex => LuidaConfigWindow.TabIndex.ExperimentVariables;

    public static bool IsApplyingVariableUpdates = false;

    private JavaScriptAsset variablesAsset;
    private JavaScriptAsset betweenSubjectsConditionSetterAsset;
    private JavaScriptAsset conditionManagerScript;
    private string conditionManagerScriptPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/ConditionManagement/ConditionManager.js";

    private List<ExperimentVariable> withinSubjectsVariables = new List<ExperimentVariable>();
    private List<ExperimentVariable> betweenSubjectsVariables = new List<ExperimentVariable>();
    private int trialsCountForEachUniqueCondition;

    private ReorderableList withinSubjectsList;
    private ReorderableList betweenSubjectsList;

    private string variablesAssetPath;
    private string betweenSubjectsConditionSetterPath;

    public void OnEnable()
    {
        RetrieveJavaScriptAsset();
        SetupReorderableLists();

        LuidaConfigWindow.OnEditorClosed -= ApplyVariableUpdates;
        LuidaConfigWindow.OnEditorClosed -= OnDisable;
        LuidaConfigWindow.OnTabSwitched -= HandleTabSwitched;

        LuidaConfigWindow.OnEditorClosed += ApplyVariableUpdates;
        LuidaConfigWindow.OnEditorClosed += OnDisable;
        LuidaConfigWindow.OnTabSwitched += HandleTabSwitched;
    }

    public void OnDisable()
    {
        LuidaConfigWindow.OnEditorClosed -= ApplyVariableUpdates;
        LuidaConfigWindow.OnEditorClosed -= OnDisable;
        LuidaConfigWindow.OnTabSwitched -= HandleTabSwitched;
    }

    private void HandleTabSwitched(LuidaConfigWindow.TabIndex prevTab, LuidaConfigWindow.TabIndex nextTab)
    {
        if (prevTab == TabIndex && nextTab != TabIndex)
        {
            ApplyVariableUpdates();
        }
    }

    private void SetupReorderableLists()
    {
        // === Within-Subjects List ===
        withinSubjectsList = new ReorderableList(withinSubjectsVariables, typeof(ExperimentVariable), true, true, true, true);

        withinSubjectsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Variables for Within-Subject Conditions", EditorStyles.boldLabel);
        };

        withinSubjectsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            if (index >= withinSubjectsVariables.Count) return;
            
            var element = withinSubjectsVariables[index];
            rect.y += 2;
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 20;

            var nameRect = new Rect(rect.x, rect.y, rect.width * 0.25f, singleLineHeight);
            var valuesRect = new Rect(nameRect.xMax + spacing, rect.y, rect.width * 0.35f, singleLineHeight);
            var randomLabelRect = new Rect(valuesRect.xMax + spacing, rect.y, 70, singleLineHeight);
            var randomToggleRect = new Rect(randomLabelRect.xMax + spacing, rect.y, 20, singleLineHeight);

            float labelWidth = 40f;
            var nameLabelRect = new Rect(nameRect.x, nameRect.y, labelWidth, nameRect.height);
            var nameFieldRect = new Rect(nameRect.x + labelWidth, nameRect.y, nameRect.width - labelWidth, nameRect.height);
            EditorGUI.LabelField(nameLabelRect, new GUIContent("Name:", "Variable name..."));
            element.name = EditorGUI.TextField(nameFieldRect, element.name);
            
            string valuesString = string.Join(",", element.values);
            labelWidth = 45f;
            var valuesLabelRect = new Rect(valuesRect.x, valuesRect.y, labelWidth, valuesRect.height);
            var valuesFieldRect = new Rect(valuesRect.x + labelWidth, valuesRect.y, valuesRect.width - labelWidth, valuesRect.height);
            EditorGUI.LabelField(valuesLabelRect, new GUIContent("Values:", "Comma-separated values..."));
            valuesString = EditorGUI.TextField(valuesFieldRect, valuesString);
            element.values = valuesString.Split(',').Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)).ToArray();
            
            EditorGUI.LabelField(randomLabelRect, "Is Random");
            element.isRandom = EditorGUI.Toggle(randomToggleRect, element.isRandom);
        };

        withinSubjectsList.onAddCallback = (ReorderableList list) => {
            withinSubjectsVariables.Add(new ExperimentVariable { 
                name = "NewVariable", 
                values = new[] { "value1", "value2" }, 
                isRandom = false 
            });
        };

        // === Between-Subjects List ===
        betweenSubjectsList = new ReorderableList(betweenSubjectsVariables, typeof(ExperimentVariable), true, true, true, true);

        betweenSubjectsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Variables for Between-Subject Conditions", EditorStyles.boldLabel);
        };

        betweenSubjectsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            if (index >= betweenSubjectsVariables.Count) return;

            var element = betweenSubjectsVariables[index];
            rect.y += 2;
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 20;
            
            var nameRect = new Rect(rect.x, rect.y, rect.width * 0.25f, singleLineHeight);
            var valuesRect = new Rect(nameRect.xMax + spacing, rect.y, rect.width * 0.35f, singleLineHeight);
            var randomLabelRect = new Rect(valuesRect.xMax + spacing, rect.y, rect.width * 0.1f, singleLineHeight);
            var debugValueRect = new Rect(randomLabelRect.xMax + spacing, rect.y, rect.width * 0.15f, singleLineHeight);


            float labelWidth = 40f;
            var nameLabelRect = new Rect(nameRect.x, nameRect.y, labelWidth, nameRect.height);
            var nameFieldRect = new Rect(nameRect.x + labelWidth, nameRect.y, nameRect.width - labelWidth, nameRect.height);
            EditorGUI.LabelField(nameLabelRect, new GUIContent("Name:", "Variable name..."));
            element.name = EditorGUI.TextField(nameFieldRect, element.name);
            
            string valuesString = string.Join(",", element.values);
            labelWidth = 45f;
            var valuesLabelRect = new Rect(valuesRect.x, valuesRect.y, labelWidth, valuesRect.height);
            var valuesFieldRect = new Rect(valuesRect.x + labelWidth, valuesRect.y, valuesRect.width - labelWidth, valuesRect.height);
            EditorGUI.LabelField(valuesLabelRect, new GUIContent("Values:", "Comma-separated values..."));
            valuesString = EditorGUI.TextField(valuesFieldRect, valuesString);
            element.values = valuesString.Split(',').Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)).ToArray();

            labelWidth = 70f;
            var debugLabelRect = new Rect(debugValueRect.x, debugValueRect.y, labelWidth, debugValueRect.height);
            var debugFieldRect = new Rect(debugLabelRect.xMax, debugValueRect.y, debugValueRect.width - labelWidth, debugValueRect.height);
            EditorGUI.LabelField(debugLabelRect, new GUIContent("Debug Val:", "Force this value for testing. Leave empty for random assignment."));
            element.debugValue = EditorGUI.TextField(debugFieldRect, element.debugValue);
            
            element.isRandom = true; 
            EditorGUI.LabelField(randomLabelRect, "Is Random: true");
        };

        betweenSubjectsList.onAddCallback = (ReorderableList list) => {
            betweenSubjectsVariables.Add(new ExperimentVariable { 
                name = "NewVariable", 
                values = new[] { "value1", "value2" }, 
                isRandom = true,
                debugValue = null
            });
        };
    }

    public void OnGUI()
    {
        if (variablesAsset == null)
        {
            RetrieveOrCreateVariablesAsset();
            ApplyVariableUpdates(); 
        }
        
        EditorGUILayout.HelpBox("For fields `Values`, remember to separate multiple values using a comma.", MessageType.Info);
            
        if (withinSubjectsList == null || betweenSubjectsList == null) {
            SetupReorderableLists();
        }

        trialsCountForEachUniqueCondition = EditorGUILayout.IntField("Trials Count per Condition", trialsCountForEachUniqueCondition);
        EditorGUILayout.Space();

        withinSubjectsList.DoLayoutList();
        EditorGUILayout.Space();
        betweenSubjectsList.DoLayoutList();
    }
    
    private void GenerateJavaScript()
    {
        if (variablesAsset == null) return;
        
        string withinSubjectsVariablesJs = GenerateJavaScriptArray("within_subjects_variables", withinSubjectsVariables);
        string betweenSubjectsVariablesJs = GenerateJavaScriptArray("between_subjects_variables", betweenSubjectsVariables);

        string combinedJs = $"const trialsCountForEachUniqueCondition = {trialsCountForEachUniqueCondition};\n" +
            withinSubjectsVariablesJs + "\n" + betweenSubjectsVariablesJs + "\n";
        
        combinedJs += GetStateNamesJavaScript();

        File.WriteAllText(variablesAssetPath, combinedJs);

        SerializedObject serializedObject = new SerializedObject(variablesAsset);
        SerializedProperty textProperty = serializedObject.FindProperty("text");
        textProperty.stringValue = combinedJs;
        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(variablesAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private string GenerateJavaScriptArray(string variableName, List<ExperimentVariable> variables)
    {
        string js = $"const {variableName} = [\n";
        foreach (var variable in variables)
        {
            string values = string.Join(", ", variable.values.Select(v => $"\"{v}\""));
            string varObject = $"    {{ name: \"{variable.name}\", values: [{values}], isRandom: {variable.isRandom.ToString().ToLower()}";

            if (variableName == "between_subjects_variables")
            {
                string debugValueJs = string.IsNullOrEmpty(variable.debugValue) ? "null" : $"\"{variable.debugValue}\"";
                varObject += $", debugValue: {debugValueJs}";
            }
            
            varObject += " },\n";
            js += varObject;
        }
        js += "];";
        return js;
    }
    
    private string GetStateNamesJavaScript()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string stateListAssetPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
        StateList stateList = AssetDatabase.LoadAssetAtPath<StateList>(stateListAssetPath);

        if (stateList != null && stateList.States != null)
        {
            var stateNames = stateList.States.Select(s => $"\"{s.StateName}\"");
            return $"const state_names = [{string.Join(", ", stateNames)}];\n";
        }
        else
        {
            Debug.LogWarning($"StateList asset not found at path: {stateListAssetPath}. State names will not be written to JS file.");
            return ""; // Return an empty string if the asset isn't found
        }
    }

    private void RetrieveJavaScriptAsset()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        variablesAssetPath = $"Assets/_Experiment_/Settings/ExperimentVariables/{sceneName}.js";

        variablesAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(variablesAssetPath);
        if (variablesAsset != null && !string.IsNullOrEmpty(variablesAsset.text))
        {
            ParseJavaScriptAsset(variablesAsset.text);
        }

        conditionManagerScript = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(conditionManagerScriptPath);
    }

    private void RetrieveOrCreateVariablesAsset()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        variablesAssetPath = $"Assets/_Experiment_/Settings/ExperimentVariables/{sceneName}.js";
        variablesAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(variablesAssetPath);

        if (variablesAsset == null)
        {
            string directoryPath = Path.GetDirectoryName(variablesAssetPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            string templatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/VariablesTemplate.js";
            if (File.Exists(templatePath))
            {
                AssetDatabase.CopyAsset(templatePath, variablesAssetPath);
                AssetDatabase.Refresh();
                variablesAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(variablesAssetPath);
            }
            else
            {
                Debug.LogWarning("Template JavaScript asset not found at: " + templatePath);
            }
        }
    }

    private void ParseJavaScriptAsset(string jsContent)
    {
        var trialsCountMatch = Regex.Match(jsContent, @"const trialsCountForEachUniqueCondition = (\d+);");
        if (trialsCountMatch.Success)
        {
            trialsCountForEachUniqueCondition = int.Parse(trialsCountMatch.Groups[1].Value);
        }        
        withinSubjectsVariables = ParseJavaScriptArray("within_subjects_variables", jsContent);
        betweenSubjectsVariables = ParseJavaScriptArray("between_subjects_variables", jsContent);
    }

    private List<ExperimentVariable> ParseJavaScriptArray(string variableName, string jsContent)
    {
        string pattern = $@"const {variableName} = \[(.*?)\];";
        Match match = Regex.Match(jsContent, pattern, RegexOptions.Singleline);

        if (!match.Success) return new List<ExperimentVariable>();
        
        string arrayContent = match.Groups[1].Value;
        var variableMatches = Regex.Matches(arrayContent, @"\{(.*?)\}", RegexOptions.Singleline);

        List<ExperimentVariable> variables = new List<ExperimentVariable>();
        foreach (Match variableMatch in variableMatches)
        {
            string variableContent = variableMatch.Groups[1].Value;

            string name = Regex.Match(variableContent, @"name: ""(.*?)""").Groups[1].Value;
            string valuesString = Regex.Match(variableContent, @"values: \[(.*?)\]").Groups[1].Value;
            bool isRandom = Regex.Match(variableContent, @"isRandom: (true|false)").Groups[1].Value == "true";
            
            string debugValue = null;
            var debugValueMatch = Regex.Match(variableContent, @"debugValue:\s*(""(.*?)""|null)");
            if (debugValueMatch.Success && debugValueMatch.Groups[2].Success)
            {
                debugValue = debugValueMatch.Groups[2].Value;
            }
            
            // defensive check for empty values array
            string[] values = string.IsNullOrEmpty(valuesString)
                ? new string[0]
                : valuesString.Split(',').Select(v => v.Trim().Trim('"')).ToArray();

            variables.Add(new ExperimentVariable { name = name, values = values, isRandom = isRandom, debugValue = debugValue });
        }
        return variables;
    }

    private void ApplyVariableUpdates()
    {
        if (IsApplyingVariableUpdates) return; // Prevent re-entry

        IsApplyingVariableUpdates = true;
        
        GenerateJavaScript();
        
        var scriptAssets = new List<JavaScriptAsset>();
        if (betweenSubjectsConditionSetterAsset != null)
        {
            scriptAssets.Add(betweenSubjectsConditionSetterAsset);
        }
        if(variablesAsset != null)
        {
             scriptAssets.Add(variablesAsset);
        }
        
        UpdateScriptableClusterScriptCombiner(scriptAssets.ToArray());

        Debug.Log($"Experiment variables saved to {variablesAssetPath}");
        IsApplyingVariableUpdates = false;
    }

    private void UpdateScriptableClusterScriptCombiner(JavaScriptAsset[] scriptAssets)
    {
        GameObject conditionManager = GameObject.Find("ConditionManager");
        if (conditionManager != null)
        {
            var scriptCombiner = conditionManager.GetComponent<ScriptableClusterScriptCombiner>();
            if (scriptCombiner != null)
            {
                scriptCombiner.ClearScripts();
                foreach(var asset in scriptAssets)
                {
                    if(asset != null) scriptCombiner.AppendScript(asset, null, false);
                }
                
                if (conditionManagerScript != null)
                {
                    scriptCombiner.AppendScript(conditionManagerScript, null, true);
                }

                EditorUtility.SetDirty(scriptCombiner);
                EditorSceneManager.MarkSceneDirty(conditionManager.scene);
            }
            else
            {
                Debug.LogWarning("ScriptableClusterScriptCombiner component not found on ConditionManager.");
            }
        }
        else
        {
            Debug.LogWarning("ConditionManager GameObject not found in the scene.");
        }
    }
    
    public static void ResetAllDebugValues()
    {
        string searchPath = "Assets/_Experiment_/Settings/ExperimentVariables";
        if (!Directory.Exists(searchPath))
        {
            Debug.LogWarning($"Directory not found, nothing to reset: {searchPath}");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:JavaScriptAsset", new[] { searchPath });
        if (guids.Length == 0)
        {
            Debug.Log("No ExperimentVariables assets found to reset.");
            return;
        }

        int filesModified = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string jsContent = File.ReadAllText(path);

            string pattern = @"(const between_subjects_variables = \[)(.*?)(\];)";
            Match match = Regex.Match(jsContent, pattern, RegexOptions.Singleline);
            
            if (match.Success)
            {
                string arrayContent = match.Groups[2].Value;
                
                string updatedArrayContent = Regex.Replace(arrayContent, @"debugValue:\s*("".*?""|null|undefined|\d+)", "debugValue: null");

                if (arrayContent != updatedArrayContent)
                {
                    string newBlock = match.Groups[1].Value + updatedArrayContent + match.Groups[3].Value;
                    string newJsContent = jsContent.Replace(match.Value, newBlock);
                    File.WriteAllText(path, newJsContent);
                    filesModified++;
                }
            }
        }

        if (filesModified > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Successfully reset debug values for {filesModified} ExperimentVariables asset(s).");
        }
        else
        {
            Debug.Log("No assets required debug value reset.");
        }
    }
}
