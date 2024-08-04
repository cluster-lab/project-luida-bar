namespace ClusterVR.CreatorKit.Tutorial
{
    public interface IVROrNonVRActiveObject
    {
        void SetActive(bool active);
        bool IsObjectForVR { get; }
    }
}
