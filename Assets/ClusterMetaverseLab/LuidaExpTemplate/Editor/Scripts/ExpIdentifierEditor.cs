using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public class ExpIdentifierEditor
{
    private string expID = "";
    private string token = "";
    private string filePath;
    private bool isSubscribed = false;

    public void OnEnable()
    {
        // Set the file path to the single ExpIdentifiers.js file
        filePath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";

        // Load the existing values if the file exists
        if (File.Exists(filePath))
        {
            LoadExpIdentifiers();
        }

        if (!isSubscribed)
        {
            TabbedEditor.OnEditorClosed += SaveExpIdentifiers;
            TabbedEditor.OnEditorClosed += OnDisable;
            TabbedEditor.OnItemsManagerTabLostFocus += SaveExpIdentifiers;
            isSubscribed = true;
        }
    }

    public void OnDisable()
    {
        if (isSubscribed)
        {
            TabbedEditor.OnEditorClosed -= SaveExpIdentifiers;
            TabbedEditor.OnEditorClosed -= OnDisable;
            TabbedEditor.OnItemsManagerTabLostFocus -= SaveExpIdentifiers;
            isSubscribed = false;
        }
    }

    public void OnGUI()
    {
        GUILayout.Label("Experiment Identifiers", EditorStyles.boldLabel);

        expID = EditorGUILayout.TextField("Experiment ID", expID);
        token = EditorGUILayout.TextField("Token", token);

        if (GUILayout.Button("Save Identifiers"))
        {
            SaveExpIdentifiers();
        }
    }

    private void LoadExpIdentifiers()
    {
        // Read the content of the file
        string content = File.ReadAllText(filePath);

        // Use regular expressions to extract the expID and token from the file
        expID = ExtractValue(content, "expID");
        token = ExtractValue(content, "token");
    }

    private string ExtractValue(string content, string key)
    {
        // Use regular expressions to find the key-value pair with double quotes
        string pattern = $@"{key}\s*=\s*""([^""]+)""";
        Match match = Regex.Match(content, pattern);

        // Return the captured value if the match is successful, otherwise return an empty string
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return "";
    }

    private void SaveExpIdentifiers()
    {
        // Ensure the directory exists
        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        // Create the new content with the updated expID and token using double quotes
        string content = $"expID = \"{expID}\";\ntoken = \"{token}\";\n";
        File.WriteAllText(filePath, content);

        // Refresh the asset database to reflect changes in Unity
        AssetDatabase.Refresh();

        Debug.Log($"Experiment identifiers saved to {filePath}");
    }
}
