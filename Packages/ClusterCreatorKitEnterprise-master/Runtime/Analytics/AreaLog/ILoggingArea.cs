using System;
using UnityEngine;

namespace ClusterVR.CreatorKit.Analytics.AreaLog
{
    public interface ILoggingArea
    {
        event LoggingAreaEventHandler OnEnter;
        event LoggingAreaEventHandler OnExit;
    }

    public delegate void LoggingAreaEventHandler(LoggingAreaEventArgs e);

    public class LoggingAreaEventArgs : EventArgs
    {
        public string AreaId { get; }
        public GameObject OtherGameObject { get; }

        public LoggingAreaEventArgs(string areaId, GameObject otherGameObject)
        {
            AreaId = areaId;
            OtherGameObject = otherGameObject;
        }
    }
}
