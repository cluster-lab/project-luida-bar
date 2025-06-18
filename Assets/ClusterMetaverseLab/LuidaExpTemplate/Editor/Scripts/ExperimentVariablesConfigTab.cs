using ClusterVR.CreatorKit.Item.Implements;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

public class ExperimentVariablesConfigTab : EditorWindow
{
    private JavaScriptAsset variablesAsset;
    private JavaScriptAsset betweenSubjectsConditionSetterAsset;
    private JavaScriptAsset conditionManagerScript;
    private string conditionManagerScriptPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/ConditionManagement/ConditionManager.js";


    private ExperimentVariable[] withinSubjectsVariables;
    private ExperimentVariable[] betweenSubjectsVariables;
    private int trialsCountForEachUniqueCondition;

    private string variablesAssetPath;
    private string betweenSubjectsConditionSetterPath;
    private bool isSubscribed = false;

    public void OnEnable()
    {
        RetrieveJavaScriptAsset();
        if (!isSubscribed)
        {
            LuidaConfigWindow.OnEditorClosed += ApplyVariableUpdates;
            LuidaConfigWindow.OnEditorClosed += OnDisable;
            LuidaConfigWindow.OnItemsManagerTabLostFocus += ApplyVariableUpdates;
            isSubscribed = true;
        }
    }

    public void OnDisable()
    {
        if (isSubscribed)
        {
            LuidaConfigWindow.OnEditorClosed -= ApplyVariableUpdates;
            LuidaConfigWindow.OnEditorClosed -= OnDisable;
            LuidaConfigWindow.OnItemsManagerTabLostFocus -= ApplyVariableUpdates;
            isSubscribed = false;
        }
    }

    public void OnGUI()
    {
        if (variablesAsset == null)
        {
            GUILayout.Label("Variables Asset not found at path:", EditorStyles.boldLabel);
            GUILayout.Label(variablesAssetPath, EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Create New Variables Asset"))
            {
                RetrieveOrCreateVariablesAsset();
                ApplyVariableUpdates();
            }
        }
        else
        {
            trialsCountForEachUniqueCondition = EditorGUILayout.IntField("Trials Count per Condition", trialsCountForEachUniqueCondition);

            GUILayout.Label("Variables for Within-Subject Conditions", EditorStyles.boldLabel);
            if (withinSubjectsVariables == null)
            {
                withinSubjectsVariables = new ExperimentVariable[0];
            }

            DrawVariables(ref withinSubjectsVariables);

            GUILayout.Label("Variables for Between-Subject Conditions", EditorStyles.boldLabel);
            if (betweenSubjectsVariables == null)
            {
                betweenSubjectsVariables = new ExperimentVariable[0];
            }

            DrawVariables(ref betweenSubjectsVariables, forceIsRandom: true);

            /*
            if (betweenSubjectsConditionSetterAsset == null)
            {
                if (GUILayout.Button("Retrieve/Create Between-Subject Condition Setter"))
                {
                    RetrieveOrCreateBetweenSubjectsConditionSetter();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Between Subjects Condition Setter Asset", betweenSubjectsConditionSetterPath, EditorStyles.textField);
            }

            if (GUILayout.Button("Apply Updated Variables"))
            {
                ApplyVariableUpdates();
            }
            */
        }
    }

    private void DrawVariables(ref ExperimentVariable[] variables, bool forceIsRandom = false)
    {
        int newLength = EditorGUILayout.IntField("Length", variables.Length);
        if (newLength != variables.Length)
        {
            System.Array.Resize(ref variables, newLength);
            for (int i = 0; i < newLength; i++)
            {
                if (variables[i] == null)
                {
                    variables[i] = new ExperimentVariable();
                    variables[i].name = "";
                    variables[i].values = new string[0];
                    variables[i].isRandom = false;
                }
            }
        }

        for (int i = 0; i < variables.Length; i++)
        {
            EditorGUILayout.BeginHorizontal("box");

            GUILayout.Label("Name", GUILayout.Width(50));
            variables[i].name = EditorGUILayout.TextField(variables[i].name, GUILayout.Width(150));

            GUILayout.Label("Values (comma-separated)", GUILayout.Width(150));
            string valuesString = string.Join(",", variables[i].values);
            valuesString = EditorGUILayout.TextField(valuesString, GUILayout.Width(150));
            variables[i].values = valuesString.Split(',').Select(v => v.Trim()).ToArray();

            if (forceIsRandom)
            {
                variables[i].isRandom = true;
                GUILayout.Label("Is Random: true", GUILayout.Width(90));
            }
            else
            {
                GUILayout.Label("Is Random", GUILayout.Width(70));
                variables[i].isRandom = EditorGUILayout.Toggle(variables[i].isRandom, GUILayout.Width(20));
            }

            if (GUILayout.Button("▲", GUILayout.Width(20)))
            {
                if (i > 0)
                {
                    var temp = variables[i];
                    variables[i] = variables[i - 1];
                    variables[i - 1] = temp;
                }
            }

            if (GUILayout.Button("▼", GUILayout.Width(20)))
            {
                if (i < variables.Length - 1)
                {
                    var temp = variables[i];
                    variables[i] = variables[i + 1];
                    variables[i + 1] = temp;
                }
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                var variablesList = variables.ToList();
                variablesList.RemoveAt(i);
                variables = variablesList.ToArray();
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void GenerateJavaScript()
    {
        if (variablesAsset == null)
        {
            // EditorUtility.DisplayDialog("Error", "Please assign a JavaScriptAsset.", "OK");
            return;
        }

        string withinSubjectsVariablesJs = GenerateJavaScriptArray("within_subjects_variables", withinSubjectsVariables);
        string betweenSubjectsVariablesJs = GenerateJavaScriptArray("between_subjects_variables", betweenSubjectsVariables);

        string combinedJs = $"const trialsCountForEachUniqueCondition = {trialsCountForEachUniqueCondition};\n" +
            withinSubjectsVariablesJs + "\n" + betweenSubjectsVariablesJs + "\n";

        // Write the changes to the actual file
        File.WriteAllText(variablesAssetPath, combinedJs);

        // Update the ScriptableObject
        SerializedObject serializedObject = new SerializedObject(variablesAsset);
        SerializedProperty textProperty = serializedObject.FindProperty("text");
        textProperty.stringValue = combinedJs;
        serializedObject.ApplyModifiedProperties();

        // Mark the asset as dirty and save
        EditorUtility.SetDirty(variablesAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(variablesAssetPath);
    }

    private string GenerateJavaScriptArray(string variableName, ExperimentVariable[] variables)
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
        string templatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/VariablesTemplate.js";

        variablesAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(variablesAssetPath);
        if (variablesAsset == null)
        {
            if (File.Exists(templatePath))
            {
                File.Copy(templatePath, variablesAssetPath);
                AssetDatabase.Refresh();
                variablesAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(variablesAssetPath);
                AssetDatabase.ImportAsset(variablesAssetPath);
            }
            else
            {
                Debug.LogWarning("Template JavaScript asset not found.");
            }
        }

        RetrieveJavaScriptAsset();
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

    private ExperimentVariable[] ParseJavaScriptArray(string variableName, string jsContent)
    {
        string pattern = $@"const {variableName} = \[(.*?)\];";
        Match match = Regex.Match(jsContent, pattern, RegexOptions.Singleline);

        if (!match.Success)
        {
            return new ExperimentVariable[0];
        }

        string arrayContent = match.Groups[1].Value;
        var variableMatches = Regex.Matches(arrayContent, @"\{(.*?)\}", RegexOptions.Singleline);

        List<ExperimentVariable> variables = new List<ExperimentVariable>();
        foreach (Match variableMatch in variableMatches)
        {
            string variableContent = variableMatch.Groups[1].Value;

            string name = Regex.Match(variableContent, @"name: ""(.*?)""").Groups[1].Value;
            string valuesString = Regex.Match(variableContent, @"values: \[(.*?)\]").Groups[1].Value;
            bool isRandom = Regex.Match(variableContent, @"isRandom: (true|false)").Groups[1].Value == "true";

            string[] values = valuesString.Split(',').Select(v => v.Trim().Trim('"')).ToArray();

            ExperimentVariable variable = new ExperimentVariable
            {
                name = name,
                values = values,
                isRandom = isRandom
            };
            variables.Add(variable);
        }

        return variables.ToArray();
    }

    private void RetrieveOrCreateBetweenSubjectsConditionSetter()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        betweenSubjectsConditionSetterPath = $"Assets/_Experiment_/Settings/BetweenSubjectsConditionSetter/{sceneName}.js";
        string templatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/BetweenSubjectsConditionSetterTemplate.js";

        betweenSubjectsConditionSetterAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(betweenSubjectsConditionSetterPath);
        if (betweenSubjectsConditionSetterAsset == null)
        {
            if (File.Exists(templatePath))
            {
                File.Copy(templatePath, betweenSubjectsConditionSetterPath);
                AssetDatabase.Refresh();
                betweenSubjectsConditionSetterAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(betweenSubjectsConditionSetterPath);
            }
            else
            {
                Debug.LogWarning("Template JavaScript asset not found.");
            }
        }
    }

    private void ApplyVariableUpdates()
    {
        GenerateJavaScript();
        if (betweenSubjectsConditionSetterAsset != null)
        {
            UpdateScriptableClusterScriptCombiner(new JavaScriptAsset[] { betweenSubjectsConditionSetterAsset, variablesAsset }, false);
        }
        else
        {
            UpdateScriptableClusterScriptCombiner(new JavaScriptAsset[] { variablesAsset });
        }

        Debug.Log($"Experiment variables saved to {variablesAssetPath}");
    }

    private void UpdateScriptableClusterScriptCombiner(JavaScriptAsset[] scriptAssets, bool prepend = true)
    {
        GameObject conditionManager = GameObject.Find("ConditionManager");
        if (conditionManager != null)
        {
            var scriptCombiner = conditionManager.GetComponent<ScriptableClusterScriptCombiner>();
            if (scriptCombiner != null)
            {
                scriptCombiner.ClearScripts();
                for (int i = 0; i < scriptAssets.Length; i++)
                {
                    scriptCombiner.AppendScript(scriptAssets[i], null, false);
                }
                scriptCombiner.AppendScript(conditionManagerScript, null, true);
/*
                int existingScriptIndex = scriptCombiner.GetClusterScripts().IndexOf(scriptAsset);
                if (existingScriptIndex != -1)
                {
                    scriptCombiner.ReplaceScript(scriptAsset, existingScriptIndex, null, 0, true);
                }
                else
                {
                    if (prepend)
                    {
                        scriptCombiner.PrependScript(scriptAsset, null, true);
                    }
                    else
                    {
                        scriptCombiner.AppendScript(scriptAsset, null, true);
                    }
                }
*/
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
