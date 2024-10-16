using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class DataRecorderListEditor : EditorWindow
{
    private const string DataRecorderPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/CustomDataRecording/CustomDataRecorder.prefab";
    private const string RequiredObjectsWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";
    private const string DataRecordersParentName = "DataRecorders";
    private const string WorldItemRefListObjectName = "WorldItemRefList";

    private const string IdentifiersAssetPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string CalculatorTemplateAssetPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/CustomDataRecording/CustomDataCalculatorTemplate.js";
    private const string DataRecorderScriptFolderPath = "Assets/_Experiment_/Scripts/DataRecorder/";

    private List<GameObject> customDataRecorders;
    private Dictionary<GameObject, ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset> recorderToCalculatorMap;

    public void OnEnable()
    {
        FindCustomDataRecorders();
    }

    public void OnGUI()
    {
        GUILayout.Label("Custom Data Recorder Instances", EditorStyles.boldLabel);

        if (customDataRecorders == null || customDataRecorders.Count == 0)
        {
            GUILayout.Label("No Custom Data Recorder instances found in the scene.");
        }
        else
        {
            for (int i = 0; i < customDataRecorders.Count; i++)
            {
                EditorGUILayout.BeginHorizontal(); // Start a horizontal layout to display the recorder, calculator field, and button in the same row

                // Display the Custom Data Recorder object field
                EditorGUILayout.ObjectField("Custom Data Recorder", customDataRecorders[i], typeof(GameObject), true);

                // Display the reference to the associated calculator asset
                if (recorderToCalculatorMap.ContainsKey(customDataRecorders[i]))
                {
                    EditorGUILayout.ObjectField("Calculator JS", recorderToCalculatorMap[customDataRecorders[i]], typeof(ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset), false);
                }
                else
                {
                    GUILayout.Label("No Calculator Found");
                }

                // Add a button to remove the Custom Data Recorder
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    RemoveCustomDataRecorder(customDataRecorders[i]);
                }

                EditorGUILayout.EndHorizontal(); // End the horizontal layout
            }
        }

        if (GUILayout.Button("Add Custom Data Recorder"))
        {
            AddCustomDataRecorder();
        }
    }

    private void FindCustomDataRecorders()
    {
        customDataRecorders = new List<GameObject>();
        recorderToCalculatorMap = new Dictionary<GameObject, ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>();

        GameObject dataRecordersParent = GameObject.Find(DataRecordersParentName);

        if (dataRecordersParent != null)
        {
            foreach (Transform child in dataRecordersParent.transform)
            {
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) == DataRecorderPrefabPath)
                {
                    customDataRecorders.Add(child.gameObject);
                    LinkCalculatorScript(child.gameObject); // Link the associated calculator JS asset
                }
            }
        }
    }

    private void AddCustomDataRecorder()
    {
        GameObject dataRecordersParent = GameObject.Find(DataRecordersParentName);
        if (dataRecordersParent == null)
        {
            // Create the 'DataRecorders' parent object if it doesn't exist
            dataRecordersParent = new GameObject(DataRecordersParentName);
        }

        // Load the CustomDataRecorder prefab
        GameObject customDataRecorderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DataRecorderPrefabPath);
        if (customDataRecorderPrefab == null)
        {
            Debug.LogError("CustomDataRecorder prefab not found at path: " + DataRecorderPrefabPath);
            return;
        }

        // Instantiate the CustomDataRecorder prefab under 'DataRecorders'
        GameObject newRecorderInstance = (GameObject)PrefabUtility.InstantiatePrefab(customDataRecorderPrefab);
        newRecorderInstance.name = GetUniqueName(dataRecordersParent, "CustomDataRecorder");
        newRecorderInstance.transform.SetParent(dataRecordersParent.transform);

        // Find WorldItemRefList in the ExpTemplateRequiredObjects prefab instance
        GameObject expTemplateInstance = FindRequiredObjectsWrapperInstance();
        if (expTemplateInstance != null)
        {
            GameObject worldItemRefList = expTemplateInstance.transform.Find(WorldItemRefListObjectName)?.gameObject;
            if (worldItemRefList != null)
            {
                // Copy the WorldItemReferenceList component to the new CustomDataRecorder instance
                var worldItemReferenceList = worldItemRefList.GetComponent<ClusterVR.CreatorKit.Item.Implements.WorldItemReferenceList>();
                if (worldItemReferenceList != null)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(worldItemReferenceList);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(newRecorderInstance);
                    Debug.Log("WorldItemReferenceList component copied to new CustomDataRecorder.");
                }
                else
                {
                    Debug.LogError("WorldItemReferenceList component not found in WorldItemRefList.");
                }
            }
            else
            {
                Debug.LogError($"WorldItemRefList GameObject not found in {expTemplateInstance.name}.");
            }
        }
        else
        {
            Debug.LogError("ExpTemplateRequiredObjects prefab instance not found in the scene.");
        }

        // Duplicate the calculator script and set up the ScriptableClusterScriptCombiner
        DuplicateAndSetupCalculatorScript(newRecorderInstance);

        // Refresh the list of CustomDataRecorders in the scene
        FindCustomDataRecorders();
    }

    private void RemoveCustomDataRecorder(GameObject recorder)
    {
        if (recorder != null)
        {
            DestroyImmediate(recorder);
            Debug.Log("CustomDataRecorder removed.");
        }

        // Refresh the list of CustomDataRecorders after removal
        FindCustomDataRecorders();
    }

    private GameObject FindRequiredObjectsWrapperInstance()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) == RequiredObjectsWrapperPrefabPath)
            {
                Debug.Log(obj.name);
                return obj;
            }
        }
        return null;
    }

    private void DuplicateAndSetupCalculatorScript(GameObject newRecorderInstance)
    {
        // Load the identifiers and calculator template assets
        var identifiersAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(IdentifiersAssetPath);
        var calculatorTemplateAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(CalculatorTemplateAssetPath);

        if (identifiersAsset == null || calculatorTemplateAsset == null)
        {
            Debug.LogError("Failed to load Identifiers or Calculator Template assets.");
            return;
        }

        // Create the script folder if it doesn't exist
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string newCalculatorPath = $"{DataRecorderScriptFolderPath}{sceneName}-DataCalculator.js";
        newCalculatorPath = GetUniqueScriptPath(newCalculatorPath);

        if (!Directory.Exists(DataRecorderScriptFolderPath))
        {
            Directory.CreateDirectory(DataRecorderScriptFolderPath);
        }

        // Duplicate the calculator template to the scene-specific path
        AssetDatabase.CopyAsset(CalculatorTemplateAssetPath, newCalculatorPath);
        AssetDatabase.Refresh();

        var newCalculatorAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(newCalculatorPath);
        if (newCalculatorAsset == null)
        {
            Debug.LogError("Failed to duplicate the Calculator template asset.");
            return;
        }

        // Retrieve the ScriptableClusterScriptCombiner and run ReplaceScript
        var scriptCombiner = newRecorderInstance.GetComponent<ScriptableClusterScriptCombiner>();
        if (scriptCombiner != null)
        {
            scriptCombiner.ReplaceScript(identifiersAsset, 0, null, 0, false);
            scriptCombiner.ReplaceScript(newCalculatorAsset, 2, null, 0, true);
            Debug.Log("Scripts added to ScriptableClusterScriptCombiner.");
        }
        else
        {
            Debug.LogError("ScriptableClusterScriptCombiner component not found on the CustomDataRecorder instance.");
        }

        // Map the new CustomDataRecorder instance to its calculator JS asset
        recorderToCalculatorMap[newRecorderInstance] = newCalculatorAsset;
    }

    // Links an existing CustomDataRecorder to its corresponding calculator JS asset (if it exists)
    private void LinkCalculatorScript(GameObject recorder)
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string calculatorPath = $"{DataRecorderScriptFolderPath}{sceneName}-DataCalculator.js";
        var calculatorAsset = AssetDatabase.LoadAssetAtPath<ClusterVR.CreatorKit.Item.Implements.JavaScriptAsset>(calculatorPath);

        if (calculatorAsset != null)
        {
            recorderToCalculatorMap[recorder] = calculatorAsset;
        }
    }

    // Ensures the instance name is unique by appending a number if necessary
    private string GetUniqueName(GameObject parent, string baseName)
    {
        int counter = 1;
        string newName = baseName;
        while (parent.transform.Find(newName) != null)
        {
            newName = baseName + " (" + counter + ")";
            counter++;
        }
        return newName;
    }

    // Ensures the script path is unique by appending a number if necessary
    private string GetUniqueScriptPath(string basePath)
    {
        string directory = Path.GetDirectoryName(basePath);
        string filename = Path.GetFileNameWithoutExtension(basePath);
        string extension = Path.GetExtension(basePath);

        int counter = 1;
        string newPath = basePath;
        while (File.Exists(newPath))
        {
            newPath = Path.Combine(directory, $"{filename} ({counter}){extension}");
            counter++;
        }
        return newPath;
    }
}
