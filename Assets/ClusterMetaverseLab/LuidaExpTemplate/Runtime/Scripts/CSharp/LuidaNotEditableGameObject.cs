#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class LuidaNotEditableGameObject : MonoBehaviour
{
    [SerializeField] private bool recursive = false;
    [SerializeField] private string[] editableChildNames;
    
    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            LockChildren();
        }
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            UnlockChildren();
        }
    }

    private void LockChildren()
    {
        ProcessLock(transform);
    }

    private void UnlockChildren()
    {
        ProcessUnlock(transform);
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    private void ProcessLock(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // Skip if already locked or explicitly editable
            if ((child.hideFlags & HideFlags.NotEditable) == 0 && System.Array.IndexOf(editableChildNames, child.name) < 0)
            {
                child.gameObject.hideFlags |= HideFlags.NotEditable;
                EditorUtility.SetDirty(child.gameObject);
            }

            // Recurse into grandchildren if requested
            if (recursive)
            {
                ProcessLock(child);
            }
        }
    }

    private void ProcessUnlock(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // Always unlock when destroying
            child.gameObject.hideFlags &= ~HideFlags.NotEditable;
            EditorUtility.SetDirty(child.gameObject);

            if (recursive)
            {
                ProcessUnlock(child);
            }
        }
    }
}
#endif