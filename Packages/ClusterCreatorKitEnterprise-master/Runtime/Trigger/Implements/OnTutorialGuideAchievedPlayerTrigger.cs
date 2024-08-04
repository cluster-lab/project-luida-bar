using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClusterVR.CreatorKit.Trigger.Implements
{
    public sealed class OnTutorialGuideAchievedPlayerTrigger : MonoBehaviour, IOnTutorialGuideAchievedPlayerTrigger
    {
        [SerializeField] string guideType;
        [SerializeField] string customId;
        [SerializeField, PlayerConstantTriggerParam] ConstantTriggerParam[] triggers;

        public event PlayerTriggerEventHandler TriggerEvent;

        IEnumerable<TriggerParam> ITrigger.TriggerParams => triggers.Select(t => t.Convert());

        public string GuideType => guideType;
        public string CustomId => customId;

        public void Invoke()
        {
            TriggerEvent?.Invoke(
                this,
                new TriggerEventArgs(triggers.Select(t => t.Convert()).ToArray())
                );
        }
    }
}
