#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class GenerateWorldGatesEditor : EditorWindow
{
    private string prefabPath = "Assets/_Luida_/Prefabs/QuestLocalUI.prefab";

    [MenuItem("Tools/Generate World Gates Editor")]
    public static void ShowWindow()
    {
        GetWindow<GenerateWorldGatesEditor>("Generate World Gates Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Generate World Gates Editor", EditorStyles.boldLabel);

        prefabPath = EditorGUILayout.TextField("Prefab Path", prefabPath);

        if (GUILayout.Button("Generate World Gates"))
        {
            UpdateWorldGates(prefabPath);
        }
    }

    public static void UpdateWorldGates(string prefabPath)
    {
        // Load the prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError("Prefab not found at " + prefabPath);
            return;
        }

        // Open the prefab for editing
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

        if (prefabInstance == null)
        {
            Debug.LogError("Failed to load prefab contents.");
            return;
        }

        // Find the WorldGates GameObject
        Transform worldGatesTransform = prefabInstance.transform.Find("WorldGates");
        if (worldGatesTransform == null)
        {
            Debug.LogError("WorldGates GameObject not found in the prefab.");
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            return;
        }

        // Remove all children except the first one
        for (int i = worldGatesTransform.childCount - 1; i > 0; i--)
        {
            DestroyImmediate(worldGatesTransform.GetChild(i).gameObject);
        }

        if (worldGatesTransform.childCount == 0)
        {
            Debug.LogError("No child GameObjects in WorldGates to duplicate.");
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            return;
        }

        // Get the first child to duplicate
        Transform firstChild = worldGatesTransform.GetChild(0);

        // Duplicate and configure new children
        for (int i = 2; i <= 300; i++)
        {
            // Duplicate the first child
            Transform newChild = Instantiate(firstChild, worldGatesTransform);
            newChild.name = "WorldGateToExpWorld_" + i;

            // Use SerializedObject to modify private or non-public fields
            var gimmickComponent = newChild.GetComponent<ClusterVR.CreatorKit.Gimmick.Implements.SetGameObjectActiveGimmick>();
            if (gimmickComponent != null)
            {
                SerializedObject serializedGimmick = new SerializedObject(gimmickComponent);
                SerializedProperty globalGimmickKeyProperty = serializedGimmick.FindProperty("globalGimmickKey");
                if (globalGimmickKeyProperty != null)
                {
                    SerializedProperty keyProperty = globalGimmickKeyProperty.FindPropertyRelative("key.key");
                    if (keyProperty != null)
                    {
                        keyProperty.stringValue = "DropWorldGate" + i;
                        serializedGimmick.ApplyModifiedProperties();
                    }
                }
            }
            else
            {
                Debug.LogWarning("SetGameObjectActiveGimmick component not found on " + newChild.name);
            }

            // Configure the new child's RoutingWorldGate component
            Transform worldGateChild = newChild.Find("WorldGate");
            if (worldGateChild != null)
            {
                var routingWorldGate = worldGateChild.GetComponent<ClusterVR.CreatorKit.RoutingWorldGate.Implements.RoutingWorldGate>();
                if (routingWorldGate != null)
                {
                    SerializedObject serializedRoutingWorldGate = new SerializedObject(routingWorldGate);
                    SerializedProperty routingKeyProperty = serializedRoutingWorldGate.FindProperty("routingKey");
                    if (routingKeyProperty != null)
                    {
                        routingKeyProperty.stringValue = "luida-bar-exp-gate-" + i;
                        serializedRoutingWorldGate.ApplyModifiedProperties();
                    }
                }
                else
                {
                    Debug.LogWarning("RoutingWorldGate component not found on " + worldGateChild.name);
                }
            }
            else
            {
                Debug.LogWarning("Child GameObject named 'WorldGate' not found in " + newChild.name);
            }
        }

        // Save changes and unload the prefab
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabInstance);

        Debug.Log("WorldGates updated successfully.");
    }
}
#endif
