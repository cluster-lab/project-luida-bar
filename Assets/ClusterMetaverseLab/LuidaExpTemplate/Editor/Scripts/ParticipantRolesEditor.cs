using ClusterVR.CreatorKit.Item.Implements;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

public class ParticipantRolesEditor : EditorWindow
{
    private const string RequiredObjectsWrapperPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";
    private string rolesAssetPath;
    private JavaScriptAsset rolesAsset;
    private List<RoleSetting> roleSettings = new List<RoleSetting>();
    private string assetRemainingContent = "";

    public void OnEnable()
    {
        RetrieveOrCreateRolesAsset();
        LoadRolesFromAsset();
    }

    public void OnGUI()
    {
        if (rolesAsset == null)
        {
            GUILayout.Label("Roles Asset not found at path:", EditorStyles.boldLabel);
            GUILayout.Label(rolesAssetPath, EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Create New Roles Asset"))
            {
                RetrieveOrCreateRolesAsset();
            }
        }
        else
        {
            GUILayout.Label("Participants Number & Roles Settings", EditorStyles.boldLabel);
            if (roleSettings.Count == 0)
            {
                GUILayout.Label("No roles defined. Loading default content.", EditorStyles.wordWrappedLabel);
                LoadDefaultRolesIfEmpty();
            }
            DrawRoleSettingsForm();

            if (GUILayout.Button("Save Roles"))
            {
                if (ValidateRoles())
                {
                    SaveRolesToAsset();
                }
                else
                {
                    EditorUtility.DisplayDialog("Warning", "All roles must have a valid name and the number must be greater than 0.", "OK");
                }
            }
        }
    }

    private void DrawRoleSettingsForm()
    {
        int removeIndex = -1;
        for (int i = 0; i < roleSettings.Count; i++)
        {
            EditorGUILayout.BeginHorizontal("box");

            GUILayout.Label("Role Name", GUILayout.Width(70));
            roleSettings[i].role = EditorGUILayout.TextField(roleSettings[i].role, GUILayout.Width(150));

            GUILayout.Space(50);

            GUILayout.Label("Participants Number of this Role", GUILayout.Width(190));
            roleSettings[i].number = EditorGUILayout.IntField(roleSettings[i].number, GUILayout.Width(50));

            GUILayout.Space(50);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                removeIndex = i;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex != -1)
        {
            roleSettings.RemoveAt(removeIndex);
        }

        if (GUILayout.Button("Add New Role"))
        {
            roleSettings.Add(new RoleSetting { role = "", number = 0 });
        }
    }

    private bool ValidateRoles()
    {
        foreach (var role in roleSettings)
        {
            if (string.IsNullOrEmpty(role.role) || role.number <= 0)
            {
                return false;
            }
        }
        return true;
    }

    private void RetrieveOrCreateRolesAsset()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        rolesAssetPath = $"Assets/_Experiment_/Settings/ParticipantRoles/{sceneName}.js";
        string templatePath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/ExpSettings/ParticipantRoles.js";

        rolesAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(rolesAssetPath);
        if (rolesAsset == null)
        {
            if (!Directory.Exists(Path.GetDirectoryName(rolesAssetPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(rolesAssetPath));
            }

            if (File.Exists(templatePath))
            {
                File.Copy(templatePath, rolesAssetPath);
                AssetDatabase.Refresh();
                rolesAsset = AssetDatabase.LoadAssetAtPath<JavaScriptAsset>(rolesAssetPath);
                AssignToScriptableItem(rolesAsset);
            }
            else
            {
                Debug.LogError("Template ParticipantRoles.js not found.");
            }
        }
        if (rolesAsset != null && !string.IsNullOrEmpty(rolesAsset.text))
        {
            ParseRolesAsset(rolesAsset.text);
        }
    }

    private void LoadDefaultRolesIfEmpty()
    {
        if (roleSettings.Count == 0)
        {
            roleSettings.Add(new RoleSetting { role = "default", number = 1 });
        }
    }

    private void AssignToScriptableItem(JavaScriptAsset asset)
    {
        GameObject participantRoles = FindParticipantRolesGameObject();
        if (participantRoles == null)
        {
            Debug.LogError("ParticipantRoles GameObject not found in the scene.");
            return;
        }

        var scriptableItem = participantRoles.GetComponent<ScriptableItem>();
        if (scriptableItem == null)
        {
            Debug.LogError("ScriptableItem component not found on ParticipantRoles GameObject.");
            return;
        }

        SerializedObject serializedObject = new SerializedObject(scriptableItem);
        SerializedProperty sourceCodeAssetProp = serializedObject.FindProperty("sourceCodeAsset");
        sourceCodeAssetProp.objectReferenceValue = asset;
        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(scriptableItem);
        Debug.Log("Assigned JavaScript asset to ScriptableItem.");
    }

    private GameObject FindParticipantRolesGameObject()
    {
        GameObject requiredObjectsWrapper = FindRequiredObjectsWrapperInstance();
        if (!requiredObjectsWrapper) return null;

        for (int i = 0; i < requiredObjectsWrapper.transform.childCount; i++)
        {
            Transform child = requiredObjectsWrapper.transform.GetChild(i);
            if (child.gameObject.name == "ParticipantRoles")
            {
                return child.gameObject;
            }
        }

        return null;
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

    private void LoadRolesFromAsset()
    {
        if (rolesAsset != null && !string.IsNullOrEmpty(rolesAsset.text))
        {
            ParseRolesAsset(rolesAsset.text);
        }
    }

    private void ParseRolesAsset(string content)
    {
        roleSettings.Clear();
        MatchCollection matches = Regex.Matches(content, $@"role:\s*""([^""]+)"",\s*number:\s*(\d+)");

        foreach (Match match in matches)
        {
            roleSettings.Add(new RoleSetting
            {
                role = match.Groups[1].Value,
                number = int.Parse(match.Groups[2].Value)
            });
        }

        // Preserve the remaining content after roleSettings
        int startIndex = content.IndexOf("const roleSettings");
        int endIndex = content.IndexOf("];", startIndex) + 2;
        assetRemainingContent = content.Substring(endIndex).TrimStart();
    }

    private void SaveRolesToAsset()
    {
        string content = GenerateRolesJavaScript() + "\n" + assetRemainingContent;
        File.WriteAllText(rolesAssetPath, content);

        SerializedObject serializedObject = new SerializedObject(rolesAsset);
        SerializedProperty textProperty = serializedObject.FindProperty("text");
        textProperty.stringValue = content;
        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(rolesAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Participant roles saved successfully.");
    }

    private string GenerateRolesJavaScript()
    {
        string js = "const roleSettings = [\n";
        foreach (var role in roleSettings)
        {
            js += $"  {{ role: \"{role.role}\", number: {role.number} }},\n";
        }
        js += "];\n";
        return js;
    }

    private class RoleSetting
    {
        public string role;
        public int number;
    }
}
