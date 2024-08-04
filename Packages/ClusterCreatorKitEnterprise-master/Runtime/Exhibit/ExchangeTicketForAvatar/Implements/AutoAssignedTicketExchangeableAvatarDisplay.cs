using System.Collections.Generic;
using ClusterVR.CreatorKit.Constants;
using ClusterVR.CreatorKit.Exhibit.AvatarDisplay;
using ClusterVR.CreatorKit.Exhibit.AvatarDisplay.Implements;
using ClusterVR.CreatorKit.Extensions;
using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.ExchangeTicketForAvatar
{
    public sealed class AutoAssignedTicketExchangeableAvatarDisplay : MonoBehaviour, IAutoAssignedTicketExchangeableAvatarDisplay, IAutoAssignedTicketExchangeableAvatar, ITicketExchangeableAvatar
    {
        [SerializeField] int displayAvatarIndex;
        [SerializeField] Transform avatarDisplayRoot;
        [SerializeField] AvatarDisplayBounds avatarDisplayBounds;
        [SerializeField] AvatarDisplayTemporaryObject temporaryObject;
        [SerializeField] List<AnimationClip> avatarDisplayPoseList;

        AutoAssignableAvatarInfo? avatarInfo;

        public bool IsActiveAndEnabled => gameObject.activeInHierarchy;
        public string ProductUgcId => avatarInfo.Value.ProductUgcId;
        public string AvatarInfo => avatarInfo.Value.AvatarInfo;
        public Transform AvatarDisplayRoot => (avatarDisplayRoot != null) ? avatarDisplayRoot : transform;
        public IAvatarDisplayBounds AvatarDisplayBounds => avatarDisplayBounds;
        public AnimationClip AvatarDisplayPose => avatarDisplayPoseList[avatarInfo.Value.AvatarDisplayPoseIndex];
        public AvatarDisplayFacialExpressionType AvatarDisplayFacialExpressionType => avatarInfo.Value.AvatarDisplayFacialExpressionType;
        public IAvatarDisplayTemporaryObject TemporaryObject => temporaryObject;
        public bool IsAssigned => avatarInfo.HasValue;

        public int AvatarIndex => displayAvatarIndex;

        public void SetAvatarInfo(AutoAssignableAvatarInfo avatarInfo)
        {
            this.avatarInfo = avatarInfo;
        }

        void Reset()
        {
            gameObject.SetLayerRecursively(LayerName.InteractableExhibit);
        }

        void Start()
        {
            gameObject.SetLayerRecursively(LayerName.InteractableExhibit);
        }
    }
}
