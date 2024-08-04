using System;
using System.Collections.Generic;
using System.Linq;
using RenderHeads.Media.AVProVideo;
using UnityEngine;

namespace ClusterVR.CreatorKit.Media.Implements
{
    public sealed class MediaPlayer : MonoBehaviour, IMediaPlayer
    {
        [SerializeField] string sourceUrl;
        [SerializeField] List<MediaPlayerRenderer> targetRenderers;
        [SerializeField] List<MediaPlayerMaterial> targetMaterials;

        public string SourceUrl => sourceUrl;
        public IEnumerable<IMediaPlayerRenderer> TargetRenderers => targetRenderers.Cast<IMediaPlayerRenderer>();
        public IEnumerable<IMediaPlayerMaterial> TargetMaterials => targetMaterials.Cast<IMediaPlayerMaterial>();

        RenderHeads.Media.AVProVideo.MediaPlayer player;
        bool IsInitialized => player != null;

        const string MediaPathPropertyName = "MediaPath";
        const string MediaSourcePropertyName = "MediaSource";

        /// <summary>
        ///   再生を開始する
        ///   すでに再生中の場合、特に何も起きない
        /// </summary>
        public void Play()
        {
            if (!IsInitialized)
            {
                InitializePlayer();
            }
            player.enabled = true;
            player.Play();
        }

        /// <summary>
        ///   リロードする
        /// </summary>
        /// <returns>成功すれば<c>true</c>を返す</returns>
        public bool Reload()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("MediaPlayer is not playing");
                return false;
            }
            player.CloseMedia();
            return player.OpenMedia();
        }

        void InitializePlayer()
        {
            player = gameObject.AddComponent<RenderHeads.Media.AVProVideo.MediaPlayer>();
            player.enabled = false;

            foreach (var targetRenderer in targetRenderers)
            {
                targetRenderer.Build(player);
            }

            foreach (var targetMaterial in targetMaterials)
            {
                targetMaterial.Build(player);
            }

            SetPrivateProperty(player, MediaSourcePropertyName, MediaSource.Path);
            var mediaPath = new MediaPath(SourceUrl, MediaPathType.AbsolutePathOrURL);
            SetPrivateProperty(player, MediaPathPropertyName, mediaPath);
        }

        static void SetPrivateProperty(RenderHeads.Media.AVProVideo.MediaPlayer mediaPlayer, string propertyName, object value)
        {
            var playerType = typeof(RenderHeads.Media.AVProVideo.MediaPlayer);
            var property = playerType.GetProperty(propertyName);
            var setter = property?.GetSetMethod(true);
            if (setter == null)
            {
                throw new NotSupportedException($"MediaPlayer does not support setter for {propertyName}");
            }
            setter.Invoke(mediaPlayer, new object[] { value });
        }
    }
}
