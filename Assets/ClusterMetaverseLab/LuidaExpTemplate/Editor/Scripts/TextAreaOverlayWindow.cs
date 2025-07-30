using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

public class TextAreaOverlayWindow : EditorWindow
{
    private Action<string> onUpdate;
    private string textContent = "";
    private string initialText; // The text when the window was opened
    
    private GUIStyle viewStyle;
    private GUIStyle editStyle;

    private Vector2 scrollPosition;
    private bool isEditing = false;
    
    // Custom undo/redo system
    private List<string> undoStack = new List<string>();
    private List<string> redoStack = new List<string>();
    private string lastRecordedText = "";
    private double lastTextChangeTime = 0;
    private const double UNDO_GROUP_TIME = 0.5; // Group changes within 0.5 seconds
    
    // Focus and cursor position preservation
    private bool shouldRestoreFocus = false;
    private int savedCursorPos = 0;
    private int savedSelectPos = 0;

    public static void Show(Rect screenRect, string text, Action<string> updateCallback, GUIStyle textStyle)
    {
        TextAreaOverlayWindow window = GetWindow<TextAreaOverlayWindow>(true, "", true);
        window.position = screenRect;
        window.onUpdate = updateCallback;
        window.textContent = text;
        window.initialText = text; // Store the original text to compare against on close
        window.lastRecordedText = text;
        
        window.viewStyle = new GUIStyle(textStyle) { richText = true };
        window.editStyle = new GUIStyle(textStyle);
        
        window.isEditing = false;
        window.undoStack.Clear();
        window.redoStack.Clear();
        window.ShowPopup();
        window.Focus();
    }

    void OnGUI()
    {
        if (viewStyle == null || editStyle == null)
        {
            this.Close();
            return;
        }

        Event e = Event.current;

        EditorGUILayout.BeginHorizontal();
        string buttonText = isEditing ? "✓ Apply" : "Edit";
        if (GUILayout.Button(buttonText, GUILayout.Width(60)))
        {
            isEditing = !isEditing;
            if (isEditing)
            {
                GUI.FocusControl("OverlayTextArea");
            }
        }

        if (textContent != initialText)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("Modified", EditorStyles.miniLabel);
            GUI.color = Color.white;
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (isEditing)
        {
            // --- EDIT MODE ---
            
            // Handle keyboard shortcuts before the TextArea processes them
            if (e.type == EventType.KeyDown && GUI.GetNameOfFocusedControl() == "OverlayTextArea")
            {
                bool isCtrlOrCmd = (e.modifiers & EventModifiers.Control) != 0 || (e.modifiers & EventModifiers.Command) != 0;
                
                if (isCtrlOrCmd)
                {
                    if (e.keyCode == KeyCode.Z)
                    {
                        if ((e.modifiers & EventModifiers.Shift) != 0)
                        {
                            // Redo
                            PerformRedo();
                            e.Use();
                        }
                        else
                        {
                            // Undo
                            PerformUndo();
                            e.Use();
                        }
                    }
                    else if (e.keyCode == KeyCode.Y)
                    {
                        // Redo
                        PerformRedo();
                        e.Use();
                    }
                }
            }
            
            // Give the control a unique name
            GUI.SetNextControlName("OverlayTextArea");
            
            // Store the text before potential changes
            string beforeText = textContent;
            
            textContent = EditorGUILayout.TextArea(textContent, editStyle, GUILayout.ExpandHeight(true));
            
            // Restore focus and cursor position after undo/redo if needed
            if (shouldRestoreFocus && e.type == EventType.Repaint)
            {
                shouldRestoreFocus = false;
                EditorGUI.FocusTextInControl("OverlayTextArea");
                
                // Use EditorGUIUtility to set cursor position in the next frame
                EditorApplication.delayCall += () =>
                {
                    if (this != null) // Check if window still exists
                    {
                        TextEditor textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        if (textEditor != null)
                        {
                            textEditor.cursorIndex = Mathf.Min(savedCursorPos, textContent.Length);
                            textEditor.selectIndex = Mathf.Min(savedSelectPos, textContent.Length);
                        }
                    }
                };
            }
            
            // Check if text changed
            if (beforeText != textContent)
            {
                RecordTextChange(beforeText);
            }
        }
        else
        {
            // --- VIEW MODE ---
            string coloredText = ItemsManagerUIDrawer.HighlightJsSyntax(textContent);
            EditorGUILayout.SelectableLabel(coloredText, viewStyle, GUILayout.ExpandHeight(true));
        }

        EditorGUILayout.EndScrollView();
    }

    private void RecordTextChange(string previousText)
    {
        double currentTime = EditorApplication.timeSinceStartup;
        
        // If enough time has passed since the last change, record a new undo state
        if (currentTime - lastTextChangeTime > UNDO_GROUP_TIME)
        {
            // Clear redo stack when new changes are made
            redoStack.Clear();
            
            // Add the previous state to undo stack
            undoStack.Add(previousText);
            
            // Limit undo stack size to prevent memory issues
            if (undoStack.Count > 100)
            {
                undoStack.RemoveAt(0);
            }
            
            lastRecordedText = previousText;
        }
        
        lastTextChangeTime = currentTime;
    }

    private void PerformUndo()
    {
        // Save current cursor position
        SaveCursorPosition();
        
        if (undoStack.Count > 0)
        {
            // Push current state to redo stack
            redoStack.Add(textContent);
            
            // Pop from undo stack and apply
            textContent = undoStack[undoStack.Count - 1];
            undoStack.RemoveAt(undoStack.Count - 1);
            
            // Update the last recorded text
            lastRecordedText = textContent;
            lastTextChangeTime = 0; // Reset timer to force new undo group
            
            // Clear GUI control states and schedule focus restoration
            ClearGUIStateAndRestoreFocus();
        }
        else if (textContent != initialText)
        {
            // If no undo history but text has changed, revert to initial
            redoStack.Add(textContent);
            textContent = initialText;
            lastRecordedText = textContent;
            lastTextChangeTime = 0;
            
            // Clear GUI control states and schedule focus restoration
            ClearGUIStateAndRestoreFocus();
        }
    }

    private void PerformRedo()
    {
        if (redoStack.Count > 0)
        {
            // Save current cursor position
            SaveCursorPosition();
            
            // Push current state to undo stack
            undoStack.Add(textContent);
            
            // Pop from redo stack and apply
            textContent = redoStack[redoStack.Count - 1];
            redoStack.RemoveAt(redoStack.Count - 1);
            
            // Update the last recorded text
            lastRecordedText = textContent;
            lastTextChangeTime = 0; // Reset timer to force new undo group
            
            // Clear GUI control states and schedule focus restoration
            ClearGUIStateAndRestoreFocus();
        }
    }

    private void SaveCursorPosition()
    {
        TextEditor textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
        if (textEditor != null)
        {
            savedCursorPos = textEditor.cursorIndex;
            savedSelectPos = textEditor.selectIndex;
        }
        else
        {
            savedCursorPos = 0;
            savedSelectPos = 0;
        }
    }

    private void ClearGUIStateAndRestoreFocus()
    {
        // Force TextArea to update by clearing its cached state
        GUIUtility.keyboardControl = 0;
        GUIUtility.hotControl = 0;
        EditorGUIUtility.editingTextField = false;
        
        // Schedule focus restoration for the next repaint
        shouldRestoreFocus = true;
        
        Repaint();
    }

    /// <summary>
    /// Commits the final text change to the parent window's Undo stack.
    /// </summary>
    private void CommitChanges()
    {
        // Only fire the update if the text has actually changed from its initial state.
        if (textContent != initialText)
        {
            onUpdate?.Invoke(textContent);
        }
    }

    // OnDestroy is called when the window is closed (e.g., by losing focus).
    // This is the correct place to ensure changes are always committed.
    void OnDestroy()
    {
        CommitChanges();
    }
    
    private void OnLostFocus()
    {
        // Save current state before closing if text has been modified
        if (isEditing && textContent != lastRecordedText)
        {
            RecordTextChange(lastRecordedText);
        }
        Close();
    }
}
