using System.Collections.Generic;

namespace ClusterVR.CreatorKit.Media
{
    public interface IMediaPlayer
    {
        string SourceUrl { get; }
        IEnumerable<IMediaPlayerRenderer> TargetRenderers { get; }
        IEnumerable<IMediaPlayerMaterial> TargetMaterials { get; }

        void Play();
        bool Reload();
    }
}
