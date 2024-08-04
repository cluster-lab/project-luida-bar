using UnityEngine;

namespace ClusterVR.CreatorKit.Tutorial.Implements
{
    public sealed class VROrNonVRActiveObject : MonoBehaviour, IVROrNonVRActiveObject
    {
        [SerializeField] bool isObjectForVr;

        bool IVROrNonVRActiveObject.IsObjectForVR => isObjectForVr;

        void IVROrNonVRActiveObject.SetActive(bool active) => gameObject.SetActive(active);
    }
}
