using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Defaults CSEmulator's test VRM (動作確認用のVRM) to LUIDA's transparent
/// avatar whenever the emulator is installed.
///
/// Why: CSEmulator answers player.getHumanoidBoneRotation() with the raw bone
/// world rotations of its test VRM. Its bundled VRM1 dummy has a
/// non-normalized (bone-aligned) skeleton, so those rotations carry per-limb
/// rest offsets and LUIDA's avatar pose sync deforms (legs skyward, mirrored
/// arms). Real Cluster always supplies normalized-convention rotations, so
/// preview must use a normalized test VRM. The transparent avatar is
/// normalized and also keeps the preview camera unobstructed.
///
/// The value is stored by CSEmulator in PlayerPrefs as an asset path. This
/// only heals missing, unresolvable, or bundled-dummy values — a VRM the
/// researcher picked by hand (via the CSEmulator options window) stays
/// untouched.
/// </summary>
public static class CSEmulatorVrmDefaultSetter
{
    private const string PlayerPrefsKey = "KaomoCSEmulator_vrm";
    private const string CSEmulatorFolder = "Assets/KaomoLab/CSEmulator";
    private const string TransparentAvatarPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Models/transparent-avatar.vrm";
    private const string BundledDummyPath = "Assets/KaomoLab/CSEmulator/Scripts/VRM/CSEmulatorDummyHumanoid10.vrm";

    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        // Defer so the asset database is ready before paths are resolved.
        EditorApplication.delayCall += ApplyDefaultIfNeeded;
    }

    private static void ApplyDefaultIfNeeded()
    {
        if (!Directory.Exists(CSEmulatorFolder)) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(TransparentAvatarPath) == null) return;

        string current = PlayerPrefs.GetString(PlayerPrefsKey, "");
        if (current == TransparentAvatarPath) return;

        // Respect a VRM the researcher chose deliberately. The bundled dummy
        // does not count — its non-normalized skeleton breaks pose sync.
        bool currentIsValid = !string.IsNullOrEmpty(current) &&
            current != BundledDummyPath &&
            AssetDatabase.LoadAssetAtPath<GameObject>(current) != null;
        if (currentIsValid) return;

        SetEmulatorVrm();
    }

    private static void SetEmulatorVrm()
    {
        PlayerPrefs.SetString(PlayerPrefsKey, TransparentAvatarPath);
        PlayerPrefs.Save();
        Debug.Log($"[LUIDA] CSEmulator test VRM (動作確認用のVRM) set to {TransparentAvatarPath}");
    }
}
