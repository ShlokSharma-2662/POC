namespace Ecommerce.Infrastructure.Caching
{
    public interface ICacheInvalidationService
    {
        Task InvalidateEntityAsync(string entityKey);
        Task InvalidateProductCacheAsync(int? productId = null);
        Task InvalidateAllProductsCacheAsync();
        Task InvalidateUserCacheAsync();
        Task InvalidateCategoryCacheAsync();
        Task InvalidateOrderCacheAsync(Guid? orderId = null);
        Task InvalidateAllOrdersCacheAsync();
        Task InvalidateCartCacheAsync(long userId);
        Task InvalidateWishlistCacheAsync(long userId);
    }
}
