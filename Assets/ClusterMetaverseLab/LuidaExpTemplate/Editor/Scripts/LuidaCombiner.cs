#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using ClusterVR.CreatorKit.Item.Implements; // JavaScriptAsset (a CCK type, not KaomoLab)
using UnityEditor;
using UnityEngine;

/// <summary>
/// LUIDA's port onto a ClusterScript combiner. The whole tooling talks to this
/// instead of naming KaomoLab's <c>CSCombiner</c> directly, so KaomoLab stays an
/// isolated, optional dependency behind <see cref="LuidaCombiner"/>.
/// </summary>
public interface ILuidaScriptCombiner
{
    bool IsAvailable { get; }
    /// <summary>The underlying combiner component (for EditorUtility.SetDirty / SerializedObject), or null.</summary>
    UnityEngine.Object Target { get; }
    void PrependScript(JavaScriptAsset clusterScript, JavaScriptAsset playerScript, bool combineNow = false);
    void AppendScript(JavaScriptAsset clusterScript, JavaScriptAsset playerScript, bool combineNow = false);
    void ReplaceScript(JavaScriptAsset clusterScript, int clusterScriptIndex, JavaScriptAsset playerScript, int playerScriptIndex, bool combineNow = false);
    void ClearScripts(bool combineNow = false);
    void Combine();
    List<JavaScriptAsset> GetClusterScripts();
}

/// <summary>No-op combiner returned when CSCombiner is unavailable. Reason is logged at hand-out time.</summary>
internal sealed class NullCombiner : ILuidaScriptCombiner
{
    public static readonly NullCombiner Instance = new NullCombiner();
    public bool IsAvailable => false;
    public UnityEngine.Object Target => null;
    public void PrependScript(JavaScriptAsset c, JavaScriptAsset p, bool combineNow = false) { }
    public void AppendScript(JavaScriptAsset c, JavaScriptAsset p, bool combineNow = false) { }
    public void ReplaceScript(JavaScriptAsset c, int ci, JavaScriptAsset p, int pi, bool combineNow = false) { }
    public void ClearScripts(bool combineNow = false) { }
    public void Combine() { }
    public List<JavaScriptAsset> GetClusterScripts() => new List<JavaScriptAsset>();
}

#if LUIDA_HAS_CSCOMBINER
/// <summary>Adapter forwarding the LUIDA port to KaomoLab's combiner. Compiled only when KaomoLab is present.</summary>
internal sealed class KaomoCombinerAdapter : ILuidaScriptCombiner
{
    private readonly ScriptableClusterScriptCombiner _combiner;
    public KaomoCombinerAdapter(ScriptableClusterScriptCombiner combiner) { _combiner = combiner; }
    public bool IsAvailable => _combiner != null;
    public UnityEngine.Object Target => _combiner;
    public void PrependScript(JavaScriptAsset c, JavaScriptAsset p, bool combineNow = false) => _combiner.PrependScript(c, p, combineNow);
    public void AppendScript(JavaScriptAsset c, JavaScriptAsset p, bool combineNow = false) => _combiner.AppendScript(c, p, combineNow);
    public void ReplaceScript(JavaScriptAsset c, int ci, JavaScriptAsset p, int pi, bool combineNow = false) => _combiner.ReplaceScript(c, ci, p, pi, combineNow);
    public void ClearScripts(bool combineNow = false) => _combiner.ClearScripts(combineNow);
    public void Combine() => _combiner.CombineScripts();
    public List<JavaScriptAsset> GetClusterScripts() => _combiner.GetClusterScripts();
}
#endif

/// <summary>
/// The single seam between LUIDA and KaomoLab's CSCombiner. CSCombiner is a
/// necessary dependency for actually running experiments, but LUIDA "connects"
/// to it only here: when it's absent, every entry point degrades to a
/// <see cref="NullCombiner"/> with a clear message instead of failing.
/// Compile-time absence is handled by the <c>LUIDA_HAS_CSCOMBINER</c> guard,
/// toggled automatically by <see cref="LuidaDependencies"/>.
/// </summary>
public static class LuidaCombiner
{
    /// <summary>True when the KaomoLab CSCombiner integration is compiled in.</summary>
    public static bool IsAvailable =>
#if LUIDA_HAS_CSCOMBINER
        true;
#else
        false;
#endif

    /// <summary>Wraps the combiner on <paramref name="host"/>, or a logged no-op if none/unavailable.</summary>
    public static ILuidaScriptCombiner Get(GameObject host)
    {
#if LUIDA_HAS_CSCOMBINER
        if (host == null) return NullCombiner.Instance;
        var combiner = host.GetComponent<ScriptableClusterScriptCombiner>();
        if (combiner != null) return new KaomoCombinerAdapter(combiner);
        Debug.LogError($"[LUIDA] No script combiner component found on '{host.name}'.");
        return NullCombiner.Instance;
#else
        WarnUnavailable(host);
        return NullCombiner.Instance;
#endif
    }

    /// <summary>
    /// Returns the combiner component on <paramref name="host"/> (as a plain Component) or null,
    /// without logging — for callers that just need to inspect/hide it if it happens to be there.
    /// </summary>
    public static Component FindOn(GameObject host)
    {
#if LUIDA_HAS_CSCOMBINER
        return host != null ? host.GetComponent<ScriptableClusterScriptCombiner>() : null;
#else
        return null;
#endif
    }

    /// <summary>Like <see cref="Get"/>, but adds a combiner component if the host doesn't have one.</summary>
    public static ILuidaScriptCombiner EnsureOn(GameObject host)
    {
#if LUIDA_HAS_CSCOMBINER
        if (host == null) return NullCombiner.Instance;
        var combiner = host.GetComponent<ScriptableClusterScriptCombiner>()
                       ?? host.AddComponent<ScriptableClusterScriptCombiner>();
        return new KaomoCombinerAdapter(combiner);
#else
        WarnUnavailable(host);
        return NullCombiner.Instance;
#endif
    }

    /// <summary>Null-safe EditorUtility.SetDirty on the underlying combiner component.</summary>
    public static void MarkDirty(ILuidaScriptCombiner combiner)
    {
        if (combiner != null && combiner.Target != null) EditorUtility.SetDirty(combiner.Target);
    }

    /// <summary>True if the component is a LUIDA script combiner (KaomoLab-type-free check for callers).</summary>
    public static bool IsCombiner(Component component)
    {
#if LUIDA_HAS_CSCOMBINER
        return component is ScriptableClusterScriptCombiner;
#else
        return false;
#endif
    }

    /// <summary>
    /// Invokes KaomoLab's static <c>CSCombiner.CombineAll()</c> via reflection,
    /// searching every loaded assembly (robust to KaomoLab's assembly name).
    /// </summary>
    public static void CombineAll()
    {
        var type = ResolveCSCombinerType();
        if (type == null) { WarnUnavailable(null); return; }
        var method = type.GetMethod("CombineAll", BindingFlags.Public | BindingFlags.Static);
        if (method != null) method.Invoke(null, null);
        else Debug.LogWarning("[LUIDA] KaomoLab CSCombiner.CombineAll() was not found.");
    }

    /// <summary>Locates KaomoLab's CSCombiner type in any loaded assembly, or null.</summary>
    internal static Type ResolveCSCombinerType()
    {
        var type = Type.GetType("Assets.KaomoLab.CSCombiner.CSCombiner, Assembly-CSharp-Editor");
        if (type != null) return type;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = asm.GetType("Assets.KaomoLab.CSCombiner.CSCombiner");
            if (type != null) return type;
        }
        return null;
    }

    private static double _lastWarn;
    private static void WarnUnavailable(GameObject host)
    {
        // Throttle so a loop of combiner operations doesn't flood the console.
        var now = EditorApplication.timeSinceStartup;
        if (now - _lastWarn < 1.0) return;
        _lastWarn = now;
        var where = host != null ? $" (requested for '{host.name}')" : "";
        Debug.LogWarning($"[LUIDA] CSCombiner unavailable{where} — ClusterScript combining is disabled. " +
                         "Import KaomoLab CSCombiner to enable it.");
    }
}
#endif
