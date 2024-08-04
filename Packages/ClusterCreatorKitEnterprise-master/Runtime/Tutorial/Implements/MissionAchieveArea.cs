using UnityEngine;

namespace ClusterVR.CreatorKit.Tutorial.Implements
{
    [RequireComponent(typeof(Collider))]
    public sealed class MissionAchieveArea : MonoBehaviour, IMissionAchieveArea
    {
        [SerializeField] string missionId;
        public event MissionAreaEventHandler OnEnter;

        void OnTriggerEnter(Collider other)
        {
            OnEnter?.Invoke(new MissionAreaEventArgs(missionId));
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
