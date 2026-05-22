using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.IO;

public class LuidaConfigWindow : EditorWindow
{
    public static LuidaConfigWindow Instance { get; private set; }
    public static event Action OnEditorClosed;
    public static event Action<TabIndex, TabIndex> OnTabSwitched;

    public enum TabIndex
    {
        ExperimentVariables,
        StateMachine,
        StateListeningItems
    }

    private TabIndex currentTab = TabIndex.ExperimentVariables;
    private string[] tabNames = { "Experiment Variables", "State Machine (& Questionnaires)", "State-listening Items" };

    private StateMachineConfigTab stateMachineConfigTab;
    private ItemsManagerConfigTab itemsManagerEditor;
    private ExperimentVariablesConfigTab experimentVariablesConfigTab;

    private string newSceneName = "";
    private const string scenePath = "Assets/_Experiment_/Scenes/";
    private const string expIdentifiersPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string templateExpIdentifiersPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/ExpIdentifiers.js";
    
    private bool isAutomationFeatureActive = false;

    public StateMachineConfigTab StateTab => stateMachineConfigTab;
    
    [MenuItem("LUIDA/Configure experiment automation")]
    public static void ShowWindow()
    {
        GetWindow<LuidaConfigWindow>("LUIDA Experiment Automation Config Window");
    }

    private void OnEnable()
    {
        CheckAutomationFeatureStatus();
        if (isAutomationFeatureActive)
        {
            InitializeTabs();
        }

        CheckAndCreateExpIdentifiers();
        Instance = this;
    }

    private void InitializeTabs()
    {
        experimentVariablesConfigTab = new ExperimentVariablesConfigTab();
        stateMachineConfigTab = new StateMachineConfigTab();
        itemsManagerEditor = new ItemsManagerConfigTab();

        experimentVariablesConfigTab.OnEnable();
        stateMachineConfigTab.OnEnable();
        itemsManagerEditor.OnEnable();

        isAutomationFeatureActive = true;
    }

    private void CheckAutomationFeatureStatus()
    {
        isAutomationFeatureActive = LuidaAutomationStatus.IsActiveForActiveScene();
    }
    
    private void OnGUI()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;

        if (!currentScenePath.StartsWith(scenePath))
        {
            DrawInvalidSceneUI();
        }
        else
        {
            CheckAutomationFeatureStatus();

            if (!isAutomationFeatureActive)
            {
                DrawActivationUI();
            }
            else
            {
                DrawMainConfigurationUI();
            }
        }
    }
    
    private void DrawInvalidSceneUI()
    {
        GUILayout.Label("No valid experiment scene is currently active.", EditorStyles.boldLabel);
        GUILayout.Label("Please use the form below to create a scene for your experiment.", EditorStyles.wordWrappedLabel);

        GUILayout.Label("Create New Experiment Scene", EditorStyles.boldLabel);
        newSceneName = EditorGUILayout.TextField("New Scene Name", newSceneName);

        if (GUILayout.Button("Create and Open Scene"))
        {
            if (!string.IsNullOrEmpty(newSceneName))
            {
                LuidaSceneUtility.CreateNewSceneFromTemplate(newSceneName);
                // After scene is created, this window will show the activation button
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please enter a valid scene name.", "OK");
            }
        }
    }

    private void DrawActivationUI()
    {
        EditorGUILayout.HelpBox("LUIDA experiment automation feature is not activated yet for this scene. Press the button below if you choose to activate it.", MessageType.Info);
        if (GUILayout.Button("Activate Experiment Automation Feature", GUILayout.Height(40)))
        {
            CreateStateListAssetForCurrentScene();
            CreateExperimentVariablesAssetForCurrentScene();
            AssetDatabase.Refresh();
            InitializeTabs(); // This will set isAutomationFeatureActive to true and init tabs
        }
    }

    private void DrawMainConfigurationUI()
    {
        if (stateMachineConfigTab == null) InitializeTabs();

        string currentScenePath = EditorSceneManager.GetActiveScene().path;

        GUILayout.Label("Current Active Scene: " + currentScenePath, EditorStyles.boldLabel);

        int newTab = GUILayout.Toolbar((int)currentTab, tabNames);
        if (newTab != (int)currentTab)
        {
            OnTabSwitched?.Invoke(currentTab, (TabIndex)newTab);
            currentTab = (TabIndex)newTab;
        }

        switch (currentTab)
        {
            case TabIndex.ExperimentVariables:
                experimentVariablesConfigTab.OnGUI();
                break;
            case TabIndex.StateMachine:
                stateMachineConfigTab.OnGUI();
                break;
            case TabIndex.StateListeningItems:
                itemsManagerEditor.OnGUI();
                break;
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
            Directory.CreateDirectory(Path.GetDirectoryName(expIdentifiersPath));
            File.Copy(templateExpIdentifiersPath, expIdentifiersPath);
            AssetDatabase.Refresh();
        }
    }
    
    private void RefreshAllTabs()
    {
        experimentVariablesConfigTab.OnEnable();
        stateMachineConfigTab.OnEnable();
        itemsManagerEditor.OnEnable();
    }
    
    private void CreateStateListAssetForCurrentScene()
    {
        string sceneName = EditorSceneManager.GetActiveScene().name;
        string stateListPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
        const string templatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/StateList/Template.asset";
        
        if (!File.Exists(stateListPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(stateListPath));
            AssetDatabase.CopyAsset(templatePath, stateListPath);
            Debug.Log($"Created StateList asset at {stateListPath}");
        }
    }
    
    private void CreateExperimentVariablesAssetForCurrentScene()
    {
        string sceneName = EditorSceneManager.GetActiveScene().name;
        string variablesAssetPath = $"Assets/_Experiment_/Settings/ExperimentVariables/{sceneName}.js";
        const string templatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/VariablesTemplate.js";
        
        if (!File.Exists(variablesAssetPath))
        {
            string directoryPath = Path.GetDirectoryName(variablesAssetPath);
            Directory.CreateDirectory(directoryPath);
            File.Copy(templatePath, variablesAssetPath);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(variablesAssetPath);
            Debug.Log($"Created ExperimentVariables asset at {variablesAssetPath}");
        }
    }
}
