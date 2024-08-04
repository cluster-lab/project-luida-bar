using System;
using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.AvatarDisplay.Implements
{
    public sealed class ProductAvatarDisplay : MonoBehaviour, IProductAvatarDisplay
    {
        [SerializeField] string avatarProductId;
        [SerializeField] Transform avatarDisplayRoot;
        [SerializeField] AvatarDisplayBounds avatarDisplayBounds;
        [SerializeField] AnimationClip avatarDisplayPose;
        [SerializeField] AvatarDisplayFacialExpressionType avatarDisplayFacialExpressionType;
        [SerializeField] AvatarDisplayTemporaryObject temporaryObject;

        public bool IsActiveAndEnabled => this.isActiveAndEnabled;
        public event Action<bool> OnEnabledChanged;

        void OnEnable() => OnEnabledChanged?.Invoke(true);
        void OnDisable() => OnEnabledChanged?.Invoke(false);

        public string AvatarProductId => avatarProductId;
        public Transform AvatarDisplayRoot => (avatarDisplayRoot != null) ? avatarDisplayRoot : transform;
        public IAvatarDisplayBounds AvatarDisplayBounds => avatarDisplayBounds;
        public AnimationClip AvatarDisplayPose => avatarDisplayPose;
        public AvatarDisplayFacialExpressionType AvatarDisplayFacialExpressionType => avatarDisplayFacialExpressionType;
        public IAvatarDisplayTemporaryObject TemporaryObject => temporaryObject;
    }
}
