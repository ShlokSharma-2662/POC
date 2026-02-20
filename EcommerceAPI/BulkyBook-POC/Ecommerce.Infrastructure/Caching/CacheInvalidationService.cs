using Microsoft.Extensions.Configuration;

namespace Ecommerce.Infrastructure.Caching
{
    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public CacheInvalidationService(ICacheService cacheService, IConfiguration configuration)
        {
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task InvalidateEntityAsync(string entityKey)
        {
            if (_isCacheEnabled != "true")
                return;

            try
            {
                await _cacheService.RemoveByPatternAsync($"*{entityKey}*");
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache invalidation failure should not affect application functionality
            }
        }

        public async Task InvalidateProductCacheAsync(int? productId = null)
        {
            if (_isCacheEnabled != "true")
                return;

            try
            {
                // Invalidate all product-related cache entries
                await _cacheService.RemoveByPatternAsync("products_*");
                
                // If specific product ID is provided, also invalidate that specific product
                if (productId.HasValue)
                {
                    await _cacheService.RemoveAsync(CacheKeyBuilder.ProductById(productId.Value));
                }
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache invalidation failure should not affect application functionality
            }
        }

        public async Task InvalidateAllProductsCacheAsync()
        {
            if (_isCacheEnabled != "true")
                return;

            try
            {
                // Invalidate all product-related cache entries
                await _cacheService.RemoveByPatternAsync("products_*");
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache invalidation failure should not affect application functionality
            }
        }

        public async Task InvalidateUserCacheAsync()
        {
            if (_isCacheEnabled != "true")
                return;

            try
            {
                // Invalidate all user-related cache entries
                await _cacheService.RemoveByPatternAsync("users_*");
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache invalidation failure should not affect application functionality
            }
        }

        public async Task InvalidateCategoryCacheAsync()
        {
            if (_isCacheEnabled != "true")
                return;

            try
            {
                // Invalidate all category-related cache entries
                await _cacheService.RemoveByPatternAsync("categories_*");
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache invalidation failure should not affect application functionality
            }
        }

        public async Task InvalidateOrderCacheAsync(Guid? orderId = null)
        {
            if (_isCacheEnabled != "true")
                return;

            try
            {
                // Invalidate all order-related cache entries
                await _cacheService.RemoveByPatternAsync("orders_*");
                
                // If specific order ID is provided, also invalidate that specific order
                if (orderId.HasValue)
                {
                    await _cacheService.RemoveAsync(CacheKeyBuilder.OrderById(orderId.Value));
                }
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache invalidation failure should not affect application functionality
            }
        }

        public async Task InvalidateAllOrdersCacheAsync()
        {
            if (_isCacheEnabled != "true")
                return;

            try
            {
                // Invalidate all order-related cache entries
                await _cacheService.RemoveByPatternAsync("orders_*");
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache invalidation failure should not affect application functionality
            }
        }

        public async Task InvalidateWishlistCacheAsync(long userId)
        {
            if (_isCacheEnabled != "true")
                return;

            try
            {
                // Invalidate user-specific wishlist cache
                var cacheKey = CacheKeyBuilder.UserWishlist(userId);
                await _cacheService.RemoveAsync(cacheKey);
                
                // Also invalidate any wishlist status cache entries for this user
                await _cacheService.RemoveByPatternAsync($"user_{userId}_wishlist_status_*");
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache invalidation failure should not affect application functionality
            }
        }
    }
}
