namespace ClusterVR.CreatorKit.Exhibit.ExchangeTicketForAvatar
{
    public interface IAutoAssignedTicketExchangeableAvatar
    {
        int AvatarIndex { get; }
        void SetAvatarInfo(AutoAssignableAvatarInfo avatarInfo);
    }
}
