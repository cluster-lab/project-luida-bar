using System;
using RenderHeads.Media.AVProVideo;
using UnityEngine;

namespace ClusterVR.CreatorKit.Media.Implements
{
    [Serializable]
    public struct MediaPlayerMaterial : IMediaPlayerMaterial
    {
        [SerializeField] Material material;
        [SerializeField] string texturePropertyName;

        public Material Material => material;
        public string TexturePropertyName => texturePropertyName;

        internal void Build(RenderHeads.Media.AVProVideo.MediaPlayer player)
        {
            var applyToMaterial = player.gameObject.AddComponent<ApplyToMaterial>();
            applyToMaterial.Player = player;
            applyToMaterial.Material = material;
            applyToMaterial.TexturePropertyName = texturePropertyName;
        }
    }
}
