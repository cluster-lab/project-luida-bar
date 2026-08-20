using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ClusterMetaverseLab.Luida.Scripting;
using ClusterVR.CreatorKit.Item.Implements;
using ClusterVR.CreatorKit.Operation.Implements;

[CustomEditor(typeof(LuidaDataCollector))]
public class LuidaDataCollectorEditor : Editor
{
    private string markdownFilePath = "Assets/Doc/LUIDA-DataCollectorScriptDoc.md";
    private static readonly System.Type[] TypesToHide =
    {
        typeof(ItemLogic),
        typeof(ScriptableItem),
        typeof(ItemGroupMember),
        typeof(LuidaScriptCombiner)
    };
    private readonly List<Component> hiddenComponents = new List<Component>();

    private void OnEnable()
    {
        var dataCollector = (LuidaDataCollector)target;

        // Automatically find and assign the script if the reference is missing.
        if (dataCollector.calculationScript == null)
        {
            var scriptAsset = FindExistingCalculatorScript();
            if (scriptAsset != null)
            {
                dataCollector.calculationScript = scriptAsset;
                EditorUtility.SetDirty(dataCollector);
            }
        }

        // Hide complex underlying components for a cleaner inspector.
        hiddenComponents.Clear();
        foreach (var typeToHide in TypesToHide)
        {
            var components = dataCollector.GetComponents(typeToHide);
            foreach (var component in components)
            {
                if (component != null)
                {
                    component.hideFlags |= HideFlags.HideInInspector;
                    hiddenComponents.Add(component);
                }
            }
        }
    }

    private void OnDisable()
    {
        foreach (var component in hiddenComponents)
        {
            if (component != null)
            {
                component.hideFlags &= ~HideFlags.HideInInspector;
            }
        }
        hiddenComponents.Clear();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); // Draws the default 'Script' field for the MonoBehaviour
        var dataCollector = (LuidaDataCollector)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Double-click the script below to edit what and how to save your custom data:", EditorStyles.boldLabel);
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script Asset", dataCollector.calculationScript, typeof(JavaScriptAsset), false);
        GUI.enabled = true;
        
        /*
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Documentation of available variables are in the markdown file below:", EditorStyles.boldLabel);
        TextAsset markdownAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(markdownFilePath);
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Documentation", markdownAsset, typeof(TextAsset), false);
        GUI.enabled = true;
        */
    }

    private JavaScriptAsset FindExistingCalculatorScript()
    {
        const string DataCollectorScriptFolderPath = "Assets/_Experiment_/Scripts/DataCollectors/";
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string calculatorPath = $"{DataCollectorScriptFolderPath}{sceneName}.js";
        return AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(calculatorPath);
    }
}
