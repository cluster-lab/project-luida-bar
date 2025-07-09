using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public class ExpIdentifierConfigTab : EditorWindow
{
    private string expID = "";
    private string token = "";
    private string callExternalEndpointID = "";
    private int pNum = 1;
    private string filePath;
    private bool isSubscribed = false;
    private const string formPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/Questionnaire/Questionnaire.prefab";

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
            LuidaConfigWindow.OnEditorClosed += SaveExpIdentifiers;
            LuidaConfigWindow.OnEditorClosed += OnDisable;
            LuidaConfigWindow.OnItemsManagerTabLostFocus += SaveExpIdentifiers;
            isSubscribed = true;
        }
    }

    public void OnDisable()
    {
        if (isSubscribed)
        {
            LuidaConfigWindow.OnEditorClosed -= SaveExpIdentifiers;
            LuidaConfigWindow.OnEditorClosed -= OnDisable;
            LuidaConfigWindow.OnItemsManagerTabLostFocus -= SaveExpIdentifiers;
            isSubscribed = false;
        }
    }

    public void OnGUI()
    {
        GUILayout.Label("Experiment Identifiers", EditorStyles.boldLabel);

        expID = EditorGUILayout.TextField("Experiment ID", expID);
        token = EditorGUILayout.TextField("Verify Token", token);
        callExternalEndpointID = EditorGUILayout.TextField("callExternal Endpoint ID", callExternalEndpointID);
        pNum = EditorGUILayout.IntField("Number of Participants", pNum);

        // if (GUILayout.Button("Save Identifiers"))
        // {
        //     SaveExpIdentifiers();
        // }
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

        UpdateQuestionnaireObjects();
    }

    private void UpdateQuestionnaireObjects()
    {
        var wnd = LuidaConfigWindow.Instance;
        if (!wnd || !wnd.StateTab.stateList || wnd.StateTab.stateList.States == null) return;

        int newPNum = pNum;

        // for each state, count how many questionnaire children are in the scene
        for (var i = 0; i < wnd.StateTab.stateList.States.Length; i++)
        {
            var state = wnd.StateTab.stateList.States[i];
            if (state.qID <= 0) continue;
            
            var go = wnd.StateTab.FindStateObject(state.StateName);
            var objs = go?.transform.Find("Objects");
            var existingCount = 0;
            if (objs)
            {
                foreach (Transform child in objs)
                {
                    if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject)
                        == formPrefabPath)
                        existingCount++;
                }
            }

            if (existingCount != newPNum)
            {
                wnd.StateTab.AddOrEnableQuestionnaireForm(i, state.StateName);
            }
        }
    }
}
