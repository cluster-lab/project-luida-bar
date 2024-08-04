using ClusterVR.CreatorKit.Exhibit.AvatarDisplay;
using ClusterVR.CreatorKit.Exhibit.AvatarDisplay.Implements;
using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.ExchangeTicketForAvatar
{
    public sealed class TicketExchangeableAvatarDisplay : MonoBehaviour, ITicketExchangeableAvatarDisplay
    {
        [SerializeField] string productUgcId;
        [SerializeField] Transform avatarDisplayRoot;
        [SerializeField] AvatarDisplayBounds avatarDisplayBounds;
        [SerializeField] AnimationClip avatarDisplayPose;
        [SerializeField] AvatarDisplayFacialExpressionType avatarDisplayFacialExpressionType;
        [SerializeField] AvatarDisplayTemporaryObject temporaryObject;

        public bool IsActiveAndEnabled => gameObject.activeInHierarchy;
        public string ProductUgcId => productUgcId;
        public Transform AvatarDisplayRoot => (avatarDisplayRoot != null) ? avatarDisplayRoot : transform;
        public IAvatarDisplayBounds AvatarDisplayBounds => avatarDisplayBounds;
        public AnimationClip AvatarDisplayPose => avatarDisplayPose;
        public AvatarDisplayFacialExpressionType AvatarDisplayFacialExpressionType => avatarDisplayFacialExpressionType;
        public IAvatarDisplayTemporaryObject TemporaryObject => temporaryObject;
    }
}
