using System;
using ClusterVR.CreatorKit.Item;
using UnityEngine;

namespace ClusterVR.CreatorKit.Gimmick.Implements
{
    public sealed class SendAnalyticsPlayerGimmick : MonoBehaviour, ISendAnalyticsPlayerGimmick
    {
        [SerializeField] PlayerGimmickKey key;
        [SerializeField] string analyticsId;

        DateTime lastTriggeredAt;

        GimmickTarget IGimmick.Target => key.Key.Target;
        string IGimmick.Key => key.Key.Key;
        ItemId IGimmick.ItemId => key.ItemId;
        ParameterType IGimmick.ParameterType => ParameterType.Signal;

        public event SendAnalyticsEventHandler OnRun;

        public void Run(GimmickValue value, DateTime current)
        {
            if (value.TimeStamp <= lastTriggeredAt)
            {
                return;
            }
            lastTriggeredAt = value.TimeStamp;
            if ((current - value.TimeStamp).TotalSeconds > Constants.TriggerGimmick.TriggerExpireSeconds)
            {
                return;
            }

            var args = new SendAnalyticsEventArgs(analyticsId);
            OnRun?.Invoke(args);
        }
    }
}
