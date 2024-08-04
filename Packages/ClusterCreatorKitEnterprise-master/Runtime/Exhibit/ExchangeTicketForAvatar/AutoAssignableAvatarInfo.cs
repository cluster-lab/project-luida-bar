using ClusterVR.CreatorKit.Exhibit.AvatarDisplay;

namespace ClusterVR.CreatorKit.Exhibit.ExchangeTicketForAvatar
{
    public readonly struct AutoAssignableAvatarInfo
    {
        public string ProductUgcId { get; }
        public string AvatarInfo { get; }
        public int AvatarDisplayPoseIndex { get; }
        public AvatarDisplayFacialExpressionType AvatarDisplayFacialExpressionType { get; }

        public AutoAssignableAvatarInfo(string productUgcId, string avatarInfo, int avatarDisplayPoseIndex, AvatarDisplayFacialExpressionType avatarDisplayFacialExpressionType)
        {
            ProductUgcId = productUgcId;
            AvatarInfo = avatarInfo;
            AvatarDisplayPoseIndex = avatarDisplayPoseIndex;
            AvatarDisplayFacialExpressionType = avatarDisplayFacialExpressionType;
        }
    }
}
