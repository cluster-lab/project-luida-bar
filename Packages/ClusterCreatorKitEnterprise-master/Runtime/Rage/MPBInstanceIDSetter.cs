using UnityEngine;

namespace ClusterVR.CreatorKit.Rage
{
    [ExecuteInEditMode]
    public sealed class MPBInstanceIDSetter : MonoBehaviour
    {
        static readonly string propName = "_RandomSeed";
        static int propId = Shader.PropertyToID(propName);

        void Start()
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            int id = GetInstanceID();
            id = Mathf.Abs(id);
            mpb.SetInt(propId, id);
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null) return;
            foreach (var renderer in renderers)
            {
                renderer.SetPropertyBlock(mpb);
            }
        }
    }
}
