using System.Collections.Generic;
using System.Linq;
using ClusterVR.CreatorKit.ProductUgc;
using UnityEngine;

namespace ClusterVR.CreatorKit.Trigger.Implements
{
    public class OnProductPurchasedPlayerTrigger : MonoBehaviour, IOnProductPurchasedPlayerTrigger
    {
        [SerializeField, Tooltip("商品Id")] ProductId productId;

        [SerializeField, PlayerConstantTriggerParam] ConstantTriggerParam[] triggers;

        public event PlayerTriggerEventHandler TriggerEvent;

        IEnumerable<TriggerParam> ITrigger.TriggerParams => triggers.Select(t => t.Convert());
        ProductId IOnProductPurchasedPlayerTrigger.ProductId => productId;

        public void Invoke()
        {
            TriggerEvent?.Invoke(this, new TriggerEventArgs(triggers.Select(t => t.Convert()).ToArray()));
        }
    }
}
