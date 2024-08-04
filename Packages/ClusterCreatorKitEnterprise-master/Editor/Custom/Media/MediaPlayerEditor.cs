using ClusterVR.CreatorKit.Media.Implements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ClusterVR.CreatorKit.Editor.Custom
{
    /// Unity2021.3.4f1以下のバージョンで、PropertyDrawerの表示が崩れる問題へのworkaround
    /// cf. https://issuetracker.unity3d.com/issues/first-array-element-expansion-is-broken-for-arrays-that-use-custom-property-drawers
    ///
    /// NOTE: このclassがない時に対して、ドロップダウン選択時にmousedownの状態が外れない問題がある
    /// TODO(homuler): 上記問題を修正する or Unityのバージョンアップ後にこのclassを削除する
    [CustomEditor(typeof(MediaPlayer)), CanEditMultipleObjects]
    public sealed class MediaPlayerEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();

            InspectorElement.FillDefaultInspector(container, serializedObject, this);
            container.Bind(serializedObject);

            return container;
        }
    }
}
