#if UNITY_EDITOR
using Assets.KaomoLab.CSCombiner;
using ClusterVR.CreatorKit.Item.Implements;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScriptableClusterScriptCombiner : CSCombiner
{
    public void PrependScript(JavaScriptAsset clusterScript, JavaScriptAsset playerScript, bool combineNow = false)
    {
        var clusterScripts = this.GetClusterScripts();
        var playerScripts = this.GetPlayerScripts();

        if (clusterScript != null) clusterScripts.Insert(0, clusterScript);
        if (playerScript != null) playerScripts.Insert(0, playerScript);

        this.SetClusterScripts(clusterScripts);
        this.SetPlayerScripts(playerScripts);

        if (combineNow) Combine();
    }
    
    public void AppendScript(JavaScriptAsset clusterScript, JavaScriptAsset playerScript, bool combineNow = false)
    {
        var clusterScripts = this.GetClusterScripts();
        var playerScripts = this.GetPlayerScripts();

        if (clusterScript != null) clusterScripts.Add(clusterScript);
        if (playerScript != null) playerScripts.Add(playerScript);

        this.SetClusterScripts(clusterScripts);
        this.SetPlayerScripts(playerScripts);

        if (combineNow) Combine();
    }
    
    public void ReplaceScript(JavaScriptAsset clusterScript, int clusterScriptIndex, JavaScriptAsset playerScript, int playerScriptIndex, bool combineNow = false)
    {
        var clusterScripts = this.GetClusterScripts();
        var playerScripts = this.GetPlayerScripts();

        if (clusterScript != null) clusterScripts[clusterScriptIndex] = clusterScript;
        if (playerScript != null) playerScripts[playerScriptIndex] = playerScript;

        this.SetClusterScripts(clusterScripts);
        this.SetPlayerScripts(playerScripts);

        if (combineNow) Combine();
    }

    public void ClearScripts(bool combineNow = false) {
        this.SetClusterScripts(new List<JavaScriptAsset>());
        this.SetPlayerScripts(new List<JavaScriptAsset>());

        if (combineNow) Combine();
    }

    public void CombineScripts() {
        Combine();
    }
}
#endif
