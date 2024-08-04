namespace ClusterVR.CreatorKit.Constants
{
    public static partial class LayerName
    {
        // note: RaycastのMaskを他のContactableと揃えたので、このレイヤー粉砕して他と合わせられるかも
        public const int InteractableExhibit = 13;
        public const int InteractableExhibitMask = 1 << InteractableExhibit;
    }
}
