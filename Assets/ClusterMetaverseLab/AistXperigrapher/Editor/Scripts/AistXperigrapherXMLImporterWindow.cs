using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Linq;
using System;
using System.Linq;

public class AistXperigrapherXMLImporterWindow : EditorWindow
{
    private string xmlFilePath = "";

    [MenuItem("Tools/AIST Xperigrapher XML Importer")]
    public static void ShowWindow()
    {
        GetWindow<AistXperigrapherXMLImporterWindow>("AIST Xperigrapher XML Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Import XML File", EditorStyles.boldLabel);
        if (GUILayout.Button("Select XML File"))
        {
            string path = EditorUtility.OpenFilePanel("Select XML file", "", "xml");
            if (!string.IsNullOrEmpty(path))
                xmlFilePath = path;
        }
        GUILayout.Label("Selected File: " + (string.IsNullOrEmpty(xmlFilePath) ? "None" : xmlFilePath));
        if (!string.IsNullOrEmpty(xmlFilePath))
        {
            if (GUILayout.Button("Import XML"))
                ImportXML(xmlFilePath);
        }
    }

    private void ImportXML(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist: " + path);
            return;
        }
        try
        {
            XDocument doc = XDocument.Load(path);
            XElement root = doc.Root;
            if (root == null)
            {
                Debug.LogError("Invalid XML: missing root element.");
                return;
            }
            string xmlFolder = Path.GetDirectoryName(path);
            string prefabPath = "Assets/ClusterMetaverseLab/AistXperigrapher/Runtime/Prefabs/AistXperigrapherObject.prefab";
            GameObject aistPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (aistPrefab == null)
            {
                Debug.LogError("Cannot load prefab at " + prefabPath);
                return;
            }
            foreach (XElement vi in root.Elements("VideoAndImage"))
            {
                string modelPath = (string)vi.Attribute("path") ?? "";
                if (string.IsNullOrEmpty(modelPath))
                {
                    Debug.LogWarning("Missing 'path' attribute.");
                    continue;
                }
                string assetPath = GetAssetPathForExternalModel(modelPath, xmlFolder);
                XElement placement = vi.Element("Placement");
                if (placement == null)
                {
                    Debug.LogWarning("No <Placement> element for " + modelPath);
                    continue;
                }
                Vector3 pos = ParseVector3((string)placement.Attribute("position"), Vector3.zero);
                Vector3 rot = ParseVector3((string)placement.Attribute("rotation"), Vector3.zero);
                Vector3 scl = ParseVector3((string)placement.Attribute("size"), Vector3.one);
                
                GameObject rootObj = (GameObject)PrefabUtility.InstantiatePrefab(aistPrefab);
                if (rootObj == null)
                {
                    Debug.LogError("Failed to instantiate prefab.");
                    continue;
                }
                rootObj.name = Path.GetFileNameWithoutExtension(modelPath);
                rootObj.transform.position = pos;
                rootObj.transform.eulerAngles = rot;
                rootObj.transform.localScale = scl;
                
                GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (modelPrefab != null)
                {
                    GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, rootObj.transform);
                    if (modelInstance != null)
                    {
                        modelInstance.name = modelPrefab.name;
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localRotation = Quaternion.identity;
                        modelInstance.transform.localScale = Vector3.one;
                    }
                    else
                        Debug.LogWarning("Failed to instantiate model at " + assetPath);
                }
                else
                {
                    Debug.LogWarning("Could not load model at " + assetPath);
                }
                
                PlacementSettings ps = rootObj.GetComponent<PlacementSettings>();
                if (ps == null)
                    ps = rootObj.AddComponent<PlacementSettings>();
                ps.dataPath = modelPath;
                ps.position = pos;
                ps.rotation = rot;
                ps.size = scl;
                ps.coordSys = (string)placement.Attribute("coordSys") ?? "World";
                ps.myID = (string)placement.Attribute("myID") ?? "";
                ps.variableFilePath = (string)placement.Attribute("variableFilePath") ?? "";
                ps.shape = (string)placement.Attribute("shape") ?? "Plane";
                ps.maxDistance = ParseFloat((string)placement.Attribute("maxDistance"), 50f);
                ps.volumeRolloff = (string)placement.Attribute("volumeRolloff") ?? "Logarithmic";
                ps.loop = ParseBool((string)placement.Attribute("loop"), true);
                ps.volume = ParseFloat((string)placement.Attribute("volume"), 1f);
                
                var triggers = vi.Elements("Trigger");
                if (triggers != null && triggers.Any())
                {
                    foreach (XElement trig in triggers)
                    {
                        TriggerSettings ts = rootObj.AddComponent<TriggerSettings>();
                        ts.triggerTime = ParseFloat((string)trig.Attribute("time"), 0f);
                        ts.actionType = (string)trig.Attribute("actionType") ?? "Start";
                        ts.eventType = (string)trig.Attribute("eventType") ?? "Time";
                        ts.vector = ParseVector3((string)trig.Attribute("vector"), Vector3.zero);
                        ts.radius = ParseFloat((string)trig.Attribute("radius"), 0f);
                        ts.basicColliderNumber = ParseInt((string)trig.Attribute("basicColliderNumber"), 0);
                        ts.coordSys = (string)trig.Attribute("coordSys") ?? "World";
                        ts.objectFilePath = (string)trig.Attribute("objectFilePath") ?? "";
                        ts.size = ParseVector3((string)trig.Attribute("size"), Vector3.one);
                        ts.value = ParseFloat((string)trig.Attribute("value"), 0f);
                        ts.compareOp = (string)trig.Attribute("compareOp") ?? "MoreThan";
                        ts.key = (string)trig.Attribute("key") ?? "None";
                        ts.targetLink = (string)trig.Attribute("targetLink") ?? "LeftArm";
                        ts.colliderID = (string)trig.Attribute("colliderID") ?? "";
                        ts.triggerRotation = ParseVector3((string)trig.Attribute("rotation"), Vector3.zero);
                        ts.triggerPosition = ParseVector3((string)trig.Attribute("position"), Vector3.zero);
                        ts.ReflectXmlSettingsToCCK();
                    }
                }
                else
                    Debug.Log("No <Trigger> element for " + modelPath);
                
                ps.position = rootObj.transform.position;
                ps.rotation = rootObj.transform.eulerAngles;
                ps.size = rootObj.transform.localScale;
                Debug.LogFormat("Created '{0}' for model '{1}' at {2}, {3}, {4}.", rootObj.name, modelPath, ps.position, ps.rotation, ps.size);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error importing XML: " + ex.Message);
        }
    }

    private string GetAssetPathForExternalModel(string externalModelPath, string xmlFolder)
    {
        if (!Path.IsPathRooted(externalModelPath))
            externalModelPath = Path.Combine(xmlFolder, externalModelPath);
        const string targetFolder = "Assets/ExternalImportedModels/";
        if (!Directory.Exists(targetFolder))
            Directory.CreateDirectory(targetFolder);
        string fileName = Path.GetFileName(externalModelPath);
        string destinationPath = Path.Combine(targetFolder, fileName).Replace("\\", "/");
        if (!File.Exists(destinationPath))
        {
            try
            {
                File.Copy(externalModelPath, destinationPath, true);
                Debug.Log("Copied model from " + externalModelPath + " to " + destinationPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to copy model: " + ex.Message);
            }
        }
        AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceUpdate);
        return destinationPath;
    }

    private Vector3 ParseVector3(string s, Vector3 defaultValue)
    {
        if (string.IsNullOrEmpty(s))
            return defaultValue;
        string[] parts = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            return defaultValue;
        if (float.TryParse(parts[0], out float x) &&
            float.TryParse(parts[1], out float y) &&
            float.TryParse(parts[2], out float z))
            return new Vector3(x, y, z);
        return defaultValue;
    }

    private float ParseFloat(string s, float defaultValue)
    {
        if (string.IsNullOrEmpty(s))
            return defaultValue;
        return float.TryParse(s, out float r) ? r : defaultValue;
    }

    private int ParseInt(string s, int defaultValue)
    {
        if (string.IsNullOrEmpty(s))
            return defaultValue;
        return int.TryParse(s, out int r) ? r : defaultValue;
    }

    private bool ParseBool(string s, bool defaultValue)
    {
        if (string.IsNullOrEmpty(s))
            return defaultValue;
        return bool.TryParse(s, out bool r) ? r : defaultValue;
    }
}
