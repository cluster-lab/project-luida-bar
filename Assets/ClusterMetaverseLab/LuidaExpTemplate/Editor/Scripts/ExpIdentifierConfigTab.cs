using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class ExpIdentifierConfigTab : EditorWindow
{
    private string prevExpID = "";
    private string prevToken = "";
    private string prevCallExternalEndpointID = "";
    private int prevPNum = 1;
    
    private string expID = "";
    private string token = "";
    private string callExternalEndpointID = "";
    private int pNum = 1;
    
    private string filePath;
    private bool isSubscribed = false;
    private const string formPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/Questionnaire/Questionnaire.prefab";
    private const string StateQuestionnaireRootName = "LUIDA-QuestionnaireByState";
    
    [MenuItem("LUIDA/Configure experiment identifiers")]
    public static void ShowWindow()
    {
        GetWindow<ExpIdentifierConfigTab>("LUIDA Experiment Identifiers Config Window");
    }
    
    public void OnEnable()
    {
        // Set the file path to the single ExpIdentifiers.js file
        filePath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";

        // Load the existing values if the file exists
        if (File.Exists(filePath))
        {
            LoadExpIdentifiers();
        }
    }

    public void OnGUI()
    {
        string newExpID = EditorGUILayout.TextField("Experiment ID", expID);
        string newToken = EditorGUILayout.TextField("Verify Token", token);
        string newCallExternalEndpointID = EditorGUILayout.TextField("callExternal Endpoint ID", callExternalEndpointID);
        int newPNum = EditorGUILayout.IntField("Number of Participants", pNum);

        bool hasChanged = false;

        if (newExpID != prevExpID)
        {
            expID = newExpID;
            prevExpID = newExpID;
            hasChanged = true;
        }

        if (newToken != prevToken)
        {
            token = newToken;
            prevToken = newToken;
            hasChanged = true;
        }

        if (newCallExternalEndpointID != prevCallExternalEndpointID)
        {
            callExternalEndpointID = newCallExternalEndpointID;
            prevCallExternalEndpointID = newCallExternalEndpointID;
            hasChanged = true;
        }

        if (newPNum != prevPNum)
        {
            pNum = newPNum;
            prevPNum = newPNum;
            hasChanged = true;
            UpdateQuestionnaireObjects();
        }

        if (hasChanged)
        {
            SaveExpIdentifiers();
        }
    }

    private void LoadExpIdentifiers()
    {
        string content = File.ReadAllText(filePath);

        expID = ExtractStringValue(content, "expID");
        token = ExtractStringValue(content, "token");
        callExternalEndpointID = ExtractStringValue(content, "callExternalEndpointID");
        pNum = ExtractIntValue(content, "pNum");
    }

    private string ExtractStringValue(string content, string key)
    {
        var pattern = $@"{key}\s*=\s*""([^""]+)"";";
        var match = Regex.Match(content, pattern);
        return match.Success ? match.Groups[1].Value : "";
    }

    private int ExtractIntValue(string content, string key)
    {
        var pattern = $@"{key}\s*=\s*(\d+);";
        var match = Regex.Match(content, pattern);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private void SaveExpIdentifiers()
    {
        // Ensure the directory exists
        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        // Create the new content with the updated expID and token using double quotes
        string content =
            $"expID = \"{expID}\";\n" +
            $"token = \"{token}\";\n" +
            $"callExternalEndpointID = \"{callExternalEndpointID}\";\n" +
            $"pNum = {pNum};\n";

        File.WriteAllText(filePath, content);

        // Refresh the asset database to reflect changes in Unity
        AssetDatabase.Refresh();

        Debug.Log($"Experiment identifiers saved to {filePath}");
    }

    private void UpdateQuestionnaireObjects()
    {
        int newPNum = pNum;
        var wnd = LuidaConfigWindow.Instance;

        var questionnaireGroups = new Dictionary<Transform, List<GameObject>>();
        var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (var go in allGameObjects)
        {
            if (!go.scene.isLoaded || PrefabUtility.GetNearestPrefabInstanceRoot(go) != go)
            {
                continue;
            }

            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == formPrefabPath)
            {
                var parent = go.transform.parent;
                if (parent == null) continue;

                if (!questionnaireGroups.ContainsKey(parent))
                {
                    questionnaireGroups[parent] = new List<GameObject>();
                }
                questionnaireGroups[parent].Add(go);
            }
        }

        foreach (var group in questionnaireGroups)
        {
            var parentTransform = group.Key;
            var childrenCount = group.Value.Count;

            if (childrenCount == newPNum) continue;

            if (parentTransform.parent != null && parentTransform.parent.name == StateQuestionnaireRootName)
            {
                if (wnd != null && wnd.StateTab != null && wnd.StateTab.stateList != null)
                {
                    string stateName = parentTransform.name;
                    var stateList = wnd.StateTab.stateList;

                    for (int i = 0; i < stateList.States.Length; i++)
                    {
                        if (stateList.States[i].StateName == stateName && stateList.States[i].qID > 0)
                        {
                            Debug.Log($"Updating state-linked questionnaire '{stateName}' to have {newPNum} forms.");
                            // CHANGED: Passed newPNum as an argument
                            QuestionnaireEditorManager.AddOrEnableQuestionnaireForm(stateList, i, stateName, newPNum);
                            break;
                        }
                    }
                }
            }
            else
            {
                LuidaQuestionnaire idSync = parentTransform.GetComponent<LuidaQuestionnaire>();
                if (idSync != null)
                {
                    int qID = idSync.qId;
                    Debug.Log($"Updating directly-created questionnaire with qID {qID} to have {newPNum} forms.");
                    
                    Undo.DestroyObjectImmediate(parentTransform.gameObject);
                    // CHANGED: Passed newPNum as an argument
                    QuestionnaireEditorManager.CreateQuestionnaireDirectly(qID, newPNum);
                }
            }
        }
    }
}
