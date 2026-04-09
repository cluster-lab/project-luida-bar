using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Standalone EditorWindow for managing LUIDA avatar registration and spawner setup.
/// Independent of the main LuidaConfigWindow — works in any scene without LUIDA automation.
/// </summary>
public class LuidaAvatarsWindow : EditorWindow
{
    public AvatarRegistry Registry { get; private set; }

    [MenuItem("LUIDA/Configure avatars")]
    public static void ShowWindow()
    {
        GetWindow<LuidaAvatarsWindow>("LUIDA Avatars");
    }

    private void OnEnable()
    {
        AvatarsConfigAssetUtil.EnsureFolderLayout();
        Registry = AvatarsConfigAssetUtil.EnsureRegistryAsset();
        AvatarsConfigUIDrawer.ReloadSpawnerConfig();
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
    }

    private void OnGUI()
    {
        if (Registry == null)
            Registry = AvatarsConfigAssetUtil.EnsureRegistryAsset();

        AvatarsConfigUIDrawer.DrawGUI(this);
    }

    private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
    {
        AvatarsConfigUIDrawer.ReloadSpawnerConfig();
        Repaint();
    }
}
