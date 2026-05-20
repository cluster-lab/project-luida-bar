#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight reader for the per-scene experiment variables JS asset.
/// Returns just the variable names (within + between subjects, deduped).
///
/// Mirrors the parser in ItemsManagerAssetUtil.RefreshExperimentVariablesCache,
/// but without depending on its non-public types — so it can be called from
/// the DataCollector config UI to populate the Condition lookup dropdown.
/// </summary>
public static class ExperimentVariableNames
{
    public static string[] LoadForActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName)) return new string[0];
        string path = $"Assets/_Experiment_/Settings/ExperimentVariables/{sceneName}.js";
        if (!File.Exists(path)) return new string[0];

        string content;
        try { content = File.ReadAllText(path); }
        catch { return new string[0]; }

        var names = new List<string>();
        ParseAndCollect("within_subjects_variables", content, names);
        ParseAndCollect("between_subjects_variables", content, names);
        return names.Distinct().ToArray();
    }

    static void ParseAndCollect(string varType, string content, List<string> sink)
    {
        string pattern = $@"const {varType} = \[(.*?)\];";
        var m = Regex.Match(content, pattern, RegexOptions.Singleline);
        if (!m.Success) return;
        string body = m.Groups[1].Value;
        foreach (Match vm in Regex.Matches(body, @"\{\s*name:\s*""([^""]*)"",\s*values:\s*\[([^\]]*)\][^}]*\}", RegexOptions.Singleline))
        {
            sink.Add(vm.Groups[1].Value);
        }
    }
}
#endif
