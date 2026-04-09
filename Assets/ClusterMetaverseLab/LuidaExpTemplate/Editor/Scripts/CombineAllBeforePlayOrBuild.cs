using ClusterVR.CreatorKit.Editor.EditorEvents;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

[InitializeOnLoad]
public class CombineAllBeforePlayOrBuild
{
    private static bool _isWorldUpload = false;
    private const string ExpIdentifiersPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";

    static CombineAllBeforePlayOrBuild()
    {
        WorldUploadEvents.RegisterOnWorldUploadStart(OnWorldUploadStarted, -1);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static bool OnWorldUploadStarted(WorldUploadStartEventData data)
    {
        ExperimentVariablesConfigTab.ResetAllDebugValues();
        _isWorldUpload = true;
        OnPlayModeStateChanged(PlayModeStateChange.ExitingEditMode);
        return true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            var luidaWindow = Resources.FindObjectsOfTypeAll<LuidaConfigWindow>().FirstOrDefault();
            if (luidaWindow != null) {
                Debug.Log("luidaWindow opened");
                ExperimentVariablesConfigTab.IsApplyingVariableUpdates = true;
                ItemsManagerAssetUtil.IsApplyingAssetsToScripts = true;
                luidaWindow.Close();
                EditorApplication.delayCall += WaitForUpdatesAndExecute;
            }
            else
            {
                Debug.Log("luidaWindow closed");
                CombineAll();
            }
        }
    }
    
    private static void WaitForUpdatesAndExecute()
    {
        if (!ExperimentVariablesConfigTab.IsApplyingVariableUpdates && !ItemsManagerAssetUtil.IsApplyingAssetsToScripts)
        {
            CombineAll();
        }
        else
        {
            EditorApplication.delayCall += WaitForUpdatesAndExecute;
        }
    }
    
    private static void CombineAll() {
        if (_isWorldUpload)
        {
            SetTestModeInExpIdentifiers(false);
        }

        Type csCombinerType = Type.GetType("Assets.KaomoLab.CSCombiner.CSCombiner, Assembly-CSharp-Editor");
        if (csCombinerType != null)
        {
            var method = csCombinerType.GetMethod("CombineAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, null);
        }

        if (_isWorldUpload)
        {
            SetTestModeInExpIdentifiers(true);
            _isWorldUpload = false;
        }
    }

    private static void SetTestModeInExpIdentifiers(bool isTestMode)
    {
        if (!File.Exists(ExpIdentifiersPath)) return;

        string content = File.ReadAllText(ExpIdentifiersPath);
        string replacement = $"isTestMode = {isTestMode.ToString().ToLower()};";

        if (Regex.IsMatch(content, @"isTestMode\s*=\s*(true|false);"))
        {
            content = Regex.Replace(content, @"isTestMode\s*=\s*(true|false);", replacement);
        }
        else
        {
            content += $"\n{replacement}\n";
        }

        File.WriteAllText(ExpIdentifiersPath, content);
        AssetDatabase.ImportAsset(ExpIdentifiersPath, ImportAssetOptions.ForceSynchronousImport);
    }
}
