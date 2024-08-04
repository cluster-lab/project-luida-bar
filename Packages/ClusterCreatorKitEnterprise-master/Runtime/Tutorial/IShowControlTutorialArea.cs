using System;
using UnityEngine;

namespace ClusterVR.CreatorKit.Tutorial
{
    public interface IShowControlTutorialArea
    {
        GameObject RootObject { get; }
        GuideType GuideType { get; }
        string CustomId { get; }
        Transform VRGuideAnchor { get; }
        Transform NonVRGuideAnchor { get; }
        event ShowControlTutorialAreaEventHandler OnEnter;
        event ShowControlTutorialAreaEventHandler OnExit;
        void Activate();
    }

    public delegate void ShowControlTutorialAreaEventHandler(ShowControlTutorialAreaEventArgs e);

    /// <summary> 互換性のため、値の追加はかならず最後尾への追加で行い、不要になった値も削除しない </summary>
    public enum GuideType
    {
        Custom,
        Move,
        Jump,
        LookAround,
        PersonView,
        ItemInteract,
        HandsMove,
        Ride,
        Camera,
        Emote,
        GrabAndUseItem,
        ReleaseItem,
    }

    public sealed class ShowControlTutorialAreaEventArgs : EventArgs
    {
        public GuideType GuideType { get; }
        public string CustomId { get; }
        public GameObject EnterObject { get; }
        public Transform NonVRGuideAnchor { get; }
        public Transform VRGuideAnchor { get; }

        public ShowControlTutorialAreaEventArgs(
            GuideType guideType, string customId, GameObject enterObject,
            Transform nonVRGuideAnchor, Transform vrGuideAnchor
            )
        {
            GuideType = guideType;
            CustomId = customId;
            EnterObject = enterObject;
            NonVRGuideAnchor = nonVRGuideAnchor;
            VRGuideAnchor = vrGuideAnchor;
        }
    }
}
