using UnityEngine;
using UnityEditor;

public class InputNameWindow : EditorWindow
{
    public string itemName = "New StateListeningItem";
    public System.Action<string> OnNameEntered;

    public static void ShowWindow(System.Action<string> onNameEntered)
    {
        var window = GetWindow<InputNameWindow>("Enter Item Name");
        window.OnNameEntered = onNameEntered;
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Enter a name for the new State-Listening Item:");
        itemName = EditorGUILayout.TextField(itemName);

        if (GUILayout.Button("Create"))
        {
            if (string.IsNullOrEmpty(itemName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a name.", "OK");
            }
            else
            {
                OnNameEntered?.Invoke(itemName);
                Close();
            }
        }
    }
}
