using UnityEngine;

namespace ClusterVR.CreatorKit.Analytics.AreaLog.Implements
{
    [RequireComponent(typeof(Collider))]
    public sealed class LoggingArea : MonoBehaviour, ILoggingArea
    {
        [SerializeField] string areaId;

        public event LoggingAreaEventHandler OnEnter;
        public event LoggingAreaEventHandler OnExit;

        void OnTriggerEnter(Collider other)
        {
            OnEnter?.Invoke(new LoggingAreaEventArgs(areaId, other.gameObject));
        }

        void OnTriggerExit(Collider other)
        {
            OnExit?.Invoke(new LoggingAreaEventArgs(areaId, other.gameObject));
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
