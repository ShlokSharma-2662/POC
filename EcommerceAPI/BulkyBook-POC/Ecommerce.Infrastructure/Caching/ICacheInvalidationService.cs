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
        Task InvalidateWishlistCacheAsync(long userId);
    }
}
