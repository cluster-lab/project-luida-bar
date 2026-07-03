#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Detects whether KaomoLab's CSCombiner / CSEmulator are present and reflects
/// that in scripting define symbols (<c>LUIDA_HAS_CSCOMBINER</c> /
/// <c>LUIDA_HAS_CSEMULATOR</c>). Those defines gate the KaomoLab-typed code, so
/// LUIDA compiles even when KaomoLab is absent and lights its features back up
/// automatically once KaomoLab is imported (after one domain reload).
///
/// Also hosts "LUIDA &gt; Validate installation".
/// </summary>
[InitializeOnLoad]
public static class LuidaDependencies
{
    public const string CSCombinerDefine = "LUIDA_HAS_CSCOMBINER";
    public const string CSEmulatorDefine = "LUIDA_HAS_CSEMULATOR";

    private const string CSEmulatorBootstrapType = "Assets.KaomoLab.CSEmulator.Editor.Preview.Bootstrap";

    static LuidaDependencies()
    {
        // Defer until the asset database is ready; cheap and avoids first-import races.
        EditorApplication.delayCall += SyncDefines;
    }

    internal static bool IsCSCombinerPresent() => LuidaCombiner.ResolveCSCombinerType() != null;

    internal static bool IsCSEmulatorPresent()
    {
        if (Type.GetType($"{CSEmulatorBootstrapType}, Assembly-CSharp-Editor") != null) return true;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.GetType(CSEmulatorBootstrapType) != null) return true;
        }
        return false;
    }

    private static void SyncDefines()
    {
        var group = EditorUserBuildSettings.selectedBuildTargetGroup;
        if (group == BuildTargetGroup.Unknown) group = BuildTargetGroup.Standalone;

        NamedBuildTarget target;
        try { target = NamedBuildTarget.FromBuildTargetGroup(group); }
        catch { target = NamedBuildTarget.Standalone; }

        var defines = PlayerSettings.GetScriptingDefineSymbols(target);
        var set = new HashSet<string>(defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

        bool changed = false;
        changed |= Apply(set, CSCombinerDefine, IsCSCombinerPresent());
        changed |= Apply(set, CSEmulatorDefine, IsCSEmulatorPresent());

        if (changed)
        {
            // Triggers one recompile; the "only when changed" guard keeps it from looping.
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", set));
            Debug.Log($"[LUIDA] Updated dependency defines for {target.TargetName}: {string.Join(";", set)}");
        }
    }

    private static bool Apply(HashSet<string> set, string define, bool shouldExist)
    {
        if (shouldExist) return set.Add(define);     // true only if it wasn't already present
        return set.Remove(define);                   // true only if it was present
    }

    [MenuItem("LUIDA/Validate installation", false, 100)]
    public static void ValidateInstallation()
    {
        var sb = new StringBuilder();

        var root = LuidaPaths.PackageRoot;
        sb.AppendLine(root != null ? $"Package root: {root}" : "Package root: NOT FOUND");
        sb.AppendLine();

        int total = 0, missing = 0;
        foreach (var asset in LuidaPaths.RequiredInternalAssets())
        {
            total++;
            if (!LuidaPaths.Exists(asset.Value))
            {
                missing++;
                sb.AppendLine($"  MISSING: {asset.Key}  ->  {asset.Value ?? "(unresolved)"}");
            }
        }
        sb.AppendLine(missing == 0
            ? $"Package assets: all {total} required resolved."
            : $"Package assets: {missing} of {total} required MISSING (see above).");
        foreach (var asset in LuidaPaths.OptionalInternalAssets())
        {
            if (!LuidaPaths.Exists(asset.Value))
                sb.AppendLine($"  optional (built at runtime if absent): {asset.Key}");
        }
        sb.AppendLine();

        bool combiner = IsCSCombinerPresent();
        sb.AppendLine($"CSCombiner (required): {(combiner ? "present" : "MISSING — import KaomoLab CSCombiner")}" +
                      $"   [compiled-in: {LuidaCombiner.IsAvailable}]");
        sb.AppendLine($"CSEmulator (optional): {(IsCSEmulatorPresent() ? "present" : "absent")}");

        if (combiner && !LuidaCombiner.IsAvailable)
        {
            sb.AppendLine();
            sb.AppendLine("Note: CSCombiner was just detected; Unity will recompile to enable it.");
        }

        var report = sb.ToString();
        Debug.Log("[LUIDA] Installation check:\n" + report);
        EditorUtility.DisplayDialog("LUIDA installation", report, "OK");
    }
}
#endif
