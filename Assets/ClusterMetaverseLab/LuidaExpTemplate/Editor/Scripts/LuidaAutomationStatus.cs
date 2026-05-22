#if UNITY_EDITOR
using System.IO;
using UnityEditor.SceneManagement;

/// <summary>
/// Single source of truth for "is the LUIDA automation feature active for a
/// given scene." A scene is considered automation-active when both per-scene
/// assets exist on disk:
///   Assets/_Experiment_/Settings/StateList/{SceneName}.asset
///   Assets/_Experiment_/Settings/ExperimentVariables/{SceneName}.js
///
/// Both files are created together by LuidaConfigWindow's activation flow, so
/// either file alone is treated as half-configured / inactive.
/// </summary>
public static class LuidaAutomationStatus
{
    public static bool IsActiveForActiveScene()
    {
        string sceneName = EditorSceneManager.GetActiveScene().name;
        return IsActiveForScene(sceneName);
    }

    public static bool IsActiveForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        string stateListPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
        string variablesAssetPath = $"Assets/_Experiment_/Settings/ExperimentVariables/{sceneName}.js";
        return File.Exists(stateListPath) && File.Exists(variablesAssetPath);
    }
}
#endif
