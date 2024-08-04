using ClusterVR.CreatorKit.Exhibit.AvatarDisplay;
using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.ExchangeTicketForAvatar
{
    public interface IAutoAssignedTicketExchangeableAvatarDisplay
    {
        bool IsActiveAndEnabled { get; }

        string ProductUgcId { get; }
        string AvatarInfo { get; }

        Transform AvatarDisplayRoot { get; }

        IAvatarDisplayBounds AvatarDisplayBounds { get; }

        AnimationClip AvatarDisplayPose { get; }

        AvatarDisplayFacialExpressionType AvatarDisplayFacialExpressionType { get; }

        IAvatarDisplayTemporaryObject TemporaryObject { get; }

        bool IsAssigned { get; }
    }
}
