namespace ClusterVR.CreatorKit.Gimmick
{
    public interface ISendAnalyticsPlayerGimmick : IPlayerGimmick
    {
        event SendAnalyticsEventHandler OnRun;
    }

    public delegate void SendAnalyticsEventHandler(SendAnalyticsEventArgs args);

    public readonly struct SendAnalyticsEventArgs
    {
        public string AnalyticsId { get; }

        public SendAnalyticsEventArgs(string analyticsId)
        {
            AnalyticsId = analyticsId;
        }
    }
}
