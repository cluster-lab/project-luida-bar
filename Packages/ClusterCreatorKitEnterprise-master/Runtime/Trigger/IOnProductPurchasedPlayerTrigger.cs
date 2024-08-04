using ClusterVR.CreatorKit.ProductUgc;

namespace ClusterVR.CreatorKit.Trigger
{
    public interface IOnProductPurchasedPlayerTrigger : IPlayerTrigger
    {
        ProductId ProductId { get; }
        void Invoke();
    }
}
