using UnityEngine;

namespace ClusterVR.CreatorKit.World.Implements
{
    [RequireComponent(typeof(WorldGate.WorldGate)), DisallowMultipleComponent]
    public sealed class TutorialServerWorldGate : MonoBehaviour, ITutorialServerWorldGate
    {
        [SerializeField] WorldGate.WorldGate worldGate;

        public IWorldGate WorldGate => worldGate;

        void Reset()
        {
            worldGate = GetComponent<WorldGate.WorldGate>();
        }
    }
}
