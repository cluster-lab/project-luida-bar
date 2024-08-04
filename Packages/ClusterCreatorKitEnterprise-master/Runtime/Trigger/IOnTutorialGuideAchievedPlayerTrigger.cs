namespace ClusterVR.CreatorKit.Trigger
{
    public interface IOnTutorialGuideAchievedPlayerTrigger : IPlayerTrigger
    {
        //NOTE: イケてないが、Tutorialへの参照を断つためにGuideTypeはstringでのみ取得可とする
        string GuideType { get; }
        string CustomId { get; }
        void Invoke();
    }
}
