using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

[InitializeOnLoad]
public class CombineAllBeforePlayOrBuild
{
    static CombineAllBeforePlayOrBuild()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
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
        Type csCombinerType = Type.GetType("Assets.KaomoLab.CSCombiner.CSCombiner, Assembly-CSharp-Editor");
        if (csCombinerType != null)
        {
            var method = csCombinerType.GetMethod("CombineAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, null);
        }
    }
}
