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

    // Sources that declare `isTestMode`. The pre-upload flip rewrites each
    // file's `isTestMode = (true|false);` line so the uploaded combined scripts
    // ship with isTestMode=false. ExpIdentifiers.js feeds ParticipantManager /
    // DataCollector; ConditionManager.js declares its own copy (it isn't
    // CSCombined with ExpIdentifiers.js).
    private static readonly string[] TestModeSourcePaths = {
        "Assets/_Experiment_/Settings/ExpIdentifiers.js",
        "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/ConditionManagement/ConditionManager.js",
    };

    static CombineAllBeforePlayOrBuild()
    {
        WorldUploadEvents.RegisterOnWorldUploadStart(OnWorldUploadStarted, -1);
        WorldUploadEvents.RegisterOnWorldUploadEnd(OnWorldUploadEnded, -1);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void RunCSCombiner()
    {
        AvatarsConfigAssetUtil.GenerateAvatarGimmickTriggerConfig();

        Type csCombinerType = Type.GetType("Assets.KaomoLab.CSCombiner.CSCombiner, Assembly-CSharp-Editor");
        if (csCombinerType != null)
        {
            var method = csCombinerType.GetMethod("CombineAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, null);
        }
    }

    static bool OnWorldUploadStarted(WorldUploadStartEventData data)
    {
        // Loud beacon: if you don't see this red line in the Unity console
        // when you start an upload, the new build of this file isn't live
        // (compile error somewhere in the project, or Auto Refresh is off).
        Debug.LogError("[LUIDA] OnWorldUploadStarted fired");

        // The combine-and-bake pipeline runs synchronously below in the
        // common case, but defers via EditorApplication.delayCall when a
        // LuidaConfigWindow is open (see OnPlayModeStateChanged). The
        // upload itself does NOT wait for delayCall, so an upload kicked
        // off with the window open would ship a stale combined script
        // (most importantly with isTestMode = true, which silently
        // disables eligibility/platform checks at runtime). Refuse the
        // upload in that case and tell the user to close the window.
        var luidaWindow = Resources.FindObjectsOfTypeAll<LuidaConfigWindow>().FirstOrDefault();
        if (luidaWindow != null)
        {
            EditorUtility.DisplayDialog(
                "LUIDA: Close configuration window before uploading",
                "The LUIDA configuration window is open. The pre-upload " +
                "combine step would be deferred and the world would be " +
                "uploaded with a stale script (e.g. isTestMode = true, " +
                "which disables platform/eligibility rejection).\n\n" +
                "Close the LUIDA window and try the upload again.",
                "OK"
            );
            return false;
        }

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
            SetTestMode(false);
        }

        // Remove orphaned/broken GlobalLogic components before validation runs.
        GlobalLogicScrubber.ScrubActiveScene();

        // Regenerate avatar gimmick trigger config before combining
        AvatarsConfigAssetUtil.GenerateAvatarGimmickTriggerConfig();

        Type csCombinerType = Type.GetType("Assets.KaomoLab.CSCombiner.CSCombiner, Assembly-CSharp-Editor");
        if (csCombinerType != null)
        {
            var method = csCombinerType.GetMethod("CombineAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, null);
        }

        // The restore-to-test-mode step that used to live here ran inside
        // the same call as the upload-state bake, but CCK's TryExportAssets
        // can re-fire CSCombiner (via its own playModeStateChanged listeners
        // or domain reloads triggered by BuildAssetBundles) AFTER we'd
        // already restored the source — so the prefab got re-baked with
        // test-mode-state and the upload shipped that. The restore now
        // lives in OnWorldUploadEnded, which fires after CCK has finished
        // serializing and uploading the bundle.
    }

    static void OnWorldUploadEnded(WorldUploadEndEventData data)
    {
        if (!_isWorldUpload) return;
        Debug.Log($"[LUIDA] OnWorldUploadEnded fired (success={data.Success}). Restoring test-mode source.");
        SetTestMode(true);
        RunCSCombiner();
        AssetDatabase.SaveAssets();
        _isWorldUpload = false;
    }

    private static void SetTestMode(bool isTestMode)
    {
        foreach (var path in TestModeSourcePaths)
        {
            SetTestModeInFile(path, isTestMode);
        }
    }

    private static void SetTestModeInFile(string path, bool isTestMode)
    {
        if (!File.Exists(path)) return;

        string content = File.ReadAllText(path);
        string replacement = $"isTestMode = {isTestMode.ToString().ToLower()};";

        // Match either `isTestMode = true;` (the ExpIdentifiers form, implicit
        // global) or `let isTestMode = true;` (the ConditionManager form,
        // block-scoped). The regex captures only the assignment + value, so the
        // `let ` prefix (if any) survives the replace.
        if (Regex.IsMatch(content, @"isTestMode\s*=\s*(true|false);"))
        {
            content = Regex.Replace(content, @"isTestMode\s*=\s*(true|false);", replacement);
        }
        else
        {
            content += $"\n{replacement}\n";
        }

        File.WriteAllText(path, content);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
    }

}
