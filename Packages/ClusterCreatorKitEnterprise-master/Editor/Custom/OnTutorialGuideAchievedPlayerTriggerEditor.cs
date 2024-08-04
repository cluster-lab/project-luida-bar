using ClusterVR.CreatorKit.Trigger.Implements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClusterVR.CreatorKit.Editor.Custom
{
    [CustomEditor(typeof(OnTutorialGuideAchievedPlayerTrigger)), CanEditMultipleObjects]
    public sealed class OnTutorialGuideAchievedPlayerTriggerEditor : TriggerEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var element = base.CreateInspectorGUI();
            var button = new Button(() =>
            {
                if (Application.isPlaying)
                {
                    (target as OnTutorialGuideAchievedPlayerTrigger)?.Invoke();
                }
            })
            {
                text = "クリア扱いにする",
            };
            element.Add(button);
            return element;
        }
    }
}
