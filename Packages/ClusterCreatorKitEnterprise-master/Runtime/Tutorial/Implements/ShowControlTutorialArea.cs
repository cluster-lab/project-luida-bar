using System.Collections.Generic;
using UnityEngine;

namespace ClusterVR.CreatorKit.Tutorial.Implements
{
    [RequireComponent(typeof(Collider))]
    public sealed class ShowControlTutorialArea : MonoBehaviour, IShowControlTutorialArea
    {
        [SerializeField] GuideType guideType = GuideType.Custom;
        [SerializeField] string customId;
        [SerializeField] Transform vrGuideAnchor;
        [SerializeField] Transform nonVRGuideAnchor;

        public GameObject RootObject => gameObject;
        public GuideType GuideType => guideType;
        public string CustomId => customId;
        public Transform VRGuideAnchor => vrGuideAnchor;
        public Transform NonVRGuideAnchor => nonVRGuideAnchor;

        public event ShowControlTutorialAreaEventHandler OnEnter;
        public event ShowControlTutorialAreaEventHandler OnExit;

        // SpawnPointがコライダーの中、というケースでもEnterを検知するために使う
        readonly HashSet<Collider> stayingColliders = new();

        void Awake()
        {
            // Subscribeされる前にOnEnterが発火しないように、明示的に有効にするまではコライダーを無効にしておく
            gameObject.GetComponent<Collider>().enabled = false;
        }

        void IShowControlTutorialArea.Activate()
        {
            gameObject.GetComponent<Collider>().enabled = true;
        }

        void OnTriggerStay(Collider other)
        {
            if (stayingColliders.Contains(other))
            {
                return;
            }

            stayingColliders.Add(other);
            OnEnter?.Invoke(new ShowControlTutorialAreaEventArgs(
                guideType, customId, other.gameObject, nonVRGuideAnchor, vrGuideAnchor
                ));
        }

        void OnTriggerExit(Collider other)
        {
            stayingColliders.Remove(other);
            OnExit?.Invoke(new ShowControlTutorialAreaEventArgs(
                guideType, customId, other.gameObject, nonVRGuideAnchor, vrGuideAnchor
                ));
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

