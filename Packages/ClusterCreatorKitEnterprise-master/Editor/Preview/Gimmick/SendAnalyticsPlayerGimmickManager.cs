using System.Collections.Generic;
using ClusterVR.CreatorKit.Editor.Preview.Item;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Item;
using UnityEngine;

namespace ClusterVR.CreatorKit.Editor.Preview.Gimmick
{
    public sealed class SendAnalyticsPlayerGimmickManager
    {
        public SendAnalyticsPlayerGimmickManager(ItemCreator itemCreator)
        {
            itemCreator.OnCreate += OnCreateItem;
        }

        void OnCreateItem(IItem item)
        {
            Register(item.gameObject.GetComponentsInChildren<ISendAnalyticsPlayerGimmick>(true));
        }

        public void Register(IEnumerable<ISendAnalyticsPlayerGimmick> sendAnalyticsPlayerGimmicks)
        {
            foreach (var sendAnalyticsPlayerGimmick in sendAnalyticsPlayerGimmicks)
            {
                Register(sendAnalyticsPlayerGimmick);
            }
        }

        void Register(ISendAnalyticsPlayerGimmick sendAnalyticsPlayerGimmick)
        {
            sendAnalyticsPlayerGimmick.OnRun += Run;
        }

        void Run(SendAnalyticsEventArgs args)
        {
            var id = args.AnalyticsId;
            var message = $"ワールド投稿時にはGimmickID={id}で ExecuteSendAnalyticsGimmick のアナリティクスイベントが送信されます。";
            Debug.Log(message);
        }
    }
}
