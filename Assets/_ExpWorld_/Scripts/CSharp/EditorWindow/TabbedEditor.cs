using UnityEditor;
using UnityEngine;

public class TabbedEditor : EditorWindow
{
    private int currentTab = 0;
    private string[] tabNames = { "Experiment Identifiers", "Experiment Variables Editor", "State List Editor", "Objects Manager" };

    private ExpIdentifierEditor expIdentifierEditor;
    private StateListEditor stateListEditor;
    private ObjectsManagerEditor objectsManagerEditor;
    private ExperimentVariablesEditor experimentVariablesEditor;

    [MenuItem("Window/Luida Editor")]
    public static void ShowWindow()
    {
        GetWindow<TabbedEditor>("Luida Editor");
    }

    private void OnEnable()
    {
        expIdentifierEditor = new ExpIdentifierEditor();
        experimentVariablesEditor = new ExperimentVariablesEditor();
        stateListEditor = new StateListEditor();
        objectsManagerEditor = new ObjectsManagerEditor();

        expIdentifierEditor.OnEnable();
        experimentVariablesEditor.OnEnable();
        stateListEditor.OnEnable();
        objectsManagerEditor.OnEnable();
    }

    private void OnGUI()
    {
        currentTab = GUILayout.Toolbar(currentTab, tabNames);

        switch (currentTab)
        {
            case 0:
                expIdentifierEditor.OnGUI();
                break;
            case 1:
                experimentVariablesEditor.OnGUI();
                break;
            case 2:
                stateListEditor.OnGUI();
                break;
            case 3:
                objectsManagerEditor.OnGUI();
                break;
        }
    }
}