namespace Ecommerce.Infrastructure.Caching
{
    public class NullCacheInvalidationService : ICacheInvalidationService
    {
        public Task InvalidateEntityAsync(string entityKey)
        {
            return Task.CompletedTask;
        }

        public Task InvalidateProductCacheAsync(int? productId = null)
        {
            return Task.CompletedTask;
        }

        public Task InvalidateAllProductsCacheAsync()
        {
            return Task.CompletedTask;
        }

        public Task InvalidateUserCacheAsync()
        {
            return Task.CompletedTask;
        }

        public Task InvalidateCategoryCacheAsync()
        {
            return Task.CompletedTask;
        }

        public Task InvalidateOrderCacheAsync(Guid? orderId = null)
        {
            return Task.CompletedTask;
        }

        public Task InvalidateAllOrdersCacheAsync()
        {
            return Task.CompletedTask;
        }

        public Task InvalidateWishlistCacheAsync(long userId)
        {
            return Task.CompletedTask;
        }
    }
}
