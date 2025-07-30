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

            var nameRect = new Rect(rect.x, rect.y, rect.width * 0.35f, singleLineHeight);
            var valuesRect = new Rect(rect.x + rect.width * 0.4f, rect.y, rect.width * 0.4f, singleLineHeight);
            var randomLabelRect = new Rect(rect.x + rect.width * 0.82f, rect.y, 70, singleLineHeight);
            var randomToggleRect = new Rect(randomLabelRect.xMax, rect.y, 20, singleLineHeight);

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
            
            var nameRect = new Rect(rect.x, rect.y, rect.width * 0.35f, singleLineHeight);
            var valuesRect = new Rect(rect.x + rect.width * 0.4f, rect.y, rect.width * 0.4f, singleLineHeight);
            var randomLabelRect = new Rect(rect.x + rect.width * 0.82f, rect.y, rect.width * 0.18f, singleLineHeight);

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

            element.isRandom = true; 
            EditorGUI.LabelField(randomLabelRect, "Is Random: true");
        };

        betweenSubjectsList.onAddCallback = (ReorderableList list) => {
            betweenSubjectsVariables.Add(new ExperimentVariable { 
                name = "NewVariable", 
                values = new[] { "value1", "value2" }, 
                isRandom = true 
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
            js += $"    {{ name: \"{variable.name}\", values: [{values}], isRandom: {variable.isRandom.ToString().ToLower()} }},\n";
        }
        js += "];";
        return js;
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
            
            // defensive check for empty values array
            string[] values = string.IsNullOrEmpty(valuesString)
                ? new string[0]
                : valuesString.Split(',').Select(v => v.Trim().Trim('"')).ToArray();

            variables.Add(new ExperimentVariable { name = name, values = values, isRandom = isRandom });
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
}
