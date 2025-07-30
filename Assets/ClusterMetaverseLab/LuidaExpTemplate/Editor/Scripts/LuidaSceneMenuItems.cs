using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Provides top-bar menu items for LUIDA scene management.
/// </summary>
public class LuidaSceneMenuItems
{
    private const string scenePath = "Assets/_Experiment_/Scenes/";

    [MenuItem("LUIDA/Scene/Create new scene")]
    private static void CreateNewSceneMenuItem()
    {
        string newSceneName = EditorInputDialog.Show("Create New Scene", "Enter the new scene name:", "");
        if (string.IsNullOrEmpty(newSceneName)) return;

        LuidaSceneUtility.CreateNewSceneFromTemplate(newSceneName);
    }

    [MenuItem("LUIDA/Scene/Duplicate current scene")]
    private static void DuplicateCurrentSceneMenuItem()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        if (!currentScenePath.StartsWith(scenePath))
        {
            EditorUtility.DisplayDialog("Error", "The current scene is not a valid experiment scene.\nPlease open a scene inside 'Assets/_Experiment_/Scenes/'.", "OK");
            return;
        }

        string newSceneName = EditorInputDialog.Show("Duplicate Current Scene", "Enter the new scene name:", "");
        if (string.IsNullOrEmpty(newSceneName)) return;
        
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            LuidaSceneUtility.DuplicateCurrentScene(newSceneName);
        }
    }
}

/// <summary>
/// A simple dialog window to get a string input from the user.
/// </summary>
public class EditorInputDialog : EditorWindow
{
    private string description;
    private string inputText;
    private System.Action<string> onOk;
    private bool shouldClose = false;

    public static string Show(string title, string description, string initialText)
    {
        EditorInputDialog window = CreateInstance<EditorInputDialog>();
        window.titleContent = new GUIContent(title);
        window.description = description;
        window.inputText = initialText;
        string result = null;
        window.onOk = (text) => { result = text; };
        window.ShowModal();
        return result;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);
        inputText = EditorGUILayout.TextField(inputText);

        GUILayout.Space(10);
        
        if (GUILayout.Button("OK"))
        {
            onOk?.Invoke(inputText);
            shouldClose = true;
        }

        if (GUILayout.Button("Cancel"))
        {
            inputText = null; // Signal that it was cancelled
            onOk?.Invoke(null);
            shouldClose = true;
        }

        if (shouldClose)
        {
            Close();
        }
    }
}
