using System.Text.RegularExpressions;
using ClusterVR.CreatorKit.Constants;
using ClusterVR.CreatorKit.Extensions;
using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.ExternalUrl.Implements
{
    [RequireComponent(typeof(Collider))]
    public sealed class ExternalUrlLink : MonoBehaviour, IExternalUrlLink
    {
        [SerializeField] string title;
        [SerializeField] string url;

        public string Title => title;
        public string Url => url;

        // レイヤー設定はResetでもやったほうが良いかも (enableをあんまり気にしなくて良くなる & Editorでもランタイムの見た目に近くなる)
        void Start()
        {
            gameObject.SetLayerRecursively(LayerName.InteractableExhibit);
        }

        void OnValidate()
        {
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                col.isTrigger = true;
            }

            //不正なURLが入力されることによる事故が多発したのでその対策
            url ??= "";
            url = url.Replace(" ", "");
            if (!Regex.IsMatch(url, @"^s?https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+$"))
                url = "";
        }
    }
}
