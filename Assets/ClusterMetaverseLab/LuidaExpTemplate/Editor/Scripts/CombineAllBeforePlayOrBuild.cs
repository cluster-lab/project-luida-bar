using ClusterVR.CreatorKit.Editor.EditorEvents;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

[InitializeOnLoad]
public class CombineAllBeforePlayOrBuild
{
    private static bool _isWorldUpload = false;
    private const string ExpIdentifiersPath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
    private const string ClusterApiUrl = "https://luida.cluster.mu/api/cluster";

    // Set in OnWorldUploadStarted from the web console's questInfo response,
    // baked into ExpIdentifiers.js by CombineAll right before CSCombiner runs,
    // and cleared back to [] after the upload so local test-mode runs are
    // never platform-filtered in the editor.
    private static string[] _pendingAllowedPlatforms = null;

    [Serializable] private class ClusterApiResponseEnvelope { public string response; public string verify; }
    [Serializable] private class QuestInfoEnvelope { public QuestInfo quest; }
    [Serializable] private class QuestInfo { public string[] allowedPlatforms; }

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

        // Pull allowedPlatforms from the web console so the uploaded world
        // has the same platform restriction the researcher configured there.
        // The user is the single source of truth (web console) — not Unity.
        string expID = ReadExpIDFromExpIdentifiers();
        if (string.IsNullOrEmpty(expID) || expID == "expID_example")
        {
            Debug.LogWarning("LUIDA: expID is not configured; uploading without an allowedPlatforms restriction.");
            _pendingAllowedPlatforms = new string[0];
        }
        else
        {
            try
            {
                _pendingAllowedPlatforms = FetchAllowedPlatformsFromBackend(expID);
                Debug.Log($"LUIDA: Fetched allowedPlatforms = [{string.Join(", ", _pendingAllowedPlatforms)}] for eID {expID}.");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "LUIDA: Failed to fetch platform config",
                    "Could not fetch this experiment's allowedPlatforms from " +
                    "the LUIDA web console. Aborting upload to avoid shipping " +
                    "a world with the wrong platform restriction.\n\n" +
                    "Error: " + ex.Message,
                    "OK"
                );
                _pendingAllowedPlatforms = null;
                return false;
            }
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
            SetTestModeInExpIdentifiers(false);
            SetAllowedPlatformsInExpIdentifiers(_pendingAllowedPlatforms ?? new string[0]);
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
        SetTestModeInExpIdentifiers(true);
        SetAllowedPlatformsInExpIdentifiers(new string[0]);
        RunCSCombiner();
        AssetDatabase.SaveAssets();
        _pendingAllowedPlatforms = null;
        _isWorldUpload = false;
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

    private static string ReadExpIDFromExpIdentifiers()
    {
        if (!File.Exists(ExpIdentifiersPath)) return null;
        string content = File.ReadAllText(ExpIdentifiersPath);
        var match = Regex.Match(content, @"expID\s*=\s*""([^""]+)"";");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static void SetAllowedPlatformsInExpIdentifiers(string[] platforms)
    {
        if (!File.Exists(ExpIdentifiersPath)) return;

        var quoted = (platforms ?? new string[0]).Select(p => "\"" + p + "\"");
        string replacement = $"allowedPlatforms = [{string.Join(", ", quoted)}];";

        string content = File.ReadAllText(ExpIdentifiersPath);
        if (Regex.IsMatch(content, @"allowedPlatforms\s*=\s*\[[^\]]*\];"))
        {
            content = Regex.Replace(content, @"allowedPlatforms\s*=\s*\[[^\]]*\];", replacement);
        }
        else
        {
            content += "\n" + replacement + "\n";
        }

        File.WriteAllText(ExpIdentifiersPath, content);
        AssetDatabase.ImportAsset(ExpIdentifiersPath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static string[] FetchAllowedPlatformsFromBackend(string expID)
    {
        // Mirrors the runtime callExternal contract: route.ts unwraps
        // body.request as a JSON string then dispatches by `type`.
        string innerJson = "{\"type\":\"questInfo\",\"id\":\"" + expID + "\"}";
        string outerJson = "{\"request\":\"" + innerJson.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}";

        var req = (HttpWebRequest)WebRequest.Create(ClusterApiUrl);
        req.Method = "POST";
        req.ContentType = "application/json";
        req.Timeout = 15000;
        req.ReadWriteTimeout = 15000;

        var bodyBytes = Encoding.UTF8.GetBytes(outerJson);
        req.ContentLength = bodyBytes.Length;
        using (var stream = req.GetRequestStream())
        {
            stream.Write(bodyBytes, 0, bodyBytes.Length);
        }

        string responseText;
        try
        {
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                responseText = reader.ReadToEnd();
            }
        }
        catch (WebException wex)
        {
            // Surface the server's actual error body (e.g. "Quest not found")
            // instead of the generic "The remote server returned an error" message.
            string detail = wex.Message;
            if (wex.Response is HttpWebResponse errResp)
            {
                using (var reader = new StreamReader(errResp.GetResponseStream(), Encoding.UTF8))
                {
                    detail = $"HTTP {(int)errResp.StatusCode}: {reader.ReadToEnd()}";
                }
            }
            throw new Exception("questInfo request failed — " + detail, wex);
        }

        var envelope = JsonUtility.FromJson<ClusterApiResponseEnvelope>(responseText);
        if (envelope == null || string.IsNullOrEmpty(envelope.response))
            throw new Exception("Empty or malformed response from /api/cluster questInfo: " + responseText);

        var quest = JsonUtility.FromJson<QuestInfoEnvelope>(envelope.response);
        if (quest?.quest == null)
            throw new Exception("questInfo response missing `quest` field — check that eID is valid. Inner: " + envelope.response);

        return quest.quest.allowedPlatforms ?? new string[0];
    }
}
