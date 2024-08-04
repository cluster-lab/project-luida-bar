using System;

namespace ClusterVR.CreatorKit.Tutorial
{
    public interface IControlRestrictedArea
    {
        event ControlRestrictedAreaEventHandler OnEnter;
        event ControlRestrictedAreaEventHandler OnExit;
    }

    public delegate void ControlRestrictedAreaEventHandler(ControlRestrictedAreaEventArgs e);

    public sealed class ControlRestrictedAreaEventArgs : EventArgs
    {
    }
}
