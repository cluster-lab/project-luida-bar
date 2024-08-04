using ClusterVR.CreatorKit.Item.Implements;
using UnityEditor;

namespace ClusterVR.CreatorKit.Editor.Custom
{
    [CustomEditor(typeof(AutoAssignableProductDisplayItem)), CanEditMultipleObjects]
    public sealed class AutoAssignableProductDisplayItemEditor : VisualElementEditor
    {
        void OnSceneGUI()
        {
            if (target is not AutoAssignableProductDisplayItem productDisplayItem)
            {
                return;
            }

            MoveAndRotateHandle.Draw(productDisplayItem.ProductDisplayRoot, "ProductDisplayRoot");
        }
    }
}
