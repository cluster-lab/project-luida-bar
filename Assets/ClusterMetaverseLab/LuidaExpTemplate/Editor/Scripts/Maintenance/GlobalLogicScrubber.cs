#if UNITY_EDITOR
using System;
using System.Reflection;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Gimmick.Implements;
using ClusterVR.CreatorKit.Operation.Implements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GlobalLogicScrubber
{
    private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

    private static readonly FieldInfo GlobalLogicGimmickKeyField =
        typeof(GlobalLogic).GetField("globalGimmickKey", FieldFlags);
    private static readonly FieldInfo GimmickKeyInnerField =
        typeof(GlobalGimmickKey).GetField("key", FieldFlags);
    private static readonly FieldInfo GimmickKeyStringField =
        typeof(GimmickKey).GetField("key", FieldFlags);

    public static void ScrubActiveScene()
    {
        if (GlobalLogicGimmickKeyField == null || GimmickKeyInnerField == null || GimmickKeyStringField == null)
        {
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return;

        int removed = 0;
        int repaired = 0;
        int needsManual = 0;

        foreach (var root in scene.GetRootGameObjects())
        {
            var globals = root.GetComponentsInChildren<GlobalLogic>(true);
            foreach (var gl in globals)
            {
                if (gl == null) continue;
                if (!IsBroken(gl)) continue;

                bool hidden = (gl.hideFlags & HideFlags.HideInInspector) != 0;
                if (!hidden)
                {
                    Debug.LogWarning(
                        $"[LuidaScrub] Visible GlobalLogic on '{GetPath(gl.gameObject)}' has a null gimmick key. Set the Key in the inspector before uploading.",
                        gl.gameObject);
                    needsManual++;
                    continue;
                }

                if (IsReferencedByLuidaGimmick(gl))
                {
                    RepairKey(gl);
                    EditorUtility.SetDirty(gl);
                    repaired++;
                    continue;
                }

                Debug.Log($"[LuidaScrub] Removing orphaned hidden GlobalLogic on '{GetPath(gl.gameObject)}'.", gl.gameObject);
                UnityEngine.Object.DestroyImmediate(gl);
                removed++;
            }
        }

        if (removed == 0 && repaired == 0 && needsManual == 0) return;

        Debug.Log(
            $"[LuidaScrub] removed {removed} orphaned GlobalLogic, repaired {repaired}, {needsManual} visible components still need manual attention.");

        if (removed > 0 || repaired > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static bool IsBroken(GlobalLogic gl)
    {
        try
        {
            var ggk = GlobalLogicGimmickKeyField.GetValue(gl);
            if (ggk == null) return true;

            var inner = GimmickKeyInnerField.GetValue(ggk);
            if (inner == null) return true;

            var keyStr = GimmickKeyStringField.GetValue(inner) as string;
            return keyStr == null;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static bool IsReferencedByLuidaGimmick(GlobalLogic gl)
    {
        var holders = gl.GetComponents<LuidaFakeGimmick>();
        foreach (var holder in holders)
        {
            var t = holder.GetType();
            while (t != null && t != typeof(MonoBehaviour))
            {
                var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var f in fields)
                {
                    if (!typeof(GlobalLogic).IsAssignableFrom(f.FieldType)) continue;
                    if (f.GetValue(holder) as GlobalLogic == gl) return true;
                }
                t = t.BaseType;
            }
        }
        return false;
    }

    private static void RepairKey(GlobalLogic gl)
    {
        var ggk = GlobalLogicGimmickKeyField.GetValue(gl);
        if (ggk == null)
        {
            ggk = Activator.CreateInstance(typeof(GlobalGimmickKey));
            GlobalLogicGimmickKeyField.SetValue(gl, ggk);
        }

        var inner = GimmickKeyInnerField.GetValue(ggk);
        if (inner == null)
        {
            GimmickKeyInnerField.SetValue(ggk, new GimmickKey(GimmickTarget.Global, string.Empty));
            return;
        }

        if (GimmickKeyStringField.GetValue(inner) as string == null)
        {
            GimmickKeyStringField.SetValue(inner, string.Empty);
        }
    }

    private static string GetPath(GameObject go)
    {
        var t = go.transform;
        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif
