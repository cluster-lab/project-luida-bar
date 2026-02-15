#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AssignRandomTaikoController
{
    private static readonly string[] TaikoControllerPaths = new[]
    {
        "Assets/_Experiment_/Animations/Taiko/TaikoDrummingV1.controller",
        "Assets/_Experiment_/Animations/Taiko/TaikoDrummingV2.controller",
        "Assets/_Experiment_/Animations/Taiko/TaikoDrumming.controller",
    };

    [MenuItem("Tools/Assign Random Taiko Controller")]
    public static void Assign()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        var controllers = new RuntimeAnimatorController[TaikoControllerPaths.Length];
        for (int i = 0; i < TaikoControllerPaths.Length; i++)
        {
            controllers[i] = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(TaikoControllerPaths[i]);
            if (controllers[i] == null)
            {
                Debug.LogError($"Could not load controller at {TaikoControllerPaths[i]}");
                return;
            }
        }

        int assignedCount = 0;
        foreach (Transform child in selected.transform)
        {
            var animator = child.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"No Animator found in '{child.name}', skipping.");
                continue;
            }

            var chosen = controllers[assignedCount % controllers.Length];
            animator.runtimeAnimatorController = chosen;
            EditorUtility.SetDirty(animator);
            assignedCount++;

            Debug.Log($"Assigned '{chosen.name}' to Animator on '{animator.gameObject.name}' (child of '{child.name}').");
        }

        Debug.Log($"Done. Assigned controllers to {assignedCount} animator(s) across {selected.transform.childCount} children.");
    }
}
#endif
