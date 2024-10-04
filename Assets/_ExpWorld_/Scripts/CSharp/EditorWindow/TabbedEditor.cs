using UnityEditor;
using UnityEngine;

public class TabbedEditor : EditorWindow
{
    private int currentTab = 0;
    private string[] tabNames = { "State List Editor", "Experiment Variables Editor", "Experiment Identifiers" };

    private StateListEditor stateListEditor;
    private ExperimentVariablesEditor experimentVariablesEditor;
    private ExpIdentifierEditor expIdentifierEditor;

    [MenuItem("Window/Luida Editor")]
    public static void ShowWindow()
    {
        GetWindow<TabbedEditor>("Luida Editor");
    }

    private void OnEnable()
    {
        stateListEditor = new StateListEditor();
        experimentVariablesEditor = new ExperimentVariablesEditor();
        expIdentifierEditor = new ExpIdentifierEditor();

        stateListEditor.OnEnable();
        experimentVariablesEditor.OnEnable();
        expIdentifierEditor.OnEnable();
    }

    private void OnGUI()
    {
        currentTab = GUILayout.Toolbar(currentTab, tabNames);

        switch (currentTab)
        {
            case 0:
                stateListEditor.OnGUI();
                break;
            case 1:
                experimentVariablesEditor.OnGUI();
                break;
            case 2:
                expIdentifierEditor.OnGUI();
                break;
        }
    }
}