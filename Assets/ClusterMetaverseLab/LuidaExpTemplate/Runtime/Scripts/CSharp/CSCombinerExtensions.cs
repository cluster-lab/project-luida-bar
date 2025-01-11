#if UNITY_EDITOR
using Assets.KaomoLab.CSCombiner;
using ClusterVR.CreatorKit.Item.Implements;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class CSCombinerExtensions
{
    public static List<JavaScriptAsset> GetClusterScripts(this CSCombiner combiner)
    {
        var field = typeof(CSCombiner).GetField("clusterScripts", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(combiner) as List<JavaScriptAsset>;
    }

    public static List<JavaScriptAsset> GetPlayerScripts(this CSCombiner combiner)
    {
        var field = typeof(CSCombiner).GetField("playerScripts", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(combiner) as List<JavaScriptAsset>;
    }

    public static void SetClusterScripts(this CSCombiner combiner, List<JavaScriptAsset> scripts)
    {
        var field = typeof(CSCombiner).GetField("clusterScripts", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(combiner, scripts);
    }

    public static void SetPlayerScripts(this CSCombiner combiner, List<JavaScriptAsset> scripts)
    {
        var field = typeof(CSCombiner).GetField("playerScripts", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(combiner, scripts);
    }
}
#endif
