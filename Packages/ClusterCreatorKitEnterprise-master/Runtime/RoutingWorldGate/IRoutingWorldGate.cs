using System;
using UnityEngine;

namespace ClusterVR.CreatorKit.RoutingWorldGate
{
    public interface IRoutingWorldGate
    {
        event OnEnterRoutingWorldGateEventHandler OnEnterRoutingWorldGateEvent;
        event OnExitRoutingWorldGateEventHandler OnExitRoutingWorldGateEvent;
    }

    public delegate void OnEnterRoutingWorldGateEventHandler(OnEnterRoutingWorldGateEventArgs e);

    public delegate void OnExitRoutingWorldGateEventHandler(OnExitRoutingWorldGateEventArgs e);

    public sealed class OnEnterRoutingWorldGateEventArgs : EventArgs
    {
        public string RoutingKey { get; }
        public GameObject EnterObject { get; }
        public string Key { get; }
        public bool ConfirmTransition { get; }

        public OnEnterRoutingWorldGateEventArgs(string routingKey, GameObject enterObject, string key, bool confirmTransition)
        {
            RoutingKey = routingKey;
            EnterObject = enterObject;
            Key = key;
            ConfirmTransition = confirmTransition;
        }
    }

    public sealed class OnExitRoutingWorldGateEventArgs : EventArgs
    {
        public GameObject ExitObject { get; }

        public OnExitRoutingWorldGateEventArgs(GameObject exitObject)
        {
            ExitObject = exitObject;
        }
    }
}
