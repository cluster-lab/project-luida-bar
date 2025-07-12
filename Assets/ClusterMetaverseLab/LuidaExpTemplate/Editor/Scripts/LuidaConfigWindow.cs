using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using ClusterVR.CreatorKit.Item.Implements;

public class LuidaConfigWindow : EditorWindow
{
    public static LuidaConfigWindow Instance { get; private set; }
    public static event Action OnEditorClosed;
    public static event Action OnTabSwitched;

    private int currentTab = 0;
    private string[] tabNames = { "Experiment Identifiers", "Experiment Variables", "State Machine (& Questionnaires)", "State-listening Items", "Data Collector" };

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
    
    [MenuItem("LUIDA/Configure experiment automation")]
    public static void ShowWindow()
    {
        GetWindow<LuidaConfigWindow>("LUIDA Experiment Automation Config Window");
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
                        RefreshAllTabs(); // Force refresh all tabs
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
            
            if (GUILayout.Button("Duplicate And Switch Scene"))
            {
                if (!string.IsNullOrEmpty(newSceneName))
                {
                    string newScenePath = scenePath + newSceneName + ".unity";

                    if (!File.Exists(newScenePath))
                    {
                        DuplicateSceneAndAssets(currentScenePath, newSceneName);
                        AssetDatabase.Refresh();
                        EditorSceneManager.OpenScene(newScenePath);
                        RefreshAllTabs(); // Force refresh all tabs
                        var newStateListenersFolder = $"Assets/_Experiment_/Scripts/StateManagement/{newSceneName}";
                        var newDataCollectorScriptPath = $"Assets/_Experiment_/Scripts/DataCollectors/{newSceneName}.js";
                        UpdateScriptableClusterScriptCombiners(newSceneName, newStateListenersFolder);
                        UpdateDataCollectorScriptCombiner(newSceneName, newDataCollectorScriptPath);
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
                OnTabSwitched?.Invoke();
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
    
    private void RefreshAllTabs()
    {
        expIdentifierConfigTab.OnEnable();
        experimentVariablesConfigTab.OnEnable();
        stateMachineConfigTab.OnEnable();
        itemsManagerEditor.OnEnable();
        dataCollectorConfigTab.OnEnable();
    }

    private void DuplicateSceneAndAssets(string currentScenePath, string newSceneName)
    {
        string newScenePath = scenePath + newSceneName + ".unity";
        File.Copy(currentScenePath, newScenePath);

        string currentSceneName = Path.GetFileNameWithoutExtension(currentScenePath);

        // Duplicate StateList asset
        string stateListPath = $"Assets/_Experiment_/Settings/StateList/{currentSceneName}.asset";
        string newStateListPath = $"Assets/_Experiment_/Settings/StateList/{newSceneName}.asset";
        if (File.Exists(stateListPath))
        {
            File.Copy(stateListPath, newStateListPath);
        }

        // Duplicate ExperimentVariables asset
        string experimentVariablesPath = $"Assets/_Experiment_/Settings/ExperimentVariables/{currentSceneName}.js";
        string newExperimentVariablesPath = $"Assets/_Experiment_/Settings/ExperimentVariables/{newSceneName}.js";
        if (File.Exists(experimentVariablesPath))
        {
            File.Copy(experimentVariablesPath, newExperimentVariablesPath);
        }

        // Duplicate StateListenersItemData assets
        string stateListenersFolder = $"Assets/_Experiment_/Scripts/StateManagement/{currentSceneName}/StateListeners";
        string newStateListenersFolder = $"Assets/_Experiment_/Scripts/StateManagement/{newSceneName}/StateListeners";
        if (Directory.Exists(stateListenersFolder))
        {
            Directory.CreateDirectory(newStateListenersFolder);
            foreach (string file in Directory.GetFiles(stateListenersFolder))
            {
                string newFilePath = Path.Combine(newStateListenersFolder, Path.GetFileName(file));
                File.Copy(file, newFilePath);
            }
        }

        // Duplicate DataCollector script
        string dataCollectorScriptPath = $"Assets/_Experiment_/Scripts/DataCollectors/{currentSceneName}.js";
        string newDataCollectorScriptPath = $"Assets/_Experiment_/Scripts/DataCollectors/{newSceneName}.js";
        if (File.Exists(dataCollectorScriptPath))
        {
            File.Copy(dataCollectorScriptPath, newDataCollectorScriptPath);
        }

        // Open the new scene
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(newScenePath);
    }
    
    private void UpdateScriptableClusterScriptCombiners(string newSceneName, string newStateListenersFolder)
    {
        // Find all LuidaStateListeningItem objects in the scene
        LuidaStateListeningItem[] stateListeningItems = FindObjectsOfType<LuidaStateListeningItem>();

        foreach (var item in stateListeningItems)
        {
            // Get the ScriptableClusterScriptCombiner component
            var scriptCombiner = item.GetComponent<ScriptableClusterScriptCombiner>();
            if (scriptCombiner == null)
            {
                Debug.LogWarning($"No ScriptableClusterScriptCombiner found on {item.name}");
                continue;
            }

            // Find the corresponding JavaScript asset for this StateListeningItem
            string itemName = item.name;
            string newScriptPath = Path.Combine(newStateListenersFolder, $"{itemName}.js").Replace("\\", "/");

            var newScriptAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(newScriptPath);
            if (newScriptAsset == null)
            {
                Debug.LogWarning($"JavaScript asset not found for {itemName} at {newScriptPath}");
                continue;
            }

            // Update the ScriptableClusterScriptCombiner's reference
            // scriptCombiner.ClearScripts();
            scriptCombiner.ReplaceScript(newScriptAsset, 1, null, 0, false);
            scriptCombiner.CombineScripts();
            
            // Mark the component as dirty
            EditorUtility.SetDirty(scriptCombiner);
        }

        // Save changes to the asset database
        AssetDatabase.SaveAssets();
        Debug.Log("ScriptableClusterScriptCombiners updated successfully.");
    }
    
    private void UpdateDataCollectorScriptCombiner(string newSceneName, string newDataCollectorScriptPath)
    {
        // Find the LuidaDataCollector object in the scene
        LuidaDataCollector dataCollector = FindObjectOfType<LuidaDataCollector>();
        if (dataCollector == null)
        {
            Debug.LogWarning("No LuidaDataCollector found in the scene.");
            return;
        }

        // Get the ScriptableClusterScriptCombiner component
        var scriptCombiner = dataCollector.GetComponent<ScriptableClusterScriptCombiner>();
        if (scriptCombiner == null)
        {
            Debug.LogWarning("No ScriptableClusterScriptCombiner found on LuidaDataCollector.");
            return;
        }

        // Load the new JavaScript asset
        var newScriptAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(newDataCollectorScriptPath);
        if (newScriptAsset == null)
        {
            Debug.LogWarning($"JavaScript asset not found at {newDataCollectorScriptPath}");
            return;
        }

        // Update the ScriptableClusterScriptCombiner's reference
        // scriptCombiner.ClearScripts();
        scriptCombiner.ReplaceScript(newScriptAsset, 2, null, 0, false);
        scriptCombiner.CombineScripts();

        // Mark the component as dirty
        EditorUtility.SetDirty(scriptCombiner);

        // Save changes to the asset database
        AssetDatabase.SaveAssets();
        Debug.Log("LuidaDataCollector ScriptableClusterScriptCombiner updated successfully.");
    }
}
