using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.IO;

public class LuidaConfigWindow : EditorWindow
{
    public static LuidaConfigWindow Instance { get; private set; }
    public static event Action OnEditorClosed;
    public static event Action OnItemsManagerTabLostFocus;

    private int currentTab = 0;
    private string[] tabNames = { "Experiment Identifiers", "Experiment Variables", "States List (& Questionnaires)", "State-listening Items", "Data Collector" };

    private ExpIdentifierConfigTab expIdentifierConfigTab;
    private StateMachineConfigTab stateMachineConfigTab;
    private ItemsManagerConfigTab itemsManagerEditor;
    private ExperimentVariablesConfigTab experimentVariablesConfigTab;
    private DataCollectorConfigTab dataCollectorConfigTab;

    private string newSceneName = "";
    private const string scenePath = "Assets/_Experiment_/Scenes/";
    private const string templateScenePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scenes/Template.unity";
    private const string expIdentifiersPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string templateExpIdentifiersPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/ExpIdentifiers.js";
    
    public StateMachineConfigTab StateTab => stateMachineConfigTab;
    
    [MenuItem("Window/LUIDA Config Window")]
    public static void ShowWindow()
    {
        GetWindow<LuidaConfigWindow>("LUIDA Config Window");
    }

    private void OnEnable()
    {
        expIdentifierConfigTab = new ExpIdentifierConfigTab();
        experimentVariablesConfigTab = new ExperimentVariablesConfigTab();
        stateMachineConfigTab = new StateMachineConfigTab();
        itemsManagerEditor = new ItemsManagerConfigTab();
        dataCollectorConfigTab = new DataCollectorConfigTab();

        expIdentifierConfigTab.OnEnable();
        experimentVariablesConfigTab.OnEnable();
        stateMachineConfigTab.OnEnable();
        itemsManagerEditor.OnEnable();
        dataCollectorConfigTab.OnEnable();

        CheckAndCreateExpIdentifiers();
        
        Instance = this;
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
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Active Scene: " + currentScenePath, EditorStyles.boldLabel);

            // --- New Scene Creation Form
            GUILayout.FlexibleSpace();
            newSceneName = EditorGUILayout.TextField("New Experiment Name", newSceneName, GUILayout.Width(250));

            if (GUILayout.Button("Create and Switch Scene"))
            {
                if (!string.IsNullOrEmpty(newSceneName))
                {
                    string newScenePath = scenePath + newSceneName + ".unity";

                    if (!File.Exists(newScenePath))
                    {
                        // Save current scene if it has been modified
                        if (EditorSceneManager.GetActiveScene().isDirty)
                        {
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
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
            GUILayout.EndHorizontal();

            // draw toolbar
            int newTab = GUILayout.Toolbar(currentTab, tabNames);

            // detect switching away from the ItemsManager tab:
            if (newTab != currentTab)
            {
                Debug.Log("LuidaConfigWindow tab switched from " + currentTab + " to " + newTab);
                OnItemsManagerTabLostFocus?.Invoke();
                currentTab = newTab;
            }

            switch (currentTab)
            {
                case 0:
                    expIdentifierConfigTab.OnGUI();
                    break;
                case 1:
                    experimentVariablesConfigTab.OnGUI();
                    break;
                case 2:
                    stateMachineConfigTab.OnGUI();
                    break;
                case 3:
                    itemsManagerEditor.OnGUI();
                    break;
                case 4:
                    dataCollectorConfigTab.OnGUI();
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        OnEditorClosed?.Invoke();
        Instance = null;
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
