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
        ReloadForActiveScene();
        AvatarsConfigUIDrawer.ReloadSpawnerConfig();
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
    }

    private void OnGUI()
    {
        // Registry is scene-scoped — a lazy reload here keeps the UI honest when
        // the user just renamed the scene (the active-scene event won't fire for
        // that, but the registry path it resolves to has changed).
        if (Registry == null)
            ReloadForActiveScene();

        AvatarsConfigUIDrawer.DrawGUI(this);
    }

    private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
    {
        ReloadForActiveScene();
        AvatarsConfigUIDrawer.ReloadSpawnerConfig();
        Repaint();
    }

    /// <summary>
    /// Loads (without creating) the registry for the active scene. We don't
    /// auto-create on view so opening the window in a scene that has no avatars
    /// doesn't leave behind an empty folder — folder + asset are created on the
    /// first drop, or when the user explicitly clicks the migration button.
    /// </summary>
    public void ReloadForActiveScene()
    {
        string sceneFolder = AvatarsConfigAssetUtil.GetActiveSceneFolderName();
        if (sceneFolder == null) { Registry = null; return; }

        string path = AvatarsConfigAssetUtil.GetRegistryPath(sceneFolder);
        Registry = AssetDatabase.LoadAssetAtPath<AvatarRegistry>(path);
    }
}
