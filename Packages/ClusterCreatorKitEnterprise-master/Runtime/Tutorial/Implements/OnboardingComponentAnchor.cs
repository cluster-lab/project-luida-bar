using UnityEngine;

namespace ClusterVR.CreatorKit.Tutorial.Implements
{
    public sealed class OnboardingComponentAnchor : MonoBehaviour, IOnboardingComponentAnchor
    {
        [SerializeField] AnchorType anchorType;
        [SerializeField] string customId;

        public Transform Transform => transform;
        public AnchorType AnchorType => anchorType;
        public string CustomId => customId;
    }
}

