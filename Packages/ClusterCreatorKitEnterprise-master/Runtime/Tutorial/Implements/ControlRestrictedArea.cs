using UnityEngine;

namespace ClusterVR.CreatorKit.Tutorial.Implements
{
    [RequireComponent(typeof(Collider))]
    public sealed class ControlRestrictedArea : MonoBehaviour, IControlRestrictedArea
    {
        public event ControlRestrictedAreaEventHandler OnEnter;
        public event ControlRestrictedAreaEventHandler OnExit;

        void OnTriggerEnter(Collider other)
        {
            OnEnter?.Invoke(new ControlRestrictedAreaEventArgs());
        }

        void OnTriggerExit(Collider other)
        {
            OnExit?.Invoke(new ControlRestrictedAreaEventArgs());
        }

        void OnValidate()
        {
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                col.isTrigger = true;
            }
        }
    }
}

