using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.IO;

public class TabbedEditor : EditorWindow
{
    public static event Action OnEditorClosed;
    public static event Action OnItemsManagerTabLostFocus;

    private int currentTab = 0;
    private string[] tabNames = { "Experiment Identifiers", "Experiment Variables", "States List (& Questionnaires)", "State-listening Items", "Data Recorder" };

    private ExpIdentifierEditor expIdentifierEditor;
    private StateListEditor stateListEditor;
    private ItemsManagerEditor itemsManagerEditor;
    private ExperimentVariablesEditor experimentVariablesEditor;
    private DataRecorderEditor dataRecorderEditor;

    private string newSceneName = "";
    private const string scenePath = "Assets/_Experiment_/Scenes/";
    private const string templateScenePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scenes/Template.unity";
    private const string expIdentifiersPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string templateExpIdentifiersPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/ExpIdentifiers.js";

    [MenuItem("Window/Luida Editor")]
    public static void ShowWindow()
    {
        GetWindow<TabbedEditor>("Luida Editor");
    }

    private void OnEnable()
    {
        expIdentifierEditor = new ExpIdentifierEditor();
        experimentVariablesEditor = new ExperimentVariablesEditor();
        stateListEditor = new StateListEditor();
        itemsManagerEditor = new ItemsManagerEditor();
        dataRecorderEditor = new DataRecorderEditor();

        expIdentifierEditor.OnEnable();
        experimentVariablesEditor.OnEnable();
        stateListEditor.OnEnable();
        itemsManagerEditor.OnEnable();
        dataRecorderEditor.OnEnable();

        CheckAndCreateExpIdentifiers();
    }

    private void OnGUI()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;

        // Check if the current scene is inside Assets/_Experiment_/Scenes/
        if (!currentScenePath.StartsWith(scenePath))
        {
            GUILayout.Label("No valid experiment scene is currently active.", EditorStyles.boldLabel);
            GUILayout.Label("Please use the form below to create a scene for your experiment.", EditorStyles.wordWrappedLabel);

            GUILayout.Label("Create New Experiment Scene", EditorStyles.boldLabel);
            newSceneName = EditorGUILayout.TextField("New Scene Name", newSceneName);

            if (GUILayout.Button("Create and Open Scene"))
            {
                if (!string.IsNullOrEmpty(newSceneName))
                {
                    string newScenePath = scenePath + newSceneName + ".unity";

                    if (!File.Exists(newScenePath))
                    {
                        File.Copy(templateScenePath, newScenePath);
                        AssetDatabase.Refresh();
                        EditorSceneManager.OpenScene(newScenePath);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "A scene with that name already exists!", "OK");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please enter a valid scene name.", "OK");
                }
            }
        }
        else
        {
            GUILayout.Label("Current Active Scene: " + currentScenePath, EditorStyles.boldLabel);
            // draw toolbar
            int newTab = GUILayout.Toolbar(currentTab, tabNames);

            // detect switching away from the ItemsManager tab:
            if (newTab != currentTab)
            {
                Debug.Log("TabbedEditor tab switched from " + currentTab + " to " + newTab);
                OnItemsManagerTabLostFocus?.Invoke();
                currentTab = newTab;
            }

            switch (currentTab)
            {
                case 0:
                    expIdentifierEditor.OnGUI();
                    break;
                case 1:
                    experimentVariablesEditor.OnGUI();
                    break;
                case 2:
                    stateListEditor.OnGUI();
                    break;
                case 3:
                    itemsManagerEditor.OnGUI();
                    break;
                case 4:
                    dataRecorderEditor.OnGUI();
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        OnEditorClosed?.Invoke();
    }

    private void CheckAndCreateExpIdentifiers()
    {
        if (!File.Exists(expIdentifiersPath))
        {
            File.Copy(templateExpIdentifiersPath, expIdentifiersPath);
            AssetDatabase.Refresh();
        }
    }
}
