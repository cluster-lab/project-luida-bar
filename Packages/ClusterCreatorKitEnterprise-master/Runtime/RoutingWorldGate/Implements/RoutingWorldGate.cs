using System;
using UnityEngine;

namespace ClusterVR.CreatorKit.RoutingWorldGate.Implements
{
    [RequireComponent(typeof(Collider))]
    public sealed class RoutingWorldGate : MonoBehaviour, IRoutingWorldGate
    {
        [SerializeField, Tooltip("サーバーのKey")] string routingKey;
        [SerializeField, Tooltip("ワープ先のSpawnPointのKey(任意)")] string key;
        [SerializeField, Tooltip("移動前に確認UIを表示するか")] bool confirmTransition;

        public event OnEnterRoutingWorldGateEventHandler OnEnterRoutingWorldGateEvent;
        public event OnExitRoutingWorldGateEventHandler OnExitRoutingWorldGateEvent;

        void OnTriggerEnter(Collider other)
        {
            OnEnterRoutingWorldGateEvent?.Invoke(new OnEnterRoutingWorldGateEventArgs(routingKey, other.gameObject, key, confirmTransition));
        }

        void OnTriggerExit(Collider other)
        {
            OnExitRoutingWorldGateEvent?.Invoke(new OnExitRoutingWorldGateEventArgs(other.gameObject));
        }

        void OnValidate()
        {
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                col.isTrigger = true;
            }
        }

        void Reset()
        {
            confirmTransition = true;
        }
    }
}
