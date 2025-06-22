#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class LuidaNotEditableGameObject : MonoBehaviour
{
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
        foreach (Transform child in transform)
        {
            if ((child.hideFlags & HideFlags.NotEditable) == 0 && System.Array.IndexOf(editableChildNames, child.name) < 0)
            {
                child.gameObject.hideFlags |= HideFlags.NotEditable;
                EditorUtility.SetDirty(child.gameObject);
            }
        }
    }

    private void UnlockChildren()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.hideFlags &= ~HideFlags.NotEditable;
            EditorUtility.SetDirty(child.gameObject);
        }

        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
}
#endif
