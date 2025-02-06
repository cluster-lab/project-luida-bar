using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;

public class AistXperigrapherXMLExporterWindow : EditorWindow
{
    [MenuItem("Tools/AIST Xperigrapher XML Exporter")]
    public static void ShowWindow()
    {
        GetWindow<AistXperigrapherXMLExporterWindow>("AIST Xperigrapher XML Exporter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Scene to XML", EditorStyles.boldLabel);
        if (GUILayout.Button("Export Scene"))
            ExportScene();
    }

    private void ExportScene()
    {
        string exportFolder = Path.Combine(Directory.GetCurrentDirectory(), "ExportedScene_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(exportFolder);
        string modelsFolder = Path.Combine(exportFolder, "Models");
        Directory.CreateDirectory(modelsFolder);

        AistXperigrapherObject[] aistObjects = FindObjectsOfType<AistXperigrapherObject>();
        XElement root = new XElement("VideoAndImages");
        HashSet<string> copiedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (AistXperigrapherObject aistObj in aistObjects)
        {
            GameObject go = aistObj.gameObject;
            PlacementSettings ps = go.GetComponent<PlacementSettings>();
            if (ps == null)
                ps = go.AddComponent<PlacementSettings>();
            ps.position = go.transform.position;
            ps.rotation = go.transform.eulerAngles;
            ps.size = go.transform.localScale;

            string modelSourcePath = ps.dataPath;
            if (string.IsNullOrEmpty(modelSourcePath))
            {
                string childModelPath = GetModelFilePathFromChildPrefab(go);
                if (!string.IsNullOrEmpty(childModelPath))
                    modelSourcePath = childModelPath;
                else if (PrefabUtility.GetPrefabInstanceStatus(go) != PrefabInstanceStatus.NotAPrefab)
                    modelSourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                ps.dataPath = modelSourcePath;
            }

            string relativeModelPath = "";
            if (!string.IsNullOrEmpty(modelSourcePath))
            {
                string absoluteModelPath = GetAbsolutePathForModel(modelSourcePath);
                string fileName = Path.GetFileName(absoluteModelPath);
                if (!copiedModels.Contains(absoluteModelPath))
                {
                    string destPath = Path.Combine(modelsFolder, fileName);
                    try
                    {
                        File.Copy(absoluteModelPath, destPath, true);
                        copiedModels.Add(absoluteModelPath);
                        Debug.Log("Copied model: " + absoluteModelPath + " to " + destPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Error copying model: " + absoluteModelPath + "\n" + ex.Message);
                    }
                }
                relativeModelPath = "Models/" + fileName;
            }

            XElement videoAndImage = new XElement("VideoAndImage",
                new XAttribute("path", relativeModelPath));
            XElement placementElement = new XElement("Placement",
                new XAttribute("position", FormatVector3(go.transform.position)),
                new XAttribute("rotation", FormatVector3(go.transform.eulerAngles)),
                new XAttribute("size", FormatVector3(go.transform.localScale)),
                new XAttribute("coordSys", ps.coordSys),
                new XAttribute("myID", ps.myID),
                new XAttribute("variableFilePath", ps.variableFilePath),
                new XAttribute("shape", ps.shape),
                new XAttribute("maxDistance", ps.maxDistance),
                new XAttribute("volumeRolloff", ps.volumeRolloff),
                new XAttribute("loop", ps.loop.ToString().ToLower()),
                new XAttribute("volume", ps.volume));
            videoAndImage.Add(placementElement);

            TriggerSettings[] triggers = go.GetComponents<TriggerSettings>();
            foreach (TriggerSettings ts in triggers)
            {
                XElement triggerElement = new XElement("Trigger",
                    new XAttribute("time", ts.triggerTime),
                    new XAttribute("actionType", ts.actionType),
                    new XAttribute("eventType", ts.eventType),
                    new XAttribute("vector", FormatVector3(ts.vector)),
                    new XAttribute("radius", ts.radius),
                    new XAttribute("basicColliderNumber", ts.basicColliderNumber),
                    new XAttribute("coordSys", ts.coordSys),
                    new XAttribute("objectFilePath", ts.objectFilePath),
                    new XAttribute("size", FormatVector3(ts.size)),
                    new XAttribute("value", ts.value),
                    new XAttribute("compareOp", ts.compareOp),
                    new XAttribute("key", ts.key),
                    new XAttribute("targetLink", ts.targetLink),
                    new XAttribute("colliderID", ts.colliderID),
                    new XAttribute("rotation", FormatVector3(ts.triggerRotation)),
                    new XAttribute("position", FormatVector3(ts.triggerPosition)));
                videoAndImage.Add(triggerElement);
            }
            root.Add(videoAndImage);
        }
        XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
        string xmlPath = Path.Combine(exportFolder, "scene.xml");
        try
        {
            doc.Save(xmlPath);
            Debug.Log("Scene exported to " + xmlPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error exporting scene: " + ex.Message);
        }
    }

    private string GetModelFilePathFromChildPrefab(GameObject go)
    {
        foreach (Transform child in go.transform)
        {
            if (PrefabUtility.GetPrefabInstanceStatus(child.gameObject) != PrefabInstanceStatus.NotAPrefab)
            {
                string childPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject);
                if (!string.IsNullOrEmpty(childPath))
                    return childPath;
            }
        }
        return "";
    }

    private string GetAbsolutePathForModel(string modelPath)
    {
        if (string.IsNullOrEmpty(modelPath))
            return "";
        if (Path.IsPathRooted(modelPath))
            return modelPath;
        else
            return Path.Combine(Directory.GetCurrentDirectory(), modelPath);
    }

    private string FormatVector3(Vector3 vec)
    {
        return $"{vec.x} {vec.y} {vec.z}";
    }
}
