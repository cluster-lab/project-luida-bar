using System;

namespace ClusterVR.CreatorKit.Tutorial
{
    public interface IMissionAchieveArea
    {
        event MissionAreaEventHandler OnEnter;
    }

    public delegate void MissionAreaEventHandler(MissionAreaEventArgs e);

    public sealed class MissionAreaEventArgs : EventArgs
    {
        public string MissionId { get; }

        public MissionAreaEventArgs(string missionId)
        {
            MissionId = missionId;
        }
    }
}
