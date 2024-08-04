using System;
using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.AvatarDisplay
{
    public interface IProductAvatarDisplay
    {
        bool IsActiveAndEnabled { get; }
        event Action<bool> OnEnabledChanged;

        string AvatarProductId { get; }

        Transform AvatarDisplayRoot { get; }

        IAvatarDisplayBounds AvatarDisplayBounds { get; }

        AnimationClip AvatarDisplayPose { get; }

        AvatarDisplayFacialExpressionType AvatarDisplayFacialExpressionType { get; }

        IAvatarDisplayTemporaryObject TemporaryObject { get; }
    }
}
