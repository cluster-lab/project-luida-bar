using System;
using RenderHeads.Media.AVProVideo;
using UnityEngine;

namespace ClusterVR.CreatorKit.Media.Implements
{
    [Serializable]
    public struct MediaPlayerRenderer : IMediaPlayerRenderer
    {
        [SerializeField] Renderer renderer;
        [SerializeField] string texturePropertyName;

        public Renderer Renderer => renderer;
        public string TexturePropertyName => texturePropertyName;

        internal void Build(RenderHeads.Media.AVProVideo.MediaPlayer player)
        {
            var applyToMesh = player.gameObject.AddComponent<ApplyToMesh>();
            applyToMesh.Player = player;
            applyToMesh.MeshRenderer = renderer;
            applyToMesh.TexturePropertyName = texturePropertyName;
        }
    }
}
